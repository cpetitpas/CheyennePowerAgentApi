using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CheyennePowerAgentApi.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CheyennePowerAgentApi.Services;

public class MultiNodeInvestigateService : IMultiNodeInvestigateService
{
    private readonly HttpClient _http;
    private readonly IInvestigateService _investigateService;
    private readonly string _apiKey;
    private readonly ILogger<MultiNodeInvestigateService> _logger;

    public MultiNodeInvestigateService(
        HttpClient http,
        IInvestigateService investigateService,
        IConfiguration config,
        ILogger<MultiNodeInvestigateService> logger)
    {
        _http               = http;
        _investigateService = investigateService;
        _apiKey             = AnthropicConfiguration.GetApiKey(config) ?? string.Empty;
        _logger             = logger;
        _http.BaseAddress   = new Uri("https://api.anthropic.com");
    }

    public async Task<MultiNodeInvestigateResponse> InvestigateAsync(
        MultiNodeInvestigateRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("Anthropic API key is not configured.");

        // Phase 1 — parallel per-node investigations
        var tasks = request.Nodes.Select(n => _investigateService.InvestigateAsync(new InvestigateRequest
        {
            NodeId      = n.NodeId,
            AlarmType   = n.AlarmType,
            SensorValue = n.SensorValue,
            Unit        = n.Unit
        }, ct));

        var perNodeResults = await Task.WhenAll(tasks);

        var nodeResults = perNodeResults.Select(r => new NodeInvestigationResult
        {
            NodeId            = r.NodeId,
            Conclusion        = r.Conclusion,
            Severity          = r.Severity,
            RecommendedAction = r.RecommendedAction,
            ToolsInvoked      = r.ToolsInvoked,
            Iterations        = r.Iterations
        }).ToList();

        // Phase 2 — synthesis
        var summaries = string.Join("\n\n", nodeResults.Select(r =>
            $"Node {r.NodeId} [{r.Severity}]: {r.Conclusion}\nAction: {r.RecommendedAction}"));

        var synthesisPrompt = $@"
            You are an AI agent analyzing a multi-node event at a natural gas-fired power generation facility.
            {(request.RegionContext is not null ? $"Region context: {request.RegionContext}" : "")}

            Per-node investigation results:
            {summaries}

            Synthesize these findings and respond with a JSON object using snake_case keys:
            {{
              ""overall_severity"": ""<LOW|MEDIUM|HIGH>"",
              ""root_cause_hypothesis"": ""<most likely root cause across all nodes>"",
              ""correlation_summary"": ""<how the node events are related>"",
              ""recommended_action"": ""<priority action for operators>"",
              ""affected_nodes"": [""<nodeId>"", ...]
            }}

            Respond with JSON only.
            ";

        var body = new
        {
            model      = "claude-opus-4-6",
            max_tokens = 1024,
            messages   = new[] { new { role = "user", content = synthesisPrompt } }
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/v1/messages");
        req.Headers.Add("x-api-key", _apiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");
        req.Content = new StringContent(
            JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var res  = await _http.SendAsync(req, ct);
        var json = await res.Content.ReadAsStringAsync(ct);
        var doc  = JsonDocument.Parse(json);

        var text = doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text").GetString() ?? string.Empty;

        var synthesis = TryParseSynthesis(text);

        return new MultiNodeInvestigateResponse
        {
            OverallSeverity      = synthesis?.OverallSeverity      ?? "UNKNOWN",
            RootCauseHypothesis  = synthesis?.RootCauseHypothesis  ?? text,
            CorrelationSummary   = synthesis?.CorrelationSummary   ?? string.Empty,
            RecommendedAction    = synthesis?.RecommendedAction     ?? string.Empty,
            AffectedNodes        = synthesis?.AffectedNodes        ?? [],
            NodeResults          = nodeResults
        };
    }

    private static MultiNodeInvestigateResponse? TryParseSynthesis(string raw)
    {
        if (AnthropicResponseParser.TryParseJson<MultiNodeInvestigateResponse>(raw, out var response))
            return response;

        return null;
    }
}
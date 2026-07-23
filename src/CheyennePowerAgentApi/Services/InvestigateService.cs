using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CheyennePowerAgentApi.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CheyennePowerAgentApi.Services;

public class InvestigateService : IInvestigateService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly ILogger<InvestigateService> _logger;
    private const int MaxIterations = 8;

    public InvestigateService(HttpClient http, IConfiguration config, ILogger<InvestigateService> logger)
    {
        _http   = http;
        _apiKey = AnthropicConfiguration.GetApiKey(config) ?? string.Empty;
        _logger = logger;
        _http.BaseAddress = new Uri("https://api.anthropic.com");
    }

    public async Task<InvestigateResponse> InvestigateAsync(InvestigateRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("Anthropic API key is not configured.");

        var toolsInvoked = new List<string>();
        var messages     = new List<object>
        {
            new {
                role    = "user",
                    content = $@"                    You are an AI agent monitoring a natural gas-fired power generation facility.
                    Investigate the following alarm on node {request.NodeId}.
                    Use the available tools to gather data before reaching a conclusion.
                    When you have enough information, respond with a final JSON object using snake_case keys:
                    {{
                      ""conclusion"": ""<detailed assessment>"",
                      ""severity"": ""<LOW|MEDIUM|HIGH>"",
                      ""recommended_action"": ""<specific operator action>""
                    Sensor value: {request.SensorValue} {request.Unit}
                    {(request.Context is not null ? $"Context: {request.Context}" : "")}
                    """
            }
        };

        int iterations = 0;
        string finalRaw = string.Empty;

        while (iterations < MaxIterations)
        {
            iterations++;
            var body = new
            {
                model      = "claude-opus-4-6",
                max_tokens = 1024,
                tools      = PowerGenerationTools.Definitions,
                messages
            };

            var req = new HttpRequestMessage(HttpMethod.Post, "/v1/messages");
            req.Headers.Add("x-api-key", _apiKey);
            req.Headers.Add("anthropic-version", "2023-06-01");
            req.Content = new StringContent(
                JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var res  = await _http.SendAsync(req, ct);
            var json = await res.Content.ReadAsStringAsync(ct);
            var doc  = JsonDocument.Parse(json);

            var stopReason = doc.RootElement
                .GetProperty("stop_reason").GetString();

            var contentArr = doc.RootElement.GetProperty("content");

            // Add assistant message to history
            messages.Add(new { role = "assistant", content = contentArr });

            if (stopReason == "end_turn")
            {
                // Extract last text block as final answer
                foreach (var block in contentArr.EnumerateArray())
                {
                    if (block.GetProperty("type").GetString() == "text")
                        finalRaw = block.GetProperty("text").GetString() ?? string.Empty;
                }
                break;
            }

            if (stopReason == "tool_use")
            {
                var toolResults = new List<object>();

                foreach (var block in contentArr.EnumerateArray())
                {
                    if (block.GetProperty("type").GetString() != "tool_use") continue;

                    var toolName = block.GetProperty("name").GetString()!;
                    var toolId   = block.GetProperty("id").GetString()!;
                    var input    = block.GetProperty("input");

                    toolsInvoked.Add(toolName);
                    var result = PowerGenerationTools.Invoke(toolName, input);
                    _logger.LogInformation("Tool {Tool} invoked for {Node}", toolName, request.NodeId);

                    toolResults.Add(new
                    {
                        type        = "tool_result",
                        tool_use_id = toolId,
                        content     = result
                    });
                }

                messages.Add(new { role = "user", content = toolResults });
            }
        }

        var parsed = TryParse(finalRaw);
        return new InvestigateResponse
        {
            NodeId            = request.NodeId,
            Conclusion        = parsed?.Conclusion        ?? finalRaw,
            Severity          = parsed?.Severity          ?? "UNKNOWN",
            RecommendedAction = parsed?.RecommendedAction ?? string.Empty,
            ToolsInvoked      = toolsInvoked,
            Iterations        = iterations
        };
    }

    private static InvestigateResponse? TryParse(string raw)
    {
        try
        {
            // Strip markdown fences if present
            var clean = raw.Trim();
            if (clean.StartsWith("```")) clean = string.Join('\n', clean.Split('\n').Skip(1).SkipLast(1));
            return JsonSerializer.Deserialize<InvestigateResponse>(clean,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
        }
        catch { return null; }
    }
}
using System.Text;
using System.Text.Json;

namespace CheyennePowerAgentApi.Services;

public class ClaudeService : IClaudeService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string Model = "claude-opus-4-1";

    public ClaudeService(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClient = httpClientFactory.CreateClient();
        _apiKey = config["Anthropic:ApiKey"] ?? throw new InvalidOperationException("Anthropic API key not configured");
    }

    public Task<string> AnalyzeAlarmAsync(string prompt, CancellationToken ct = default)
        => SendToClaudeAsync(prompt, ct);

    public Task<string> AnalyzeFlowAsync(string prompt, CancellationToken ct = default)
        => SendToClaudeAsync(prompt, ct);

    public Task<string> AnalyzeMultiNodeAsync(string prompt, CancellationToken ct = default)
        => SendToClaudeAsync(prompt, ct);

    private async Task<string> SendToClaudeAsync(string prompt, CancellationToken ct)
    {
        var requestBody = new
        {
            model = Model,
            max_tokens = 512,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        httpRequest.Headers.Add("x-api-key", _apiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");

        var response = await _httpClient.SendAsync(httpRequest, ct);
        var responseJson = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Anthropic API error {(int)response.StatusCode}: {responseJson}");

        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        if (root.TryGetProperty("content", out var contentArray) &&
            contentArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var block in contentArray.EnumerateArray())
            {
                if (block.ValueKind != JsonValueKind.Object)
                    continue;

                if (block.TryGetProperty("type", out var typeEl) &&
                    string.Equals(typeEl.GetString(), "text", StringComparison.Ordinal) &&
                    block.TryGetProperty("text", out var textEl))
                {
                    var text = textEl.GetString() ?? string.Empty;
                    return StripCodeFence(text);
                }
            }
        }

        return string.Empty;
    }

    private static string StripCodeFence(string text)
    {
        text = text.Trim();
        if (!text.StartsWith("```", StringComparison.Ordinal))
            return text;

        var firstNewLine = text.IndexOf('\n');
        var lastFence = text.LastIndexOf("```", StringComparison.Ordinal);

        if (firstNewLine >= 0 && lastFence > firstNewLine)
            return text[(firstNewLine + 1)..lastFence].Trim();

        return text;
    }
}

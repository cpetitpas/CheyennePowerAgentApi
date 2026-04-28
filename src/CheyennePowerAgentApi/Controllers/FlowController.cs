using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using CheyennePowerAgentApi.Models;
using CheyennePowerAgentApi.Services;

namespace CheyennePowerAgentApi.Controllers;

[ApiController]
[Route("api/flow")]
public class FlowController : ControllerBase
{
    private readonly IClaudeService _claude;

    public FlowController(IClaudeService claude) => _claude = claude;

    [HttpPost("analyze")]
    public async Task<ActionResult<FlowAnalysisResponse>> Analyze(
        [FromBody] FlowAnalysisRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.NodeId))
            return BadRequest("NodeId is required.");

        var contextLine = request.Context is not null ? $"Context: {request.Context}" : string.Empty;

                var prompt = $$"""
            You are an AI agent monitoring fuel gas flow to a natural gas-fired power generation facility.
            Analyze the following flow data and respond with a JSON object using snake_case keys:
                        {
              "analysis": "<one or two sentence assessment>",
              "action": "<recommended operator action>",
              "severity": "<LOW|MEDIUM|HIGH>",
              "variance": <variance as a percentage, numeric only>
                        }

                        Node: {{request.NodeId}}
                        Flow rate: {{request.FlowRate}} {{request.Unit}}
                        {{contextLine}}

            Respond with JSON only. No explanation outside the JSON object.
            """;

        var raw = await _claude.AnalyzeFlowAsync(prompt, ct);

        var result = JsonSerializer.Deserialize<FlowAnalysisResponse>(raw,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

        return result is null ? StatusCode(502, "Failed to parse Claude response.") : Ok(result);
    }
}
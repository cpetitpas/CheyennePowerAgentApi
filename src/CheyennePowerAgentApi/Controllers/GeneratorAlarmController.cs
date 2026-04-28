using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using CheyennePowerAgentApi.Models;
using CheyennePowerAgentApi.Services;

namespace CheyennePowerAgentApi.Controllers;

[ApiController]
[Route("api/generator")]
public class GeneratorAlarmController : ControllerBase
{
    private readonly IClaudeService _claude;

    public GeneratorAlarmController(IClaudeService claude) => _claude = claude;

    [HttpPost("analyze")]
    public async Task<ActionResult<GeneratorAlarmResponse>> Analyze(
        [FromBody] GeneratorAlarmRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.NodeId) ||
            string.IsNullOrWhiteSpace(request.AlarmType))
            return BadRequest("NodeId and AlarmType are required.");

        var contextLine = request.Context is not null ? $"Context: {request.Context}" : string.Empty;

                var prompt = $$"""
            You are an AI agent monitoring a natural gas-fired power generation facility.
            Analyze the following generator alarm and respond with a JSON object using snake_case keys:
                        {
              "analysis": "<one or two sentence assessment>",
              "action": "<recommended operator action>",
              "severity": "<LOW|MEDIUM|HIGH>"
                        }

                        Node: {{request.NodeId}}
                        Alarm: {{request.AlarmType}}
                        Sensor value: {{request.SensorValue}} {{request.Unit}}
                        {{contextLine}}

            Respond with JSON only. No explanation outside the JSON object.
            """;

        var raw = await _claude.AnalyzeAlarmAsync(prompt, ct);

        var result = JsonSerializer.Deserialize<GeneratorAlarmResponse>(raw,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

        return result is null ? StatusCode(502, "Failed to parse Claude response.") : Ok(result);
    }
}
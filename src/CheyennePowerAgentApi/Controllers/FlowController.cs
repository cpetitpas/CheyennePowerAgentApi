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
    private const double DefaultExpectedFlowMmscfd = 175.0;

    private static readonly IReadOnlyDictionary<string, double> ExpectedFlowByNode =
        new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["FUEL-001"] = 180.0,
            ["FUEL-002"] = 170.0
        };

    public FlowController(IClaudeService claude) => _claude = claude;

    [HttpPost("analyze")]
    public async Task<ActionResult<FlowAnalysisResponse>> Analyze(
        [FromBody] FlowAnalysisRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.NodeId))
            return BadRequest("NodeId is required.");

        if (request.FlowRate <= 0)
            return BadRequest("FlowRate must be greater than zero.");

        var expectedFlowRate = ResolveExpectedFlowRate(request);
        var computedVariance = CalculateVariancePercent(request.FlowRate, expectedFlowRate);

        var contextLine = request.Context is not null ? $"Context: {request.Context}" : string.Empty;
        var expectedFlowLine = $"Expected/setpoint flow rate: {expectedFlowRate:F1} {request.Unit}";

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
                        {{expectedFlowLine}}
                        {{contextLine}}

            Compute variance using this formula:
            variance = ((flow_rate - expected_flow_rate) / expected_flow_rate) * 100
            Return numeric variance only (no percent sign).

            Respond with JSON only. No explanation outside the JSON object.
            """;

        try
        {
            var raw = await _claude.AnalyzeFlowAsync(prompt, ct);

            var result = JsonSerializer.Deserialize<FlowAnalysisResponse>(raw,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

            var response = BuildResponse(request, expectedFlowRate, computedVariance, result);
            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Anthropic API key", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(503, new { error = ex.Message });
        }
    }

    private static double ResolveExpectedFlowRate(FlowAnalysisRequest request)
    {
        if (request.ExpectedFlowRate is > 0)
            return request.ExpectedFlowRate.Value;

        if (ExpectedFlowByNode.TryGetValue(request.NodeId, out var expected))
            return expected;

        return DefaultExpectedFlowMmscfd;
    }

    private static double CalculateVariancePercent(double actual, double expected)
        => Math.Round(((actual - expected) / expected) * 100.0, 1);

    private static FlowAnalysisResponse BuildResponse(
        FlowAnalysisRequest request,
        double expectedFlowRate,
        double computedVariance,
        FlowAnalysisResponse? modelResponse)
    {
        var absVariance = Math.Abs(computedVariance);
        var computedSeverity = absVariance switch
        {
            >= 15.0 => "HIGH",
            >= 8.0 => "MEDIUM",
            _ => "LOW"
        };

        var analysis = modelResponse?.Analysis;
        if (string.IsNullOrWhiteSpace(analysis) || LooksLikeMissingBaselineResponse(analysis))
        {
            var direction = computedVariance >= 0 ? "above" : "below";
            analysis = $"Fuel gas flow at node {request.NodeId} is {request.FlowRate:F1} {request.Unit}, {Math.Abs(computedVariance):F1}% {direction} the expected {expectedFlowRate:F1} {request.Unit}.";
        }

        var action = modelResponse?.Action;
        if (string.IsNullOrWhiteSpace(action) || LooksLikeMissingBaselineResponse(action))
        {
            action = absVariance >= 8.0
                ? "Inspect upstream pressure regulation and valve positions; verify transmitter health and adjust dispatch controls if deviation persists."
                : "Continue monitoring trend and verify sensor calibration during routine checks.";
        }

        return new FlowAnalysisResponse
        {
            Analysis = analysis,
            Action = action,
            Severity = NormalizeSeverity(modelResponse?.Severity) ?? computedSeverity,
            Variance = computedVariance
        };
    }

    private static bool LooksLikeMissingBaselineResponse(string text)
    {
        var normalized = text.ToLowerInvariant();
        return normalized.Contains("additional context") ||
               normalized.Contains("no baseline") ||
               normalized.Contains("no setpoint") ||
               normalized.Contains("expected") && normalized.Contains("provide") ||
               normalized.Contains("single data point");
    }

    private static string? NormalizeSeverity(string? severity)
    {
        if (string.IsNullOrWhiteSpace(severity))
            return null;

        return severity.Trim().ToUpperInvariant() switch
        {
            "LOW" => "LOW",
            "MEDIUM" => "MEDIUM",
            "HIGH" => "HIGH",
            _ => null
        };
    }
}
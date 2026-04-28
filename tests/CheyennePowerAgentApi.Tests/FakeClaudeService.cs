using System.Text.Json;
using CheyennePowerAgentApi.Services;

namespace CheyennePowerAgentApi.Tests;

public class FakeClaudeService : IClaudeService
{
    public Task<string> AnalyzeAlarmAsync(string prompt, CancellationToken ct = default)
    {
        var response = new
        {
            analysis = "Alarm pattern indicates abnormal generator behavior that warrants immediate operator attention.",
            recommended_action = "Dispatch field technician to inspect the node immediately.",
            severity = "MEDIUM"
        };
        return Task.FromResult(JsonSerializer.Serialize(response));
    }

    public Task<string> AnalyzeFlowAsync(string prompt, CancellationToken ct = default)
    {
        var response = new
        {
            analysis = "Flow variance suggests possible upstream fuel supply instability.",
            recommended_action = "Verify fuel header pressure and inspect for partial blockage.",
            severity = "LOW",
            variance = 12.5
        };
        return Task.FromResult(JsonSerializer.Serialize(response));
    }

    public Task<string> AnalyzeMultiNodeAsync(string prompt, CancellationToken ct = default)
    {
        var response = new
        {
            overall_status = "NORMAL",
            summary = "No cross-node anomalies detected in the current snapshot.",
            recommended_action = "Continue routine monitoring.",
            affected_nodes = Array.Empty<string>()
        };
        return Task.FromResult(JsonSerializer.Serialize(response));
    }
}
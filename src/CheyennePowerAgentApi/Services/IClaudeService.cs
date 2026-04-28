namespace CheyennePowerAgentApi.Services;

public interface IClaudeService
{
    Task<string> AnalyzeAlarmAsync(string prompt, CancellationToken ct = default);
    Task<string> AnalyzeFlowAsync(string prompt, CancellationToken ct = default);
    Task<string> AnalyzeMultiNodeAsync(string prompt, CancellationToken ct = default);
}
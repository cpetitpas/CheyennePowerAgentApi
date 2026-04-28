namespace CheyennePowerAgentApi.Models;

public class TelemetryEvent
{
    public string EventType { get; set; } = string.Empty;  // ALARM | FLOW
    public string NodeId { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;   // LOW | MEDIUM | HIGH
    public string Analysis { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public double? Variance { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
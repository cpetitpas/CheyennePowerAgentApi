namespace CheyennePowerAgentApi.Models;

public class TelemetryEvent
{
    public string EventType { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public List<string> Nodes { get; set; } = [];
    public string Severity { get; set; } = string.Empty;
    public string Analysis { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public double? Variance { get; set; }
    public string AlarmType { get; set; } = string.Empty;
    public double? SensorValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
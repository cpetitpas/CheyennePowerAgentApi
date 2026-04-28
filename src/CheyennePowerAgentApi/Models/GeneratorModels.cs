namespace CheyennePowerAgentApi.Models;

public class GeneratorAlarmRequest
{
    public string NodeId { get; set; } = string.Empty;
    public string AlarmType { get; set; } = string.Empty;
    public double SensorValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Context { get; set; }
}

public class GeneratorAlarmResponse
{
    public string Analysis { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
}

public class FlowAnalysisRequest
{
    public string NodeId { get; set; } = string.Empty;
    public double FlowRate { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Context { get; set; }
}

public class FlowAnalysisResponse
{
    public string Analysis { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public double Variance { get; set; }
}
namespace CheyennePowerAgentApi.Models;

public class InvestigateRequest
{
    public string NodeId { get; set; } = string.Empty;
    public string AlarmType { get; set; } = string.Empty;
    public double SensorValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Context { get; set; }
}

public class InvestigateResponse
{
    public string NodeId { get; set; } = string.Empty;
    public string Conclusion { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public List<string> ToolsInvoked { get; set; } = [];
    public int Iterations { get; set; }
}

public class NodeAlarmInput
{
    public string NodeId { get; set; } = string.Empty;
    public string AlarmType { get; set; } = string.Empty;
    public double SensorValue { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public class MultiNodeInvestigateRequest
{
    public string? RegionContext { get; set; }
    public List<NodeAlarmInput> Nodes { get; set; } = [];
}

public class MultiNodeInvestigateResponse
{
    public string OverallSeverity { get; set; } = string.Empty;
    public string RootCauseHypothesis { get; set; } = string.Empty;
    public string CorrelationSummary { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public List<string> AffectedNodes { get; set; } = [];
    public List<NodeInvestigationResult> NodeResults { get; set; } = [];
}

public class NodeInvestigationResult
{
    public string NodeId { get; set; } = string.Empty;
    public string Conclusion { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public List<string> ToolsInvoked { get; set; } = [];
    public int Iterations { get; set; }
}
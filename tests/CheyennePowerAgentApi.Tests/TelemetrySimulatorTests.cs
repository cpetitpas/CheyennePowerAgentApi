using CheyennePowerAgentApi.Models;
using CheyennePowerAgentApi.Services;
using Xunit;

namespace CheyennePowerAgentApi.Tests;

public class TelemetrySimulatorTests
{
    [Fact]
    public void CreateMultiNodeAlarmEvent_UsesCorrelatedNodesAndSeverity()
    {
        var nodes = new[] { "GT-001", "FUEL-001", "FC-001" };

        var evt = TelemetrySimulator.CreateMultiNodeAlarmEvent(nodes, "control network segment", "HIGH");

        Assert.Equal("MULTI_NODE_ALARM", evt.EventType);
        Assert.Equal("GT-001, FUEL-001, FC-001", evt.NodeId);
        Assert.Equal(3, evt.Nodes.Count);
        Assert.Equal("HIGH", evt.Severity);
        Assert.Contains("control network segment", evt.Analysis, StringComparison.OrdinalIgnoreCase);
    }
}

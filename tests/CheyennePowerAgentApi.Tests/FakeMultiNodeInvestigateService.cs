using CheyennePowerAgentApi.Models;
using CheyennePowerAgentApi.Services;

namespace CheyennePowerAgentApi.Tests;

public class FakeMultiNodeInvestigateService : IMultiNodeInvestigateService
{
    public Task<MultiNodeInvestigateResponse> InvestigateAsync(
        MultiNodeInvestigateRequest request, CancellationToken ct)
    {
        return Task.FromResult(new MultiNodeInvestigateResponse
        {
            OverallSeverity     = "MEDIUM",
            RootCauseHypothesis = "Fake correlated root cause for test.",
            CorrelationSummary  = "Nodes share a common fuel header.",
            RecommendedAction   = "Inspect shared infrastructure.",
            AffectedNodes       = request.Nodes.Select(n => n.NodeId).ToList(),
            NodeResults         = request.Nodes.Select(n => new NodeInvestigationResult
            {
                NodeId            = n.NodeId,
                Conclusion        = $"Fake conclusion for {n.NodeId}.",
                Severity          = "MEDIUM",
                RecommendedAction = "Monitor closely.",
                ToolsInvoked      = ["get_generator_spec"],
                Iterations        = 1
            }).ToList()
        });
    }
}
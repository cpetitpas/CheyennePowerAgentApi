using CheyennePowerAgentApi.Models;
using CheyennePowerAgentApi.Services;

namespace CheyennePowerAgentApi.Tests;

public class FakeInvestigateService : IInvestigateService
{
    public Task<InvestigateResponse> InvestigateAsync(InvestigateRequest request, CancellationToken ct)
    {
        return Task.FromResult(new InvestigateResponse
        {
            NodeId            = request.NodeId,
            Conclusion        = "Fake investigation conclusion for test.",
            Severity          = "MEDIUM",
            RecommendedAction = "Inspect the node and verify sensor readings.",
            ToolsInvoked      = ["get_generator_spec", "get_recent_telemetry"],
            Iterations        = 2
        });
    }
}
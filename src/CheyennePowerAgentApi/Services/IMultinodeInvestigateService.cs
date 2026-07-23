using CheyennePowerAgentApi.Models;

namespace CheyennePowerAgentApi.Services;

public interface IMultiNodeInvestigateService
{
    Task<MultiNodeInvestigateResponse> InvestigateAsync(MultiNodeInvestigateRequest request, CancellationToken ct);
}
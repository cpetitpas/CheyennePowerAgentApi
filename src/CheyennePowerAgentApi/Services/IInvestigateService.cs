namespace CheyennePowerAgentApi.Services;

using CheyennePowerAgentApi.Models;

public interface IInvestigateService
{
    Task<InvestigateResponse> InvestigateAsync(InvestigateRequest request, CancellationToken ct);
}
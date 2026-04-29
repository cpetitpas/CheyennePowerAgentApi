using CheyennePowerAgentApi.Models;
using CheyennePowerAgentApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CheyennePowerAgentApi.Controllers;

[ApiController]
[Route("api/generation")]
public class GeneratorController : ControllerBase
{
    private readonly IGenerationTools _tools;

    public GeneratorController(IGenerationTools tools)
    {
        _tools = tools;
    }

    [HttpPost("dispatch")]
    public async Task<ActionResult<GeneratorDispatchResponse>> Dispatch(
        [FromBody] GeneratorDispatchRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.GeneratorId) ||
            string.IsNullOrWhiteSpace(request.DataCenterId))
            return BadRequest("GeneratorId and DataCenterId are required.");

        var dispatch = await _tools.GetGeneratorDispatchStateAsync(
            request.GeneratorId,
            request.CurrentMw,
            request.ContractedLoadMw,
            ct);

        var fuelCell = await _tools.GetFuelCellStatusAsync(request.GeneratorId, ct);
        var gas = await _tools.GetGasSupplyAdequacyAsync(request.GeneratorId, ct);
        var forecast = await _tools.GetLoadForecastAsync(request.DataCenterId, ct);
        var emissions = await _tools.GetEmissionsStateAsync(request.GeneratorId, ct);

        var targetLoad = Math.Max(request.ContractedLoadMw, forecast.ForecastMw15Min);
        var gap = targetLoad - dispatch.CurrentMw;
        var availableSupport = fuelCell.AvailableMw + Math.Max(0, request.ReserveTargetMw);

        var severity = "LOW";
        if (!gas.IsAdequate || !emissions.IsCompliant)
            severity = "HIGH";
        else if (gap > availableSupport)
            severity = "HIGH";
        else if (gap > 0)
            severity = "MEDIUM";

        var response = new GeneratorDispatchResponse
        {
            DispatchGapMw = Math.Round(gap, 1),
            RecommendedSetpointMw = Math.Round(dispatch.CurrentMw + Math.Max(0, gap), 1),
            ShedLoad = severity == "HIGH" && gap > availableSupport,
            Severity = severity,
            Analysis = $"Current output is {dispatch.CurrentMw:F1} MW versus target demand {targetLoad:F1} MW.",
            Action = severity switch
            {
                "HIGH" => "Increase dispatch immediately, commit fuel-cell support, and prepare controlled load shed if deficit persists.",
                "MEDIUM" => "Ramp gas generation and preload fuel-cell reserve to cover near-term forecast demand.",
                _ => "Maintain current dispatch and continue monitoring forecast and fuel conditions."
            }
        };

        return Ok(response);
    }
}

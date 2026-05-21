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

        var dispatchResult  = await _tools.GetGeneratorDispatchStateAsync(
            request.GeneratorId, request.CurrentMw, request.ContractedLoadMw, ct);
        var fuelCellResult  = await _tools.GetFuelCellStatusAsync(request.GeneratorId, ct);
        var gasResult       = await _tools.GetGasSupplyAdequacyAsync(request.GeneratorId, ct);
        var forecastResult  = await _tools.GetLoadForecastAsync(request.DataCenterId, ct);
        var emissionsResult = await _tools.GetEmissionsStateAsync(request.GeneratorId, ct);

        var dispatch  = dispatchResult.Data!;
        var fuelCell  = fuelCellResult.Data!;
        var gas       = gasResult.Data!;
        var forecast  = forecastResult.Data!;
        var emissions = emissionsResult.Data!;

        var anyDegraded = !dispatchResult.IsOk || !fuelCellResult.IsOk ||
                          !gasResult.IsOk || !forecastResult.IsOk || !emissionsResult.IsOk;

        var targetLoad       = Math.Max(request.ContractedLoadMw, forecast.ForecastMw15Min);
        var gap              = targetLoad - dispatch.CurrentMw;
        var availableSupport = fuelCell.AvailableMw + Math.Max(0, request.ReserveTargetMw);

        var severity = "LOW";
        if (anyDegraded || !gas.IsAdequate || !emissions.IsCompliant)
            severity = "HIGH";
        else if (gap > availableSupport)
            severity = "HIGH";
        else if (gap > 0)
            severity = "MEDIUM";

        var analysisBase = $"Current output is {dispatch.CurrentMw:F1} MW versus target demand {targetLoad:F1} MW.";
        var response = new GeneratorDispatchResponse
        {
            DispatchGapMw         = Math.Round(gap, 1),
            RecommendedSetpointMw = Math.Round(dispatch.CurrentMw + Math.Max(0, gap), 1),
            ShedLoad              = severity == "HIGH" && gap > availableSupport,
            Severity              = severity,
            Analysis              = anyDegraded
                ? $"{analysisBase} WARNING: one or more data sources degraded — values are fallback estimates."
                : analysisBase,
            Action = severity switch
            {
                "HIGH" when anyDegraded
                    => "One or more tool data sources are degraded — treat dispatch as at-risk and verify connectivity before committing load changes.",
                "HIGH"   => "Increase dispatch immediately, commit fuel-cell support, and prepare controlled load shed if deficit persists.",
                "MEDIUM" => "Ramp gas generation and preload fuel-cell reserve to cover near-term forecast demand.",
                _        => "Maintain current dispatch and continue monitoring forecast and fuel conditions."
            }
        };

        return Ok(response);
    }
}

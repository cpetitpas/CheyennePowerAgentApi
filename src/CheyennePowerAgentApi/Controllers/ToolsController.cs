using CheyennePowerAgentApi.Models;
using CheyennePowerAgentApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CheyennePowerAgentApi.Controllers;

/// <summary>
/// Exposes each generation tool as a direct GET endpoint for monitoring and debugging.
/// All responses are wrapped in ToolResult&lt;T&gt; so callers can inspect status,
/// confidence, source, and staleness alongside the payload.
/// </summary>
[ApiController]
[Route("api/tools")]
public class ToolsController : ControllerBase
{
    private readonly IGenerationTools _tools;

    public ToolsController(IGenerationTools tools) => _tools = tools;

    [HttpGet("dispatch-state/{generatorId}")]
    public async Task<ActionResult<ToolResult<GeneratorDispatchState>>> GetDispatchState(
        string generatorId,
        [FromQuery] double current_mw = 0,
        [FromQuery] double contracted_load_mw = 0,
        CancellationToken ct = default)
    {
        var result = await _tools.GetGeneratorDispatchStateAsync(generatorId, current_mw, contracted_load_mw, ct);
        return Ok(result);
    }

    [HttpGet("fuel-cell/{generatorId}")]
    public async Task<ActionResult<ToolResult<FuelCellStatus>>> GetFuelCell(
        string generatorId,
        CancellationToken ct = default)
    {
        var result = await _tools.GetFuelCellStatusAsync(generatorId, ct);
        return Ok(result);
    }

    [HttpGet("gas-supply/{generatorId}")]
    public async Task<ActionResult<ToolResult<GasSupplyAdequacy>>> GetGasSupply(
        string generatorId,
        CancellationToken ct = default)
    {
        var result = await _tools.GetGasSupplyAdequacyAsync(generatorId, ct);
        return Ok(result);
    }

    [HttpGet("load-forecast/{dataCenterId}")]
    public async Task<ActionResult<ToolResult<LoadForecast>>> GetLoadForecast(
        string dataCenterId,
        CancellationToken ct = default)
    {
        var result = await _tools.GetLoadForecastAsync(dataCenterId, ct);
        return Ok(result);
    }

    [HttpGet("emissions/{generatorId}")]
    public async Task<ActionResult<ToolResult<EmissionsState>>> GetEmissions(
        string generatorId,
        CancellationToken ct = default)
    {
        var result = await _tools.GetEmissionsStateAsync(generatorId, ct);
        return Ok(result);
    }
}

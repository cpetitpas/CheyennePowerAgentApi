using CheyennePowerAgentApi.Models;
using CheyennePowerAgentApi.Services;

namespace CheyennePowerAgentApi.Tests;

/// <summary>
/// Configurable test double for IGenerationTools.
/// When SimulateDegraded is true every method returns a DEGRADED result
/// (safe-fail fallback values, Confidence=0, Status="DEGRADED").
/// </summary>
public class FakeGenerationTools : IGenerationTools
{
    public bool SimulateDegraded { get; init; }

    public Task<ToolResult<GeneratorDispatchState>> GetGeneratorDispatchStateAsync(
        string generatorId, double currentMw, double contractedLoadMw, CancellationToken ct = default)
    {
        var data = new GeneratorDispatchState
        {
            GeneratorId      = generatorId,
            CurrentMw        = currentMw,
            ContractedLoadMw = contractedLoadMw
        };
        return Task.FromResult(SimulateDegraded
            ? ToolResult<GeneratorDispatchState>.Degraded(data, "fake", "simulated degradation")
            : ToolResult<GeneratorDispatchState>.Ok(data, "fake", staleAfterSeconds: 30));
    }

    public Task<ToolResult<FuelCellStatus>> GetFuelCellStatusAsync(string generatorId, CancellationToken ct = default)
    {
        var data = new FuelCellStatus { GeneratorId = generatorId, AvailableMw = 4.0, Health = "GOOD" };
        return Task.FromResult(SimulateDegraded
            ? ToolResult<FuelCellStatus>.Degraded(new FuelCellStatus { GeneratorId = generatorId }, "fake", "simulated degradation")
            : ToolResult<FuelCellStatus>.Ok(data, "fake", staleAfterSeconds: 60));
    }

    public Task<ToolResult<GasSupplyAdequacy>> GetGasSupplyAdequacyAsync(string generatorId, CancellationToken ct = default)
    {
        var data = new GasSupplyAdequacy { GeneratorId = generatorId, PressureBar = 52.0, FlowMmscfd = 160.0, IsAdequate = true };
        return Task.FromResult(SimulateDegraded
            ? ToolResult<GasSupplyAdequacy>.Degraded(new GasSupplyAdequacy { GeneratorId = generatorId, IsAdequate = false }, "fake", "simulated degradation")
            : ToolResult<GasSupplyAdequacy>.Ok(data, "fake", staleAfterSeconds: 30));
    }

    public Task<ToolResult<LoadForecast>> GetLoadForecastAsync(string dataCenterId, CancellationToken ct = default)
    {
        var data = new LoadForecast { DataCenterId = dataCenterId, ForecastMw15Min = 94.0, ForecastMw60Min = 96.0 };
        return Task.FromResult(SimulateDegraded
            ? ToolResult<LoadForecast>.Degraded(new LoadForecast { DataCenterId = dataCenterId }, "fake", "simulated degradation")
            : ToolResult<LoadForecast>.Ok(data, "fake", staleAfterSeconds: 300));
    }

    public Task<ToolResult<EmissionsState>> GetEmissionsStateAsync(string generatorId, CancellationToken ct = default)
    {
        var data = new EmissionsState { GeneratorId = generatorId, NoxPpm = 21.0, CoPpm = 6.0, IsCompliant = true };
        return Task.FromResult(SimulateDegraded
            ? ToolResult<EmissionsState>.Degraded(new EmissionsState { GeneratorId = generatorId, IsCompliant = false }, "fake", "simulated degradation")
            : ToolResult<EmissionsState>.Ok(data, "fake", staleAfterSeconds: 60));
    }
}

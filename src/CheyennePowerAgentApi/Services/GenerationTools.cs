using CheyennePowerAgentApi.Models;

namespace CheyennePowerAgentApi.Services;

public interface IGenerationTools
{
    Task<ToolResult<GeneratorDispatchState>> GetGeneratorDispatchStateAsync(
        string generatorId,
        double currentMw,
        double contractedLoadMw,
        CancellationToken ct = default);

    Task<ToolResult<FuelCellStatus>> GetFuelCellStatusAsync(string generatorId, CancellationToken ct = default);
    Task<ToolResult<GasSupplyAdequacy>> GetGasSupplyAdequacyAsync(string generatorId, CancellationToken ct = default);
    Task<ToolResult<LoadForecast>> GetLoadForecastAsync(string dataCenterId, CancellationToken ct = default);
    Task<ToolResult<EmissionsState>> GetEmissionsStateAsync(string generatorId, CancellationToken ct = default);
}

public class GenerationTools : IGenerationTools
{
    public Task<ToolResult<GeneratorDispatchState>> GetGeneratorDispatchStateAsync(
        string generatorId,
        double currentMw,
        double contractedLoadMw,
        CancellationToken ct = default)
        => ToolExecutor.ExecuteAsync(
            _ => Task.FromResult(new GeneratorDispatchState
            {
                GeneratorId      = generatorId,
                CurrentMw        = currentMw,
                ContractedLoadMw = contractedLoadMw
            }),
            fallback: new GeneratorDispatchState
            {
                GeneratorId      = generatorId,
                CurrentMw        = currentMw,
                ContractedLoadMw = contractedLoadMw
            },
            source:            "stub",
            staleAfterSeconds: 30,
            ct:                ct);

    public Task<ToolResult<FuelCellStatus>> GetFuelCellStatusAsync(string generatorId, CancellationToken ct = default)
        => ToolExecutor.ExecuteAsync(
            _ => Task.FromResult(new FuelCellStatus
            {
                GeneratorId = generatorId,
                AvailableMw = 4.0,
                Health      = "GOOD"
            }),
            fallback: new FuelCellStatus { GeneratorId = generatorId },
            source:            "stub",
            staleAfterSeconds: 60,
            ct:                ct);

    public Task<ToolResult<GasSupplyAdequacy>> GetGasSupplyAdequacyAsync(string generatorId, CancellationToken ct = default)
        => ToolExecutor.ExecuteAsync(
            _ => Task.FromResult(new GasSupplyAdequacy
            {
                GeneratorId = generatorId,
                PressureBar = 52.0,
                FlowMmscfd  = 160.0,
                IsAdequate  = true
            }),
            // Fallback is conservative: treat supply as inadequate until confirmed
            fallback: new GasSupplyAdequacy { GeneratorId = generatorId, IsAdequate = false },
            source:            "stub",
            staleAfterSeconds: 30,
            ct:                ct);

    public Task<ToolResult<LoadForecast>> GetLoadForecastAsync(string dataCenterId, CancellationToken ct = default)
        => ToolExecutor.ExecuteAsync(
            _ => Task.FromResult(new LoadForecast
            {
                DataCenterId    = dataCenterId,
                ForecastMw15Min = 94.0,
                ForecastMw60Min = 96.0
            }),
            fallback: new LoadForecast { DataCenterId = dataCenterId },
            source:            "stub",
            staleAfterSeconds: 300,
            ct:                ct);

    public Task<ToolResult<EmissionsState>> GetEmissionsStateAsync(string generatorId, CancellationToken ct = default)
        => ToolExecutor.ExecuteAsync(
            _ => Task.FromResult(new EmissionsState
            {
                GeneratorId = generatorId,
                NoxPpm      = 21.0,
                CoPpm       = 6.0,
                IsCompliant = true
            }),
            // Fallback is conservative: treat emissions as non-compliant until confirmed
            fallback: new EmissionsState { GeneratorId = generatorId, IsCompliant = false },
            source:            "stub",
            staleAfterSeconds: 60,
            ct:                ct);
}

using CheyennePowerAgentApi.Models;

namespace CheyennePowerAgentApi.Services;

public interface IGenerationTools
{
    Task<GeneratorDispatchState> GetGeneratorDispatchStateAsync(
        string generatorId,
        double currentMw,
        double contractedLoadMw,
        CancellationToken ct = default);

    Task<FuelCellStatus> GetFuelCellStatusAsync(string generatorId, CancellationToken ct = default);
    Task<GasSupplyAdequacy> GetGasSupplyAdequacyAsync(string generatorId, CancellationToken ct = default);
    Task<LoadForecast> GetLoadForecastAsync(string dataCenterId, CancellationToken ct = default);
    Task<EmissionsState> GetEmissionsStateAsync(string generatorId, CancellationToken ct = default);
}

public class GenerationTools : IGenerationTools
{
    public Task<GeneratorDispatchState> GetGeneratorDispatchStateAsync(
        string generatorId,
        double currentMw,
        double contractedLoadMw,
        CancellationToken ct = default)
    {
        return Task.FromResult(new GeneratorDispatchState
        {
            GeneratorId = generatorId,
            CurrentMw = currentMw,
            ContractedLoadMw = contractedLoadMw
        });
    }

    public Task<FuelCellStatus> GetFuelCellStatusAsync(string generatorId, CancellationToken ct = default)
    {
        return Task.FromResult(new FuelCellStatus
        {
            GeneratorId = generatorId,
            AvailableMw = 4.0,
            Health = "GOOD"
        });
    }

    public Task<GasSupplyAdequacy> GetGasSupplyAdequacyAsync(string generatorId, CancellationToken ct = default)
    {
        return Task.FromResult(new GasSupplyAdequacy
        {
            GeneratorId = generatorId,
            PressureBar = 52.0,
            FlowMmscfd = 160.0,
            IsAdequate = true
        });
    }

    public Task<LoadForecast> GetLoadForecastAsync(string dataCenterId, CancellationToken ct = default)
    {
        return Task.FromResult(new LoadForecast
        {
            DataCenterId = dataCenterId,
            ForecastMw15Min = 94.0,
            ForecastMw60Min = 96.0
        });
    }

    public Task<EmissionsState> GetEmissionsStateAsync(string generatorId, CancellationToken ct = default)
    {
        return Task.FromResult(new EmissionsState
        {
            GeneratorId = generatorId,
            NoxPpm = 21.0,
            CoPpm = 6.0,
            IsCompliant = true
        });
    }
}

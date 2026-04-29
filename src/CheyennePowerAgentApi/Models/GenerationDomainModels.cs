namespace CheyennePowerAgentApi.Models;

public class GeneratorDispatchRequest
{
    public string GeneratorId { get; set; } = string.Empty;
    public string DataCenterId { get; set; } = string.Empty;
    public double CurrentMw { get; set; }
    public double ContractedLoadMw { get; set; }
    public double ReserveTargetMw { get; set; }
}

public class GeneratorDispatchResponse
{
    public string Analysis { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public double DispatchGapMw { get; set; }
    public double RecommendedSetpointMw { get; set; }
    public bool ShedLoad { get; set; }
}

public class TurbineAlarmRequest
{
    public string TurbineId { get; set; } = string.Empty;
    public string AlarmType { get; set; } = string.Empty;
    public double SensorValue { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public class TurbineAlarmResponse
{
    public string Analysis { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public double RecommendedDeratePercent { get; set; }
}

public class GeneratorDispatchState
{
    public string GeneratorId { get; set; } = string.Empty;
    public double CurrentMw { get; set; }
    public double ContractedLoadMw { get; set; }
}

public class FuelCellStatus
{
    public string SiteId { get; set; } = string.Empty;
    public double AvailableMw { get; set; }
    public string Health { get; set; } = string.Empty;
}

public class GasSupplyAdequacy
{
    public string SiteId { get; set; } = string.Empty;
    public double PressureBar { get; set; }
    public double FlowMmscfd { get; set; }
    public bool IsAdequate { get; set; }
}

public class LoadForecast
{
    public string DataCenterId { get; set; } = string.Empty;
    public double ForecastMw15Min { get; set; }
    public double ForecastMw60Min { get; set; }
}

public class EmissionsState
{
    public string SiteId { get; set; } = string.Empty;
    public double NoxPpm { get; set; }
    public double CoPpm { get; set; }
    public bool IsCompliant { get; set; }
}

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
    public string GeneratorId { get; set; } = string.Empty;
    public double AvailableMw { get; set; }
    public string Health { get; set; } = string.Empty;
}

public class GasSupplyAdequacy
{
    public string GeneratorId { get; set; } = string.Empty;
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
    public string GeneratorId { get; set; } = string.Empty;
    public double NoxPpm { get; set; }
    public double CoPpm { get; set; }
    public bool IsCompliant { get; set; }
}

// ── Load-balance decision ────────────────────────────────────────────────────

public class LoadBalanceRequest
{
    public string GeneratorId { get; set; } = string.Empty;
    public string DataCenterId { get; set; } = string.Empty;
    public double CurrentMw { get; set; }
    public double ContractedLoadMw { get; set; }
    public double FuelCellAvailableMw { get; set; }
}

public class LoadBalanceResponse
{
    public string Analysis { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    /// <summary>MAINTAIN | SHED | BOOST</summary>
    public string Decision { get; set; } = string.Empty;
    public double DeficitMw { get; set; }
    public double SurplusMw { get; set; }
}

// ── Hybrid dispatch (gas + Bloom fuel cell) ──────────────────────────────────

public class HybridDispatchRequest
{
    public string GeneratorId { get; set; } = string.Empty;
    public string DataCenterId { get; set; } = string.Empty;
    public double CurrentGasMw { get; set; }
    public double FuelCellAvailableMw { get; set; }
    public double ContractedLoadMw { get; set; }
}

public class HybridDispatchResponse
{
    public string Analysis { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public double GasSetpointMw { get; set; }
    public double FuelCellSetpointMw { get; set; }
    public double TotalSetpointMw { get; set; }
    public bool IsHybridRequired { get; set; }
}

// ── Telemetry ingest ─────────────────────────────────────────────────────────

public class TelemetryIngestRequest
{
    public string NodeId { get; set; } = string.Empty;
    /// <summary>ALARM | FLOW | TURBINE_ALARM | FUEL_CELL_ALARM | DISPATCH</summary>
    public string EventType { get; set; } = string.Empty;
    public string AlarmCode { get; set; } = string.Empty;
    public double SensorValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class TelemetryIngestResponse
{
    public string EventId { get; set; } = string.Empty;
    public bool Accepted { get; set; }
    public string Severity { get; set; } = string.Empty;
    /// <summary>NORMAL | ADVISORY | WARNING | CRITICAL</summary>
    public string Classification { get; set; } = string.Empty;
}

// ── Short-horizon reserve margin threshold ───────────────────────────────────

public class ReserveMarginRequest
{
    public string GeneratorId { get; set; } = string.Empty;
    /// <summary>Forecast look-ahead window in minutes; typically 15 or 60.</summary>
    public int ForecastWindowMinutes { get; set; }
    public double CurrentMw { get; set; }
    public double ContractedLoadMw { get; set; }
    public double FuelCellAvailableMw { get; set; }
}

public class ReserveMarginResponse
{
    public string Analysis { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public double ReserveMarginPercent { get; set; }
    public double ThresholdPercent { get; set; }
    public bool IsThresholdBreached { get; set; }
}

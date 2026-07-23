using System.Text.Json;
using CheyennePowerAgentApi.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CheyennePowerAgentApi.Services;

public class TelemetrySimulator : BackgroundService
{
    private readonly TelemetryChannel _channel;
    private readonly IServiceProvider _services;
    private readonly ILogger<TelemetrySimulator> _logger;

    private static readonly string[] GasTurbines  = ["GT-001", "GT-002", "GT-003", "GT-004"];
    private static readonly string[] FuelNodes    = ["FUEL-001", "FUEL-002"];
    private static readonly string[] FuelCells    = ["FC-001", "FC-002"];

    private static readonly string[] TurbineAlarms =
    [
        "HIGH_EXHAUST_TEMP", "HIGH_VIBRATION", "LOW_OIL_PRESSURE",
        "COMPRESSOR_SURGE", "FLAME_OUT", "OVERSPEED_TRIP"
    ];

    private static readonly string[] FlowAlarms =
    [
        "LOW_FUEL_FLOW", "HIGH_FUEL_FLOW", "PRESSURE_DROP", "FLOW_IMBALANCE"
    ];

    private static readonly string[] FuelCellAlarms =
    [
        "HIGH_STACK_TEMP", "LOW_FUEL_UTILIZATION", "INVERTER_FAULT", "COOLANT_LEAK"
    ];

    private readonly Random _rng = new();
    private const double DefaultExpectedFlowMmscfd = 175.0;

    private static readonly IReadOnlyDictionary<string, double> ExpectedFlowByNode =
        new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["FUEL-001"] = 180.0,
            ["FUEL-002"] = 170.0
        };

    public TelemetrySimulator(
        TelemetryChannel channel,
        IServiceProvider services,
        ILogger<TelemetrySimulator> logger)
    {
        _channel = channel;
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(_rng.Next(3, 9)), ct);

            try
            {
                await using var scope = _services.CreateAsyncScope();

                var eventClass = _rng.Next(4);
                TelemetryEvent evt;
                if (eventClass == 3)
                {
                    var tools = scope.ServiceProvider.GetRequiredService<IGenerationTools>();
                    evt = await SimulateDispatchAsync(tools, ct);
                }
                else
                {
                    var claude = scope.ServiceProvider.GetRequiredService<IClaudeService>();
                    evt = eventClass switch
                    {
                        0 => await SimulateFlowAsync(claude, ct),
                        1 => await SimulateFuelCellAlarmAsync(claude, ct),
                        _ => await SimulateTurbineAlarmAsync(claude, ct)
                    };
                }

                await _channel.Writer.WriteAsync(evt, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TelemetrySimulator error");
            }
        }
    }

    private async Task<TelemetryEvent> SimulateFuelCellAlarmAsync(IClaudeService claude, CancellationToken ct)
    {
        var nodeId    = FuelCells[_rng.Next(FuelCells.Length)];
        var alarmType = FuelCellAlarms[_rng.Next(FuelCellAlarms.Length)];
        var (sensorValue, unit) = alarmType switch
        {
            "HIGH_STACK_TEMP"      => (680.0 + _rng.NextDouble() * 60,  "degC"),
            "LOW_FUEL_UTILIZATION" => (60.0  + _rng.NextDouble() * 15,  "%"),
            "INVERTER_FAULT"       => (480.0 + _rng.NextDouble() * 20,  "V"),
            "COOLANT_LEAK"         => (2.5   + _rng.NextDouble() * 1.5, "L/min"),
            _                      => (_rng.NextDouble() * 100,           "units")
        };

        var prompt = $$"""
            You are an AI agent monitoring Bloom Energy fuel cells at a power generation facility.
            Analyze the following alarm and respond with a JSON object using snake_case keys:
            {
              "analysis": "<one sentence assessment>",
              "action": "<recommended operator action>",
              "severity": "<LOW|MEDIUM|HIGH>"
            }
            Node: {{nodeId}}
            Alarm: {{alarmType}}
            Sensor value: {{sensorValue:F1}} {{unit}}
            Respond with JSON only.
            """;

        var raw = await claude.AnalyzeAlarmAsync(prompt, ct);
        var parsed = ParseOrDefault<GeneratorAlarmResponse>(raw);

        return new TelemetryEvent
        {
            EventType = "FUEL_CELL_ALARM",
            NodeId    = nodeId,
            Severity  = parsed?.Severity ?? "LOW",
            Analysis  = parsed?.Analysis ?? raw,
            Action    = parsed?.Action   ?? string.Empty,
            Timestamp = DateTime.UtcNow
        };
    }

    private async Task<TelemetryEvent> SimulateTurbineAlarmAsync(IClaudeService claude, CancellationToken ct)
    {
        var nodeId    = GasTurbines[_rng.Next(GasTurbines.Length)];
        var alarmType = TurbineAlarms[_rng.Next(TurbineAlarms.Length)];
        var (sensorValue, unit) = alarmType switch
        {
            "HIGH_EXHAUST_TEMP"  => (600.0 + _rng.NextDouble() * 120, "degC"),
            "HIGH_VIBRATION"     => (6.0   + _rng.NextDouble() * 6,   "mm/s"),
            "LOW_OIL_PRESSURE"   => (0.5   + _rng.NextDouble() * 2.5, "bar"),
            "COMPRESSOR_SURGE"   => (88.0  + _rng.NextDouble() * 10,  "%"),
            "OVERSPEED_TRIP"     => (3000  + _rng.NextDouble() * 400,  "RPM"),
            _                    => (_rng.NextDouble() * 100,           "units")
        };

        var prompt = $$"""
            You are an AI agent monitoring gas turbines at a natural gas-fired power generation facility.
            Analyze the following turbine alarm and respond with a JSON object using snake_case keys:
            {
              "analysis": "<one sentence assessment>",
              "action": "<recommended operator action>",
              "severity": "<LOW|MEDIUM|HIGH>"
            }
            Turbine: {{nodeId}}
            Alarm: {{alarmType}}
            Sensor value: {{sensorValue:F1}} {{unit}}
            Respond with JSON only.
            """;

        var raw = await claude.AnalyzeAlarmAsync(prompt, ct);
        var parsed = ParseOrDefault<GeneratorAlarmResponse>(raw);

        return new TelemetryEvent
        {
            EventType = "TURBINE_ALARM",
            NodeId    = nodeId,
            Severity  = parsed?.Severity ?? "LOW",
            Analysis  = parsed?.Analysis ?? raw,
            Action    = parsed?.Action   ?? string.Empty,
            Timestamp = DateTime.UtcNow
        };
    }

    private async Task<TelemetryEvent> SimulateDispatchAsync(IGenerationTools tools, CancellationToken ct)
    {
        var generatorId      = GasTurbines[_rng.Next(GasTurbines.Length)];
        var currentMw        = 60.0 + _rng.NextDouble() * 50;
        var contractedLoadMw = 85.0 + _rng.NextDouble() * 20;

        var dispatchResult  = await tools.GetGeneratorDispatchStateAsync(generatorId, currentMw, contractedLoadMw, ct);
        var fuelCellResult  = await tools.GetFuelCellStatusAsync(generatorId, ct);
        var gasResult       = await tools.GetGasSupplyAdequacyAsync(generatorId, ct);
        var emissionsResult = await tools.GetEmissionsStateAsync(generatorId, ct);

        var dispatch  = dispatchResult.Data!;
        var fuelCell  = fuelCellResult.Data!;
        var gas       = gasResult.Data!;
        var emissions = emissionsResult.Data!;

        var anyDegraded = !dispatchResult.IsOk || !fuelCellResult.IsOk ||
                          !gasResult.IsOk || !emissionsResult.IsOk;

        var gap = dispatch.ContractedLoadMw - dispatch.CurrentMw;

        string severity;
        if (anyDegraded || !gas.IsAdequate || !emissions.IsCompliant)
            severity = "HIGH";
        else if (gap > fuelCell.AvailableMw)
            severity = "HIGH";
        else if (gap > 0)
            severity = "MEDIUM";
        else
            severity = "LOW";

        return new TelemetryEvent
        {
            EventType = "DISPATCH",
            NodeId    = generatorId,
            Severity  = severity,
            Analysis  = anyDegraded
                ? $"{generatorId}: dispatch data degraded — operating on fallback values (gap {gap:+0.0;-0.0} MW)."
                : $"{generatorId} at {currentMw:F1} MW vs contracted {contractedLoadMw:F1} MW (gap {gap:+0.0;-0.0} MW).",
            Action    = severity switch
            {
                "HIGH" when anyDegraded
                    => "Verify tool data sources; treat dispatch as at-risk until sources recover.",
                "HIGH"   => "Increase dispatch immediately; commit fuel-cell reserve or shed load.",
                "MEDIUM" => "Ramp generation; pre-stage fuel-cell reserve for demand forecast.",
                _        => "Maintain current dispatch; continue monitoring."
            },
            Timestamp = DateTime.UtcNow
        };
    }

    private async Task<TelemetryEvent> SimulateFlowAsync(IClaudeService claude, CancellationToken ct)
    {
        var nodeId   = FuelNodes[_rng.Next(FuelNodes.Length)];
        var flowRate = 100.0 + _rng.NextDouble() * 100;
        var unit     = "MMSCFD";
        var expectedFlowRate = ResolveExpectedFlowRate(nodeId);
        var computedVariance = CalculateVariancePercent(flowRate, expectedFlowRate);

        var prompt = $$"""
            You are an AI agent monitoring fuel gas flow to a natural gas-fired power generation facility.
            Analyze the following flow data and respond with a JSON object using snake_case keys:
            {
              "analysis": "<one sentence assessment>",
              "action": "<recommended operator action>",
              "severity": "<LOW|MEDIUM|HIGH>",
              "variance": <variance as a percentage, numeric only>
            }
            Node: {{nodeId}}
            Flow rate: {{flowRate:F1}} {{unit}}
            Expected/setpoint flow rate: {{expectedFlowRate:F1}} {{unit}}
            Compute variance using this formula:
            variance = ((flow_rate - expected_flow_rate) / expected_flow_rate) * 100
            Return numeric variance only (no percent sign).
            Respond with JSON only.
            """;

        var raw = await claude.AnalyzeFlowAsync(prompt, ct);
        var parsed = ParseOrDefault<FlowAnalysisResponse>(raw);
        var absVariance = Math.Abs(computedVariance);
        var computedSeverity = absVariance switch
        {
            >= 15.0 => "HIGH",
            >= 8.0 => "MEDIUM",
            _ => "LOW"
        };

        return new TelemetryEvent
        {
            EventType = "FLOW",
            NodeId    = nodeId,
            Severity  = NormalizeSeverity(parsed?.Severity) ?? computedSeverity,
            Analysis  = parsed?.Analysis ?? raw,
            Action    = parsed?.Action   ?? string.Empty,
            Variance  = computedVariance,
            Timestamp = DateTime.UtcNow
        };
    }

    private static double ResolveExpectedFlowRate(string nodeId)
        => ExpectedFlowByNode.TryGetValue(nodeId, out var expected)
            ? expected
            : DefaultExpectedFlowMmscfd;

    private static double CalculateVariancePercent(double actual, double expected)
        => Math.Round(((actual - expected) / expected) * 100.0, 1);

    private static string? NormalizeSeverity(string? severity)
    {
        if (string.IsNullOrWhiteSpace(severity))
            return null;

        return severity.Trim().ToUpperInvariant() switch
        {
            "LOW" => "LOW",
            "MEDIUM" => "MEDIUM",
            "HIGH" => "HIGH",
            _ => null
        };
    }

    private static T? ParseOrDefault<T>(string raw)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(raw,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
        }
        catch { return default; }
    }
}
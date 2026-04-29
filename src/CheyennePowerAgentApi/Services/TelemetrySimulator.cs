using System.Text.Json;
using CheyennePowerAgentApi.Models;
using CheyennePowerAgentApi.Services;
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
                var claude = scope.ServiceProvider.GetRequiredService<IClaudeService>();
                var tools  = scope.ServiceProvider.GetRequiredService<IGenerationTools>();

                var eventClass = _rng.Next(4);
                TelemetryEvent evt = eventClass switch
                {
                    0 => await SimulateFlowAsync(claude, ct),
                    1 => await SimulateFuelCellAlarmAsync(claude, ct),
                    2 => await SimulateTurbineAlarmAsync(claude, ct),
                    _ => await SimulateDispatchAsync(tools, ct)
                };

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

        var dispatch  = await tools.GetGeneratorDispatchStateAsync(generatorId, currentMw, contractedLoadMw, ct);
        var fuelCell  = await tools.GetFuelCellStatusAsync(generatorId, ct);
        var gas       = await tools.GetGasSupplyAdequacyAsync(generatorId, ct);
        var emissions = await tools.GetEmissionsStateAsync(generatorId, ct);

        var gap = dispatch.ContractedLoadMw - dispatch.CurrentMw;

        string severity;
        if (!gas.IsAdequate || !emissions.IsCompliant)
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
            Analysis  = $"{generatorId} at {currentMw:F1} MW vs contracted {contractedLoadMw:F1} MW (gap {gap:+0.0;-0.0} MW).",
            Action    = severity switch
            {
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
            Respond with JSON only.
            """;

        var raw = await claude.AnalyzeFlowAsync(prompt, ct);
        var parsed = ParseOrDefault<FlowAnalysisResponse>(raw);

        return new TelemetryEvent
        {
            EventType = "FLOW",
            NodeId    = nodeId,
            Severity  = parsed?.Severity ?? "LOW",
            Analysis  = parsed?.Analysis ?? raw,
            Action    = parsed?.Action   ?? string.Empty,
            Variance  = parsed?.Variance,
            Timestamp = DateTime.UtcNow
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
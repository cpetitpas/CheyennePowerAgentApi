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

                // Pick event type
                var isFlow = _rng.Next(2) == 0;
                TelemetryEvent evt;

                if (isFlow)
                    evt = await SimulateFlowAsync(claude, ct);
                else
                    evt = await SimulateAlarmAsync(claude, ct);

                await _channel.Writer.WriteAsync(evt, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TelemetrySimulator error");
            }
        }
    }

    private async Task<TelemetryEvent> SimulateAlarmAsync(IClaudeService claude, CancellationToken ct)
    {
        // Pick a node and alarm type
        string nodeId;
        string alarmType;
        double sensorValue;
        string unit;

        var nodeClass = _rng.Next(3);
        if (nodeClass == 0)
        {
            nodeId    = GasTurbines[_rng.Next(GasTurbines.Length)];
            alarmType = TurbineAlarms[_rng.Next(TurbineAlarms.Length)];
            (sensorValue, unit) = alarmType switch
            {
                "HIGH_STACK_TEMP"      => (680.0 + _rng.NextDouble() * 60,  "degC"),
                "LOW_FUEL_UTILIZATION" => (60.0  + _rng.NextDouble() * 15,  "%"),
                "INVERTER_FAULT"       => (480.0 + _rng.NextDouble() * 20,  "V"),
                "COOLANT_LEAK"         => (2.5   + _rng.NextDouble() * 1.5, "L/min"),
                _                      => (_rng.NextDouble() * 100,           "units")
            };
        }
        else if (nodeClass == 1)
        {
            nodeId    = FuelCells[_rng.Next(FuelCells.Length)];
            alarmType = FuelCellAlarms[_rng.Next(FuelCellAlarms.Length)];
            (sensorValue, unit) = alarmType switch
            {
                "HIGH_STACK_TEMP"        => (680.0 + _rng.NextDouble() * 60, "degC"),
                "LOW_FUEL_UTILIZATION"   => (60.0  + _rng.NextDouble() * 15, "%"),
                _                        => (_rng.NextDouble() * 100,          "units")
            };
        }
        else
        {
            nodeId    = FuelNodes[_rng.Next(FuelNodes.Length)];
            alarmType = FlowAlarms[_rng.Next(FlowAlarms.Length)];
            (sensorValue, unit) = ("PRESSURE_DROP" == alarmType)
                ? (40.0 + _rng.NextDouble() * 20, "bar")
                : (120.0 + _rng.NextDouble() * 60, "MMSCFD");
        }

        var prompt = $$"""
            You are an AI agent monitoring a natural gas-fired power generation facility.
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
            EventType = "ALARM",
            NodeId    = nodeId,
            Severity  = parsed?.Severity  ?? "LOW",
            Analysis  = parsed?.Analysis  ?? raw,
            Action    = parsed?.Action    ?? string.Empty,
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
using System.Text.Json;

namespace CheyennePowerAgentApi.Services;

public static class PowerGenerationTools
{
    public static readonly IReadOnlyList<object> Definitions =
    [
        new {
            name = "get_generator_spec",
            description = "Returns the rated capacity, fuel type, manufacturer, commissioning date, and operating limits for a generator or fuel cell unit.",
            input_schema = new {
                type = "object",
                properties = new {
                    node_id = new { type = "string", description = "The node identifier, e.g. GT-001, FC-002" }
                },
                required = new[] { "node_id" }
            }
        },
        new {
            name = "get_recent_telemetry",
            description = "Returns the last 10 sensor readings for a node including timestamp, parameter name, value, and unit.",
            input_schema = new {
                type = "object",
                properties = new {
                    node_id = new { type = "string", description = "The node identifier" }
                },
                required = new[] { "node_id" }
            }
        },
        new {
            name = "get_maintenance_history",
            description = "Returns the last 5 maintenance events for a node including date, work performed, and technician notes.",
            input_schema = new {
                type = "object",
                properties = new {
                    node_id = new { type = "string", description = "The node identifier" }
                },
                required = new[] { "node_id" }
            }
        },
        new {
            name = "get_fuel_supply_status",
            description = "Returns current fuel gas supply pressure and flow rate at the plant gate and per-unit fuel headers.",
            input_schema = new {
                type = "object",
                properties = new {
                    node_id = new { type = "string", description = "The node identifier to check fuel supply for" }
                },
                required = new[] { "node_id" }
            }
        },
        new {
            name = "get_output_thresholds",
            description = "Returns the normal operating ranges and alarm thresholds for all monitored parameters on a node.",
            input_schema = new {
                type = "object",
                properties = new {
                    node_id = new { type = "string", description = "The node identifier" }
                },
                required = new[] { "node_id" }
            }
        }
    ];

    public static string Invoke(string toolName, JsonElement input)
    {
        try
        {
            var nodeId = input.TryGetProperty("node_id", out var n) ? n.GetString() ?? "UNKNOWN" : "UNKNOWN";

            return toolName switch
            {
                "get_generator_spec"     => GetGeneratorSpec(nodeId),
                "get_recent_telemetry"   => GetRecentTelemetry(nodeId),
                "get_maintenance_history"=> GetMaintenanceHistory(nodeId),
                "get_fuel_supply_status" => GetFuelSupplyStatus(nodeId),
                "get_output_thresholds"  => GetOutputThresholds(nodeId),
                _                        => $"{{\"error\": \"Unknown tool: {toolName}\"}}"
            };
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private static string GetGeneratorSpec(string nodeId) => nodeId switch
    {
        "GT-001" or "GT-002" => """{"node_id":"GT-001","type":"Gas Turbine","manufacturer":"GE","model":"LM6000","rated_mw":50,"fuel":"Natural Gas","commissioned":"2019-03-15","coolant":"Air-cooled"}""",
        "GT-003" or "GT-004" => """{"node_id":"GT-003","type":"Gas Turbine","manufacturer":"Siemens","model":"SGT-800","rated_mw":50,"fuel":"Natural Gas","commissioned":"2021-07-20","coolant":"Air-cooled"}""",
        "FC-001" or "FC-002" => """{"node_id":"FC-001","type":"Bloom Fuel Cell","manufacturer":"Bloom Energy","model":"ES-5710","rated_mw":1.5,"fuel":"Natural Gas","commissioned":"2023-01-10","coolant":"Liquid-cooled"}""",
        "FUEL-001"           => """{"node_id":"FUEL-001","type":"Fuel Gas Header","pressure_rating_bar":70,"flow_capacity_mmscfd":200,"serves":["GT-001","GT-002"]}""",
        "FUEL-002"           => """{"node_id":"FUEL-002","type":"Fuel Gas Header","pressure_rating_bar":70,"flow_capacity_mmscfd":200,"serves":["GT-003","GT-004"]}""",
        _                    => $"{{\"error\": \"No spec found for {nodeId}\"}}"
    };

    private static string GetRecentTelemetry(string nodeId)
    {
        var rng = new Random();
        var readings = nodeId switch
        {
            var id when id.StartsWith("GT") => new[]
            {
                new { parameter = "exhaust_temp_c",    value = 580.0 + rng.NextDouble() * 80,  unit = "degC" },
                new { parameter = "vibration_mm_s",    value = 4.0   + rng.NextDouble() * 6,   unit = "mm/s" },
                new { parameter = "oil_pressure_bar",  value = 2.0   + rng.NextDouble() * 2,   unit = "bar"  },
                new { parameter = "output_mw",         value = 35.0  + rng.NextDouble() * 15,  unit = "MW"   },
                new { parameter = "fuel_flow_mmscfd",  value = 80.0  + rng.NextDouble() * 40,  unit = "MMSCFD" }
            },
            var id when id.StartsWith("FC") => new[]
            {
                new { parameter = "stack_temp_c",      value = 650.0 + rng.NextDouble() * 60,  unit = "degC" },
                new { parameter = "fuel_utilization",  value = 70.0  + rng.NextDouble() * 15,  unit = "%"    },
                new { parameter = "output_kw",         value = 1200.0+ rng.NextDouble() * 300, unit = "kW"   },
                new { parameter = "coolant_flow_l_min",value = 25.0  + rng.NextDouble() * 10,  unit = "L/min"},
                new { parameter = "dc_voltage_v",      value = 460.0 + rng.NextDouble() * 40,  unit = "V"    }
            },
            _ => new[]
            {
                new { parameter = "inlet_pressure_bar",value = 45.0  + rng.NextDouble() * 20,  unit = "bar"  },
                new { parameter = "flow_rate_mmscfd",  value = 120.0 + rng.NextDouble() * 60,  unit = "MMSCFD"},
                new { parameter = "outlet_temp_c",     value = 15.0  + rng.NextDouble() * 10,  unit = "degC" },
                new { parameter = "differential_bar",  value = 0.5   + rng.NextDouble() * 1,   unit = "bar"  },
                new { parameter = "valve_position_pct",value = 60.0  + rng.NextDouble() * 30,  unit = "%"    }
            }
        };
        return JsonSerializer.Serialize(new { node_id = nodeId, readings });
    }

    private static string GetMaintenanceHistory(string nodeId)
    {
        var events = new[]
        {
            new { date = "2026-06-10", work = "Combustion inspection, replaced 2 fuel nozzles", technician = "J. Harmon", notes = "Found minor carbon deposits on nozzle tips" },
            new { date = "2026-04-22", work = "Hot gas path inspection", technician = "R. Valdez", notes = "Blade tip clearances within spec" },
            new { date = "2026-02-14", work = "Lube oil filter replacement", technician = "J. Harmon", notes = "Oil sample sent to lab — clean" },
            new { date = "2025-11-30", work = "Annual major overhaul", technician = "OEM team", notes = "Rotor balanced, seals replaced" },
            new { date = "2025-09-05", work = "Control system firmware update", technician = "R. Valdez", notes = "Version 4.2.1 applied" }
        };
        return JsonSerializer.Serialize(new { node_id = nodeId, maintenance_events = events });
    }

    private static string GetFuelSupplyStatus(string nodeId)
    {
        var rng = new Random();
        var header = nodeId is "GT-003" or "GT-004" ? "FUEL-002" : "FUEL-001";
        return JsonSerializer.Serialize(new
        {
            node_id          = nodeId,
            fuel_header      = header,
            gate_pressure_bar= Math.Round(55.0 + rng.NextDouble() * 10, 1),
            gate_flow_mmscfd = Math.Round(140.0 + rng.NextDouble() * 40, 1),
            unit_header_pressure_bar = Math.Round(48.0 + rng.NextDouble() * 8, 1),
            supply_status    = "NORMAL"
        });
    }

    private static string GetOutputThresholds(string nodeId) => nodeId switch
    {
        var id when id.StartsWith("GT") => """
            {"node_id":"GT-XXX","thresholds":[
              {"parameter":"exhaust_temp_c","normal_min":400,"normal_max":620,"alarm_high":650,"trip_high":700},
              {"parameter":"vibration_mm_s","normal_min":0,"normal_max":7,"alarm_high":8,"trip_high":12},
              {"parameter":"oil_pressure_bar","normal_min":2,"normal_max":5,"alarm_low":1.8,"trip_low":1.5},
              {"parameter":"output_mw","normal_min":10,"normal_max":50,"alarm_low":5}
            ]}
            """,
        var id when id.StartsWith("FC") => """
            {"node_id":"FC-XXX","thresholds":[
              {"parameter":"stack_temp_c","normal_min":600,"normal_max":700,"alarm_high":720,"trip_high":750},
              {"parameter":"fuel_utilization","normal_min":70,"normal_max":90,"alarm_low":65,"trip_low":60},
              {"parameter":"coolant_flow_l_min","normal_min":20,"normal_max":40,"alarm_low":18}
            ]}
            """,
        _ => """
            {"thresholds":[
              {"parameter":"inlet_pressure_bar","normal_min":40,"normal_max":65,"alarm_low":35,"trip_low":30},
              {"parameter":"flow_rate_mmscfd","normal_min":50,"normal_max":200,"alarm_low":40}
            ]}
            """
    };
}
using CheyennePowerAgentApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace CheyennePowerAgentApi.Controllers;

[ApiController]
[Route("api/turbine")]
public class TurbineAlarmController : ControllerBase
{
    [HttpPost("analyze")]
    public ActionResult<TurbineAlarmResponse> Analyze([FromBody] TurbineAlarmRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TurbineId) ||
            string.IsNullOrWhiteSpace(request.AlarmType))
            return BadRequest("TurbineId and AlarmType are required.");

        var (severity, deratePercent) = EvaluateSeverity(request.AlarmType, request.SensorValue);

        var response = new TurbineAlarmResponse
        {
            Severity = severity,
            RecommendedDeratePercent = deratePercent,
            Analysis = $"{request.AlarmType} on {request.TurbineId} measured {request.SensorValue:F1} {request.Unit}.",
            Action = severity switch
            {
                "HIGH" => "Immediately derate turbine output, dispatch field inspection, and prepare backup dispatch.",
                "MEDIUM" => "Apply controlled derate and schedule near-term operator inspection.",
                _ => "Continue operation with increased monitoring frequency."
            }
        };

        return Ok(response);
    }

    private static (string severity, double deratePercent) EvaluateSeverity(string alarmType, double sensorValue)
    {
        return alarmType switch
        {
            "HIGH_EXHAUST_TEMP" when sensorValue >= 700 => ("HIGH", 20),
            "HIGH_EXHAUST_TEMP" when sensorValue >= 650 => ("MEDIUM", 10),
            "HIGH_VIBRATION" when sensorValue >= 10 => ("HIGH", 25),
            "HIGH_VIBRATION" when sensorValue >= 8 => ("MEDIUM", 12),
            "OVERSPEED_TRIP" when sensorValue >= 3200 => ("HIGH", 30),
            _ => ("LOW", 0)
        };
    }
}

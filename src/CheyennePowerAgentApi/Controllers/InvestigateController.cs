using Microsoft.AspNetCore.Mvc;
using CheyennePowerAgentApi.Models;
using CheyennePowerAgentApi.Services;

namespace CheyennePowerAgentApi.Controllers;

[ApiController]
[Route("api/investigate")]
public class InvestigateController : ControllerBase
{
    private readonly IInvestigateService _investigate;
    private readonly IMultiNodeInvestigateService _multiInvestigate;
    private readonly IChatService _chat;

    public InvestigateController(
        IInvestigateService investigate,
        IMultiNodeInvestigateService multiInvestigate,
        IChatService chat)
    {
        _investigate      = investigate;
        _multiInvestigate = multiInvestigate;
        _chat             = chat;
    }

    [HttpPost("single")]
    public async Task<ActionResult<InvestigateResponse>> Single(
        [FromBody] InvestigateRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.NodeId) || string.IsNullOrWhiteSpace(request.AlarmType))
            return BadRequest("NodeId and AlarmType are required.");

        var result     = await _investigate.InvestigateAsync(request, ct);
        var incidentId = $"{request.NodeId}-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var context = $"Node: {request.NodeId}\nAlarm: {request.AlarmType}\n" +
                      $"Sensor: {request.SensorValue} {request.Unit}\n" +
                      $"Conclusion: {result.Conclusion}\nSeverity: {result.Severity}\n" +
                      $"Recommended Action: {result.RecommendedAction}\n" +
                      $"Tools Used: {string.Join(", ", result.ToolsInvoked)}\nIterations: {result.Iterations}";

        _chat.SeedConversation(incidentId, context);
        Response.Headers["X-Incident-Id"] = incidentId;
        return Ok(result);
    }

    [HttpPost("multinode")]
    public async Task<ActionResult<MultiNodeInvestigateResponse>> MultiNode(
        [FromBody] MultiNodeInvestigateRequest request, CancellationToken ct)
    {
        if (request.Nodes is null || request.Nodes.Count < 2)
            return BadRequest("At least 2 nodes are required.");
        if (request.Nodes.Count > 10)
            return BadRequest("Maximum 10 nodes per investigation.");

        var result     = await _multiInvestigate.InvestigateAsync(request, ct);
        var incidentId = $"MULTI-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var context = $"Multi-node investigation\n" +
                      $"Region: {request.RegionContext ?? "N/A"}\n" +
                      $"Nodes: {string.Join(", ", request.Nodes.Select(n => n.NodeId))}\n" +
                      $"Overall Severity: {result.OverallSeverity}\n" +
                      $"Root Cause: {result.RootCauseHypothesis}\n" +
                      $"Correlation: {result.CorrelationSummary}\n" +
                      $"Recommended Action: {result.RecommendedAction}\n\n" +
                      string.Join("\n", result.NodeResults.Select(r =>
                          $"  {r.NodeId} [{r.Severity}]: {r.Conclusion}"));

        _chat.SeedConversation(incidentId, context);
        Response.Headers["X-Incident-Id"] = incidentId;
        return Ok(result);
    }
}
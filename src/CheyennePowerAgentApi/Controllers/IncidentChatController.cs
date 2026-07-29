using Microsoft.AspNetCore.Mvc;
using CheyennePowerAgentApi.Models;
using CheyennePowerAgentApi.Services;

namespace CheyennePowerAgentApi.Controllers;

[ApiController]
[Route("api/incidents")]
public class IncidentChatController : ControllerBase
{
    private readonly IChatService _chat;
    public IncidentChatController(IChatService chat) => _chat = chat;

    [HttpPost("{incidentId}/chat")]
    public async Task<ActionResult<ChatResponse>> Chat(
        string incidentId, [FromBody] ChatRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("Message is required.");
        try
        {
            var response = await _chat.ChatAsync(incidentId, request, ct);
            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Anthropic API key", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(503, new { error = ex.Message });
        }
    }

    [HttpGet("{incidentId}/chat")]
    public ActionResult<ConversationState> GetHistory(string incidentId)
    {
        var state = _chat.GetConversation(incidentId);
        return state is null ? NotFound() : Ok(state);
    }

    [HttpGet]
    public ActionResult<IEnumerable<ConversationState>> GetAll() =>
        Ok(_chat.GetAllConversations());

    [HttpDelete("{incidentId}")]
    public IActionResult Delete(string incidentId)
    {
        _chat.DeleteConversation(incidentId);
        return NoContent();
    }
}
using CheyennePowerAgentApi.Models;

namespace CheyennePowerAgentApi.Services;

public interface IChatService
{
    Task<ChatResponse> ChatAsync(string incidentId, ChatRequest request, CancellationToken ct);
    ConversationState? GetConversation(string incidentId);
    void DeleteConversation(string incidentId);
    IEnumerable<ConversationState> GetAllConversations();
    void SeedConversation(string incidentId, string systemContext);
}
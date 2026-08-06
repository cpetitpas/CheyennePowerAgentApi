using CheyennePowerAgentApi.Models;
using CheyennePowerAgentApi.Services;

namespace CheyennePowerAgentApi.Tests;

public class FakeChatService : IChatService
{
    private readonly InMemoryConversationStore _store = new();

    public Task<ChatResponse> ChatAsync(string incidentId, ChatRequest request, CancellationToken ct)
    {
        var state = _store.GetOrCreate(incidentId);
        state.Messages.Add(new ChatMessage { Role = "user",      Content = request.Message });
        state.Messages.Add(new ChatMessage { Role = "assistant", Content = "Fake chat reply for test." });
        _store.Save(state);

        return Task.FromResult(new ChatResponse
        {
            Reply      = "Fake chat reply for test.",
            IncidentId = incidentId
        });
    }

    public ConversationState? GetConversation(string incidentId)
    {
        var state = _store.GetOrCreate(incidentId);
        return state.Messages.Count == 0 ? null : state;
    }

    public void DeleteConversation(string incidentId) => _store.Delete(incidentId);

    public IEnumerable<ConversationState> GetAllConversations() => _store.GetAll();

    public void SeedConversation(string incidentId, string systemContext)
    {
        var state = _store.GetOrCreate(incidentId);
        if (state.Messages.Count == 0)
        {
            state.Messages.Add(new ChatMessage { Role = "user",      Content = systemContext });
            state.Messages.Add(new ChatMessage { Role = "assistant", Content = "Understood." });
            _store.Save(state);
        }
    }
}
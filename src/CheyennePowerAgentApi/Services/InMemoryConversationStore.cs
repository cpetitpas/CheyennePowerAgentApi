using System.Collections.Concurrent;
using CheyennePowerAgentApi.Models;

namespace CheyennePowerAgentApi.Services;

public class InMemoryConversationStore : IConversationStore
{
    private readonly ConcurrentDictionary<string, ConversationState> _store = new();

    public ConversationState GetOrCreate(string incidentId) =>
        _store.GetOrAdd(incidentId, id => new ConversationState { IncidentId = id });

    public void Save(ConversationState state)
    {
        state.LastActivityAt = DateTime.UtcNow;
        _store[state.IncidentId] = state;
    }

    public void Delete(string incidentId) => _store.TryRemove(incidentId, out _);

    public IEnumerable<ConversationState> GetAll() => _store.Values.OrderByDescending(s => s.LastActivityAt);
}
using CheyennePowerAgentApi.Models;

namespace CheyennePowerAgentApi.Services;

public interface IConversationStore
{
    ConversationState GetOrCreate(string incidentId);
    void Save(ConversationState state);
    void Delete(string incidentId);
    IEnumerable<ConversationState> GetAll();
}
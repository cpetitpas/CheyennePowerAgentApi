using CheyennePowerAgentApi.Services;
using Xunit;

namespace CheyennePowerAgentApi.Tests;

public class ConversationStoreTests
{
    private readonly InMemoryConversationStore _store = new();

    [Fact]
    public void GetOrCreate_ReturnsNewState_ForUnknownId()
    {
        var state = _store.GetOrCreate("INC-NEW");
        Assert.NotNull(state);
        Assert.Equal("INC-NEW", state.IncidentId);
        Assert.Empty(state.Messages);
    }

    [Fact]
    public void Save_PersistsMessages()
    {
        var state = _store.GetOrCreate("INC-SAVE");
        state.Messages.Add(new() { Role = "user", Content = "Hello" });
        _store.Save(state);

        var retrieved = _store.GetOrCreate("INC-SAVE");
        Assert.Single(retrieved.Messages);
        Assert.Equal("Hello", retrieved.Messages[0].Content);
    }

    [Fact]
    public void Delete_RemovesConversation()
    {
        var state = _store.GetOrCreate("INC-DEL");
        state.Messages.Add(new() { Role = "user", Content = "Test" });
        _store.Save(state);

        _store.Delete("INC-DEL");

        var retrieved = _store.GetOrCreate("INC-DEL");
        Assert.Empty(retrieved.Messages);
    }

    [Fact]
    public void GetAll_ReturnsAllSavedConversations()
    {
        _store.GetOrCreate("INC-A");
        _store.Save(_store.GetOrCreate("INC-B"));

        var all = _store.GetAll().ToList();
        Assert.True(all.Count >= 1);
    }
}
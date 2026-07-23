using CheyennePowerAgentApi.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CheyennePowerAgentApi.Tests;

public class AnthropicConfigurationTests
{
    [Fact]
    public void ChatService_CanBeConstructed_WhenApiKeyIsMissing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var exception = Record.Exception(() =>
            new ChatService(new HttpClient(), new InMemoryConversationStore(), config));

        Assert.Null(exception);
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}

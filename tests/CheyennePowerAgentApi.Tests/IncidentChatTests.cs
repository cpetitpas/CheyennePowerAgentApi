using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using CheyennePowerAgentApi.Models;
using CheyennePowerAgentApi.Services;
using Xunit;

namespace CheyennePowerAgentApi.Tests;

public class IncidentChatTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public IncidentChatTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var chat = services.SingleOrDefault(d => d.ServiceType == typeof(IChatService));
                if (chat != null) services.Remove(chat);
                services.AddScoped<IChatService, FakeChatService>();
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Chat_Returns200_WithValidMessage()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/incidents/INC-001/chat",
            new ChatRequest { Message = "What caused the alarm?" },
            TestBase.JsonOpts);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ChatResponse>(TestBase.JsonOpts);
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.Reply));
        Assert.Equal("INC-001", result.IncidentId);
    }

    [Fact]
    public async Task Chat_Returns400_WhenMessageEmpty()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/incidents/INC-001/chat",
            new ChatRequest { Message = "" },
            TestBase.JsonOpts);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetHistory_Returns404_ForUnknownIncident()
    {
        var response = await _client.GetAsync("/api/incidents/UNKNOWN-999/chat");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetHistory_Returns200_AfterChatTurn()
    {
        const string id = "INC-HIST-001";
        await _client.PostAsJsonAsync($"/api/incidents/{id}/chat",
            new ChatRequest { Message = "Any updates?" }, TestBase.JsonOpts);

        var response = await _client.GetAsync($"/api/incidents/{id}/chat");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_Returns200_WithList()
    {
        var response = await _client.GetAsync("/api/incidents");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns204_AndConversationGone()
    {
        const string id = "INC-DEL-001";
        await _client.PostAsJsonAsync($"/api/incidents/{id}/chat",
            new ChatRequest { Message = "Test message" }, TestBase.JsonOpts);

        var delete = await _client.DeleteAsync($"/api/incidents/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var get = await _client.GetAsync($"/api/incidents/{id}/chat");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }
}
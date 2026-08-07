using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using CheyennePowerAgentApi.Models;
using Xunit;

namespace CheyennePowerAgentApi.Tests;

[Trait("Category", "Integration")]
public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Single_ReturnsValidInvestigation_FromRealClaude()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/investigate/single",
            new InvestigateRequest
            {
                NodeId      = "GT-001",
                AlarmType   = "HIGH_EXHAUST_TEMP",
                SensorValue = 672.0,
                Unit        = "degC",
                Context     = "Unit running at 90% load for past 4 hours"
            },
            TestBase.JsonOpts);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<InvestigateResponse>(TestBase.JsonOpts);
        Assert.NotNull(result);
        Assert.Equal("GT-001", result!.NodeId);
        Assert.False(string.IsNullOrWhiteSpace(result.Conclusion));
        Assert.Contains(result.Severity, new[] { "LOW", "MEDIUM", "HIGH" });
        Assert.False(string.IsNullOrWhiteSpace(result.RecommendedAction));
        Assert.NotEmpty(result.ToolsInvoked);
        Assert.True(result.Iterations > 0);
        Assert.True(response.Headers.Contains("X-Incident-Id"));
    }

    [Fact]
    public async Task MultiNode_ReturnsValidSynthesis_FromRealClaude()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/investigate/multinode",
            new MultiNodeInvestigateRequest
            {
                RegionContext = "Both units on FUEL-001 header showing simultaneous anomalies",
                Nodes =
                [
                    new() { NodeId = "GT-001", AlarmType = "HIGH_EXHAUST_TEMP", SensorValue = 668.0, Unit = "degC" },
                    new() { NodeId = "GT-002", AlarmType = "HIGH_VIBRATION",    SensorValue = 9.1,   Unit = "mm/s" }
                ]
            },
            TestBase.JsonOpts);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<MultiNodeInvestigateResponse>(TestBase.JsonOpts);
        Assert.NotNull(result);
        Assert.Contains(result!.OverallSeverity, new[] { "LOW", "MEDIUM", "HIGH" });
        Assert.False(string.IsNullOrWhiteSpace(result.RootCauseHypothesis));
        Assert.False(string.IsNullOrWhiteSpace(result.CorrelationSummary));
        Assert.False(string.IsNullOrWhiteSpace(result.RecommendedAction));
        Assert.Equal(2, result.NodeResults.Count);
        Assert.All(result.NodeResults, r =>
        {
            Assert.False(string.IsNullOrWhiteSpace(r.Conclusion));
            Assert.Contains(r.Severity, new[] { "LOW", "MEDIUM", "HIGH" });
        });
        Assert.True(response.Headers.Contains("X-Incident-Id"));
    }

    [Fact]
    public async Task Chat_ReturnsReply_FromRealClaude()
    {
        // First seed a conversation via investigate
        var invResponse = await _client.PostAsJsonAsync(
            "/api/investigate/single",
            new InvestigateRequest
            {
                NodeId      = "FC-001",
                AlarmType   = "HIGH_STACK_TEMP",
                SensorValue = 718.0,
                Unit        = "degC"
            },
            TestBase.JsonOpts);

        Assert.Equal(HttpStatusCode.OK, invResponse.StatusCode);
        var incidentId = invResponse.Headers.GetValues("X-Incident-Id").First();

        // Then send a chat message against it
        var chatResponse = await _client.PostAsJsonAsync(
            $"/api/incidents/{incidentId}/chat",
            new ChatRequest { Message = "What is the most likely root cause?" },
            TestBase.JsonOpts);

        Assert.Equal(HttpStatusCode.OK, chatResponse.StatusCode);

        var result = await chatResponse.Content.ReadFromJsonAsync<ChatResponse>(TestBase.JsonOpts);
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.Reply));
        Assert.Equal(incidentId, result.IncidentId);
    }

    [Fact]
    public async Task GeneratorAnalyze_ReturnsValidResponse_FromRealClaude()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/generator/analyze",
            new GeneratorAlarmRequest
            {
                NodeId      = "GT-003",
                AlarmType   = "COMPRESSOR_SURGE",
                SensorValue = 91.5,
                Unit        = "%"
            },
            TestBase.JsonOpts);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GeneratorAlarmResponse>(TestBase.JsonOpts);
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.Analysis));
        Assert.Contains(result.Severity, new[] { "LOW", "MEDIUM", "HIGH" });
    }
}
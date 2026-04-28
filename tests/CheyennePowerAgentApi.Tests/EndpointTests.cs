using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using CheyennePowerAgentApi.Models;
using Xunit;

namespace CheyennePowerAgentApi.Tests;

public class EndpointTests : TestBase
{
    public EndpointTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task GeneratorAnalyze_Returns200_WithValidRequest()
    {
        var response = await PostAndDeserialize<GeneratorAlarmResponse>(
            "/api/generator/analyze",
            ValidGeneratorAlarmRequest());

        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.Analysis));
        Assert.Contains(response.Severity, new[] { "LOW", "MEDIUM", "HIGH" });
    }

    [Fact]
    public async Task GeneratorAnalyze_Returns400_WhenNodeIdMissing()
    {
        var req = ValidGeneratorAlarmRequest();
        req.NodeId = "";
        var result = await PostAsync("/api/generator/analyze", req);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task FlowAnalyze_Returns200_WithValidRequest()
    {
        var response = await PostAndDeserialize<FlowAnalysisResponse>(
            "/api/flow/analyze",
            ValidFlowRequest());

        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.Analysis));
        Assert.Contains(response.Severity, new[] { "LOW", "MEDIUM", "HIGH" });
    }
}
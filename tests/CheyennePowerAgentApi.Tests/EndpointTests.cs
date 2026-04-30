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

    [Fact]
    public async Task GeneratorDispatch_Returns200_WithValidRequest()
    {
        var request = ValidGeneratorDispatchRequest();
        var response = await PostAndDeserialize<GeneratorDispatchResponse>(
            "/api/generation/dispatch",
            request);

        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.Analysis));
        Assert.Equal("MEDIUM", response.Severity);
        Assert.Equal(7.0, response.DispatchGapMw, 1);
        Assert.Equal(95.0, response.RecommendedSetpointMw, 1);
        Assert.False(response.ShedLoad);
        Assert.True(response.DispatchGapMw > 0);
        Assert.Equal(
            Math.Round(request.CurrentMw + Math.Max(0, response.DispatchGapMw), 1),
            response.RecommendedSetpointMw,
            1);
    }

    [Fact]
    public async Task TurbineAnalyze_Returns200_WithValidRequest()
    {
        var request = ValidTurbineAlarmRequest();
        var response = await PostAndDeserialize<TurbineAlarmResponse>(
            "/api/turbine/analyze",
            request);

        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.Analysis));
        Assert.Equal("MEDIUM", response.Severity);
        Assert.Equal(10.0, response.RecommendedDeratePercent, 1);
        Assert.True(response.RecommendedDeratePercent > 0);
        Assert.True(response.RecommendedDeratePercent <= 30);
    }
}
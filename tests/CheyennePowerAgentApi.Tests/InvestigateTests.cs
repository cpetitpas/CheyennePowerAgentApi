using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using CheyennePowerAgentApi.Models;
using CheyennePowerAgentApi.Services;
using Xunit;

namespace CheyennePowerAgentApi.Tests;

public class InvestigateTests : TestBase
{
    public InvestigateTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    protected override void ConfigureAdditionalServices(IServiceCollection services)
    {
        // Replace InvestigateService and MultiNodeInvestigateService
        // with fakes that don't call Anthropic
        var inv = services.SingleOrDefault(d => d.ServiceType == typeof(IInvestigateService));
        if (inv != null) services.Remove(inv);
        services.AddScoped<IInvestigateService, FakeInvestigateService>();

        var multi = services.SingleOrDefault(d => d.ServiceType == typeof(IMultiNodeInvestigateService));
        if (multi != null) services.Remove(multi);
        services.AddScoped<IMultiNodeInvestigateService, FakeMultiNodeInvestigateService>();

        var chat = services.SingleOrDefault(d => d.ServiceType == typeof(IChatService));
        if (chat != null) services.Remove(chat);
        services.AddScoped<IChatService, FakeChatService>();
    }

    // ── Single node ──────────────────────────────────────────────────────

    [Fact]
    public async Task Single_Returns200_WithValidRequest()
    {
        var response = await PostAsync("/api/investigate/single", ValidSingleRequest());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<InvestigateResponse>(TestBase.JsonOpts);
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.Conclusion));
        Assert.Contains(result.Severity, new[] { "LOW", "MEDIUM", "HIGH" });
    }

    [Fact]
    public async Task Single_Returns400_WhenNodeIdMissing()
    {
        var req = ValidSingleRequest();
        req.NodeId = "";
        var response = await PostAsync("/api/investigate/single", req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Single_Returns400_WhenAlarmTypeMissing()
    {
        var req = ValidSingleRequest();
        req.AlarmType = "";
        var response = await PostAsync("/api/investigate/single", req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Single_ReturnsIncidentIdHeader()
    {
        var response = await PostAsync("/api/investigate/single", ValidSingleRequest());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Incident-Id"));
        Assert.False(string.IsNullOrWhiteSpace(
            response.Headers.GetValues("X-Incident-Id").FirstOrDefault()));
    }

    // ── Multi node ───────────────────────────────────────────────────────

    [Fact]
    public async Task MultiNode_Returns200_WithValidRequest()
    {
        var response = await PostAsync("/api/investigate/multinode", ValidMultiRequest());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<MultiNodeInvestigateResponse>(TestBase.JsonOpts);
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.RootCauseHypothesis));
        Assert.Contains(result.OverallSeverity, new[] { "LOW", "MEDIUM", "HIGH" });
        Assert.True(result.NodeResults.Count >= 2);
    }

    [Fact]
    public async Task MultiNode_Returns400_WhenFewerThanTwoNodes()
    {
        var req = ValidMultiRequest();
        req.Nodes = req.Nodes.Take(1).ToList();
        var response = await PostAsync("/api/investigate/multinode", req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MultiNode_Returns400_WhenMoreThanTenNodes()
    {
        var req = ValidMultiRequest();
        req.Nodes = Enumerable.Range(1, 11).Select(i => new NodeAlarmInput
        {
            NodeId      = $"GT-00{i % 4 + 1}",
            AlarmType   = "HIGH_VIBRATION",
            SensorValue = 8.0,
            Unit        = "mm/s"
        }).ToList();
        var response = await PostAsync("/api/investigate/multinode", req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MultiNode_ReturnsIncidentIdHeader()
    {
        var response = await PostAsync("/api/investigate/multinode", ValidMultiRequest());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Incident-Id"));
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static InvestigateRequest ValidSingleRequest() => new()
    {
        NodeId      = "GT-001",
        AlarmType   = "HIGH_EXHAUST_TEMP",
        SensorValue = 665.0,
        Unit        = "degC"
    };

    private static MultiNodeInvestigateRequest ValidMultiRequest() => new()
    {
        RegionContext = "Units on FUEL-001 header",
        Nodes =
        [
            new() { NodeId = "GT-001", AlarmType = "HIGH_EXHAUST_TEMP", SensorValue = 665.0, Unit = "degC" },
            new() { NodeId = "GT-002", AlarmType = "HIGH_VIBRATION",    SensorValue = 8.5,   Unit = "mm/s" }
        ]
    };
}
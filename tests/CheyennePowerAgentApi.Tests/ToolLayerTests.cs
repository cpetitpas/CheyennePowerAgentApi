using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using CheyennePowerAgentApi.Models;
using CheyennePowerAgentApi.Services;
using Xunit;

namespace CheyennePowerAgentApi.Tests;

public class ToolLayerTests : TestBase
{
    private readonly HttpClient _degradedClient;

    public ToolLayerTests(WebApplicationFactory<Program> factory) : base(factory)
    {
        _degradedClient = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var claude = services.SingleOrDefault(d => d.ServiceType == typeof(IClaudeService));
                if (claude != null) services.Remove(claude);
                services.AddScoped<IClaudeService, FakeClaudeService>();

                var tools = services.SingleOrDefault(d => d.ServiceType == typeof(IGenerationTools));
                if (tools != null) services.Remove(tools);
                services.AddScoped<IGenerationTools>(_ => new FakeGenerationTools { SimulateDegraded = true });
            });
        }).CreateClient();
    }

    // ── Tool endpoint envelope shape ─────────────────────────────────────────

    [Theory]
    [InlineData("/api/tools/fuel-cell/GT-001")]
    [InlineData("/api/tools/gas-supply/GT-001")]
    [InlineData("/api/tools/load-forecast/DC-001")]
    [InlineData("/api/tools/emissions/GT-001")]
    [InlineData("/api/tools/dispatch-state/GT-001")]
    public async Task ToolEndpoint_ReturnsOkEnvelope(string url)
    {
        var response = await Client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        // Read as raw JsonElement so we can inspect envelope fields without typing each generic
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("OK",  json.GetProperty("status").GetString());
        Assert.Equal("stub", json.GetProperty("source").GetString());
        Assert.Equal(1.0,   json.GetProperty("confidence").GetDouble(), precision: 2);
        Assert.True(json.GetProperty("stale_after_seconds").GetInt32() > 0);
        Assert.True(json.TryGetProperty("data", out _));
        Assert.Null(json.GetProperty("fallback_reason").GetString());

        var ts = json.GetProperty("timestamp").GetDateTime();
        Assert.True((DateTime.UtcNow - ts).TotalSeconds < 10, "timestamp should be recent");
    }

    // ── Specific tool payloads ────────────────────────────────────────────────

    [Fact]
    public async Task FuelCell_ReturnsExpectedPayload()
    {
        var result = await GetAndDeserialize<ToolResult<FuelCellStatus>>("/api/tools/fuel-cell/GT-001");

        Assert.NotNull(result.Data);
        Assert.Equal("GT-001", result.Data!.GeneratorId);
        Assert.Equal(4.0, result.Data.AvailableMw, precision: 2);
        Assert.Equal("GOOD", result.Data.Health);
        Assert.Equal(60, result.StaleAfterSeconds);
    }

    [Fact]
    public async Task GasSupply_ReturnsAdequate()
    {
        var result = await GetAndDeserialize<ToolResult<GasSupplyAdequacy>>("/api/tools/gas-supply/GT-001");

        Assert.NotNull(result.Data);
        Assert.True(result.Data!.IsAdequate);
        Assert.True(result.Data.PressureBar > 0);
        Assert.Equal(30, result.StaleAfterSeconds);
    }

    [Fact]
    public async Task LoadForecast_ReturnsForecastValues()
    {
        var result = await GetAndDeserialize<ToolResult<LoadForecast>>("/api/tools/load-forecast/DC-001");

        Assert.NotNull(result.Data);
        Assert.Equal("DC-001", result.Data!.DataCenterId);
        Assert.True(result.Data.ForecastMw15Min > 0);
        Assert.Equal(300, result.StaleAfterSeconds);
    }

    [Fact]
    public async Task Emissions_ReturnsCompliant()
    {
        var result = await GetAndDeserialize<ToolResult<EmissionsState>>("/api/tools/emissions/GT-001");

        Assert.NotNull(result.Data);
        Assert.True(result.Data!.IsCompliant);
        Assert.True(result.Data.NoxPpm > 0);
        Assert.Equal(60, result.StaleAfterSeconds);
    }

    // ── Degraded tool path ────────────────────────────────────────────────────

    [Fact]
    public async Task GeneratorDispatch_WithDegradedTools_ReturnsHighSeverityWithWarning()
    {
        var body = JsonSerializer.Serialize(ValidGeneratorDispatchRequest(), JsonOpts);
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await _degradedClient.PostAsync("/api/generation/dispatch", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GeneratorDispatchResponse>(JsonOpts);
        Assert.NotNull(result);
        Assert.Equal("HIGH", result!.Severity);
        Assert.Contains("degraded", result.Analysis, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("degraded", result.Action,   StringComparison.OrdinalIgnoreCase);
    }
}

using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CheyennePowerAgentApi.Tests;

public class InputValidationTests : TestBase
{
    public InputValidationTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task GeneratorAnalyze_Returns400_WhenAlarmTypeMissing()
    {
        var req = ValidGeneratorAlarmRequest();
        req.AlarmType = "";
        var result = await PostAsync("/api/generator/analyze", req);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task FlowAnalyze_Returns400_WhenNodeIdMissing()
    {
        var req = ValidFlowRequest();
        req.NodeId = "";
        var result = await PostAsync("/api/flow/analyze", req);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }
}
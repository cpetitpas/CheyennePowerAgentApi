using CheyennePowerAgentApi.Services;
using Microsoft.Extensions.Logging;

namespace CheyennePowerAgentApi.Tests;

public class ToolExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_WhenTimeoutOccurs_ReturnsTimeoutCode()
    {
        var result = await ToolExecutor.ExecuteAsync(
            async token =>
            {
                await Task.Delay(50, token);
                return 7;
            },
            fallback: -1,
            source: "test",
            timeoutMs: 1,
            maxRetries: 0);

        Assert.Equal("DEGRADED", result.Status);
        Assert.Equal("tool_timeout", result.FallbackReason);
        Assert.Equal(-1, result.Data);
    }

    [Fact]
    public async Task ExecuteAsync_WhenHttpRequestFails_ReturnsSanitizedReasonAndLogsFullException()
    {
        var logger = new CapturingLogger();
        const string sensitiveMessage = "request to https://internal-host.local failed";

        var result = await ToolExecutor.ExecuteAsync(
            _ => throw new HttpRequestException(sensitiveMessage),
            fallback: "fallback",
            source: "test",
            maxRetries: 0,
            logger: logger);

        Assert.Equal("DEGRADED", result.Status);
        Assert.Equal("upstream_unreachable", result.FallbackReason);
        Assert.Equal("fallback", result.Data);
        Assert.DoesNotContain("internal-host", result.FallbackReason!, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(logger.LastException);
        Assert.Contains("internal-host", logger.LastException!.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class CapturingLogger : ILogger
    {
        public Exception? LastException { get; private set; }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            LastException = exception;
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}

using CheyennePowerAgentApi.Models;

namespace CheyennePowerAgentApi.Services;

/// <summary>
/// Executes a tool call with per-attempt timeout, automatic retry, and safe fallback.
/// </summary>
public static class ToolExecutor
{
    /// <param name="fn">The tool function; receives a timeout-linked CancellationToken.</param>
    /// <param name="fallback">Value to use when all retries are exhausted.</param>
    /// <param name="source">Logical source identifier for the result envelope.</param>
    /// <param name="staleAfterSeconds">How long a successful result is considered fresh.</param>
    /// <param name="timeoutMs">Per-attempt timeout in milliseconds.</param>
    /// <param name="maxRetries">Maximum retry attempts (total calls = maxRetries + 1).</param>
    /// <param name="ct">Caller-supplied cancellation token.</param>
    public static async Task<ToolResult<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> fn,
        T? fallback,
        string source,
        int staleAfterSeconds = 60,
        int timeoutMs = 3000,
        int maxRetries = 2,
        CancellationToken ct = default)
    {
        Exception? lastEx = null;

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
                linked.CancelAfter(timeoutMs);
                var data = await fn(linked.Token);
                return ToolResult<T>.Ok(data, source, staleAfterSeconds);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // Timeout on this attempt — record and retry
                lastEx = new TimeoutException(
                    $"Tool call timed out after {timeoutMs} ms (attempt {attempt + 1}/{maxRetries + 1})");
            }
            catch (OperationCanceledException)
            {
                // Caller cancelled — propagate immediately
                throw;
            }
            catch (Exception ex)
            {
                lastEx = ex;
            }
        }

        return ToolResult<T>.Degraded(
            fallback, source,
            lastEx?.Message ?? "unknown error after retries");
    }
}

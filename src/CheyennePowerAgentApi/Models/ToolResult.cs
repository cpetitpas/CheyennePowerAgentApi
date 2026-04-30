namespace CheyennePowerAgentApi.Models;

/// <summary>
/// Standard envelope for all generation tool outputs.
/// Carries status, provenance, staleness, and confidence alongside the payload.
/// </summary>
public sealed class ToolResult<T>
{
    public T? Data { get; init; }

    /// <summary>OK | DEGRADED</summary>
    public string Status { get; init; } = "OK";

    /// <summary>Data source identifier, e.g. "stub" | "scada" | "ems"</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>0.0 – 1.0 confidence in the returned data.</summary>
    public double Confidence { get; init; } = 1.0;

    /// <summary>Number of seconds before callers should consider this result stale.</summary>
    public int StaleAfterSeconds { get; init; } = 60;

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>Populated when Status == DEGRADED; describes why the fallback was used.</summary>
    public string? FallbackReason { get; init; }

    public bool IsOk => Status == "OK";

    public static ToolResult<T> Ok(T data, string source, int staleAfterSeconds) => new()
    {
        Data             = data,
        Status           = "OK",
        Source           = source,
        Confidence       = 1.0,
        StaleAfterSeconds = staleAfterSeconds,
        Timestamp        = DateTime.UtcNow
    };

    public static ToolResult<T> Degraded(T? fallback, string source, string reason) => new()
    {
        Data              = fallback,
        Status            = "DEGRADED",
        Source            = source,
        Confidence        = 0.0,
        StaleAfterSeconds = 0,
        Timestamp         = DateTime.UtcNow,
        FallbackReason    = reason
    };
}

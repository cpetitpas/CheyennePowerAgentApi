using System.Text.Json;
using System.Text.RegularExpressions;

namespace CheyennePowerAgentApi.Services;

public static partial class AnthropicResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public static bool TryParseJson<T>(string? raw, out T? result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        foreach (var candidate in EnumerateCandidates(raw))
        {
            try
            {
                result = JsonSerializer.Deserialize<T>(candidate, JsonOptions);
                if (result is not null)
                    return true;
            }
            catch (JsonException)
            {
                // Try the next candidate.
            }
        }

        return false;
    }

    private static IEnumerable<string> EnumerateCandidates(string raw)
    {
        var trimmed = raw.Trim();
        if (trimmed.Length == 0)
            yield break;

        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
            yield return trimmed;

        foreach (Match match in FenceRegex().Matches(trimmed))
        {
            var candidate = match.Groups[1].Value.Trim();
            if (!string.IsNullOrWhiteSpace(candidate))
                yield return candidate;
        }

        var extracted = ExtractBalancedJson(trimmed);
        if (!string.IsNullOrWhiteSpace(extracted))
            yield return extracted;
    }

    private static string? ExtractBalancedJson(string raw)
    {
        var start = raw.IndexOf('{');
        while (start >= 0)
        {
            var depth = 0;
            var inString = false;
            var escaped = false;

            for (var i = start; i < raw.Length; i++)
            {
                var ch = raw[i];
                if (inString)
                {
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (ch == '\\')
                    {
                        escaped = true;
                    }
                    else if (ch == '"')
                    {
                        inString = false;
                    }
                    continue;
                }

                if (ch == '"')
                {
                    inString = true;
                }
                else if (ch == '{')
                {
                    depth++;
                }
                else if (ch == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        var candidate = raw[start..(i + 1)].Trim();
                        return candidate;
                    }
                }
            }

            start = raw.IndexOf('{', start + 1);
        }

        return null;
    }

    [GeneratedRegex(@"```(?:json)?\s*([\s\S]*?)```", RegexOptions.IgnoreCase)]
    private static partial Regex FenceRegex();
}

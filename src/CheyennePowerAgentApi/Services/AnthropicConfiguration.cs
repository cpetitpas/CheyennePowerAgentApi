using Microsoft.Extensions.Configuration;

namespace CheyennePowerAgentApi.Services;

internal static class AnthropicConfiguration
{
    public static string? GetApiKey(IConfiguration config)
    {
        return config["Anthropic__ApiKey"]
            ?? config["Anthropic:ApiKey"]
            ?? config["AnthropicApiKey"]
            ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
            ?? Environment.GetEnvironmentVariable("Anthropic__ApiKey")
            ?? Environment.GetEnvironmentVariable("Anthropic:ApiKey");
    }

    public static string GetRequiredApiKey(IConfiguration config)
    {
        var apiKey = GetApiKey(config);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Anthropic API key is not configured. Set Anthropic__ApiKey, Anthropic:ApiKey, or the ANTHROPIC_API_KEY environment variable.");
        }

        return apiKey;
    }
}

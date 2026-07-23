using System.Text;
using System.Text.Json;
using CheyennePowerAgentApi.Models;
using Microsoft.Extensions.Configuration;

namespace CheyennePowerAgentApi.Services;

public class ChatService : IChatService
{
    private readonly HttpClient _http;
    private readonly IConversationStore _store;
    private readonly string _apiKey;

    public ChatService(HttpClient http, IConversationStore store, IConfiguration config)
    {
        _http           = http;
        _store          = store;
        _apiKey         = AnthropicConfiguration.GetApiKey(config) ?? string.Empty;
        _http.BaseAddress = new Uri("https://api.anthropic.com");
    }

    public async Task<ChatResponse> ChatAsync(string incidentId, ChatRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("Anthropic API key is not configured.");

        var state = _store.GetOrCreate(incidentId);
        state.Messages.Add(new ChatMessage { Role = "user", Content = request.Message });

        var messages = state.Messages.Select(m => new { role = m.Role, content = m.Content }).ToList();

        var body = new
        {
            model      = "claude-opus-4-6",
            max_tokens = 1024,
            system     = "You are an expert power generation operations assistant for a natural gas-fired facility. " +
                         "Answer operator questions concisely and accurately based on the investigation context provided.",
            messages
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/v1/messages");
        req.Headers.Add("x-api-key", _apiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var res   = await _http.SendAsync(req, ct);
        var json  = await res.Content.ReadAsStringAsync(ct);
        var doc   = JsonDocument.Parse(json);
        var reply = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? string.Empty;

        state.Messages.Add(new ChatMessage { Role = "assistant", Content = reply });
        _store.Save(state);

        return new ChatResponse { Reply = reply, IncidentId = incidentId };
    }

    public void SeedConversation(string incidentId, string systemContext)
    {
        var state = _store.GetOrCreate(incidentId);
        if (state.Messages.Count == 0)
        {
            state.Messages.Add(new ChatMessage
            {
                Role    = "user",
                Content = $"Investigation context:\n{systemContext}\n\nI am an operator reviewing this incident. I may ask follow-up questions."
            });
            state.Messages.Add(new ChatMessage
            {
                Role    = "assistant",
                Content = "Understood. I have reviewed the investigation findings. What would you like to know?"
            });
            _store.Save(state);
        }
    }

    public ConversationState? GetConversation(string incidentId)
    {
        var state = _store.GetOrCreate(incidentId);
        return state.Messages.Count == 0 ? null : state;
    }

    public void DeleteConversation(string incidentId) => _store.Delete(incidentId);

    public IEnumerable<ConversationState> GetAllConversations() => _store.GetAll();
}
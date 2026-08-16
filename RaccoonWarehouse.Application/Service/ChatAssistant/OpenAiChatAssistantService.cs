using RaccoonWarehouse.Core.ChatAssistant;
using RaccoonWarehouse.Domain.ChatAssistant.DTOs;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RaccoonWarehouse.Application.Service.ChatAssistant;

public sealed class OpenAiChatAssistantService : IChatAssistantService
{
    private readonly IChatAssistantSettingsService _settings;
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(60) };

    public OpenAiChatAssistantService(IChatAssistantSettingsService settings) => _settings = settings;

    public async Task<ChatMessageDto> GetResponseAsync(string message, CancellationToken cancellationToken = default)
    {
        var settings = await _settings.GetSettingsAsync(cancellationToken);
        var apiKey = await _settings.GetApiKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey)) throw new InvalidOperationException("Configure an OpenAI API key before using the assistant.");
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(new { model = settings.Model, input = message.Trim(), store = false }), Encoding.UTF8, "application/json");
        using var response = await Client.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException("The OpenAI request was not accepted. Check the API key, model, and internet connection.");
        using var document = JsonDocument.Parse(content);
        var answer = document.RootElement.GetProperty("output").EnumerateArray().SelectMany(item => item.TryGetProperty("content", out var parts) ? parts.EnumerateArray() : Enumerable.Empty<JsonElement>()).FirstOrDefault(part => part.TryGetProperty("type", out var type) && type.GetString() == "output_text").GetProperty("text").GetString();
        if (string.IsNullOrWhiteSpace(answer)) throw new InvalidOperationException("The OpenAI response did not contain text.");
        return new ChatMessageDto { Text = answer };
    }
}

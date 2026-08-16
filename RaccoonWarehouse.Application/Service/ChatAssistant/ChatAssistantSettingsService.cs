using RaccoonWarehouse.Core.ChatAssistant;
using RaccoonWarehouse.Domain.ChatAssistant.DTOs;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RaccoonWarehouse.Application.Service.ChatAssistant;

public sealed class ChatAssistantSettingsService : IChatAssistantSettingsService
{
    private const string DefaultModel = "gemini-3.5-flash";
    private readonly string _settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ROCCOPOS", "chat-assistant.settings.json");

    public async Task<ChatAssistantSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await ReadAsync(cancellationToken);
        return new ChatAssistantSettingsDto
        {
            HasApiKey = !string.IsNullOrWhiteSpace(settings.EncryptedApiKey),
            Model = IsGeminiModel(settings.Model) ? settings.Model.Trim() : DefaultModel
        };
    }

    public async Task SaveAsync(string apiKey, string model, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        var protectedBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(apiKey.Trim()), null, DataProtectionScope.CurrentUser);
        var settings = new PersistedSettings { EncryptedApiKey = Convert.ToBase64String(protectedBytes), Model = model.Trim() };
        await File.WriteAllTextAsync(_settingsPath, JsonSerializer.Serialize(settings), cancellationToken);
    }

    public async Task<bool> TestConnectionAsync(string apiKey, string model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || !IsGeminiModel(model)) return false;
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model.Trim())}:generateContent");
        request.Headers.Add("x-goog-api-key", apiKey.Trim());
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = "Reply with OK." } } }
                },
                generationConfig = new { maxOutputTokens = 8 }
            }),
            Encoding.UTF8,
            "application/json");
        using var response = await client.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private static bool IsGeminiModel(string? model) =>
        !string.IsNullOrWhiteSpace(model) &&
        model.Trim().StartsWith("gemini-", StringComparison.OrdinalIgnoreCase);

    public async Task<string?> GetApiKeyAsync(CancellationToken cancellationToken = default)
    {
        var settings = await ReadAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(settings.EncryptedApiKey)) return null;
        try { return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(settings.EncryptedApiKey), null, DataProtectionScope.CurrentUser)); }
        catch (CryptographicException) { return null; }
        catch (FormatException) { return null; }
    }

    private async Task<PersistedSettings> ReadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_settingsPath)) return new PersistedSettings { Model = DefaultModel };
        try { return JsonSerializer.Deserialize<PersistedSettings>(await File.ReadAllTextAsync(_settingsPath, cancellationToken)) ?? new PersistedSettings { Model = DefaultModel }; }
        catch (JsonException) { return new PersistedSettings { Model = DefaultModel }; }
    }

    private sealed class PersistedSettings
    {
        public string? EncryptedApiKey { get; init; }
        public string Model { get; init; } = DefaultModel;
    }
}

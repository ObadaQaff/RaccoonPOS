using RaccoonWarehouse.Domain.ChatAssistant.DTOs;

namespace RaccoonWarehouse.Core.ChatAssistant;

public interface IChatAssistantSettingsService
{
    Task<ChatAssistantSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(string apiKey, string model, CancellationToken cancellationToken = default);
    Task<bool> TestConnectionAsync(string apiKey, string model, CancellationToken cancellationToken = default);
    Task<string?> GetApiKeyAsync(CancellationToken cancellationToken = default);
}

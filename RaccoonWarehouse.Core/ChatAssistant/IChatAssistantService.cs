using RaccoonWarehouse.Domain.ChatAssistant.DTOs;

namespace RaccoonWarehouse.Core.ChatAssistant;

public interface IChatAssistantService
{
    Task<ChatMessageDto> GetResponseAsync(string message, CancellationToken cancellationToken = default);
}

using RaccoonWarehouse.Domain.ChatAssistant.DTOs;

namespace RaccoonWarehouse.Core.ChatAssistant;

public interface IChatAssistantDataService
{
    Task<ChatAssistantDataResultDto?> FindDataAsync(string question, CancellationToken cancellationToken = default);
}

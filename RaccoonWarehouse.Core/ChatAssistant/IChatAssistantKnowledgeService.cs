using RaccoonWarehouse.Domain.ChatAssistant.DTOs;

namespace RaccoonWarehouse.Core.ChatAssistant;

public interface IChatAssistantKnowledgeService
{
    Task<ChatAssistantHelpTopicDto?> FindTopicAsync(string question, CancellationToken cancellationToken = default);
}

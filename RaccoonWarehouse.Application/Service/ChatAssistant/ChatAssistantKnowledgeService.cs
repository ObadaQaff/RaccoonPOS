using RaccoonWarehouse.Core.ChatAssistant;
using RaccoonWarehouse.Domain.ChatAssistant.DTOs;
using System.Text.Json;

namespace RaccoonWarehouse.Application.Service.ChatAssistant;

public sealed class ChatAssistantKnowledgeService : IChatAssistantKnowledgeService
{
    private readonly Lazy<Task<IReadOnlyList<ChatAssistantHelpTopicDto>>> _topics = new(LoadTopicsAsync);

    public async Task<ChatAssistantHelpTopicDto?> FindTopicAsync(string question, CancellationToken cancellationToken = default)
    {
        var topics = await _topics.Value.WaitAsync(cancellationToken);
        return topics.Select(topic => new
            {
                Topic = topic,
                Score = topic.Keywords.Count(keyword => question.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            })
            .Where(result => result.Score > 0)
            .OrderByDescending(result => result.Score)
            .ThenByDescending(result => result.Topic.Keywords.Max(keyword => keyword.Length))
            .Select(result => result.Topic)
            .FirstOrDefault();
    }

    private static async Task<IReadOnlyList<ChatAssistantHelpTopicDto>> LoadTopicsAsync()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "ChatAssistant", "Knowledge", "ROCCOPOS_HELP.json");
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<ChatAssistantHelpTopicDto>>(stream,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new List<ChatAssistantHelpTopicDto>();
    }
}

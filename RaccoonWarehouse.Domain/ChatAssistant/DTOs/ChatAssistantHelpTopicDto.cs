namespace RaccoonWarehouse.Domain.ChatAssistant.DTOs;

using System.Text.Json.Serialization;

public sealed class ChatAssistantHelpTopicDto
{
    public string Id { get; init; } = string.Empty;
    public IReadOnlyList<string> Keywords { get; init; } = Array.Empty<string>();
    public string TitleEn { get; init; } = string.Empty;
    public string TitleAr { get; init; } = string.Empty;
    public IReadOnlyList<string> StepsEn { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> StepsAr { get; init; } = Array.Empty<string>();
    public string? ActionKey { get; init; }
    public string? ActionLabelEn { get; init; }
    public string? ActionLabelAr { get; init; }

    [JsonIgnore]
    public double MatchScore { get; set; }

    [JsonIgnore]
    public bool IsAmbiguous { get; set; }
}

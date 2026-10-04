namespace RaccoonWarehouse.Domain.ChatAssistant.DTOs;

public sealed class ChatAssistantDataResultDto
{
    public string Type { get; init; } = string.Empty;
    public string Query { get; init; } = string.Empty;
    public string DataJson { get; init; } = string.Empty;
    public string ActionKey { get; init; } = string.Empty;
    public string ActionLabelEn { get; init; } = string.Empty;
    public string ActionLabelAr { get; init; } = string.Empty;
}

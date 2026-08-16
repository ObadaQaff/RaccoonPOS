namespace RaccoonWarehouse.Domain.ChatAssistant.DTOs;

public sealed class ChatAssistantSettingsDto
{
    public bool HasApiKey { get; init; }
    public string Model { get; init; } = "gemini-3.5-flash";
}

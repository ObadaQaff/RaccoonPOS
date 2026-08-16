namespace RaccoonWarehouse.Domain.ChatAssistant.DTOs
{
    public sealed class ChatMessageDto
    {
        public string Text { get; init; } = string.Empty;
        public bool IsFromUser { get; init; }
        public bool IsDemo { get; init; }
        public bool IsThinking { get; init; }
        public string? ActionKey { get; init; }
        public string? ActionLabel { get; init; }
        public DateTime CreatedAt { get; init; } = DateTime.Now;
    }
}

using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.Notifications
{
    public class AppNotificationDto
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Category { get; set; }
        public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;
        public int? RecipientUserId { get; set; }
        public UserRole? RecipientRole { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

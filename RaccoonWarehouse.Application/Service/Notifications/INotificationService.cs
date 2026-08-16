using RaccoonWarehouse.Domain.Notifications;

namespace RaccoonWarehouse.Application.Service.Notifications
{
    public interface INotificationService
    {
        event EventHandler<AppNotificationDto>? NotificationRaised;

        Task<bool> PublishAsync(AppNotificationDto notification, CancellationToken cancellationToken = default);
    }
}

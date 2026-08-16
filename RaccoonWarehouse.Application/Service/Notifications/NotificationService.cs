using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Notifications;

namespace RaccoonWarehouse.Application.Service.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly IUserSession _userSession;

        public NotificationService(IUserSession userSession)
        {
            _userSession = userSession;
        }

        public event EventHandler<AppNotificationDto>? NotificationRaised;

        public Task<bool> PublishAsync(AppNotificationDto notification, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!ShouldDeliver(notification))
                return Task.FromResult(false);

            NotificationRaised?.Invoke(this, notification);
            return Task.FromResult(true);
        }

        internal bool ShouldDeliver(AppNotificationDto notification)
        {
            var currentUser = _userSession.CurrentUser;
            if (currentUser == null)
                return false;

            if (notification.RecipientUserId.HasValue && notification.RecipientUserId.Value != currentUser.Id)
                return false;

            if (notification.RecipientRole.HasValue && notification.RecipientRole.Value != currentUser.Role)
                return false;

            return true;
        }
    }
}

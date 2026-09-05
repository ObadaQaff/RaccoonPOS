using RaccoonWarehouse.Application.Service.Notifications;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Notifications;
using RaccoonWarehouse.Domain.Users.DTOs;
using Xunit;

namespace RaccoonWarehouse.Tests;

public class NotificationServiceTests
{
    private static NotificationService CreateService(UserRole role)
    {
        var session = new UserSession();
        session.SetCurrentUser(new UserReadDto
        {
            Id = 7,
            Name = "Test User",
            Role = role,
            Password = "123",
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        });

        return new NotificationService(session);
    }

    [Fact]
    public async Task PublishAsync_ShouldDeliverToTargetAdmin()
    {
        var service = CreateService(UserRole.Admin);
        AppNotificationDto? received = null;
        service.NotificationRaised += (_, notification) => received = notification;

        var delivered = await service.PublishAsync(new AppNotificationDto
        {
            Title = "Low Stock",
            Message = "Item A is below threshold",
            Severity = NotificationSeverity.Warning,
            RecipientRole = UserRole.Admin
        });

        Assert.True(delivered);
        Assert.NotNull(received);
        Assert.Equal("Low Stock", received!.Title);
        Assert.Equal(UserRole.Admin, received.RecipientRole);
    }

    [Fact]
    public async Task PublishAsync_ShouldNotDeliverToNonTargetUser()
    {
        var service = CreateService(UserRole.Casher);
        AppNotificationDto? received = null;
        service.NotificationRaised += (_, notification) => received = notification;

        var delivered = await service.PublishAsync(new AppNotificationDto
        {
            Title = "Low Stock",
            Message = "Item A is below threshold",
            Severity = NotificationSeverity.Warning,
            RecipientRole = UserRole.Admin
        });

        Assert.False(delivered);
        Assert.Null(received);
    }

    [Fact]
    public async Task PublishAsync_ShouldPreserveCategoryForOrderNotifications()
    {
        var service = CreateService(UserRole.Admin);
        AppNotificationDto? received = null;
        service.NotificationRaised += (_, notification) => received = notification;

        var delivered = await service.PublishAsync(new AppNotificationDto
        {
            Title = "Order received",
            Message = "BOX-CART-123",
            Category = "OrderReceived",
            Severity = NotificationSeverity.Info,
            RecipientUserId = 7
        });

        Assert.True(delivered);
        Assert.NotNull(received);
        Assert.Equal("OrderReceived", received!.Category);
        Assert.Equal(7, received.RecipientUserId);
    }
}

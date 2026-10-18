using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Helper;
using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.PurchaseOrders;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.PurchaseOrders.DTOs;
using RaccoonWarehouse.Domain.PurchaseOrders;
using RaccoonWarehouse.Domain.Users.DTOs;
using Xunit;

namespace RaccoonWarehouse.Tests;

public class PurchaseOrderServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldRejectAnOrderWithoutLines()
    {
        var context = CreateContext(nameof(CreateAsync_ShouldRejectAnOrderWithoutLines));
        var session = CreateSession(UserRole.Casher);
        var permissionService = CreatePermissionService(context, session);
        var service = new PurchaseOrderService(
            context, null!, new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper(),
            null!, null!, permissionService, session);

        var result = await service.CreateAsync(new PurchaseOrderWriteDto());

        Assert.False(result.Success);
        Assert.Contains("at least one line", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectUserWithoutCreatePermission()
    {
        var context = CreateContext(nameof(CreateAsync_ShouldRejectUserWithoutCreatePermission));
        var session = CreateSession(UserRole.Casher);
        await context.UserPermissions.AddAsync(new RaccoonWarehouse.Domain.Permissions.UserPermission
        {
            UserId = 1,
            PermissionKey = "PurchaseOrder.Create",
            IsAllowed = false
        });
        await context.SaveChangesAsync();

        var permissionService = CreatePermissionService(context, session);
        var service = new PurchaseOrderService(
            context, null!, new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper(),
            null!, null!, permissionService, session);

        var result = await service.CreateAsync(new PurchaseOrderWriteDto
        {
            Lines = new List<PurchaseOrderLineWriteDto>
            {
                new() { ProductId = 1, ProductUnitId = 1, Quantity = 1, ExpiryDate = DateTime.Today }
            }
        });

        Assert.False(result.Success);
        Assert.Contains("permission", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApproveAsync_ShouldNotCreateInvoiceOrStockMovement()
    {
        var context = CreateContext(nameof(ApproveAsync_ShouldNotCreateInvoiceOrStockMovement));
        var session = CreateSession(UserRole.Manager);
        await context.UserPermissions.AddAsync(new RaccoonWarehouse.Domain.Permissions.UserPermission
        {
            UserId = 1,
            PermissionKey = "PurchaseOrder.Approve",
            IsAllowed = true
        });
        var order = new PurchaseOrder
        {
            OrderNumber = "PO-TEST-001",
            Status = PurchaseOrderStatus.Submitted,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
        await context.PurchaseOrders.AddAsync(order);
        await context.SaveChangesAsync();

        var service = new PurchaseOrderService(
            context, null!, new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper(),
            null!, null!, CreatePermissionService(context, session), session);

        var result = await service.ApproveAsync(order.Id);

        Assert.True(result.Success, result.Message);
        Assert.Equal(PurchaseOrderStatus.Approved, (await context.PurchaseOrders.FindAsync(order.Id))!.Status);
        Assert.Empty(context.Set<RaccoonWarehouse.Domain.Invoices.Invoice>());
        Assert.Empty(context.Set<RaccoonWarehouse.Domain.StockTransactions.StockTransaction>());
    }

    [Fact]
    public async Task RejectAsync_ShouldRequireReason()
    {
        var context = CreateContext(nameof(RejectAsync_ShouldRequireReason));
        var session = CreateSession(UserRole.Manager);
        await context.UserPermissions.AddAsync(new RaccoonWarehouse.Domain.Permissions.UserPermission
        {
            UserId = 1,
            PermissionKey = "PurchaseOrder.Reject",
            IsAllowed = true
        });
        var order = new PurchaseOrder
        {
            OrderNumber = "PO-TEST-002",
            Status = PurchaseOrderStatus.Submitted,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
        await context.PurchaseOrders.AddAsync(order);
        await context.SaveChangesAsync();

        var service = new PurchaseOrderService(
            context, null!, new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper(),
            null!, null!, CreatePermissionService(context, session), session);

        var result = await service.RejectAsync(order.Id, "");

        Assert.False(result.Success);
        Assert.Contains("reason", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ApplicationDbContext CreateContext(string name)
    {
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options);
    }

    private static UserSession CreateSession(UserRole role)
    {
        var session = new UserSession();
        session.SetCurrentUser(new UserReadDto
        {
            Id = 1,
            Name = "Test User",
            Role = role,
            Password = "test",
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        });
        return session;
    }

    private static PermissionService CreatePermissionService(ApplicationDbContext context, UserSession session)
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        return new PermissionService(context, mapper, session);
    }
}

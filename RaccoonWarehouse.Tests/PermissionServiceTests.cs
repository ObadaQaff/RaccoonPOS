using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Helper;
using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Permissions;
using RaccoonWarehouse.Domain.Permissions.DTOs;
using RaccoonWarehouse.Domain.Users.DTOs;
using Xunit;

namespace RaccoonWarehouse.Tests;

public class PermissionServiceTests
{
    private static PermissionService CreateService(string databaseName, UserRole currentRole, out ApplicationDbContext context)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        context = new ApplicationDbContext(options);
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        var userSession = new UserSession();
        userSession.SetCurrentUser(new UserReadDto
        {
            Id = 1,
            Name = "Admin User",
            Role = currentRole,
            Password = "123",
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        });

        return new PermissionService(context, mapper, userSession);
    }

    [Fact]
    public async Task EnsureSeedData_ShouldCreateDefinitions()
    {
        var service = CreateService(nameof(EnsureSeedData_ShouldCreateDefinitions), UserRole.Admin, out var context);

        await service.EnsureSeedDataAsync();

        Assert.True(await context.Set<PermissionDefinition>().AnyAsync());
        Assert.Contains(await context.Set<PermissionDefinition>().Select(x => x.Key).ToListAsync(), x => x == "Users.Create");
    }

    [Fact]
    public async Task HasPermission_ShouldDefaultToAllowed_WhenNoOverrideExists()
    {
        var service = CreateService(nameof(HasPermission_ShouldDefaultToAllowed_WhenNoOverrideExists), UserRole.Casher, out _);

        var allowed = await service.HasPermissionAsync(UserRole.Casher, "Users.View");

        Assert.True(allowed);
    }

    [Fact]
    public async Task SavePermissions_ShouldPersistRoleOverrides()
    {
        var service = CreateService(nameof(SavePermissions_ShouldPersistRoleOverrides), UserRole.Admin, out var context);
        await service.EnsureSeedDataAsync();

        var result = await service.SavePermissionsAsync(UserRole.Casher, new[]
        {
            new RolePermissionWriteDto
            {
                Role = UserRole.Casher,
                PermissionKey = "Users.Delete",
                IsAllowed = false
            }
        });

        var saved = await context.Set<RolePermission>().FirstOrDefaultAsync(x => x.Role == UserRole.Casher && x.PermissionKey == "Users.Delete");

        Assert.True(result.Success);
        Assert.NotNull(saved);
        Assert.False(saved!.IsAllowed);
    }

    [Fact]
    public async Task GetPermissionMatrix_ShouldReturnRowsWithActions()
    {
        var service = CreateService(nameof(GetPermissionMatrix_ShouldReturnRowsWithActions), UserRole.Admin, out _);
        await service.EnsureSeedDataAsync();

        var rows = await service.GetPermissionMatrixAsync(UserRole.Admin, module: "Administration");
        var usersRow = rows.FirstOrDefault(x => x.Resource == "Users");

        Assert.NotNull(usersRow);
        Assert.True(usersRow!.Actions.ContainsKey("View"));
        Assert.True(usersRow.Actions.ContainsKey("Create"));
        Assert.True(usersRow.Actions.ContainsKey("Delete"));
    }
}

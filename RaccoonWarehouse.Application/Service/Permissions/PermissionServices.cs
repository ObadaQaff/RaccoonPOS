using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Modules;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Modules;
using RaccoonWarehouse.Core.Permissions;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Permissions;
using RaccoonWarehouse.Domain.Users;
using RaccoonWarehouse.Application.Service.Users;

namespace RaccoonWarehouse.Application.Service.Permissions;

public sealed class PermissionService : IPermissionService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IModuleStateStore _modules;
    private readonly object _gate = new();
    private IReadOnlyDictionary<string, PermissionLevel> _snapshot = new Dictionary<string, PermissionLevel>(StringComparer.OrdinalIgnoreCase);

    public PermissionService(IServiceScopeFactory scopeFactory, IModuleStateStore modules)
    {
        _scopeFactory = scopeFactory;
        _modules = modules;
    }

    public PermissionLevel GetLevel(string areaKey)
    {
        if (!PermissionAreaRegistry.IsKnown(areaKey)) return PermissionLevel.None;
        var module = PermissionAreaRegistry.Find(areaKey);
        if (module?.ModuleBound == true && !_modules.IsEnabled(module.ModuleKey!)) return PermissionLevel.None;
        PermissionLevel level;
        lock (_gate)
        {
            level = _snapshot.TryGetValue(areaKey, out var ownLevel) ? ownLevel : PermissionLevel.None;
            // Legacy category keys remain a read-only compatibility fallback until the manual migration runs.
            if (level == PermissionLevel.None && module?.ParentKey == null && PermissionAreaRegistry.Find(areaKey)?.IsLegacy != true)
            {
                var legacyKey = LegacyCategoryKey(areaKey);
                if (legacyKey != null && _snapshot.TryGetValue(legacyKey, out var legacyLevel)) level = legacyLevel;
            }
        }

        if (module?.ParentKey is string parentKey && GetLevel(parentKey) == PermissionLevel.None)
            return PermissionLevel.None;

        return level;
    }

    public bool HasPermission(string areaKey, PermissionLevel minimumLevel) => GetLevel(areaKey) >= minimumLevel;

    public async Task LoadForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var rows = await db.PermissionGroupPermissions.AsNoTracking()
            .Where(x => x.PermissionGroup != null && x.PermissionGroup.IsActive && x.PermissionGroup.Users.Any(u => u.UserId == userId))
            .Select(x => new { x.AreaKey, x.Level })
            .ToListAsync(cancellationToken);

        var next = rows.GroupBy(x => x.AreaKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Max(v => v.Level), StringComparer.OrdinalIgnoreCase);
        lock (_gate) _snapshot = next;
    }

    public void Clear()
    {
        lock (_gate) _snapshot = new Dictionary<string, PermissionLevel>(StringComparer.OrdinalIgnoreCase);
    }

    private static string? LegacyCategoryKey(string areaKey) => areaKey switch
    {
        "Sales" => "POS",
        "Inventory" => "Inventory",
        "Purchasing" => "Purchasing",
        "Accounting" => "Accounting",
        "Customers" => "Customers",
        "Employees" => "Employees",
        "HR" => "HR",
        "Users" => "Users",
        "Settings" => "Settings",
        _ => null
    };
}

public sealed class PermissionManagementService : IPermissionManagementService
{
    private readonly ApplicationDbContext _db;
    private readonly IPermissionService _permissions;
    private readonly IUserSession _session;

    public PermissionManagementService(ApplicationDbContext db, IPermissionService permissions, IUserSession session)
    {
        _db = db; _permissions = permissions; _session = session;
    }

    private void RequireAdmin()
    {
        var hasPermissionManagementAccess =
            _permissions.HasPermission("Settings.PermissionManagement", PermissionLevel.Admin)
            || _permissions.HasPermission("Permissions", PermissionLevel.Admin);

        if (!hasPermissionManagementAccess)
            throw new UnauthorizedAccessException("Permission administration requires Admin access.");
    }

    public async Task<IReadOnlyList<PermissionGroupRowDto>> GetGroupsAsync(CancellationToken cancellationToken = default)
    {
        RequireAdmin();
        return await _db.PermissionGroups.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new PermissionGroupRowDto(x.Id, x.Name, x.Description, x.IsActive, x.IsProtectedAdministrator, x.Users.Count, x.Permissions.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<PermissionGroupEditDto?> GetGroupAsync(int id, CancellationToken cancellationToken = default)
    {
        RequireAdmin();
        var group = await _db.PermissionGroups.AsNoTracking().Include(x => x.Permissions).Include(x => x.Users).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return group == null ? null : new PermissionGroupEditDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            IsActive = group.IsActive,
            IsProtectedAdministrator = group.IsProtectedAdministrator,
            Permissions = group.Permissions.Select(x => new PermissionValue(x.AreaKey, x.Level)).ToList(),
            UserIds = group.Users.Select(x => x.UserId).ToList()
        };
    }

    public async Task<IReadOnlyList<PermissionUserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        RequireAdmin();
        return await _db.Set<User>().AsNoTracking().OrderBy(x => x.Name).Select(x => new PermissionUserDto(x.Id, x.Name, x.PhoneNumber)).ToListAsync(cancellationToken);
    }

    public async Task<UserPermissionSummaryDto?> GetUserAccessAsync(int userId, CancellationToken cancellationToken = default)
    {
        RequireAdmin();
        var user = await _db.Set<User>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user == null) return null;
        var rows = await _db.UserPermissionGroups.AsNoTracking().Where(x => x.UserId == userId && x.PermissionGroup != null && x.PermissionGroup.IsActive).SelectMany(x => x.PermissionGroup!.Permissions.Select(p => new { GroupName = x.PermissionGroup.Name, p.AreaKey, p.Level })).ToListAsync(cancellationToken);
        return new UserPermissionSummaryDto(user.Name, rows.Select(x => x.GroupName).Distinct().OrderBy(x => x).ToList(), rows.GroupBy(x => x.AreaKey, StringComparer.OrdinalIgnoreCase).Select(x => new PermissionValue(x.Key, x.Max(p => p.Level))).OrderBy(x => x.AreaKey).ToList());
    }

    public async Task<Result<int>> SaveGroupAsync(PermissionGroupEditDto model, CancellationToken cancellationToken = default)
    {
        RequireAdmin();
        if (string.IsNullOrWhiteSpace(model.Name)) return Result<int>.ValidationFail(new List<string> { "Group name is required." });
        if (model.Permissions.Any(x => !PermissionAreaRegistry.IsKnown(x.AreaKey) || x.Level is < PermissionLevel.View or > PermissionLevel.Admin))
            return Result<int>.ValidationFail(new List<string> { "The permission matrix contains an invalid value." });
        var levels = model.Permissions
            .GroupBy(x => x.AreaKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Last().Level, StringComparer.OrdinalIgnoreCase);
        var missingParents = PermissionAreaRegistry.DisplayAreas
            .Where(x => x.ParentKey != null
                && levels.TryGetValue(x.Key, out var childLevel)
                && childLevel != PermissionLevel.None
                && (!levels.TryGetValue(x.ParentKey, out var parentLevel) || parentLevel == PermissionLevel.None))
            .Select(x => x.ParentKey!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (missingParents.Count > 0)
            return Result<int>.ValidationFail(new List<string> { $"Parent category permission is required: {string.Join(", ", missingParents)}." });

        var group = model.Id == 0 ? new PermissionGroup { CreatedDate = DateTime.Now } : await _db.PermissionGroups.Include(x => x.Permissions).Include(x => x.Users).SingleOrDefaultAsync(x => x.Id == model.Id, cancellationToken);
        if (group == null) return Result<int>.Fail("Permission group was not found.");
        if (group.IsProtectedAdministrator && !model.IsActive) return Result<int>.Fail("The protected administrator group cannot be deactivated.");
        if (model.Id == 0) _db.PermissionGroups.Add(group);
        group.Name = model.Name.Trim(); group.Description = model.Description; group.IsActive = model.IsActive; group.IsProtectedAdministrator = group.IsProtectedAdministrator || model.IsProtectedAdministrator; group.UpdatedDate = DateTime.Now;
        group.Permissions.Clear();
        foreach (var permission in model.Permissions.GroupBy(x => x.AreaKey, StringComparer.OrdinalIgnoreCase).Select(x => x.Last()))
            group.Permissions.Add(new PermissionGroupPermission { AreaKey = permission.AreaKey, Level = permission.Level, CreatedDate = DateTime.Now });
        group.Users.Clear();
        foreach (var userId in model.UserIds.Distinct()) group.Users.Add(new UserPermissionGroup { UserId = userId, CreatedDate = DateTime.Now });
        await _db.SaveChangesAsync(cancellationToken);
        if (_session.CurrentUserId is int currentUserId) await _permissions.LoadForUserAsync(currentUserId, cancellationToken);
        return Result<int>.Ok(group.Id, "Permission group saved.");
    }

    public async Task<Result> DeactivateGroupAsync(int id, CancellationToken cancellationToken = default)
    {
        RequireAdmin();
        var group = await _db.PermissionGroups.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (group == null) return Result.Fail("Permission group was not found.");
        if (group.IsProtectedAdministrator) return Result.Fail("The protected administrator group cannot be deactivated.");
        group.IsActive = false; group.UpdatedDate = DateTime.Now;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok("Permission group deactivated.");
    }
}

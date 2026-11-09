using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users;

namespace RaccoonWarehouse.Domain.Permissions;

public sealed class PermissionGroup : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsProtectedAdministrator { get; set; }
    public ICollection<PermissionGroupPermission> Permissions { get; set; } = new List<PermissionGroupPermission>();
    public ICollection<UserPermissionGroup> Users { get; set; } = new List<UserPermissionGroup>();
}

public sealed class PermissionGroupPermission : BaseEntity
{
    public int PermissionGroupId { get; set; }
    public PermissionGroup? PermissionGroup { get; set; }
    public string AreaKey { get; set; } = string.Empty;
    public PermissionLevel Level { get; set; }
}

public sealed class UserPermissionGroup : BaseEntity
{
    public int UserId { get; set; }
    public User? User { get; set; }
    public int PermissionGroupId { get; set; }
    public PermissionGroup? PermissionGroup { get; set; }
}

using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.Permissions;

public sealed record PermissionArea(
    string Key,
    string ArabicName,
    string EnglishName,
    bool ModuleBound = false,
    string? ModuleKey = null,
    string? ParentKey = null,
    bool IsLegacy = false);
public sealed record PermissionValue(string AreaKey, PermissionLevel Level);
public sealed record PermissionGroupRowDto(int Id, string Name, string? Description, bool IsActive, bool IsProtectedAdministrator, int UserCount, int PermissionCount);
public sealed class PermissionGroupEditDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsProtectedAdministrator { get; set; }
    public List<PermissionValue> Permissions { get; set; } = new();
    public List<int> UserIds { get; set; } = new();
}
public sealed record PermissionUserDto(int Id, string Name, string? PhoneNumber);
public sealed record UserPermissionSummaryDto(string UserName, IReadOnlyList<string> GroupNames, IReadOnlyList<PermissionValue> EffectivePermissions);

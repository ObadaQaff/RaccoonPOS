using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Core.Permissions;

public interface IPermissionActionResolver
{
    string? GetArea(string actionKey);
    PermissionLevel GetRequiredLevel(string actionKey);
}

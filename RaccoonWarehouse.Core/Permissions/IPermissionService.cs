using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Core.Permissions;

public interface IPermissionService
{
    bool HasPermission(string areaKey, PermissionLevel minimumLevel);
    PermissionLevel GetLevel(string areaKey);
    Task LoadForUserAsync(int userId, CancellationToken cancellationToken = default);
    void Clear();
}

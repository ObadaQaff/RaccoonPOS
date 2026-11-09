using RaccoonWarehouse.Domain.Permissions;

namespace RaccoonWarehouse.Core.Permissions;

public interface IPermissionManagementService
{
    Task<IReadOnlyList<PermissionGroupRowDto>> GetGroupsAsync(CancellationToken cancellationToken = default);
    Task<PermissionGroupEditDto?> GetGroupAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PermissionUserDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<UserPermissionSummaryDto?> GetUserAccessAsync(int userId, CancellationToken cancellationToken = default);
    Task<RaccoonWarehouse.Core.Common.Result<int>> SaveGroupAsync(PermissionGroupEditDto model, CancellationToken cancellationToken = default);
    Task<RaccoonWarehouse.Core.Common.Result> DeactivateGroupAsync(int id, CancellationToken cancellationToken = default);
}

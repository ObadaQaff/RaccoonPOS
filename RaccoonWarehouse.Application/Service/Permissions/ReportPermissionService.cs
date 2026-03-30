using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Permissions;
using RaccoonWarehouse.Domain.Permissions.DTOs;

namespace RaccoonWarehouse.Application.Service.Permissions
{
    public interface IReportPermissionService
    {
        Task<Dictionary<string, Dictionary<UserRole, bool>>> GetPermissionsMapAsync();
        Task<HashSet<string>> GetDeniedReportKeysAsync(UserRole role);
        Task<bool> CanViewAsync(UserRole role, string reportKey);
        Task<Result<bool>> SavePermissionsAsync(IEnumerable<ReportPermissionWriteDto> permissions);
    }

    public class ReportPermissionService : IReportPermissionService
    {
        private readonly IPermissionService _permissionService;

        public ReportPermissionService(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        public async Task<Dictionary<string, Dictionary<UserRole, bool>>> GetPermissionsMapAsync()
        {
            await _permissionService.EnsureSeedDataAsync();

            var roles = Enum.GetValues<UserRole>();
            var reportDefinitions = PermissionCatalog.All
                .Where(x => x.Module == "Reports" && x.Action == "View" && x.LegacyReportKey != null)
                .ToList();
            var permissionKeys = reportDefinitions.Select(x => x.Key).ToList();

            var result = new Dictionary<string, Dictionary<UserRole, bool>>(StringComparer.OrdinalIgnoreCase);

            foreach (var role in roles)
            {
                var permissionMap = await _permissionService.GetPermissionMapAsync(role, permissionKeys);
                foreach (var report in reportDefinitions)
                {
                    if (!result.TryGetValue(report.LegacyReportKey!, out var roleMap))
                    {
                        roleMap = new Dictionary<UserRole, bool>();
                        result[report.LegacyReportKey!] = roleMap;
                    }

                    roleMap[role] = permissionMap.TryGetValue(report.Key, out var allowed) ? allowed : true;
                }
            }

            return result;
        }

        public async Task<HashSet<string>> GetDeniedReportKeysAsync(UserRole role)
        {
            await _permissionService.EnsureSeedDataAsync();

            var reportDefinitions = PermissionCatalog.All
                .Where(x => x.Module == "Reports" && x.Action == "View" && x.LegacyReportKey != null)
                .ToList();
            var permissionChecks = await _permissionService.GetPermissionMapAsync(role, reportDefinitions.Select(x => x.Key));

            return reportDefinitions
                .Where(report => !permissionChecks[report.Key])
                .Select(report => report.LegacyReportKey!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public async Task<bool> CanViewAsync(UserRole role, string reportKey)
        {
            await _permissionService.EnsureSeedDataAsync();

            var definition = PermissionCatalog.FindByLegacyReportKey(reportKey);
            if (definition == null)
                return true;

            return await _permissionService.HasPermissionAsync(role, definition.Key);
        }

        public async Task<Result<bool>> SavePermissionsAsync(IEnumerable<ReportPermissionWriteDto> permissions)
        {
            var grouped = permissions.GroupBy(x => x.Role);
            Result<bool>? lastResult = null;

            foreach (var roleGroup in grouped)
            {
                var mapped = roleGroup
                    .Select(dto =>
                    {
                        var definition = PermissionCatalog.FindByLegacyReportKey(dto.ReportKey);
                        return definition == null
                            ? null
                            : new RolePermissionWriteDto
                            {
                                Role = dto.Role,
                                PermissionKey = definition.Key,
                                IsAllowed = dto.CanView
                            };
                    })
                    .OfType<RolePermissionWriteDto>()
                    .ToList();

                if (mapped.Count == 0)
                    continue;

                lastResult = await _permissionService.SavePermissionsAsync(roleGroup.Key, mapped);
                if (!lastResult.Success)
                    return lastResult;
            }

            return lastResult ?? Result<bool>.Ok(true, "لا توجد صلاحيات تقارير بحاجة للتحديث.");
        }
    }
}

using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Permissions;
using RaccoonWarehouse.Domain.Permissions.DTOs;

namespace RaccoonWarehouse.Application.Service.Permissions
{
    public interface IPermissionService
    {
        Task EnsureSeedDataAsync();
        Task<List<string>> GetActionNamesAsync();
        Task<List<string>> GetModuleNamesAsync();
        Task<List<PermissionMatrixRowDto>> GetPermissionMatrixAsync(UserRole role, string? searchText = null, string? module = null);
        Task<bool> HasPermissionAsync(UserRole role, string permissionKey);
        Task<Dictionary<string, bool>> GetPermissionMapAsync(UserRole role, IEnumerable<string> permissionKeys);
        Task<HashSet<string>> GetDeniedPermissionKeysAsync(UserRole role, string module);
        Task<Result<bool>> SavePermissionsAsync(UserRole role, IEnumerable<RolePermissionWriteDto> permissions);
    }

    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IUserSession _userSession;
        private bool _seedEnsured;

        public PermissionService(ApplicationDbContext dbContext, IMapper mapper, IUserSession userSession)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _userSession = userSession;
        }

        public async Task EnsureSeedDataAsync()
        {
            if (_seedEnsured)
                return;

            var existingList = await _dbContext.Set<PermissionDefinition>().ToListAsync();
            var existing = existingList.ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
            var now = DateTime.Now;

            foreach (var item in PermissionCatalog.All)
            {
                if (existing.TryGetValue(item.Key, out var definition))
                {
                    definition.Module = item.Module;
                    definition.Resource = item.Resource;
                    definition.Action = item.Action;
                    definition.DisplayName = item.DisplayName;
                    definition.Description = item.Description;
                    definition.LegacyReportKey = item.LegacyReportKey;
                    definition.SortOrder = item.SortOrder;
                    definition.IsActive = true;
                    definition.UpdatedDate = now;
                    continue;
                }

                await _dbContext.Set<PermissionDefinition>().AddAsync(new PermissionDefinition
                {
                    Key = item.Key,
                    Module = item.Module,
                    Resource = item.Resource,
                    Action = item.Action,
                    DisplayName = item.DisplayName,
                    Description = item.Description,
                    LegacyReportKey = item.LegacyReportKey,
                    SortOrder = item.SortOrder,
                    IsActive = true,
                    CreatedDate = now,
                    UpdatedDate = now
                });
            }

            await _dbContext.SaveChangesAsync();
            await MigrateLegacyReportPermissionsAsync();
            _seedEnsured = true;
        }

        public Task<List<string>> GetActionNamesAsync()
        {
            return Task.FromResult(PermissionCatalog.All.Select(x => x.Action).Distinct().ToList());
        }

        public Task<List<string>> GetModuleNamesAsync()
        {
            return Task.FromResult(PermissionCatalog.All.Select(x => x.Module).Distinct().OrderBy(x => x).ToList());
        }

        public async Task<List<PermissionMatrixRowDto>> GetPermissionMatrixAsync(UserRole role, string? searchText = null, string? module = null)
        {
            await EnsureSeedDataAsync();

            var definitions = await _dbContext.Set<PermissionDefinition>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Module)
                .ThenBy(x => x.DisplayName)
                .ThenBy(x => x.SortOrder)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(module))
                definitions = definitions.Where(x => x.Module.Equals(module, StringComparison.OrdinalIgnoreCase)).ToList();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var search = searchText.Trim();
                definitions = definitions.Where(x =>
                    x.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.Module.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.Resource.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.Action.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var savedList = await _dbContext.Set<RolePermission>()
                .AsNoTracking()
                .Where(x => x.Role == role)
                .ToListAsync();
            var saved = savedList.ToDictionary(x => x.PermissionKey, x => x.IsAllowed, StringComparer.OrdinalIgnoreCase);

            return definitions
                .GroupBy(x => new { x.Module, x.Resource, x.DisplayName })
                .Select(group => new PermissionMatrixRowDto
                {
                    Module = group.Key.Module,
                    Resource = group.Key.Resource,
                    DisplayName = group.Key.DisplayName,
                    Actions = group.ToDictionary(
                        item => item.Action,
                        item => saved.TryGetValue(item.Key, out var allowed) ? allowed : true)
                })
                .ToList();
        }

        public async Task<bool> HasPermissionAsync(UserRole role, string permissionKey)
        {
            await EnsureSeedDataAsync();

            var saved = await _dbContext.Set<RolePermission>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Role == role && x.PermissionKey == permissionKey);

            return saved?.IsAllowed ?? true;
        }

        public async Task<Dictionary<string, bool>> GetPermissionMapAsync(UserRole role, IEnumerable<string> permissionKeys)
        {
            await EnsureSeedDataAsync();

            var keys = permissionKeys
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var saved = await _dbContext.Set<RolePermission>()
                .AsNoTracking()
                .Where(x => x.Role == role && keys.Contains(x.PermissionKey))
                .ToListAsync();

            var savedMap = saved.ToDictionary(x => x.PermissionKey, x => x.IsAllowed, StringComparer.OrdinalIgnoreCase);
            return keys.ToDictionary(x => x, x => savedMap.TryGetValue(x, out var allowed) ? allowed : true, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<HashSet<string>> GetDeniedPermissionKeysAsync(UserRole role, string module)
        {
            await EnsureSeedDataAsync();

            var moduleKeys = await _dbContext.Set<PermissionDefinition>()
                .AsNoTracking()
                .Where(x => x.Module == module)
                .Select(x => x.Key)
                .ToListAsync();

            var denied = await _dbContext.Set<RolePermission>()
                .AsNoTracking()
                .Where(x => x.Role == role && moduleKeys.Contains(x.PermissionKey) && !x.IsAllowed)
                .Select(x => x.PermissionKey)
                .ToListAsync();

            return denied.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public async Task<Result<bool>> SavePermissionsAsync(UserRole role, IEnumerable<RolePermissionWriteDto> permissions)
        {
            if (_userSession.CurrentUser?.Role != UserRole.Admin)
                return Result<bool>.Fail("فقط المدير يمكنه تعديل صلاحيات النظام.");

            try
            {
                await EnsureSeedDataAsync();
                var now = DateTime.Now;

                foreach (var dto in permissions)
                {
                    var existing = await _dbContext.Set<RolePermission>()
                        .FirstOrDefaultAsync(x => x.Role == role && x.PermissionKey == dto.PermissionKey);

                    if (existing == null)
                    {
                        var entity = _mapper.Map<RolePermission>(dto);
                        entity.Role = role;
                        entity.CreatedDate = now;
                        entity.UpdatedDate = now;
                        await _dbContext.Set<RolePermission>().AddAsync(entity);
                    }
                    else
                    {
                        existing.IsAllowed = dto.IsAllowed;
                        existing.UpdatedDate = now;
                    }
                }

                await _dbContext.SaveChangesAsync();
                return Result<bool>.Ok(true, "تم حفظ صلاحيات النظام بنجاح.");
            }
            catch (Exception ex)
            {
                return Result<bool>.Fail($"فشل حفظ صلاحيات النظام: {ex.Message}");
            }
        }

        private async Task MigrateLegacyReportPermissionsAsync()
        {
            var legacyPermissions = await _dbContext.ReportPermissions.AsNoTracking().ToListAsync();
            if (!legacyPermissions.Any())
                return;

            var existingList = await _dbContext.Set<RolePermission>()
                .AsNoTracking()
                .ToListAsync();
            var existing = existingList.ToDictionary(x => $"{x.Role}:{x.PermissionKey}", StringComparer.OrdinalIgnoreCase);

            var now = DateTime.Now;
            foreach (var legacy in legacyPermissions)
            {
                var target = PermissionCatalog.FindByLegacyReportKey(legacy.ReportKey);
                if (target == null || !string.Equals(target.Action, "View", StringComparison.OrdinalIgnoreCase))
                    continue;

                var composite = $"{legacy.Role}:{target.Key}";
                if (existing.ContainsKey(composite))
                    continue;

                await _dbContext.Set<RolePermission>().AddAsync(new RolePermission
                {
                    Role = legacy.Role,
                    PermissionKey = target.Key,
                    IsAllowed = legacy.CanView,
                    CreatedDate = now,
                    UpdatedDate = now
                });
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}

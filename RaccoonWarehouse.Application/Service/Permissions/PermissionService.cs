using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Audit;
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
        Task<bool> HasCurrentUserPermissionAsync(string permissionKey);
        Task<bool?> GetUserPermissionAsync(int userId, string permissionKey);
        Task<Result<bool>> SaveUserPermissionAsync(int userId, string permissionKey, bool isAllowed);
        Task<Dictionary<string, bool>> GetPermissionMapAsync(UserRole role, IEnumerable<string> permissionKeys);
        Task<HashSet<string>> GetDeniedPermissionKeysAsync(UserRole role, string module);
        Task<Result<bool>> SavePermissionsAsync(UserRole role, IEnumerable<RolePermissionWriteDto> permissions);
    }

    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IUserSession _userSession;
        private readonly IAuditLogService? _auditLogService;
        private bool _seedEnsured;

        public PermissionService(ApplicationDbContext dbContext, IMapper mapper, IUserSession userSession, IAuditLogService? auditLogService = null)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _userSession = userSession;
            _auditLogService = auditLogService;
        }

        private static bool DefaultAllowed(UserRole role,string key)
            => role==UserRole.Admin || ((PermissionCatalog.FindByKey(key)?.IsLegacy == true || PermissionCatalog.FindByLegacyReportKey(key) != null) && !(key.StartsWith("Payroll.",StringComparison.OrdinalIgnoreCase)
                || key is "Employees.SensitiveView" or "Employees.SensitiveEdit"));

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
                    if (definition.Module == item.Module && definition.Resource == item.Resource &&
                        definition.Action == item.Action && definition.DisplayName == item.DisplayName &&
                        definition.Description == item.Description && definition.LegacyReportKey == item.LegacyReportKey &&
                        definition.SortOrder == item.SortOrder && definition.IsActive) continue;
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
            if (_userSession.CurrentRole != UserRole.Admin || !await HasCurrentUserPermissionAsync("Permissions.View"))
                throw new UnauthorizedAccessException("ليس لديك صلاحية إدارة الأدوار. / Role management permission required.");
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

            var rows = definitions
                .GroupBy(x => new { x.Module, x.Resource, x.DisplayName })
                .Select(group => new PermissionMatrixRowDto
                {
                    Module = group.Key.Module,
                    Resource = group.Key.Resource,
                    DisplayName = group.Key.DisplayName,
                    Actions = group.ToDictionary(
                        item => item.Action,
                        item => saved.TryGetValue(item.Key, out var allowed) ? allowed : DefaultAllowed(role,item.Key))
                })
                .ToList();
            foreach (var row in rows)
                foreach (var action in row.Actions.Keys.ToArray())
                {
                    var key = row.Resource + "." + action;
                    if (saved.ContainsKey(key) || PermissionCatalog.FindByKey(key)?.IsLegacy != false) continue;
                    var aliases = PermissionCatalog.All.Where(item => item.IsLegacy &&
                        PermissionPresentationPolicy.CanonicalKey(item.Key).Equals(key, StringComparison.OrdinalIgnoreCase)).Select(item => item.Key).ToList();
                    if (key.Equals("POS.Edit", StringComparison.OrdinalIgnoreCase)) aliases.Add("POS.Create");
                    if (aliases.Count > 0) row.Actions[action] = aliases.All(alias => saved.TryGetValue(alias, out var allowed) ? allowed : DefaultAllowed(role, alias));
                }
            return rows;
        }

        public Task<bool> HasPermissionAsync(UserRole role, string permissionKey)
            => EvaluateAsync(role, permissionKey, _userSession.CurrentRole == role ? _userSession.CurrentUserId : null);

        public Task<bool> HasCurrentUserPermissionAsync(string permissionKey)
            => _userSession.CurrentUser is { } user
                ? EvaluateAsync(user.Role, permissionKey, user.Id)
                : Task.FromResult(false);

        private async Task<bool> EvaluateAsync(UserRole role, string key, int? userId)
        {
            // Fast path uses the existing session cache, including dependencies.
            if (!Enum.IsDefined(role)) return false;
            var definition = PermissionCatalog.FindByKey(key) ?? PermissionCatalog.FindByLegacyReportKey(key);
            if (definition == null) return false;
            key = definition.Key;
            var dependencies = PermissionPresentationPolicy.ViewDependencies(key);
            if (_userSession.TryGetCachedPermission($"authorization:{role}:{userId}:{key}", out var known) &&
                dependencies.All(view => _userSession.TryGetCachedPermission($"authorization:{role}:{userId}:{view}", out _)))
                return known && dependencies.All(view => _userSession.TryGetCachedPermission($"authorization:{role}:{userId}:{view}", out var allowed) && allowed);
            await EnsureSeedDataAsync();
            if (!await RawAllowedAsync(role, key, userId).ConfigureAwait(false)) return false;
            foreach (var viewKey in PermissionPresentationPolicy.ViewDependencies(key))
                if (!await RawAllowedAsync(role, viewKey, userId).ConfigureAwait(false)) return false;
            return true;
        }

        private async Task<bool> RawAllowedAsync(UserRole role, string key, int? userId)
        {
            var cacheKey = $"authorization:{role}:{userId}:{key}";
            if (_userSession.TryGetCachedPermission(cacheKey, out var cached)) return cached;
            var version = UserSession.PermissionVersion;
            bool? allowed = null;
            if (userId.HasValue)
                allowed = await _dbContext.Set<UserPermission>().AsNoTracking()
                    .Where(x => x.UserId == userId && x.PermissionKey == key)
                    .Select(x => (bool?)x.IsAllowed).FirstOrDefaultAsync().ConfigureAwait(false);
            if (allowed == null && userId.HasValue && key.Equals("PurchaseOrder.Approve", StringComparison.OrdinalIgnoreCase)
                && role is UserRole.Admin or UserRole.Casher) return false;
            allowed ??= await _dbContext.Set<RolePermission>().AsNoTracking()
                .Where(x => x.Role == role && x.PermissionKey == key)
                .Select(x => (bool?)x.IsAllowed).FirstOrDefaultAsync().ConfigureAwait(false);
            if (allowed == null && PermissionCatalog.FindByKey(key)?.IsLegacy == false)
            {
                // New presentation-only View aliases inherit only existing equivalent views.
                var aliases = PermissionCatalog.All.Where(x => x.IsLegacy &&
                    PermissionPresentationPolicy.CanonicalKey(x.Key).Equals(key, StringComparison.OrdinalIgnoreCase)).ToList();
                // POS historically used Create for saving both new and held drafts.
                if (key.Equals("POS.Edit", StringComparison.OrdinalIgnoreCase))
                    aliases.Add(PermissionCatalog.FindByKey("POS.Create")!);
                if (aliases.Count > 0)
                {
                    allowed = true;
                    foreach (var alias in aliases)
                        allowed &= await RawAllowedAsync(role, alias.Key, userId).ConfigureAwait(false);
                }
            }
            var result = allowed ?? DefaultAllowed(role, key);
            if (version != UserSession.PermissionVersion) return await RawAllowedAsync(role, key, userId).ConfigureAwait(false);
            _userSession.CachePermissions(new[] { new KeyValuePair<string, bool>(cacheKey, result) });
            return result;
        }

        public async Task<bool?> GetUserPermissionAsync(int userId, string permissionKey)
        {
            var permission = await _dbContext.Set<UserPermission>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId && x.PermissionKey == permissionKey);
            return permission?.IsAllowed;
        }

        public async Task<Result<bool>> SaveUserPermissionAsync(int userId, string permissionKey, bool isAllowed)
        {
            if (_userSession.CurrentUser?.Role != UserRole.Admin ||
                !await HasCurrentUserPermissionAsync("Permissions.ManageRoles"))
                return Result<bool>.Fail("Only an administrator can edit user permissions.");

            if (userId <= 0 || PermissionCatalog.FindByKey(permissionKey) is not { } definition)
                return Result<bool>.Fail("Invalid user or permission key / المستخدم أو الصلاحية غير صالح");
            permissionKey = definition.Key;
            await EnsureSeedDataAsync();
            var permission = await _dbContext.Set<UserPermission>()
                .FirstOrDefaultAsync(x => x.UserId == userId && x.PermissionKey == permissionKey);
            if (permission == null)
            {
                await _dbContext.Set<UserPermission>().AddAsync(new UserPermission
                {
                    UserId = userId,
                    PermissionKey = permissionKey,
                    IsAllowed = isAllowed,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                });
            }
            else
            {
                permission.IsAllowed = isAllowed;
                permission.UpdatedDate = DateTime.Now;
            }

            await _dbContext.SaveChangesAsync();
            _userSession.ClearPermissionCache();
            return Result<bool>.Ok(true, "User permission saved successfully.");
        }

        public async Task<Dictionary<string, bool>> GetPermissionMapAsync(UserRole role, IEnumerable<string> permissionKeys)
        {
            await EnsureSeedDataAsync();

            var keys = permissionKeys
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in keys) result[key] = await HasPermissionAsync(role, key);
            return result;
        }

        public async Task<HashSet<string>> GetDeniedPermissionKeysAsync(UserRole role, string module)
        {
            await EnsureSeedDataAsync();

            var moduleKeys = await _dbContext.Set<PermissionDefinition>()
                .AsNoTracking()
                .Where(x => x.Module == module)
                .Select(x => x.Key)
                .ToListAsync();

            var denied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in moduleKeys)
                if (!await HasPermissionAsync(role, key)) denied.Add(key);
            return denied;
        }

        public async Task<Result<bool>> SavePermissionsAsync(UserRole role, IEnumerable<RolePermissionWriteDto> permissions)
        {
            if (_userSession.CurrentUser?.Role != UserRole.Admin ||
                !await HasCurrentUserPermissionAsync("Permissions.ManageRoles"))
                return Result<bool>.Fail("فقط المدير يمكنه تعديل صلاحيات النظام.");

            try
            {
                await EnsureSeedDataAsync();
                var now = DateTime.Now;
                var permissionList = permissions
                    .Where(x => !string.IsNullOrWhiteSpace(x.PermissionKey))
                    .GroupBy(x => x.PermissionKey, StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.Last())
                    .ToList();
                if (!Enum.IsDefined(role) || permissionList.Any(dto => PermissionCatalog.FindByKey(dto.PermissionKey) == null))
                    return Result<bool>.Fail("Unknown permission key / مفتاح صلاحية غير معروف");
                foreach (var dto in permissionList) dto.PermissionKey = PermissionCatalog.FindByKey(dto.PermissionKey)!.Key;
                var existingPermissions = await _dbContext.Set<RolePermission>()
                    .Where(x => x.Role == role)
                    .ToListAsync();
                var existingByKey = existingPermissions.ToDictionary(
                    x => x.PermissionKey,
                    StringComparer.OrdinalIgnoreCase);

                foreach (var dto in permissionList)
                {
                    if (!existingByKey.TryGetValue(dto.PermissionKey, out var existing))
                    {
                        var entity = _mapper.Map<RolePermission>(dto);
                        entity.Role = role;
                        entity.CreatedDate = now;
                        entity.UpdatedDate = now;
                        await _dbContext.Set<RolePermission>().AddAsync(entity);
                        existingByKey[entity.PermissionKey] = entity;
                    }
                    else
                    {
                        existing.IsAllowed = dto.IsAllowed;
                        existing.UpdatedDate = now;
                    }
                }

                await _dbContext.SaveChangesAsync();
                _userSession.ClearPermissionCache();
                if (_auditLogService != null)
                    await _auditLogService.WriteAsync(new AuditEvent(
                        "PermissionChange",
                        PermissionKey: "Permissions.ManageRoles",
                        EntityType: "RolePermission",
                        EntityId: (int)role));
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

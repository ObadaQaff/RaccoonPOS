using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Data;
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
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;

        public ReportPermissionService(ApplicationDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<Dictionary<string, Dictionary<UserRole, bool>>> GetPermissionsMapAsync()
        {
            var permissions = await _dbContext.ReportPermissions.AsNoTracking().ToListAsync();

            return permissions
                .GroupBy(x => x.ReportKey)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToDictionary(item => item.Role, item => item.CanView));
        }

        public async Task<HashSet<string>> GetDeniedReportKeysAsync(UserRole role)
        {
            var deniedKeys = await _dbContext.ReportPermissions
                .AsNoTracking()
                .Where(x => x.Role == role && !x.CanView)
                .Select(x => x.ReportKey)
                .ToListAsync();

            return deniedKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public async Task<bool> CanViewAsync(UserRole role, string reportKey)
        {
            var permission = await _dbContext.ReportPermissions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Role == role && x.ReportKey == reportKey);

            return permission?.CanView ?? true;
        }

        public async Task<Result<bool>> SavePermissionsAsync(IEnumerable<ReportPermissionWriteDto> permissions)
        {
            try
            {
                var now = DateTime.Now;

                foreach (var dto in permissions)
                {
                    var existing = await _dbContext.ReportPermissions
                        .FirstOrDefaultAsync(x => x.ReportKey == dto.ReportKey && x.Role == dto.Role);

                    if (existing == null)
                    {
                        var entity = _mapper.Map<ReportPermission>(dto);
                        entity.CreatedDate = now;
                        entity.UpdatedDate = now;
                        await _dbContext.ReportPermissions.AddAsync(entity);
                    }
                    else
                    {
                        existing.CanView = dto.CanView;
                        existing.UpdatedDate = now;
                    }
                }

                await _dbContext.SaveChangesAsync();
                return Result<bool>.Ok(true, "تم حفظ صلاحيات التقارير بنجاح.");
            }
            catch (Exception ex)
            {
                return Result<bool>.Fail($"فشل حفظ صلاحيات التقارير: {ex.Message}");
            }
        }
    }
}

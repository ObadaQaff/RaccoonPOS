using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Settings;

namespace RaccoonWarehouse.Application.Service.Settings
{
    public interface IEmployeeFeatureService
    {
        Task<bool> IsEnabledAsync();
        Task<Result<bool>> SetEnabledAsync(bool enabled);
        Task EnsureDefaultsAsync();
    }

    public class EmployeeFeatureService : IEmployeeFeatureService
    {
        public const string EnableEmployeeSystemKey = "EnableEmployeeSystem";

        private readonly ApplicationDbContext _dbContext;

        public EmployeeFeatureService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task EnsureDefaultsAsync()
        {
             var setting = await _dbContext.AppSettings.FirstOrDefaultAsync(x => x.Key == EnableEmployeeSystemKey);
            if (setting != null)
                return;

            _dbContext.AppSettings.Add(new AppSetting
            {
                Key = EnableEmployeeSystemKey,
                Value = bool.FalseString,
                Description = "Enable or disable the employee business module.",
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            });

            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> IsEnabledAsync()
        {
            await EnsureDefaultsAsync();

            var value = await _dbContext.AppSettings
                .Where(x => x.Key == EnableEmployeeSystemKey)
                .Select(x => x.Value)
                .FirstOrDefaultAsync();

            return bool.TryParse(value, out var enabled) && enabled;
        }

        public async Task<Result<bool>> SetEnabledAsync(bool enabled)
        {
            await EnsureDefaultsAsync();

            var setting = await _dbContext.AppSettings.FirstAsync(x => x.Key == EnableEmployeeSystemKey);
            setting.Value = enabled.ToString();
            setting.UpdatedDate = DateTime.Now;
            await _dbContext.SaveChangesAsync();

            return Result<bool>.Ok(enabled, enabled ? "Employee system enabled." : "Employee system disabled.");
        }
    }
}

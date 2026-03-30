using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Settings;

namespace RaccoonWarehouse.Application.Service.Settings
{
    public interface IDelegateFeatureService
    {
        Task<bool> IsEnabledAsync();
        Task<Result<bool>> SetEnabledAsync(bool enabled);
        Task EnsureDefaultsAsync();
    }

    public class DelegateFeatureService : IDelegateFeatureService
    {
        public const string EnableDelegateSystemKey = "EnableDelegateSystem";

        private readonly ApplicationDbContext _dbContext;

        public DelegateFeatureService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task EnsureDefaultsAsync()
        {
            var setting = await _dbContext.AppSettings
                .FirstOrDefaultAsync(x => x.Key == EnableDelegateSystemKey);

            if (setting != null)
                return;

            _dbContext.AppSettings.Add(new AppSetting
            {
                Key = EnableDelegateSystemKey,
                Value = bool.FalseString,
                Description = "Enable or disable the delegate business module.",
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            });

            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> IsEnabledAsync()
        {
            await EnsureDefaultsAsync();

            var value = await _dbContext.AppSettings
                .Where(x => x.Key == EnableDelegateSystemKey)
                .Select(x => x.Value)
                .FirstOrDefaultAsync();

            return bool.TryParse(value, out var enabled) && enabled;
        }

        public async Task<Result<bool>> SetEnabledAsync(bool enabled)
        {
            await EnsureDefaultsAsync();

            var setting = await _dbContext.AppSettings
                .FirstAsync(x => x.Key == EnableDelegateSystemKey);

            setting.Value = enabled.ToString();
            setting.UpdatedDate = DateTime.Now;

            await _dbContext.SaveChangesAsync();
            return Result<bool>.Ok(enabled, enabled ? "Delegate system enabled." : "Delegate system disabled.");
        }
    }
}

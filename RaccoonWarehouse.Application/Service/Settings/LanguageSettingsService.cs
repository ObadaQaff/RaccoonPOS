using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Settings;

namespace RaccoonWarehouse.Application.Service.Settings
{
    public enum AppLanguage
    {
        Arabic = 0,
        English = 1
    }

    public interface ILanguageSettingsService
    {
        Task EnsureDefaultsAsync();
        Task<AppLanguage> GetCurrentLanguageAsync();
        Task<Result<AppLanguage>> SetCurrentLanguageAsync(AppLanguage language);
    }

    public class LanguageSettingsService : ILanguageSettingsService
    {
        public const string ApplicationLanguageKey = "ApplicationLanguage";

        private readonly ApplicationDbContext _dbContext;

        public LanguageSettingsService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task EnsureDefaultsAsync()
        {
            var setting = await _dbContext.AppSettings
                .FirstOrDefaultAsync(x => x.Key == ApplicationLanguageKey);

            if (setting != null)
                return;

            _dbContext.AppSettings.Add(new AppSetting
            {
                Key = ApplicationLanguageKey,
                Value = AppLanguage.Arabic.ToString(),
                Description = "Controls the current application UI language.",
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            });

            await _dbContext.SaveChangesAsync();
        }

        public async Task<AppLanguage> GetCurrentLanguageAsync()
        {
            await EnsureDefaultsAsync();

            var value = await _dbContext.AppSettings
                .Where(x => x.Key == ApplicationLanguageKey)
                .Select(x => x.Value)
                .FirstOrDefaultAsync();

            return Enum.TryParse<AppLanguage>(value, ignoreCase: true, out var language)
                ? language
                : AppLanguage.Arabic;
        }

        public async Task<Result<AppLanguage>> SetCurrentLanguageAsync(AppLanguage language)
        {
            await EnsureDefaultsAsync();

            var setting = await _dbContext.AppSettings
                .FirstAsync(x => x.Key == ApplicationLanguageKey);

            setting.Value = language.ToString();
            setting.UpdatedDate = DateTime.Now;

            await _dbContext.SaveChangesAsync();

            var message = language == AppLanguage.Arabic
                ? "تم حفظ اللغة العربية."
                : "English language saved.";

            return Result<AppLanguage>.Ok(language, message);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Core.Modules;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Settings;

namespace RaccoonWarehouse.Application.Service.Modules;

public sealed class ModuleStateStore : IModuleStateStore
{
    public const string EmployeesModuleKey = "Employees";
    public const string HrModuleKey = "HR";
    private const string EmployeesSettingKey = "EnableEmployeesModule";
    private const string HrSettingKey = "EnableHrModule";
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly object _gate = new();
    private Dictionary<string, bool> _snapshot = new(StringComparer.OrdinalIgnoreCase)
    {
        [EmployeesModuleKey] = true,
        [HrModuleKey] = false
    };

    public ModuleStateStore(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public IReadOnlyDictionary<string, bool> Snapshot
    {
        get { lock (_gate) return new Dictionary<string, bool>(_snapshot, StringComparer.OrdinalIgnoreCase); }
    }

    public bool IsEnabled(string moduleKey)
    {
        lock (_gate)
            return !_snapshot.TryGetValue(moduleKey, out var enabled) || enabled;
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var settings = await db.AppSettings.AsNoTracking()
            .Where(x => x.Key == EmployeesSettingKey || x.Key == HrSettingKey)
            .ToDictionaryAsync(x => x.Key, StringComparer.OrdinalIgnoreCase, cancellationToken);
        settings.TryGetValue(EmployeesSettingKey, out var setting);

        if (setting == null)
        {
            setting = new AppSetting
            {
                Key = EmployeesSettingKey,
                Value = bool.TrueString,
                Description = "Enable or disable the Employees module.",
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };
            db.AppSettings.Add(setting);
            await db.SaveChangesAsync(cancellationToken);
            settings[EmployeesSettingKey] = setting;
        }

        if (!settings.TryGetValue(HrSettingKey, out var hrSetting))
        {
            hrSetting = new AppSetting { Key = HrSettingKey, Value = bool.FalseString, Description = "Enable or disable the HR module.", CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now };
            db.AppSettings.Add(hrSetting);
            await db.SaveChangesAsync(cancellationToken);
        }

        lock (_gate)
        {
            _snapshot[EmployeesModuleKey] = !bool.TryParse(setting.Value, out var enabled) || enabled;
            _snapshot[HrModuleKey] = bool.TryParse(hrSetting.Value, out var hrEnabled) && hrEnabled;
        }
    }

    public async Task<bool> SetEnabledAsync(string moduleKey, bool enabled, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(moduleKey, EmployeesModuleKey, StringComparison.OrdinalIgnoreCase) && !string.Equals(moduleKey, HrModuleKey, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Unknown module key.", nameof(moduleKey));
        if (string.Equals(moduleKey, HrModuleKey, StringComparison.OrdinalIgnoreCase) && enabled && !IsEnabled(EmployeesModuleKey))
            throw new InvalidOperationException("HR cannot be enabled while Employees is disabled.");
        if (string.Equals(moduleKey, EmployeesModuleKey, StringComparison.OrdinalIgnoreCase) && !enabled && IsEnabled(HrModuleKey))
            throw new InvalidOperationException("Disable HR before disabling Employees.");

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var settingKey = string.Equals(moduleKey, HrModuleKey, StringComparison.OrdinalIgnoreCase) ? HrSettingKey : EmployeesSettingKey;
        var setting = await db.AppSettings.SingleOrDefaultAsync(x => x.Key == settingKey, cancellationToken);
        if (setting == null)
        {
            setting = new AppSetting { Key = settingKey, Description = string.Equals(moduleKey, HrModuleKey, StringComparison.OrdinalIgnoreCase) ? "Enable or disable the HR module." : "Enable or disable the Employees module.", CreatedDate = DateTime.Now };
            db.AppSettings.Add(setting);
        }

        setting.Value = enabled.ToString();
        setting.UpdatedDate = DateTime.Now;
        await db.SaveChangesAsync(cancellationToken);

        lock (_gate)
        {
            _snapshot[EmployeesModuleKey] = enabled;
            if (string.Equals(moduleKey, HrModuleKey, StringComparison.OrdinalIgnoreCase)) _snapshot[HrModuleKey] = enabled;
        }
        return enabled;
    }
}

using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Settings;

namespace RaccoonWarehouse.Application.Service.Settings;

public interface IInventoryCostingSettingsService
{
    Task EnsureDefaultsAsync();
    Task<InventoryCostingMethod> GetCurrentMethodAsync();
    Task<Result<InventoryCostingMethod>> SetCurrentMethodAsync(InventoryCostingMethod method);
}

public sealed class InventoryCostingSettingsService : IInventoryCostingSettingsService
{
    public const string InventoryCostingMethodKey = "InventoryCostingMethod";

    private readonly ApplicationDbContext _dbContext;

    public InventoryCostingSettingsService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task EnsureDefaultsAsync()
    {
        var setting = await _dbContext.AppSettings
            .FirstOrDefaultAsync(x => x.Key == InventoryCostingMethodKey);

        if (setting != null)
            return;

        _dbContext.AppSettings.Add(new AppSetting
        {
            Key = InventoryCostingMethodKey,
            Value = InventoryCostingMethod.WeightedAverage.ToString(),
            Description = "Controls how future inventory issues are costed.",
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        });

        await _dbContext.SaveChangesAsync();
    }

    public async Task<InventoryCostingMethod> GetCurrentMethodAsync()
    {
        await EnsureDefaultsAsync();

        var value = await _dbContext.AppSettings
            .Where(x => x.Key == InventoryCostingMethodKey)
            .Select(x => x.Value)
            .FirstOrDefaultAsync();

        return Enum.TryParse<InventoryCostingMethod>(value, true, out var method)
            && Enum.IsDefined(method)
            ? method
            : InventoryCostingMethod.WeightedAverage;
    }

    public async Task<Result<InventoryCostingMethod>> SetCurrentMethodAsync(InventoryCostingMethod method)
    {
        if (!Enum.IsDefined(method))
            return Result<InventoryCostingMethod>.Fail("The selected inventory costing method is not supported.");

        await EnsureDefaultsAsync();

        var setting = await _dbContext.AppSettings
            .FirstAsync(x => x.Key == InventoryCostingMethodKey);

        setting.Value = method.ToString();
        setting.UpdatedDate = DateTime.Now;

        await _dbContext.SaveChangesAsync();

        return Result<InventoryCostingMethod>.Ok(method, "Inventory costing method saved.");
    }
}

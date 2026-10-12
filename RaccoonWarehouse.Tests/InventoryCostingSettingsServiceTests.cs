using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using Xunit;

namespace RaccoonWarehouse.Tests;

public sealed class InventoryCostingSettingsServiceTests
{
    [Fact]
    public async Task GetCurrentMethod_WhenSettingDoesNotExist_ReturnsWeightedAverageAndCreatesDefault()
    {
        await using var context = CreateContext(nameof(GetCurrentMethod_WhenSettingDoesNotExist_ReturnsWeightedAverageAndCreatesDefault));
        var service = new InventoryCostingSettingsService(context);

        var method = await service.GetCurrentMethodAsync();
        var setting = await context.AppSettings.SingleAsync(x => x.Key == InventoryCostingSettingsService.InventoryCostingMethodKey);

        Assert.Equal(InventoryCostingMethod.WeightedAverage, method);
        Assert.Equal(nameof(InventoryCostingMethod.WeightedAverage), setting.Value);
    }

    [Fact]
    public async Task SetCurrentMethod_PersistsSelectedMethod()
    {
        await using var context = CreateContext(nameof(SetCurrentMethod_PersistsSelectedMethod));
        var service = new InventoryCostingSettingsService(context);

        var result = await service.SetCurrentMethodAsync(InventoryCostingMethod.FIFO);

        Assert.True(result.Success);
        Assert.Equal(InventoryCostingMethod.FIFO, await service.GetCurrentMethodAsync());
    }

    [Fact]
    public async Task GetCurrentMethod_WhenStoredValueIsInvalid_FallsBackToWeightedAverage()
    {
        await using var context = CreateContext(nameof(GetCurrentMethod_WhenStoredValueIsInvalid_FallsBackToWeightedAverage));
        context.AppSettings.Add(new RaccoonWarehouse.Domain.Settings.AppSetting
        {
            Key = InventoryCostingSettingsService.InventoryCostingMethodKey,
            Value = "Unsupported",
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        });
        await context.SaveChangesAsync();

        var method = await new InventoryCostingSettingsService(context).GetCurrentMethodAsync();

        Assert.Equal(InventoryCostingMethod.WeightedAverage, method);
    }

    private static ApplicationDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ApplicationDbContext(options);
    }
}

using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Helper;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Data.Repository;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Stock;
using Xunit;

namespace RaccoonWarehouse.Tests;

public class StockServiceStockOutRulesTests
{
    private static StockService CreateService(string databaseName, out ApplicationDbContext context)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        context = new ApplicationDbContext(options);
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        var uow = new UOW(context, mapper);
        return new StockService(context, uow, mapper);
    }

    [Fact]
    public async Task PostMovements_NegativeQuantity_WithNoExistingStock_ShouldFail()
    {
        var service = CreateService(nameof(PostMovements_NegativeQuantity_WithNoExistingStock_ShouldFail), out _);

        var result = await service.PostMovementsAsync(new[]
        {
            new StockMovementPostDto
            {
                ProductId = 1001,
                ProductUnitId = 2001,
                Quantity = -2,
                QuantityPerUnitSnapshot = 1,
                BaseQuantity = -2,
                UnitPrice = 10,
                TransactionType = TransactionType.Adjustment,
                TransactionDate = DateTime.Now,
                Notes = "Stock out"
            }
        });

        Assert.False(result.Success);
        Assert.Contains("not available", result.Message ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostMovements_NegativeQuantity_MoreThanAvailable_ShouldFail()
    {
        var service = CreateService(nameof(PostMovements_NegativeQuantity_MoreThanAvailable_ShouldFail), out var context);

        context.Set<Stock>().Add(new Stock
        {
            ProductId = 1002,
            ProductUnitId = 2002,
            Quantity = 3,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        });
        await context.SaveChangesAsync();

        var result = await service.PostMovementsAsync(new[]
        {
            new StockMovementPostDto
            {
                ProductId = 1002,
                ProductUnitId = 2002,
                Quantity = -5,
                QuantityPerUnitSnapshot = 1,
                BaseQuantity = -5,
                UnitPrice = 10,
                TransactionType = TransactionType.Adjustment,
                TransactionDate = DateTime.Now,
                Notes = "Stock out"
            }
        });

        Assert.False(result.Success);
        Assert.Contains("insufficient", result.Message ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostMovements_NegativeQuantity_WithinAvailable_ShouldDecreaseStock()
    {
        var service = CreateService(nameof(PostMovements_NegativeQuantity_WithinAvailable_ShouldDecreaseStock), out var context);

        context.Set<Stock>().Add(new Stock
        {
            ProductId = 1003,
            ProductUnitId = 2003,
            Quantity = 7,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        });
        await context.SaveChangesAsync();

        var result = await service.PostMovementsAsync(new[]
        {
            new StockMovementPostDto
            {
                ProductId = 1003,
                ProductUnitId = 2003,
                Quantity = -4,
                QuantityPerUnitSnapshot = 1,
                BaseQuantity = -4,
                UnitPrice = 12,
                TransactionType = TransactionType.Adjustment,
                TransactionDate = DateTime.Now,
                Notes = "Stock out"
            }
        });

        var stock = await context.Set<Stock>()
            .FirstAsync(s => s.ProductId == 1003 && s.ProductUnitId == 2003);

        Assert.True(result.Success);
        Assert.Equal(3, stock.Quantity);
    }
}

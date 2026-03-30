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
        var service = CreateService(nameof(PostMovements_NegativeQuantity_MoreThanAvailable_ShouldFail), out _);

        await service.PostMovementsAsync(new[]
        {
            new StockMovementPostDto
            {
                ProductId = 1002,
                ProductUnitId = 2002,
                Quantity = 3,
                QuantityPerUnitSnapshot = 1,
                BaseQuantity = 3,
                UnitPrice = 10,
                PurchasePrice = 8,
                SalePrice = 10,
                TransactionType = TransactionType.Purchase,
                TransactionDate = DateTime.Now,
                Notes = "Seed stock"
            }
        });

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

        await service.PostMovementsAsync(new[]
        {
            new StockMovementPostDto
            {
                ProductId = 1003,
                ProductUnitId = 2003,
                Quantity = 7,
                QuantityPerUnitSnapshot = 1,
                BaseQuantity = 7,
                UnitPrice = 12,
                PurchasePrice = 9,
                SalePrice = 12,
                TransactionType = TransactionType.Purchase,
                TransactionDate = DateTime.Now,
                Notes = "Seed stock"
            }
        });

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

        Assert.True(result.Success, result.Message);
        Assert.Equal(3, stock.Quantity);
    }

    [Fact]
    public async Task PostMovements_PositiveQuantity_ShouldRefreshCurrentStockPrices()
    {
        var service = CreateService(nameof(PostMovements_PositiveQuantity_ShouldRefreshCurrentStockPrices), out var context);

        await service.PostMovementsAsync(new[]
        {
            new StockMovementPostDto
            {
                ProductId = 1004,
                ProductUnitId = 2004,
                Quantity = 2,
                QuantityPerUnitSnapshot = 1,
                BaseQuantity = 2,
                UnitPrice = 7,
                PurchasePrice = 5,
                SalePrice = 7,
                TransactionType = TransactionType.Purchase,
                TransactionDate = DateTime.Now,
                Notes = "Seed stock"
            }
        });

        var result = await service.PostMovementsAsync(new[]
        {
            new StockMovementPostDto
            {
                ProductId = 1004,
                ProductUnitId = 2004,
                Quantity = 3,
                QuantityPerUnitSnapshot = 1,
                BaseQuantity = 3,
                UnitPrice = 6,
                PurchasePrice = 6,
                SalePrice = 9,
                TransactionType = TransactionType.Purchase,
                TransactionDate = DateTime.Now,
                Notes = "Stock in"
            }
        });

        var stock = await context.Set<Stock>()
            .FirstAsync(s => s.ProductId == 1004 && s.ProductUnitId == 2004);

        Assert.True(result.Success);
        Assert.Equal(5, stock.Quantity);
        Assert.Equal(6, stock.PurchasePrice);
        Assert.Equal(9, stock.SalePrice);
    }

    [Fact]
    public async Task AllocateOutgoingAsync_ShouldIgnoreExpiredLots()
    {
        var service = CreateService(nameof(AllocateOutgoingAsync_ShouldIgnoreExpiredLots), out var context);

        await service.PostMovementsAsync(new[]
        {
            new StockMovementPostDto
            {
                ProductId = 1011,
                ProductUnitId = 2011,
                Quantity = 4,
                QuantityPerUnitSnapshot = 1,
                BaseQuantity = 4,
                UnitPrice = 10,
                PurchasePrice = 6,
                SalePrice = 10,
                ExpiryDate = DateTime.Today.AddDays(-1),
                TransactionType = TransactionType.Purchase,
                TransactionDate = DateTime.Now,
                Notes = "Expired batch"
            },
            new StockMovementPostDto
            {
                ProductId = 1011,
                ProductUnitId = 2011,
                Quantity = 6,
                QuantityPerUnitSnapshot = 1,
                BaseQuantity = 6,
                UnitPrice = 14,
                PurchasePrice = 9,
                SalePrice = 14,
                ExpiryDate = DateTime.Today.AddDays(3),
                TransactionType = TransactionType.Purchase,
                TransactionDate = DateTime.Now,
                Notes = "Usable batch"
            }
        });

        var stock = await context.Set<Stock>()
            .FirstAsync(s => s.ProductId == 1011 && s.ProductUnitId == 2011);

        Assert.Equal(6, stock.Quantity);
        Assert.Equal(9, stock.PurchasePrice);
        Assert.Equal(14, stock.SalePrice);

        var allocationResult = await service.AllocateOutgoingAsync(new[]
        {
            new StockAllocationRequestDto
            {
                ProductId = 1011,
                ProductUnitId = 2011,
                Quantity = 2
            }
        });

        Assert.True(allocationResult.Success);
        Assert.Single(allocationResult.Data!);
        Assert.Equal(DateTime.Today.AddDays(3), allocationResult.Data![0].ExpiryDate);
        Assert.Equal(14, allocationResult.Data[0].SalePrice);
    }

    [Fact]
    public async Task AllocateOutgoingAsync_ShouldUseSoonestExpiryFirst()
    {
        var service = CreateService(nameof(AllocateOutgoingAsync_ShouldUseSoonestExpiryFirst), out _);

        await service.PostMovementsAsync(new[]
        {
            new StockMovementPostDto
            {
                ProductId = 1010,
                ProductUnitId = 2010,
                Quantity = 2,
                QuantityPerUnitSnapshot = 1,
                BaseQuantity = 2,
                UnitPrice = 11,
                PurchasePrice = 7,
                SalePrice = 11,
                ExpiryDate = new DateTime(2026, 4, 1),
                TransactionType = TransactionType.Purchase,
                TransactionDate = DateTime.Now,
                Notes = "Batch A"
            },
            new StockMovementPostDto
            {
                ProductId = 1010,
                ProductUnitId = 2010,
                Quantity = 5,
                QuantityPerUnitSnapshot = 1,
                BaseQuantity = 5,
                UnitPrice = 13,
                PurchasePrice = 8,
                SalePrice = 13,
                ExpiryDate = new DateTime(2026, 5, 1),
                TransactionType = TransactionType.Purchase,
                TransactionDate = DateTime.Now,
                Notes = "Batch B"
            }
        });

        var allocationResult = await service.AllocateOutgoingAsync(new[]
        {
            new StockAllocationRequestDto
            {
                ProductId = 1010,
                ProductUnitId = 2010,
                Quantity = 3
            }
        });

        Assert.True(allocationResult.Success);
        Assert.NotNull(allocationResult.Data);
        Assert.Equal(2, allocationResult.Data.Count);
        Assert.Equal(2, allocationResult.Data[0].Quantity);
        Assert.Equal(new DateTime(2026, 4, 1), allocationResult.Data[0].ExpiryDate);
        Assert.Equal(11, allocationResult.Data[0].SalePrice);
        Assert.Equal(1, allocationResult.Data[1].Quantity);
        Assert.Equal(new DateTime(2026, 5, 1), allocationResult.Data[1].ExpiryDate);
        Assert.Equal(13, allocationResult.Data[1].SalePrice);
    }

    [Fact]
    public async Task UpdateAsync_ShouldBlockDirectStockSummaryEdits()
    {
        var service = CreateService(nameof(UpdateAsync_ShouldBlockDirectStockSummaryEdits), out _);

        var result = await service.UpdateAsync(new Domain.Stock.DTOs.StockWriteDto
        {
            Id = 1,
            ProductId = 1,
            ProductUnitId = 1,
            Quantity = 99,
            PurchasePrice = 5,
            SalePrice = 10,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        });

        Assert.False(result.Success);
        Assert.Contains("blocked", result.Message ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}

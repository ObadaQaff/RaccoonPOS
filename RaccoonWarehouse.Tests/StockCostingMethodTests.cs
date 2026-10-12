using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Helper;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Data.Repository;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.InvoiceLines;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.StockLots;
using RaccoonWarehouse.Domain.Units;
using Xunit;

namespace RaccoonWarehouse.Tests;

public sealed class StockCostingMethodTests
{
    [Fact]
    public async Task AllocateOutgoing_FIFO_UsesOldestLotsAndLotCosts()
    {
        await using var context = CreateContext(nameof(AllocateOutgoing_FIFO_UsesOldestLotsAndLotCosts));
        const int productId = 12001;
        const int productUnitId = 12002;
        SeedProduct(context, productId, productUnitId);

        context.Set<StockLot>().AddRange(
            CreateLot(productId, productUnitId, 2m, 5m, DateTime.Today.AddDays(-10), DateTime.Today.AddDays(100)),
            CreateLot(productId, productUnitId, 2m, 9m, DateTime.Today.AddDays(-2), DateTime.Today.AddDays(10)));
        await context.SaveChangesAsync();

        var settings = new InventoryCostingSettingsService(context);
        await settings.SetCurrentMethodAsync(InventoryCostingMethod.FIFO);
        var service = CreateStockService(context, settings);

        var result = await service.AllocateOutgoingAsync(new[]
        {
            new StockAllocationRequestDto
            {
                ProductId = productId,
                ProductUnitId = productUnitId,
                Quantity = 3m
            }
        });

        Assert.True(result.Success, result.Message);
        Assert.Equal(2, result.Data!.Count);
        Assert.Equal(5m, result.Data[0].PurchasePrice);
        Assert.Equal(9m, result.Data[1].PurchasePrice);
        Assert.Equal(2m, result.Data[0].Quantity);
        Assert.Equal(1m, result.Data[1].Quantity);
    }

    [Fact]
    public async Task AllocateOutgoing_FEFO_UsesEarliestExpiryBeforeCreationDate()
    {
        await using var context = CreateContext(nameof(AllocateOutgoing_FEFO_UsesEarliestExpiryBeforeCreationDate));
        const int productId = 12101;
        const int productUnitId = 12102;
        SeedProduct(context, productId, productUnitId);

        context.Set<StockLot>().AddRange(
            CreateLot(productId, productUnitId, 2m, 5m, DateTime.Today.AddDays(-10), DateTime.Today.AddDays(80)),
            CreateLot(productId, productUnitId, 2m, 9m, DateTime.Today.AddDays(-2), DateTime.Today.AddDays(10)));
        await context.SaveChangesAsync();

        var settings = new InventoryCostingSettingsService(context);
        await settings.SetCurrentMethodAsync(InventoryCostingMethod.FEFO);
        var service = CreateStockService(context, settings);

        var result = await service.AllocateOutgoingAsync(new[]
        {
            new StockAllocationRequestDto
            {
                ProductId = productId,
                ProductUnitId = productUnitId,
                Quantity = 1m
            }
        });

        Assert.True(result.Success, result.Message);
        var allocation = Assert.Single(result.Data!);
        Assert.Equal(9m, allocation.PurchasePrice);
    }

    [Fact]
    public async Task PostMovements_FIFO_ConsumesOldestLotFirst()
    {
        await using var context = CreateContext(nameof(PostMovements_FIFO_ConsumesOldestLotFirst));
        const int productId = 12201;
        const int productUnitId = 12202;
        SeedProduct(context, productId, productUnitId);

        var oldestLot = CreateLot(productId, productUnitId, 2m, 5m, DateTime.Today.AddDays(-10), DateTime.Today.AddDays(100));
        var newestLot = CreateLot(productId, productUnitId, 2m, 9m, DateTime.Today.AddDays(-2), DateTime.Today.AddDays(10));
        context.Set<StockLot>().AddRange(oldestLot, newestLot);
        await context.SaveChangesAsync();

        var settings = new InventoryCostingSettingsService(context);
        await settings.SetCurrentMethodAsync(InventoryCostingMethod.FIFO);
        var service = CreateStockService(context, settings);

        var result = await service.PostMovementsAsync(new[]
        {
            new StockMovementPostDto
            {
                ProductId = productId,
                ProductUnitId = productUnitId,
                Quantity = -3m,
                QuantityPerUnitSnapshot = 1m,
                BaseQuantity = -3m,
                UnitPrice = 12m,
                TransactionType = TransactionType.Sale,
                TransactionDate = DateTime.Now
            }
        });

        var savedOldestLot = await context.Set<StockLot>().SingleAsync(lot => lot.Id == oldestLot.Id);
        var savedNewestLot = await context.Set<StockLot>().SingleAsync(lot => lot.Id == newestLot.Id);

        Assert.True(result.Success, result.Message);
        Assert.Equal(0m, savedOldestLot.RemainingBaseQuantity);
        Assert.Equal(1m, savedNewestLot.RemainingBaseQuantity);
        Assert.Equal(2, await context.Set<RaccoonWarehouse.Domain.StockTransactions.StockTransaction>()
            .Where(transaction => transaction.BaseQuantity < 0)
            .CountAsync());
    }

    [Fact]
    public async Task ChangingCostingMethod_DoesNotRewriteHistoricalInvoiceCost()
    {
        await using var context = CreateContext(nameof(ChangingCostingMethod_DoesNotRewriteHistoricalInvoiceCost));
        context.Set<Invoice>().Add(new Invoice
        {
            Id = 12301,
            InvoiceNumber = "12301",
            InvoiceType = InvoiceType.Sale,
            CreatedDate = DateTime.Today.AddDays(-30),
            UpdatedDate = DateTime.Today.AddDays(-30),
            InvoiceLines = new List<InvoiceLine>
            {
                new()
                {
                    ProductId = 1,
                    ProductUnitId = 2,
                    Quantity = 1m,
                    UnitPrice = 20m,
                    UnitCost = 7m,
                    QuantityPerUnitSnapshot = 1m,
                    BaseQuantity = 1m,
                    ExpiryDate = DateTime.Today.AddYears(1),
                    CreatedDate = DateTime.Today.AddDays(-30),
                    UpdatedDate = DateTime.Today.AddDays(-30)
                }
            }
        });
        await context.SaveChangesAsync();

        var settings = new InventoryCostingSettingsService(context);
        await settings.SetCurrentMethodAsync(InventoryCostingMethod.FIFO);
        await settings.SetCurrentMethodAsync(InventoryCostingMethod.FEFO);

        var line = await context.Set<InvoiceLine>().SingleAsync();
        Assert.Equal(7m, line.UnitCost);
    }

    private static StockService CreateStockService(ApplicationDbContext context, InventoryCostingSettingsService settings)
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        var uow = new UOW(context, mapper);
        return new StockService(context, uow, mapper, null, settings);
    }

    private static void SeedProduct(ApplicationDbContext context, int productId, int productUnitId)
    {
        context.Set<Product>().Add(new Product
        {
            Id = productId,
            Name = "Costing test product",
            ITEMCODE = productId,
            SubCategoryId = 1,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        });
        context.Set<Unit>().Add(new Unit
        {
            Id = productUnitId + 100,
            Name = "Piece",
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        });
        context.Set<ProductUnit>().Add(new ProductUnit
        {
            Id = productUnitId,
            ProductId = productId,
            UnitId = productUnitId + 100,
            QuantityPerUnit = 1m,
            PurchasePrice = 5m,
            SalePrice = 12m,
            IsBaseUnit = true,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        });
    }

    private static StockLot CreateLot(
        int productId,
        int productUnitId,
        decimal quantity,
        decimal cost,
        DateTime createdDate,
        DateTime expiryDate)
    {
        return new StockLot
        {
            ProductId = productId,
            ProductUnitId = productUnitId,
            Quantity = quantity,
            RemainingQuantity = quantity,
            QuantityPerUnitSnapshot = 1m,
            BaseQuantity = quantity,
            RemainingBaseQuantity = quantity,
            PurchasePrice = cost,
            SalePrice = 12m,
            ExpiryDate = expiryDate,
            Status = BatchStatus.Active,
            CreatedDate = createdDate,
            UpdatedDate = createdDate
        };
    }

    private static ApplicationDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ApplicationDbContext(options);
    }
}

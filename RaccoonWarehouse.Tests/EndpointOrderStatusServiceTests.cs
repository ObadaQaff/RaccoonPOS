using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Helper;
using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Application.Service.Orders;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Data.Repository;
using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Accounting.JournalEntries;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.InvoiceLines;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Orders.DTOs;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.Stock;
using RaccoonWarehouse.Domain.StockTransactions;
using RaccoonWarehouse.Domain.Units;
using Xunit;

namespace RaccoonWarehouse.Tests;

public class EndpointOrderStatusServiceTests
{
    [Fact]
    public async Task Unknown_ShouldDeductStockOnlyOnce()
    {
        var fixture = await CreateFixtureAsync(nameof(Unknown_ShouldDeductStockOnlyOnce), 10m, 3m);

        var first = await fixture.Service.ApplyStatusAsync(fixture.InvoiceId, InvoiceStatus.Unknown);
        var second = await fixture.Service.ApplyStatusAsync(fixture.InvoiceId, InvoiceStatus.Unknown);

        var stock = await fixture.Context.Set<Stock>()
            .SingleAsync(item => item.ProductId == fixture.ProductId);
        var movements = await fixture.Context.Set<StockTransaction>()
            .Where(item => item.InvoiceId == fixture.InvoiceId)
            .ToListAsync();

        Assert.True(first.Success, first.Message);
        Assert.True(second.Success, second.Message);
        Assert.Equal(7m, stock.Quantity);
        Assert.Equal(-3m, movements.Sum(item => item.Quantity));
    }

    [Fact]
    public async Task Completed_ShouldPostAccountingWithoutDeductingStockAgain()
    {
        var fixture = await CreateFixtureAsync(
            nameof(Completed_ShouldPostAccountingWithoutDeductingStockAgain),
            10m,
            3m,
            seedAccounts: true);

        await fixture.Service.ApplyStatusAsync(fixture.InvoiceId, InvoiceStatus.Unknown);
        await fixture.Service.ApplyStatusAsync(fixture.InvoiceId, InvoiceStatus.InProcess);
        var first = await fixture.Service.ApplyStatusAsync(fixture.InvoiceId, InvoiceStatus.Completed);
        var second = await fixture.Service.ApplyStatusAsync(fixture.InvoiceId, InvoiceStatus.Completed);

        var stock = await fixture.Context.Set<Stock>()
            .SingleAsync(item => item.ProductId == fixture.ProductId);
        var journals = await fixture.Context.Set<JournalEntry>()
            .Where(item =>
                item.ReferenceType == "Invoice" &&
                item.ReferenceId == fixture.InvoiceId &&
                item.Status == JournalEntryStatus.Posted)
            .ToListAsync();

        Assert.True(first.Success, first.Message);
        Assert.True(second.Success, second.Message);
        Assert.Equal(7m, stock.Quantity);
        Assert.Single(journals);
    }

    [Fact]
    public async Task Cancelled_ShouldRestoreHeldStockOnlyOnce()
    {
        var fixture = await CreateFixtureAsync(nameof(Cancelled_ShouldRestoreHeldStockOnlyOnce), 10m, 3m);

        await fixture.Service.ApplyStatusAsync(fixture.InvoiceId, InvoiceStatus.Unknown);
        await fixture.Service.ApplyStatusAsync(fixture.InvoiceId, InvoiceStatus.InProcess);
        var first = await fixture.Service.ApplyStatusAsync(fixture.InvoiceId, InvoiceStatus.Cancelled);
        var second = await fixture.Service.ApplyStatusAsync(fixture.InvoiceId, InvoiceStatus.Cancelled);

        var stock = await fixture.Context.Set<Stock>()
            .SingleAsync(item => item.ProductId == fixture.ProductId);
        var movementTotal = await fixture.Context.Set<StockTransaction>()
            .Where(item => item.InvoiceId == fixture.InvoiceId)
            .SumAsync(item => item.Quantity);

        Assert.True(first.Success, first.Message);
        Assert.True(second.Success, second.Message);
        Assert.Equal(10m, stock.Quantity);
        Assert.Equal(0m, movementTotal);
    }

    [Fact]
    public async Task Unknown_WithInsufficientStock_ShouldFailWithoutDeduction()
    {
        var fixture = await CreateFixtureAsync(
            nameof(Unknown_WithInsufficientStock_ShouldFailWithoutDeduction),
            2m,
            3m);

        var result = await fixture.Service.ApplyStatusAsync(fixture.InvoiceId, InvoiceStatus.Unknown);

        var stock = await fixture.Context.Set<Stock>()
            .SingleAsync(item => item.ProductId == fixture.ProductId);
        var hasInvoiceMovement = await fixture.Context.Set<StockTransaction>()
            .AnyAsync(item => item.InvoiceId == fixture.InvoiceId);

        Assert.False(result.Success);
        Assert.Equal(2m, stock.Quantity);
        Assert.False(hasInvoiceMovement);
        Assert.Contains("Product: Endpoint test product", result.Message);
        Assert.Contains("Barcode: 4009900456630", result.Message);
        Assert.Contains("Unit: Piece", result.Message);
        Assert.Contains("Requested quantity: 3", result.Message);
        Assert.Contains("Available quantity: 2", result.Message);
        Assert.Contains("Missing quantity: 1", result.Message);
    }

    [Fact]
    public async Task UpdateDetailsAsync_ShouldReplaceLocalLinesAndReservationWithoutUpdatingBox()
    {
        var fixture = await CreateFixtureAsync(
            nameof(UpdateDetailsAsync_ShouldReplaceLocalLinesAndReservationWithoutUpdatingBox),
            10m,
            3m);
        await fixture.Service.ApplyStatusAsync(
            fixture.InvoiceId,
            InvoiceStatus.Unknown,
            synchronizeBox: false);
        var result = await fixture.Service.UpdateDetailsAsync(new EndpointOrderEditDto
        {
            InvoiceId = fixture.InvoiceId,
            Lines =
            [
                new EndpointOrderLocalLineDto
                {
                    ProductId = fixture.ProductId,
                    ProductUnitId = fixture.ProductUnitId,
                    Quantity = 5m,
                    UnitPrice = 12m
                }
            ]
        });

        var stock = await fixture.Context.Set<Stock>()
            .SingleAsync(item => item.ProductId == fixture.ProductId);
        var invoice = await fixture.Context.Set<Invoice>()
            .Include(item => item.InvoiceLines)
            .SingleAsync(item => item.Id == fixture.InvoiceId);

        Assert.True(result.Success, result.Message);
        Assert.Equal(5m, stock.Quantity);
        Assert.Equal(5m, invoice.InvoiceLines!.Single().Quantity);
        Assert.Equal(12m, invoice.InvoiceLines.Single().UnitPrice);
        Assert.Equal(60m, invoice.TotalAmount);
        Assert.Equal(0, fixture.BoxApi.UpdateItemsCalls);
    }

    [Fact]
    public async Task UpdateDetailsAsync_WithUnchangedLines_ShouldNotCreateStockMovements()
    {
        var fixture = await CreateFixtureAsync(
            nameof(UpdateDetailsAsync_WithUnchangedLines_ShouldNotCreateStockMovements),
            10m,
            3m);
        await fixture.Service.ApplyStatusAsync(
            fixture.InvoiceId,
            InvoiceStatus.Unknown,
            synchronizeBox: false);
        var movementCountBefore = await fixture.Context.Set<StockTransaction>()
            .CountAsync(item => item.InvoiceId == fixture.InvoiceId);

        var result = await fixture.Service.UpdateDetailsAsync(new EndpointOrderEditDto
        {
            InvoiceId = fixture.InvoiceId,
            Lines =
            [
                new EndpointOrderLocalLineDto
                {
                    ProductId = fixture.ProductId,
                    ProductUnitId = fixture.ProductUnitId,
                    Quantity = 3m,
                    UnitPrice = 10m
                }
            ]
        });

        var movementCountAfter = await fixture.Context.Set<StockTransaction>()
            .CountAsync(item => item.InvoiceId == fixture.InvoiceId);
        var stock = await fixture.Context.Set<Stock>()
            .SingleAsync(item => item.ProductId == fixture.ProductId);

        Assert.True(result.Success, result.Message);
        Assert.Contains("No order detail changes", result.Message);
        Assert.Equal(movementCountBefore, movementCountAfter);
        Assert.Equal(7m, stock.Quantity);
    }

    [Fact]
    public async Task UpdateDetailsAsync_ShouldAllowLegacyOnHoldEndpointOrder()
    {
        var fixture = await CreateFixtureAsync(
            nameof(UpdateDetailsAsync_ShouldAllowLegacyOnHoldEndpointOrder),
            10m,
            3m);
        await fixture.Service.ApplyStatusAsync(
            fixture.InvoiceId,
            InvoiceStatus.Unknown,
            synchronizeBox: false);
        var invoice = await fixture.Context.Set<Invoice>().SingleAsync(item => item.Id == fixture.InvoiceId);
        invoice.Status = InvoiceStatus.OnHold;
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Service.UpdateDetailsAsync(new EndpointOrderEditDto
        {
            InvoiceId = fixture.InvoiceId,
            Lines =
            [
                new EndpointOrderLocalLineDto
                {
                    ProductId = fixture.ProductId,
                    ProductUnitId = fixture.ProductUnitId,
                    Quantity = 4m,
                    UnitPrice = 11m
                }
            ]
        });

        Assert.True(result.Success, result.Message);
        var updatedInvoice = await fixture.Context.Set<Invoice>()
            .Include(item => item.InvoiceLines)
            .SingleAsync(item => item.Id == fixture.InvoiceId);
        Assert.Equal(4m, updatedInvoice.InvoiceLines!.Single().Quantity);
        Assert.Equal(44m, updatedInvoice.TotalAmount);
        Assert.Equal(InvoiceStatus.OnHold, updatedInvoice.Status);
        Assert.Equal(0, fixture.BoxApi.UpdateItemsCalls);
    }

    private static async Task<TestFixture> CreateFixtureAsync(
        string databaseName,
        decimal stockQuantity,
        decimal orderQuantity,
        bool seedAccounts = false)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        var context = new ApplicationDbContext(options);
        var mapper = new MapperConfiguration(config => config.AddProfile<MappingProfile>()).CreateMapper();
        var uow = new UOW(context, mapper);
        var accountingService = new AccountingService(
            context,
            uow,
            mapper,
            new CurrencyService(context));
        var stockService = new StockService(context, uow, mapper, accountingService);
        var boxApi = new StubBoxCartApiService();
        var service = new EndpointOrderStatusService(
            context,
            stockService,
            accountingService,
            mapper,
            boxApi);

        if (seedAccounts)
            await accountingService.EnsureDefaultAccountsAsync();

        const int productId = 81001;
        const int productUnitId = 82001;
        context.Set<Product>().Add(new Product
        {
            Id = productId,
            Name = "Endpoint test product",
            ITEMCODE = 4009900456630,
            SubCategoryId = 1,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        });
        context.Set<Unit>().Add(new Unit
        {
            Id = 1,
            Name = "Piece",
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        });
        context.Set<ProductUnit>().Add(new ProductUnit
        {
            Id = productUnitId,
            ProductId = productId,
            UnitId = 1,
            QuantityPerUnit = 1m,
            PurchasePrice = 4m,
            SalePrice = 10m,
            IsBaseUnit = true,
            IsDefaultSaleUnit = true,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        });
        await context.SaveChangesAsync();

        var stockResult = await stockService.PostMovementsAsync(new[]
        {
            new StockMovementPostDto
            {
                ProductId = productId,
                ProductUnitId = productUnitId,
                Quantity = stockQuantity,
                QuantityPerUnitSnapshot = 1m,
                BaseQuantity = stockQuantity,
                UnitPrice = 10m,
                PurchasePrice = 4m,
                SalePrice = 10m,
                ExpiryDate = DateTime.Today.AddYears(1),
                TransactionType = TransactionType.Purchase,
                TransactionDate = DateTime.Now,
                Notes = "Endpoint order test stock"
            }
        });
        Assert.True(stockResult.Success, stockResult.Message);

        var invoice = new Invoice
        {
            InvoiceNumber = $"BOX-TEST-{databaseName}",
            OriginalInvoiceId = "BOX-CART-704",
            InvoiceType = InvoiceType.appCart,
            PaymentType = PaymentType.Credit,
            Status = InvoiceStatus.Unknown,
            TotalAmount = orderQuantity * 10m,
            SubTotal = orderQuantity * 10m,
            NetSales = orderQuantity * 10m,
            TotalCOGS = orderQuantity * 4m,
            GrossProfit = orderQuantity * 6m,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now,
            InvoiceLines =
            [
                new InvoiceLine
                {
                    OriginalInvoiceId = "BOX-CART-ITEM-13758",
                    ProductId = productId,
                    ProductUnitId = productUnitId,
                    Quantity = orderQuantity,
                    QuantityPerUnitSnapshot = 1m,
                    BaseQuantity = orderQuantity,
                    UnitPrice = 10m,
                    UnitCost = 4m,
                    LineSubTotal = orderQuantity * 10m,
                    TaxExempt = true,
                    ExpiryDate = DateTime.Today.AddYears(1),
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                }
            ]
        };
        context.Set<Invoice>().Add(invoice);
        await context.SaveChangesAsync();

        return new TestFixture(context, service, invoice.Id, productId, productUnitId, boxApi);
    }

    private sealed record TestFixture(
        ApplicationDbContext Context,
        EndpointOrderStatusService Service,
        int InvoiceId,
        int ProductId,
        int ProductUnitId,
        StubBoxCartApiService BoxApi);

    private sealed class StubBoxCartApiService : IBoxCartApiService
    {
        public int UpdateItemsCalls { get; private set; }

        public Task<Result<BoxPendingOrdersSnapshotDto>> GetPendingOrdersAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Result<BoxPendingOrdersSnapshotDto>.Ok(new BoxPendingOrdersSnapshotDto()));
        }

        public Task<Result> UpdateCartStatusAsync(
            int cartId,
            int cartStatus,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Ok());
        }

        public Task<Result> UpdateCartItemsAsync(
            int cartId,
            IReadOnlyCollection<EndpointOrderLineEditDto> lines,
            CancellationToken cancellationToken = default)
        {
            UpdateItemsCalls++;
            return Task.FromResult(Result.Ok());
        }
    }
}

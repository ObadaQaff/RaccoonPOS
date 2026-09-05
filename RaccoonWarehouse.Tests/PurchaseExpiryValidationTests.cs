using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Helper;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.StockDocuments;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Data.Repository;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Checks.DTOs;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.StockDocuments.DTOs;
using RaccoonWarehouse.Domain.StockItems.DTOs;
using Xunit;

namespace RaccoonWarehouse.Tests;

public class PurchaseExpiryValidationTests
{
    [Fact]
    public async Task PurchaseInvoiceChecks_ShouldPersistAndReplaceOnUpdate()
    {
        var (context, mapper, uow) = CreateFixture(nameof(PurchaseInvoiceChecks_ShouldPersistAndReplaceOnUpdate));
        var service = new InvoiceService(context, uow, mapper);
        var invoice = new InvoiceWriteDto
        {
            InvoiceNumber = "PURCHASE-CHECK",
            InvoiceType = InvoiceType.Purchase,
            PaymentType = PaymentType.Check,
            SupplierId = 88,
            InvoiceLines =
            [
                new InvoiceLineWriteDto
                {
                    ProductId = 1,
                    ProductUnitId = 0,
                    Quantity = 1m,
                    UnitPrice = 10m,
                    ExpiryDate = new DateTime(2027, 8, 4)
                }
            ],
            Checks =
            [
                new CheckWriteDto { CheckNumber = "OUT-1", BankName = "Bank", Amount = 10m, DueDate = new DateTime(2026, 9, 1) }
            ]
        };

        var createResult = await service.CreateAsync(invoice);
        Assert.True(createResult.Success, createResult.Message);
        var createdCheck = Assert.Single(await context.Set<RaccoonWarehouse.Domain.Checks.Check>().ToListAsync());
        Assert.Equal(createResult.Data!.Id, createdCheck.InvoiceId);
        Assert.Equal("OUT-1", createdCheck.CheckNumber);

        invoice.Id = createResult.Data.Id;
        invoice.Checks =
        [
            new CheckWriteDto { CheckNumber = "OUT-2", BankName = "Bank", Amount = 10m, DueDate = new DateTime(2026, 9, 2) }
        ];

        var updateResult = await service.UpdateAsync(invoice);
        Assert.True(updateResult.Success, updateResult.Message);
        var updatedCheck = Assert.Single(await context.Set<RaccoonWarehouse.Domain.Checks.Check>().ToListAsync());
        Assert.Equal("OUT-2", updatedCheck.CheckNumber);
        Assert.Equal(invoice.Id, updatedCheck.InvoiceId);
    }

    [Fact]
    public async Task PurchaseInvoiceWithoutSupplier_ShouldFailBeforePersistence()
    {
        var (context, mapper, uow) = CreateFixture(nameof(PurchaseInvoiceWithoutSupplier_ShouldFailBeforePersistence));
        var service = new InvoiceService(context, uow, mapper);

        var result = await service.CreateAsync(new InvoiceWriteDto
        {
            InvoiceNumber = "PURCHASE-NO-SUPPLIER",
            InvoiceType = InvoiceType.Purchase,
            PaymentType = PaymentType.Credit,
            InvoiceLines =
            [
                new InvoiceLineWriteDto
                {
                    ProductId = 1,
                    ProductUnitId = 1,
                    Quantity = 1m,
                    UnitPrice = 5m,
                    ExpiryDate = new DateTime(2027, 8, 4)
                }
            ]
        });

        Assert.False(result.Success);
        Assert.Contains("supplier is required", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(context.Set<RaccoonWarehouse.Domain.Invoices.Invoice>());
    }

    [Fact]
    public async Task PurchaseInvoiceWithoutExpiry_ShouldFailBeforePersistence()
    {
        var (context, mapper, uow) = CreateFixture(nameof(PurchaseInvoiceWithoutExpiry_ShouldFailBeforePersistence));
        var service = new InvoiceService(context, uow, mapper);

        var result = await service.CreateAsync(new InvoiceWriteDto
        {
            InvoiceNumber = "PURCHASE-NO-EXPIRY",
            InvoiceType = InvoiceType.Purchase,
            InvoiceLines =
            [
                new InvoiceLineWriteDto
                {
                    ProductId = 1,
                    ProductUnitId = 1,
                    Quantity = 1m,
                    UnitPrice = 5m,
                    ExpiryDate = default
                }
            ]
        });

        Assert.False(result.Success);
        Assert.Contains("Expiry date is required", result.Message);
        Assert.Empty(context.Set<RaccoonWarehouse.Domain.Invoices.Invoice>());
    }

    [Fact]
    public async Task StockInDocumentWithoutExpiry_ShouldFailBeforePersistence()
    {
        var (context, mapper, uow) = CreateFixture(nameof(StockInDocumentWithoutExpiry_ShouldFailBeforePersistence));
        var service = new StockDocumentService(context, uow, mapper);

        var result = await service.CreateAsync(new StockDocumentWriteDto
        {
            DocumentNumber = "STOCK-IN-NO-EXPIRY",
            Type = StockVoucherType.In,
            Items =
            [
                new StockItemWriteDto
                {
                    ProductId = 1,
                    ProductUnitId = 1,
                    Quantity = 1m,
                    PurchasePrice = 5m,
                    SalePrice = 7m,
                    ExpiryDate = null
                }
            ]
        });

        Assert.False(result.Success);
        Assert.Contains("Expiry date is required", result.Message);
        Assert.Empty(context.Set<RaccoonWarehouse.Domain.StockDocuments.StockDocument>());
    }

    private static (ApplicationDbContext Context, IMapper Mapper, UOW Uow) CreateFixture(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        var context = new ApplicationDbContext(options);
        var mapper = new MapperConfiguration(config => config.AddProfile<MappingProfile>()).CreateMapper();
        return (context, mapper, new UOW(context, mapper));
    }
}

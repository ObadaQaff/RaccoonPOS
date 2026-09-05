using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Helper;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Data.Repository;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Reports.Financial.Filters;
using Xunit;

namespace RaccoonWarehouse.Tests;

public class SalesReportServiceTests
{
    [Fact]
    public async Task GetSalesReportDateRangeAsync_ShouldCoverAllSalesAndReturns()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(nameof(GetSalesReportDateRangeAsync_ShouldCoverAllSalesAndReturns))
            .Options;

        await using var context = new ApplicationDbContext(options);
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        var service = new InvoiceService(context, new UOW(context, mapper), mapper);

        context.Set<Invoice>().AddRange(
            CreateInvoice(1, "SALE-OLD", InvoiceType.Sale, new DateTime(2024, 1, 15)),
            CreateInvoice(2, "RETURN", InvoiceType.Return, new DateTime(2025, 6, 20)),
            CreateInvoice(3, "PURCHASE", InvoiceType.Purchase, new DateTime(2023, 1, 1)),
            CreateInvoice(4, "ENDPOINT-NEW", InvoiceType.EndpointOrder, new DateTime(2026, 6, 9)));
        await context.SaveChangesAsync();

        var result = await service.GetSalesReportDateRangeAsync();

        Assert.True(result.Success);
        Assert.Equal(new DateTime(2024, 1, 15), result.Data.from);
        Assert.Equal(new DateTime(2026, 6, 9), result.Data.to);
    }

    [Fact]
    public async Task GetSalesReportAsync_ShouldDisplayEndpointOrdersButOnlyTotalCompletedOnes()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(nameof(GetSalesReportAsync_ShouldDisplayEndpointOrdersButOnlyTotalCompletedOnes))
            .Options;

        await using var context = new ApplicationDbContext(options);
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        var service = new InvoiceService(context, new UOW(context, mapper), mapper);

        var held = CreateInvoice(1, "BOX-CART-1", InvoiceType.EndpointOrder, new DateTime(2026, 6, 8));
        held.Status = InvoiceStatus.OnHold;
        held.SubTotal = 100m;
        held.TotalAmount = 100m;
        held.TotalCOGS = 60m;

        var completed = CreateInvoice(2, "BOX-CART-2", InvoiceType.EndpointOrder, new DateTime(2026, 6, 9));
        completed.Status = InvoiceStatus.Completed;
        completed.SubTotal = 200m;
        completed.TotalAmount = 200m;
        completed.TotalCOGS = 120m;

        context.Set<Invoice>().AddRange(held, completed);
        await context.SaveChangesAsync();

        var result = await service.GetSalesReportAsync(new FinancialSummaryFilterDto
        {
            From = new DateTime(2026, 6, 1),
            To = new DateTime(2026, 6, 30, 23, 59, 59),
            IncludeReturns = true
        });

        Assert.True(result.Success);
        Assert.Equal(2, result.Data.rows.Count);
        Assert.Contains(result.Data.rows, row => row.InvoiceNumber == "BOX-CART-1" && row.Status == "OnHold");
        Assert.Contains(result.Data.rows, row => row.InvoiceNumber == "BOX-CART-2" && row.Status == "Completed");
        Assert.Equal(200m, result.Data.summary.TotalSales);
        Assert.Equal(120m, result.Data.summary.TotalCOGS);
        Assert.Equal(80m, result.Data.summary.GrossProfit);
        Assert.Equal(1, result.Data.summary.NumberOfInvoices);
    }

    private static Invoice CreateInvoice(int id, string number, InvoiceType type, DateTime date)
    {
        return new Invoice
        {
            Id = id,
            InvoiceNumber = number,
            InvoiceType = type,
            CreatedDate = date,
            UpdatedDate = date
        };
    }
}

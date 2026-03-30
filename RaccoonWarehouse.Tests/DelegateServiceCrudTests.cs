using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Helper;
using RaccoonWarehouse.Application.Service.Delegates;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Data.Repository;
using RaccoonWarehouse.Domain.Delegates;
using RaccoonWarehouse.Domain.Delegates.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Invoices;
using Xunit;
using DelegateEntity = RaccoonWarehouse.Domain.Delegates.Delegate;

namespace RaccoonWarehouse.Tests;

public class DelegateServiceCrudTests
{
    private static DelegateService CreateService(string databaseName, out ApplicationDbContext context)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        context = new ApplicationDbContext(options);
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        var uow = new UOW(context, mapper);
        var featureService = new DelegateFeatureService(context);
        return new DelegateService(context, uow, mapper, featureService);
    }

    [Fact]
    public async Task Create_Delegate_ShouldPersistSuccessfully()
    {
        var service = CreateService(nameof(Create_Delegate_ShouldPersistSuccessfully), out var context);

        var dto = new DelegateCreateDto
        {
            Code = "DLG-001",
            FullName = "Delegate One",
            PhoneNumber = "0799999999",
            DelegateType = DelegateType.Sales,
            Status = DelegateStatus.Active
        };

        var result = await service.CreateAsync(dto);
        var created = await context.Set<DelegateEntity>().FirstOrDefaultAsync(x => x.Code == "DLG-001");

        Assert.True(result.Success);
        Assert.NotNull(created);
        Assert.Equal("Delegate One", created!.FullName);
    }

    [Fact]
    public async Task Create_Delegate_WithDuplicateCode_ShouldFail()
    {
        var service = CreateService(nameof(Create_Delegate_WithDuplicateCode_ShouldFail), out _);

        await service.CreateAsync(new DelegateCreateDto
        {
            Code = "DLG-001",
            FullName = "First Delegate"
        });

        var result = await service.CreateAsync(new DelegateCreateDto
        {
            Code = "DLG-001",
            FullName = "Second Delegate"
        });

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Update_Delegate_ShouldChangeStatusAndLink()
    {
        var service = CreateService(nameof(Update_Delegate_ShouldChangeStatusAndLink), out var context);

        var createResult = await service.CreateAsync(new DelegateCreateDto
        {
            Code = "DLG-002",
            FullName = "Updatable Delegate",
            Status = DelegateStatus.Active
        });

        var updateResult = await service.UpdateAsync(new DelegateUpdateDto
        {
            Id = createResult.Data.Id,
            Code = "DLG-002",
            FullName = "Updated Delegate",
            Status = DelegateStatus.Inactive,
            DelegateType = DelegateType.Delivery,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        });

        var updated = await context.Set<DelegateEntity>().FirstAsync(x => x.Id == createResult.Data.Id);

        Assert.True(updateResult.Success);
        Assert.Equal("Updated Delegate", updated.FullName);
        Assert.Equal(DelegateStatus.Inactive, updated.Status);
        Assert.Equal(DelegateType.Delivery, updated.DelegateType);
    }

    [Fact]
    public async Task Analytics_ShouldAggregateInvoices()
    {
        var service = CreateService(nameof(Analytics_ShouldAggregateInvoices), out var context);

        var createResult = await service.CreateAsync(new DelegateCreateDto
        {
            Code = "DLG-003",
            FullName = "Analytics Delegate"
        });

        context.Set<Invoice>().AddRange(
            new Invoice
            {
                InvoiceNumber = "INV-1",
                InvoiceType = InvoiceType.Sale,
                DelegateId = createResult.Data.Id,
                CustomerId = 10,
                TotalAmount = 100,
                Status = InvoiceStatus.Completed,
                CreatedDate = DateTime.Now.AddDays(-1),
                UpdatedDate = DateTime.Now.AddDays(-1)
            },
            new Invoice
            {
                InvoiceNumber = "INV-2",
                InvoiceType = InvoiceType.Sale,
                DelegateId = createResult.Data.Id,
                CustomerId = 11,
                TotalAmount = 50,
                Status = InvoiceStatus.OnHold,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            });

        await context.SaveChangesAsync();

        var analytics = await service.GetAnalyticsAsync(createResult.Data.Id);

        Assert.True(analytics.Success);
        Assert.Equal(2, analytics.Data.TotalInvoices);
        Assert.Equal(150, analytics.Data.TotalSalesAmount);
        Assert.Equal(2, analytics.Data.UniqueCustomersServed);
        Assert.Equal(1, analytics.Data.OpenInvoicesCount);
    }
}

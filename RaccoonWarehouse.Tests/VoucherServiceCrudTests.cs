using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Helper;
using RaccoonWarehouse.Application.Service.Vouchers;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Data.Repository;
using RaccoonWarehouse.Domain.Checks;
using RaccoonWarehouse.Domain.Checks.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Vouchers;
using RaccoonWarehouse.Domain.Vouchers.DTOs;
using Xunit;

namespace RaccoonWarehouse.Tests;

public class VoucherServiceCrudTests
{
    private static VoucherService CreateService(string databaseName, out ApplicationDbContext context)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        context = new ApplicationDbContext(options);
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        var uow = new UOW(context, mapper);
        return new VoucherService(context, uow, mapper);
    }

    [Fact]
    public async Task Create_Voucher_WithChecks_ShouldPersistVoucherAndChecks()
    {
        var service = CreateService(nameof(Create_Voucher_WithChecks_ShouldPersistVoucherAndChecks), out var context);

        var dto = new VoucherWriteDto
        {
            VoucherNumber = "V-CHK-001",
            VoucherType = VoucherType.Payment,
            PaymentType = PaymentType.Check,
            Amount = 300m,
            Notes = "check payment",
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now,
            Checks = new List<CheckWriteDto>
            {
                new()
                {
                    CheckNumber = "CHK-1",
                    BankName = "Bank A",
                    Amount = 100m,
                    DueDate = DateTime.Today.AddDays(5),
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                },
                new()
                {
                    CheckNumber = "CHK-2",
                    BankName = "Bank B",
                    Amount = 200m,
                    DueDate = DateTime.Today.AddDays(7),
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                }
            }
        };

        var result = await service.CreateAsync(dto);
        var created = await context.Set<Voucher>()
            .Include(v => v.Checks)
            .FirstOrDefaultAsync(v => v.VoucherNumber == "V-CHK-001");

        Assert.True(result.Success);
        Assert.NotNull(created);
        Assert.Equal(PaymentType.Check, created!.PaymentType);
        Assert.Equal(2, created.Checks!.Count);
    }

    [Fact]
    public async Task Create_Voucher_ShouldAllowNullableFields()
    {
        var service = CreateService(nameof(Create_Voucher_ShouldAllowNullableFields), out var context);

        var dto = new VoucherWriteDto
        {
            VoucherNumber = null,
            VoucherType = VoucherType.Receipt,
            PaymentType = PaymentType.Cash,
            Amount = 50m,
            Notes = null,
            CustomerId = null,
            SupplierId = null,
            CasherId = null,
            Checks = null,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };

        var result = await service.CreateAsync(dto);
        var created = await context.Set<Voucher>().FirstOrDefaultAsync();

        Assert.True(result.Success);
        Assert.NotNull(created);
        Assert.Null(created!.VoucherNumber);
        Assert.Null(created.Notes);
        Assert.Null(created.CustomerId);
    }

    [Fact]
    public async Task SearchVouchers_ShouldFilterByVoucherNumber_PaymentType_AndType()
    {
        var service = CreateService(nameof(SearchVouchers_ShouldFilterByVoucherNumber_PaymentType_AndType), out var context);

        context.Set<Voucher>().AddRange(
            new Voucher
            {
                VoucherNumber = "R-100",
                VoucherType = VoucherType.Receipt,
                PaymentType = PaymentType.Cash,
                Amount = 100,
                CreatedDate = DateTime.Today,
                UpdatedDate = DateTime.Today
            },
            new Voucher
            {
                VoucherNumber = "R-100",
                VoucherType = VoucherType.Payment,
                PaymentType = PaymentType.Cash,
                Amount = 120,
                CreatedDate = DateTime.Today,
                UpdatedDate = DateTime.Today
            },
            new Voucher
            {
                VoucherNumber = "R-101",
                VoucherType = VoucherType.Receipt,
                PaymentType = PaymentType.Check,
                Amount = 150,
                CreatedDate = DateTime.Today,
                UpdatedDate = DateTime.Today
            });

        await context.SaveChangesAsync();

        var result = await service.SearchVouchersAsync(
            voucherNumber: "R-100",
            customerName: null,
            dateFrom: null,
            dateTo: null,
            paymentType: PaymentType.Cash,
            type: VoucherType.Receipt);

        Assert.Single(result);
        Assert.Equal(VoucherType.Receipt, result[0].VoucherType);
        Assert.Equal(PaymentType.Cash, result[0].PaymentType);
        Assert.Equal("R-100", result[0].VoucherNumber);
    }

    [Fact]
    public async Task SearchVouchers_ShouldFilterByDateRange()
    {
        var service = CreateService(nameof(SearchVouchers_ShouldFilterByDateRange), out var context);

        context.Set<Voucher>().AddRange(
            new Voucher
            {
                VoucherNumber = "D-OLD",
                VoucherType = VoucherType.Payment,
                PaymentType = PaymentType.Cash,
                Amount = 10,
                CreatedDate = new DateTime(2026, 1, 10),
                UpdatedDate = DateTime.Today
            },
            new Voucher
            {
                VoucherNumber = "D-IN",
                VoucherType = VoucherType.Payment,
                PaymentType = PaymentType.Cash,
                Amount = 20,
                CreatedDate = new DateTime(2026, 2, 10),
                UpdatedDate = DateTime.Today
            });

        await context.SaveChangesAsync();

        var result = await service.SearchVouchersAsync(
            voucherNumber: null,
            customerName: null,
            dateFrom: new DateTime(2026, 2, 1),
            dateTo: new DateTime(2026, 2, 28),
            paymentType: null,
            type: VoucherType.Payment);

        Assert.Single(result);
        Assert.Equal("D-IN", result[0].VoucherNumber);
    }
}

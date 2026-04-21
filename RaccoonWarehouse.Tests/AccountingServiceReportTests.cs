using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Helper;
using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Data.Repository;
using RaccoonWarehouse.Domain.Accounting.Accounts;
using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Accounting.JournalEntries;
using RaccoonWarehouse.Domain.Accounting.JournalEntries.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.FinancialTransactions;
using RaccoonWarehouse.Domain.FinancialTransactions.DTOs;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Reports.Accounting.Filters;
using RaccoonWarehouse.Domain.Reports.Financial.Filters;
using RaccoonWarehouse.Domain.Settings;
using RaccoonWarehouse.Domain.StockAdjustments.DTOs;
using Xunit;

namespace RaccoonWarehouse.Tests;

public class AccountingServiceReportTests
{
    private static AccountingService CreateService(string databaseName, out ApplicationDbContext context)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        context = new ApplicationDbContext(options);
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        var uow = new UOW(context, mapper);
        return new AccountingService(context, uow, mapper);
    }

    [Fact]
    public async Task GetTrialBalanceAsync_ShouldReturnBalancedClosingTotals()
    {
        var service = CreateService(nameof(GetTrialBalanceAsync_ShouldReturnBalancedClosingTotals), out var context);
        await SeedAccountsAsync(context);

        await service.PostJournalEntryAsync(new JournalEntryWriteDto
        {
            EntryDate = new DateTime(2026, 3, 10),
            Description = "Sales entry",
            Lines =
            [
                new JournalEntryLineWriteDto { AccountId = 1, Debit = 100m },
                new JournalEntryLineWriteDto { AccountId = 3, Credit = 100m }
            ]
        });

        var result = await service.GetTrialBalanceAsync(new TrialBalanceFilterDto
        {
            From = new DateTime(2026, 3, 1),
            To = new DateTime(2026, 3, 31),
            IncludeZeroBalances = false
        });

        Assert.True(result.Success);
        Assert.Equal(100m, result.Data.summary.TotalClosingDebit);
        Assert.Equal(100m, result.Data.summary.TotalClosingCredit);
        Assert.True(result.Data.summary.IsBalanced);
        Assert.Equal(2, result.Data.rows.Count);
    }

    [Fact]
    public async Task GetGeneralLedgerAsync_ShouldIncludeOpeningBalanceAndRunningBalance()
    {
        var service = CreateService(nameof(GetGeneralLedgerAsync_ShouldIncludeOpeningBalanceAndRunningBalance), out var context);
        await SeedAccountsAsync(context);

        await service.PostJournalEntryAsync(new JournalEntryWriteDto
        {
            EntryDate = new DateTime(2026, 2, 28),
            Description = "Opening cash",
            Lines =
            [
                new JournalEntryLineWriteDto { AccountId = 1, Debit = 50m },
                new JournalEntryLineWriteDto { AccountId = 2, Credit = 50m }
            ]
        });

        await service.PostJournalEntryAsync(new JournalEntryWriteDto
        {
            EntryDate = new DateTime(2026, 3, 5),
            Description = "Cash sale",
            Lines =
            [
                new JournalEntryLineWriteDto { AccountId = 1, Debit = 20m },
                new JournalEntryLineWriteDto { AccountId = 3, Credit = 20m }
            ]
        });

        var result = await service.GetGeneralLedgerAsync(new GeneralLedgerFilterDto
        {
            From = new DateTime(2026, 3, 1),
            To = new DateTime(2026, 3, 31),
            AccountId = 1
        });

        Assert.True(result.Success);
        var ledger = Assert.Single(result.Data);
        Assert.Equal(50m, ledger.OpeningBalance);
        Assert.Equal(70m, ledger.ClosingBalance);
        Assert.Equal("OPENING", ledger.Rows[0].EntryNumber);
        Assert.Equal(70m, ledger.Rows.Last().RunningBalance);
    }

    [Fact]
    public async Task GetBalanceSheetAsync_ShouldGroupAccountsBySection()
    {
        var service = CreateService(nameof(GetBalanceSheetAsync_ShouldGroupAccountsBySection), out var context);
        await SeedAccountsAsync(context);

        await service.PostJournalEntryAsync(new JournalEntryWriteDto
        {
            EntryDate = new DateTime(2026, 3, 7),
            Description = "Initial funding",
            Lines =
            [
                new JournalEntryLineWriteDto { AccountId = 1, Debit = 150m },
                new JournalEntryLineWriteDto { AccountId = 2, Credit = 60m },
                new JournalEntryLineWriteDto { AccountId = 4, Credit = 90m }
            ]
        });

        var result = await service.GetBalanceSheetAsync(new BalanceSheetFilterDto
        {
            AsOfDate = new DateTime(2026, 3, 31),
            IncludeZeroBalances = false
        });

        Assert.True(result.Success);
        Assert.Equal(150m, result.Data.Assets.Total);
        Assert.Equal(60m, result.Data.Liabilities.Total);
        Assert.Equal(90m, result.Data.Equity.Total);
        Assert.Equal(result.Data.Assets.Total, result.Data.TotalLiabilitiesAndEquity);
    }

    [Fact]
    public async Task GetBalanceSheetAsync_ShouldIncludeCurrentPeriodEarningsInEquity()
    {
        var service = CreateService(nameof(GetBalanceSheetAsync_ShouldIncludeCurrentPeriodEarningsInEquity), out var context);
        await service.EnsureDefaultAccountsAsync();
        var cashAccountId = await context.Set<Account>()
            .Where(x => x.Code == "1000")
            .Select(x => x.Id)
            .FirstAsync();
        var salesAccountId = await context.Set<Account>()
            .Where(x => x.Code == "4000")
            .Select(x => x.Id)
            .FirstAsync();

        await service.PostJournalEntryAsync(new JournalEntryWriteDto
        {
            EntryDate = new DateTime(2026, 3, 12),
            Description = "Cash sale",
            Lines =
            [
                new JournalEntryLineWriteDto { AccountId = cashAccountId, Debit = 100m },
                new JournalEntryLineWriteDto { AccountId = salesAccountId, Credit = 100m }
            ]
        });

        var result = await service.GetBalanceSheetAsync(new BalanceSheetFilterDto
        {
            AsOfDate = new DateTime(2026, 3, 31),
            IncludeZeroBalances = false
        });

        Assert.True(result.Success);
        Assert.Equal(100m, result.Data.Assets.Total);
        Assert.Equal(100m, result.Data.Equity.Total);
        Assert.Equal(result.Data.Assets.Total, result.Data.TotalLiabilitiesAndEquity);
        Assert.Contains(result.Data.Equity.Rows, x => x.AccountCode == "CURRENT-EARNINGS" && x.Balance == 100m);
    }

    [Fact]
    public async Task PostInvoiceEntryAsync_ShouldCreateSalesAndCogsJournal()
    {
        var service = CreateService(nameof(PostInvoiceEntryAsync_ShouldCreateSalesAndCogsJournal), out var context);
        await service.EnsureDefaultAccountsAsync();

        var result = await service.PostInvoiceEntryAsync(new InvoiceWriteDto
        {
            Id = 101,
            InvoiceNumber = "S-101",
            InvoiceType = InvoiceType.Sale,
            PaymentType = PaymentType.Cash,
            Status = InvoiceStatus.Posted,
            CreatedDate = new DateTime(2026, 3, 27),
            SubTotal = 100m,
            TotalTax = 16m,
            DiscountAmount = 5m,
            NetSales = 95m,
            TotalAmount = 111m,
            TotalCOGS = 60m
        });

        var entry = await context.Set<RaccoonWarehouse.Domain.Accounting.JournalEntries.JournalEntry>()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.ReferenceType == "Invoice" && x.ReferenceId == 101);

        Assert.True(result.Success);
        Assert.NotNull(entry);
        Assert.Equal(5, entry!.Lines.Count);
    }

    [Fact]
    public async Task PostFinancialTransactionEntryAsync_ShouldCreateManualReceiptJournal()
    {
        var service = CreateService(nameof(PostFinancialTransactionEntryAsync_ShouldCreateManualReceiptJournal), out var context);
        await service.EnsureDefaultAccountsAsync();

        var result = await service.PostFinancialTransactionEntryAsync(new FinancialPostDto
        {
            Direction = TransactionDirection.In,
            Method = PaymentMethod.Cash,
            Amount = 50m,
            TransactionDate = new DateTime(2026, 3, 27),
            SourceType = FinancialSourceType.Manual,
            Notes = "Manual receipt"
        }, 201);

        var entry = await context.Set<RaccoonWarehouse.Domain.Accounting.JournalEntries.JournalEntry>()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.ReferenceType == "FinancialTransaction" && x.ReferenceId == 201);

        Assert.True(result.Success);
        Assert.NotNull(entry);
        Assert.Equal(2, entry!.Lines.Count);
    }

    [Fact]
    public async Task PostStockAdjustmentEntryAsync_ShouldCreateInventoryLossJournalForDecrease()
    {
        var service = CreateService(nameof(PostStockAdjustmentEntryAsync_ShouldCreateInventoryLossJournalForDecrease), out var context);
        await service.EnsureDefaultAccountsAsync();

        var result = await service.PostStockAdjustmentEntryAsync(new StockAdjustmentWriteDto
        {
            Id = 301,
            AdjustmentType = StockAdjustmentType.Decrease,
            BaseQuantityDelta = -3m,
            PurchasePrice = 10m,
            AdjustmentDate = new DateTime(2026, 3, 27),
            Reason = "Shrinkage"
        });

        var entry = await context.Set<RaccoonWarehouse.Domain.Accounting.JournalEntries.JournalEntry>()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.ReferenceType == "StockAdjustment" && x.ReferenceId == 301);

        Assert.True(result.Success);
        Assert.NotNull(entry);
        Assert.Equal(2, entry!.Lines.Count);
    }

    [Fact]
    public async Task PostJournalEntryAsync_ShouldRejectPostingOnOrBeforeLockDate()
    {
        var service = CreateService(nameof(PostJournalEntryAsync_ShouldRejectPostingOnOrBeforeLockDate), out var context);
        await SeedAccountsAsync(context);

        context.Set<AppSetting>().Add(new AppSetting
        {
            Key = AccountingService.PostingLockDateKey,
            Value = "2026-03-27",
            Description = "Posting lock",
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        });
        await context.SaveChangesAsync();

        var result = await service.PostJournalEntryAsync(new JournalEntryWriteDto
        {
            EntryDate = new DateTime(2026, 3, 27),
            Description = "Locked entry",
            Lines =
            [
                new JournalEntryLineWriteDto { AccountId = 1, Debit = 10m },
                new JournalEntryLineWriteDto { AccountId = 2, Credit = 10m }
            ]
        });

        Assert.False(result.Success);
        Assert.Contains("locked", result.Message!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReverseJournalEntryAsync_ShouldCreateReversalAndMarkOriginalAsReversed()
    {
        var service = CreateService(nameof(ReverseJournalEntryAsync_ShouldCreateReversalAndMarkOriginalAsReversed), out var context);
        await SeedAccountsAsync(context);

        var postResult = await service.PostJournalEntryAsync(new JournalEntryWriteDto
        {
            EntryDate = new DateTime(2026, 3, 28),
            Description = "Original entry",
            Lines =
            [
                new JournalEntryLineWriteDto { AccountId = 1, Debit = 25m },
                new JournalEntryLineWriteDto { AccountId = 2, Credit = 25m }
            ]
        });

        var reverseResult = await service.ReverseJournalEntryAsync(postResult.Data.Id, "Test reversal");

        var original = await context.Set<RaccoonWarehouse.Domain.Accounting.JournalEntries.JournalEntry>()
            .AsNoTracking()
            .FirstAsync(x => x.Id == postResult.Data.Id);

        var reversal = await context.Set<RaccoonWarehouse.Domain.Accounting.JournalEntries.JournalEntry>()
            .Include(x => x.Lines)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ReferenceType == "Reversal" && x.ReferenceId == postResult.Data.Id);

        Assert.True(reverseResult.Success);
        Assert.Equal(JournalEntryStatus.Reversed, original.Status);
        Assert.NotNull(reversal);
        Assert.Equal(2, reversal!.Lines.Count);
        Assert.Equal(25m, reversal.Lines.Sum(x => x.Debit));
        Assert.Equal(25m, reversal.Lines.Sum(x => x.Credit));
    }

    [Fact]
    public async Task ReverseJournalByReferenceAsync_ShouldReverseMatchingPostedEntry()
    {
        var service = CreateService(nameof(ReverseJournalByReferenceAsync_ShouldReverseMatchingPostedEntry), out var context);
        await SeedAccountsAsync(context);

        await service.PostJournalEntryAsync(new JournalEntryWriteDto
        {
            EntryDate = new DateTime(2026, 3, 28),
            Description = "Reference entry",
            ReferenceType = "FinancialTransaction",
            ReferenceId = 501,
            Lines =
            [
                new JournalEntryLineWriteDto { AccountId = 1, Debit = 40m },
                new JournalEntryLineWriteDto { AccountId = 2, Credit = 40m }
            ]
        });

        var result = await service.ReverseJournalByReferenceAsync("FinancialTransaction", 501, "Void test");

        var original = await context.Set<JournalEntry>()
            .AsNoTracking()
            .FirstAsync(x => x.ReferenceType == "FinancialTransaction" && x.ReferenceId == 501);

        var reversal = await context.Set<JournalEntry>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ReferenceType == "Reversal" && x.ReferenceId == original.Id);

        Assert.True(result.Success);
        Assert.Equal(JournalEntryStatus.Reversed, original.Status);
        Assert.NotNull(reversal);
    }

    [Fact]
    public async Task FinancialTransactionVoidAsync_ShouldReverseAccountingEntry()
    {
        var service = CreateService(nameof(FinancialTransactionVoidAsync_ShouldReverseAccountingEntry), out var context);
        await service.EnsureDefaultAccountsAsync();
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        var uow = new UOW(context, mapper);
        var financialService = new FinancialTransactionService(context, uow, mapper, new UserSession(), service);

        var postResult = await financialService.PostAsync(new FinancialPostDto
        {
            Direction = TransactionDirection.In,
            Method = PaymentMethod.Cash,
            Amount = 75m,
            TransactionDate = new DateTime(2026, 3, 27),
            SourceType = FinancialSourceType.Manual,
            Notes = "Manual receipt"
        });

        Assert.True(postResult.Success);

        var voidResult = await financialService.VoidAsync(postResult.Data.Id, "Cancel test");

        var original = await context.Set<JournalEntry>()
            .AsNoTracking()
            .FirstAsync(x => x.ReferenceType == "FinancialTransaction" && x.ReferenceId == postResult.Data.Id);

        var reversal = await context.Set<JournalEntry>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ReferenceType == "Reversal" && x.ReferenceId == original.Id);

        var transaction = await context.Set<FinancialTransaction>().AsNoTracking().FirstAsync(x => x.Id == postResult.Data.Id);

        Assert.True(voidResult.Success);
        Assert.Equal(FinancialTransactionStatus.Voided, transaction.Status);
        Assert.Equal(JournalEntryStatus.Reversed, original.Status);
        Assert.NotNull(reversal);
    }

    [Fact]
    public async Task GetProfitLossAsync_ShouldOnlyTreatExpenseSourceTransactionsAsExpenses()
    {
        var service = CreateService(nameof(GetProfitLossAsync_ShouldOnlyTreatExpenseSourceTransactionsAsExpenses), out var context);
        await service.EnsureDefaultAccountsAsync();
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        var uow = new UOW(context, mapper);
        var financialService = new FinancialTransactionService(context, uow, mapper, new UserSession(), service);

        context.Set<RaccoonWarehouse.Domain.Invoices.Invoice>().Add(new RaccoonWarehouse.Domain.Invoices.Invoice
        {
            Id = 1,
            InvoiceNumber = "S-PL-1",
            InvoiceType = InvoiceType.Sale,
            PaymentType = PaymentType.Cash,
            Status = InvoiceStatus.Posted,
            SubTotal = 100m,
            TotalAmount = 100m,
            TotalCOGS = 40m,
            CreatedDate = new DateTime(2026, 3, 27),
            UpdatedDate = new DateTime(2026, 3, 27)
        });
        await context.SaveChangesAsync();

        var supplierPayment = await financialService.PostAsync(new FinancialPostDto
        {
            Direction = TransactionDirection.Out,
            Method = PaymentMethod.Cash,
            Amount = 30m,
            TransactionDate = new DateTime(2026, 3, 27),
            SourceType = FinancialSourceType.PaymentVoucher,
            CashierSessionId = 1,
            CashierId = 1,
            Notes = "Supplier settlement"
        });

        var operatingExpense = await financialService.PostAsync(new FinancialPostDto
        {
            Direction = TransactionDirection.Out,
            Method = PaymentMethod.Cash,
            Amount = 10m,
            TransactionDate = new DateTime(2026, 3, 27),
            SourceType = FinancialSourceType.Expense,
            CashierSessionId = 1,
            CashierId = 1,
            Notes = "Utility expense"
        });

        Assert.True(supplierPayment.Success);
        Assert.True(operatingExpense.Success);

        var result = await financialService.GetProfitLossAsync(new ProfitLossFilterDto
        {
            From = new DateTime(2026, 3, 1),
            To = new DateTime(2026, 3, 31),
            IncludeReturns = false,
            IncludeVoidedTransactions = false
        });

        Assert.Equal(60m, result.summary.GrossProfit);
        Assert.Equal(10m, result.summary.TotalExpenses);
        Assert.Equal(50m, result.summary.NetProfit);
        Assert.DoesNotContain(result.rows, x => x.Section == "Expenses" && x.Item == FinancialSourceType.PaymentVoucher.ToString());
        Assert.Contains(result.rows, x => x.Section == "Expenses" && x.Item == FinancialSourceType.Expense.ToString());
    }

    [Fact]
    public async Task InvoiceUpdateAsync_ShouldReverseOldJournalAndPostFreshJournal()
    {
        var service = CreateService(nameof(InvoiceUpdateAsync_ShouldReverseOldJournalAndPostFreshJournal), out var context);
        await service.EnsureDefaultAccountsAsync();
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        var uow = new UOW(context, mapper);
        var invoiceService = new InvoiceService(context, uow, mapper, service);

        var createResult = await invoiceService.CreateAsync(new InvoiceWriteDto
        {
            InvoiceNumber = "S-300",
            InvoiceType = InvoiceType.Sale,
            PaymentType = PaymentType.Cash,
            Status = InvoiceStatus.Posted,
            CreatedDate = new DateTime(2026, 3, 27),
            InvoiceLines =
            [
                new InvoiceLineWriteDto
                {
                    ProductId = 1,
                    ProductUnitId = 0,
                    Quantity = 2m,
                    UnitPrice = 10m,
                    UnitCost = 4m,
                    TaxRate = 0m,
                    TaxExempt = true
                }
            ]
        });

        Assert.True(createResult.Success);

        var updateResult = await invoiceService.UpdateAsync(new InvoiceWriteDto
        {
            Id = createResult.Data.Id,
            InvoiceNumber = "S-300",
            InvoiceType = InvoiceType.Sale,
            PaymentType = PaymentType.Cash,
            Status = InvoiceStatus.Posted,
            CreatedDate = createResult.Data.CreatedDate,
            InvoiceLines =
            [
                new InvoiceLineWriteDto
                {
                    ProductId = 1,
                    ProductUnitId = 0,
                    Quantity = 3m,
                    UnitPrice = 15m,
                    UnitCost = 5m,
                    TaxRate = 0m,
                    TaxExempt = true
                }
            ]
        });

        var invoiceEntries = await context.Set<JournalEntry>()
            .Include(x => x.Lines)
            .Where(x => x.ReferenceType == "Invoice" && x.ReferenceId == createResult.Data.Id)
            .OrderBy(x => x.Id)
            .ToListAsync();

        var reversals = await context.Set<JournalEntry>()
            .Where(x => x.ReferenceType == "Reversal")
            .ToListAsync();

        Assert.True(updateResult.Success);
        Assert.Equal(2, invoiceEntries.Count);
        Assert.Contains(invoiceEntries, x => x.Status == JournalEntryStatus.Reversed);
        Assert.Contains(invoiceEntries, x => x.Status == JournalEntryStatus.Posted);
        Assert.NotEmpty(reversals);
    }

    private static async Task SeedAccountsAsync(ApplicationDbContext context)
    {
        context.Set<Account>().AddRange(
            new Account
            {
                Id = 1,
                Code = "1000",
                Name = "Cash",
                AccountType = AccountType.Asset,
                IsPosting = true,
                IsActive = true,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            },
            new Account
            {
                Id = 2,
                Code = "2000",
                Name = "Payables",
                AccountType = AccountType.Liability,
                IsPosting = true,
                IsActive = true,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            },
            new Account
            {
                Id = 3,
                Code = "4000",
                Name = "Sales",
                AccountType = AccountType.Revenue,
                IsPosting = true,
                IsActive = true,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            },
            new Account
            {
                Id = 4,
                Code = "3000",
                Name = "Equity",
                AccountType = AccountType.Equity,
                IsPosting = true,
                IsActive = true,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            });

        await context.SaveChangesAsync();
    }
}

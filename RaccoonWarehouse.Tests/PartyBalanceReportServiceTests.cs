using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Accounting.Accounts;
using RaccoonWarehouse.Domain.Accounting.JournalEntries;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Reports.Accounting.Filters;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Users;
using Xunit;

namespace RaccoonWarehouse.Tests;

public class PartyBalanceReportServiceTests
{
    [Fact]
    public async Task GetAsync_CustomerReport_UsesDebitMinusCreditAndExcludesDraftAndFutureEntries()
    {
        await using var context = CreateContext(nameof(GetAsync_CustomerReport_UsesDebitMinusCreditAndExcludesDraftAndFutureEntries));
        var customer = AddUser(context, "Customer A", UserRole.Customer);
        await AddEntryAsync(context, new DateTime(2026, 8, 1), JournalEntryStatus.Posted, customerId: customer.Id, debit: 120m, credit: 20m);
        await AddEntryAsync(context, new DateTime(2026, 8, 2), JournalEntryStatus.Draft, customerId: customer.Id, debit: 500m);
        await AddEntryAsync(context, new DateTime(2026, 8, 4), JournalEntryStatus.Posted, customerId: customer.Id, debit: 300m);

        var result = await new PartyBalanceReportService(context).GetAsync(new PartyBalanceFilterDto
        {
            Role = UserRole.Customer,
            AsOfDate = new DateTime(2026, 8, 3)
        });

        Assert.True(result.Success);
        var row = Assert.Single(result.Data!.Rows);
        Assert.Equal(120m, row.TotalDebit);
        Assert.Equal(20m, row.TotalCredit);
        Assert.Equal(-100m, row.Balance);
        Assert.Equal(100m, result.Data.TotalOutstanding);
    }

    [Fact]
    public async Task GetAsync_SupplierReport_UsesCreditMinusDebit()
    {
        await using var context = CreateContext(nameof(GetAsync_SupplierReport_UsesCreditMinusDebit));
        var supplier = AddUser(context, "Supplier A", UserRole.Supplier);
        await AddEntryAsync(context, new DateTime(2026, 8, 1), JournalEntryStatus.Posted, supplierId: supplier.Id, debit: 30m, credit: 200m);

        var result = await new PartyBalanceReportService(context).GetAsync(new PartyBalanceFilterDto
        {
            Role = UserRole.Supplier,
            AsOfDate = new DateTime(2026, 8, 3)
        });

        var row = Assert.Single(result.Data!.Rows);
        Assert.Equal(170m, row.Balance);
        Assert.Equal(170m, result.Data.TotalOutstanding);
    }

    [Fact]
    public async Task GetAsync_OutstandingOnlyAndSearch_FilterRows()
    {
        await using var context = CreateContext(nameof(GetAsync_OutstandingOnlyAndSearch_FilterRows));
        var matching = AddUser(context, "Ahmad Market", UserRole.Customer, "0790000000");
        var settled = AddUser(context, "Ahmad Settled", UserRole.Customer);
        AddUser(context, "Other Customer", UserRole.Customer);
        await context.SaveChangesAsync();
        await AddEntryAsync(context, new DateTime(2026, 8, 1), JournalEntryStatus.Posted, customerId: matching.Id, debit: 50m);
        await AddEntryAsync(context, new DateTime(2026, 8, 1), JournalEntryStatus.Posted, customerId: settled.Id, debit: 25m, credit: 25m);

        var result = await new PartyBalanceReportService(context).GetAsync(new PartyBalanceFilterDto
        {
            Role = UserRole.Customer,
            AsOfDate = new DateTime(2026, 8, 3),
            Search = "Ahmad",
            OutstandingOnly = true
        });

        var row = Assert.Single(result.Data!.Rows);
        Assert.Equal(matching.Id, row.UserId);
    }

    [Fact]
    public async Task CreditInvoice_HistoricalJournal_CountsOnlyReceivableSettlementLine()
    {
        await using var context = CreateContext(nameof(CreditInvoice_HistoricalJournal_CountsOnlyReceivableSettlementLine));
        var customer = AddUser(context, "Credit Customer", UserRole.Customer);
        var receivable = new Account { Code = "1140000000", Name = "Accounts Receivable", AccountType = AccountType.Asset };
        var sales = new Account { Code = "4110000000", Name = "Sales", AccountType = AccountType.Revenue };
        var cogs = new Account { Code = "5110000000", Name = "COGS", AccountType = AccountType.Expense };
        var inventory = new Account { Code = "1150000000", Name = "Inventory", AccountType = AccountType.Asset };
        context.Set<Account>().AddRange(receivable, sales, cogs, inventory);
        await context.SaveChangesAsync();

        var invoice = new Invoice
        {
            InvoiceNumber = "CR-1",
            InvoiceType = InvoiceType.Sale,
            PaymentType = PaymentType.Credit,
            CustomerId = customer.Id,
            Status = InvoiceStatus.Posted,
            TotalAmount = 1493m
        };
        context.Set<Invoice>().Add(invoice);
        await context.SaveChangesAsync();
        context.Set<JournalEntry>().Add(new JournalEntry
        {
            EntryNumber = "JE-CR-1",
            EntryDate = new DateTime(2026, 8, 3),
            Description = "Credit sale",
            Status = JournalEntryStatus.Posted,
            IsDraft = false,
            ReferenceType = "Invoice",
            ReferenceId = invoice.Id,
            Lines =
            [
                new JournalEntryLine { AccountId = receivable.Id, LineNumber = 1, Debit = 1493m },
                new JournalEntryLine { AccountId = sales.Id, LineNumber = 2, Credit = 1493m },
                new JournalEntryLine { AccountId = cogs.Id, LineNumber = 3, Debit = 500m },
                new JournalEntryLine { AccountId = inventory.Id, LineNumber = 4, Credit = 500m }
            ]
        });
        await context.SaveChangesAsync();

        var summary = await new PartyBalanceReportService(context).GetAsync(new PartyBalanceFilterDto
        {
            Role = UserRole.Customer,
            AsOfDate = new DateTime(2026, 8, 3)
        });
        var statement = await new UserStatementService(context).GetAsync(new UserStatementFilterDto
        {
            UserId = customer.Id,
            From = new DateTime(2026, 8, 1),
            To = new DateTime(2026, 8, 4)
        });

        var summaryRow = Assert.Single(summary.Data!.Rows);
        Assert.Equal(1493m, summaryRow.TotalDebit);
        Assert.Equal(0m, summaryRow.TotalCredit);
        Assert.Equal(-1493m, summaryRow.Balance);
        Assert.Single(statement.Data!.Rows);
        Assert.Equal(-1493m, statement.Data.ClosingBalance);
    }

    [Fact]
    public async Task DebitCardInvoice_HistoricalJournal_DoesNotCreateCustomerDebt()
    {
        await using var context = CreateContext(nameof(DebitCardInvoice_HistoricalJournal_DoesNotCreateCustomerDebt));
        var customer = AddUser(context, "Paid Customer", UserRole.Customer);
        var bank = new Account { Code = "1130000000", Name = "Bank", AccountType = AccountType.Asset };
        var sales = new Account { Code = "4110000000", Name = "Sales", AccountType = AccountType.Revenue };
        context.Set<Account>().AddRange(bank, sales);
        await context.SaveChangesAsync();
        var invoice = new Invoice
        {
            InvoiceNumber = "DB-1", InvoiceType = InvoiceType.Sale, PaymentType = PaymentType.Debit,
            CustomerId = customer.Id, Status = InvoiceStatus.Posted, TotalAmount = 1493m
        };
        context.Set<Invoice>().Add(invoice);
        await context.SaveChangesAsync();
        context.Set<JournalEntry>().Add(new JournalEntry
        {
            EntryNumber = "JE-DB-1", EntryDate = new DateTime(2026, 8, 3), Description = "Paid sale",
            Status = JournalEntryStatus.Posted, ReferenceType = "Invoice", ReferenceId = invoice.Id,
            Lines =
            [
                new JournalEntryLine { AccountId = bank.Id, LineNumber = 1, Debit = 1493m },
                new JournalEntryLine { AccountId = sales.Id, LineNumber = 2, Credit = 1493m }
            ]
        });
        await context.SaveChangesAsync();

        var result = await new PartyBalanceReportService(context).GetAsync(new PartyBalanceFilterDto
        {
            Role = UserRole.Customer, AsOfDate = new DateTime(2026, 8, 3), OutstandingOnly = false
        });

        var row = Assert.Single(result.Data!.Rows);
        Assert.Equal(0m, row.Balance);
        Assert.Null(row.LastMovementDate);
    }

    [Fact]
    public async Task GetAsync_UsesTransactionRelationship_NotProfileRole()
    {
        await using var context = CreateContext(nameof(GetAsync_UsesTransactionRelationship_NotProfileRole));
        var supplierProfile = AddUser(context, "Dual Party", UserRole.Supplier);
        await AddEntryAsync(context, new DateTime(2026, 8, 3), JournalEntryStatus.Posted,
            customerId: supplierProfile.Id, debit: 250m);

        var result = await new PartyBalanceReportService(context).GetAsync(new PartyBalanceFilterDto
        {
            Role = UserRole.Customer,
            AsOfDate = new DateTime(2026, 8, 3),
            OutstandingOnly = false
        });

        var row = Assert.Single(result.Data!.Rows);
        Assert.Equal(supplierProfile.Id, row.UserId);
        Assert.Equal(UserRole.Customer, row.Role);
        Assert.Equal(-250m, row.Balance);
    }
    [Fact]
    public async Task GetCombinedAsync_NetsCustomerAndSupplierMovementsForOneParty()
    {
        await using var context = CreateContext(nameof(GetCombinedAsync_NetsCustomerAndSupplierMovementsForOneParty));
        var party = AddUser(context, "One Party", UserRole.Customer);
        await AddEntryAsync(context, new DateTime(2026, 8, 3), JournalEntryStatus.Posted,
            customerId: party.Id, debit: 50m);
        await AddEntryAsync(context, new DateTime(2026, 8, 3), JournalEntryStatus.Posted,
            supplierId: party.Id, credit: 50m);

        var result = await new PartyBalanceReportService(context).GetCombinedAsync(new PartyBalanceFilterDto
        {
            AsOfDate = new DateTime(2026, 8, 3),
            OutstandingOnly = false
        });

        var row = Assert.Single(result.Data!.Rows);
        Assert.True(row.IsCombined);
        Assert.Equal(50m, row.TotalDebit);
        Assert.Equal(50m, row.TotalCredit);
        Assert.Equal(0m, row.Balance);

        var positive = await new PartyBalanceReportService(context).GetCombinedAsync(new PartyBalanceFilterDto
        {
            AsOfDate = new DateTime(2026, 8, 3),
            OutstandingOnly = false,
            BalanceFilter = "positive"
        });
        Assert.Empty(positive.Data!.Rows);
    }
    private static ApplicationDbContext CreateContext(string name) => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(name).Options);

    private static User AddUser(ApplicationDbContext context, string name, UserRole role, string? phone = null)
    {
        var user = new User { Name = name, Password = "test", Role = role, PhoneNumber = phone };
        context.Set<User>().Add(user);
        context.SaveChanges();
        return user;
    }

    private static async Task AddEntryAsync(
        ApplicationDbContext context,
        DateTime date,
        JournalEntryStatus status,
        int? customerId = null,
        int? supplierId = null,
        decimal debit = 0m,
        decimal credit = 0m)
    {
        context.Set<JournalEntry>().Add(new JournalEntry
        {
            EntryNumber = Guid.NewGuid().ToString("N"),
            EntryDate = date,
            Description = "Test movement",
            Status = status,
            IsDraft = status == JournalEntryStatus.Draft,
            Lines =
            [
                new JournalEntryLine
                {
                    AccountId = 1,
                    LineNumber = 1,
                    CustomerId = customerId,
                    SupplierId = supplierId,
                    Debit = debit,
                    Credit = credit
                }
            ]
        });
        await context.SaveChangesAsync();
    }
}

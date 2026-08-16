using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Reports.Financial.Dtos;

namespace RaccoonWarehouse.Application.Service.Accounting
{
    public class ProfitAndLossService
    {
        private readonly ApplicationDbContext _context;

        public ProfitAndLossService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ProfitAndLossReport> GetReportAsync(
            int fiscalYearId,
            DateTime fromDate,
            DateTime toDate,
            DateTime? compareFromDate = null,
            DateTime? compareToDate = null)
        {
            var from = fromDate.Date;
            var to = toDate.Date;

            var compareFrom = compareFromDate?.Date;
            var compareTo = compareToDate?.Date;
            var hasCompare = compareFrom.HasValue && compareTo.HasValue;

            var balances = await _context.JournalEntryLines
                .AsNoTracking()
                .Include(x => x.Account)
                .Include(x => x.JournalEntry)
                .Where(x => x.JournalEntry.FiscalYearId == fiscalYearId)
                .Where(x => x.JournalEntry.Status == JournalEntryStatus.Posted)
                .Where(x =>
                    (x.JournalEntry.EntryDate.Date >= from && x.JournalEntry.EntryDate.Date <= to) ||
                    (hasCompare && x.JournalEntry.EntryDate.Date >= compareFrom!.Value && x.JournalEntry.EntryDate.Date <= compareTo!.Value))
                .Where(x =>
                    x.Account.AccountTypeCode == "PL" ||
                    x.Account.AccountType == AccountType.Revenue ||
                    x.Account.AccountType == AccountType.Expense)
                .GroupBy(x => new
                {
                    x.AccountId,
                    x.Account.Code,
                    x.Account.Name,
                    x.Account.AccountNature
                })
                .Select(g => new
                {
                    g.Key.AccountId,
                    AccountCode = g.Key.Code,
                    AccountName = g.Key.Name,
                    Nature = g.Key.AccountNature,
                    CurrentDebit = g.Where(x => x.JournalEntry.EntryDate.Date >= from && x.JournalEntry.EntryDate.Date <= to).Sum(x => x.Debit),
                    CurrentCredit = g.Where(x => x.JournalEntry.EntryDate.Date >= from && x.JournalEntry.EntryDate.Date <= to).Sum(x => x.Credit),
                    CompareDebit = hasCompare
                        ? g.Where(x => x.JournalEntry.EntryDate.Date >= compareFrom!.Value && x.JournalEntry.EntryDate.Date <= compareTo!.Value).Sum(x => x.Debit)
                        : 0m,
                    CompareCredit = hasCompare
                        ? g.Where(x => x.JournalEntry.EntryDate.Date >= compareFrom!.Value && x.JournalEntry.EntryDate.Date <= compareTo!.Value).Sum(x => x.Credit)
                        : 0m
                })
                .ToListAsync();

            var revenue = new List<ReportLine>();
            var cogs = new List<ReportLine>();
            var operatingExpenses = new List<ReportLine>();
            var otherIncome = new List<ReportLine>();
            var otherExpenses = new List<ReportLine>();

            foreach (var row in balances)
            {
                var isCreditNature = string.Equals(row.Nature, "Credit", StringComparison.OrdinalIgnoreCase);

                var currentAmount = isCreditNature
                    ? row.CurrentCredit - row.CurrentDebit
                    : row.CurrentDebit - row.CurrentCredit;

                var compareAmount = isCreditNature
                    ? row.CompareCredit - row.CompareDebit
                    : row.CompareDebit - row.CompareCredit;

                var line = new ReportLine
                {
                    AccountId = row.AccountId,
                    AccountCode = row.AccountCode ?? string.Empty,
                    AccountName = row.AccountName ?? string.Empty,
                    CurrentPeriod = currentAmount,
                    ComparePeriod = compareAmount,
                    Variance = currentAmount - compareAmount
                };

                if (isCreditNature)
                {
                    if ((row.AccountCode ?? string.Empty).StartsWith("0000000004", StringComparison.Ordinal))
                        revenue.Add(line);
                    else
                        otherIncome.Add(line);
                }
                else
                {
                    var accountCode = row.AccountCode ?? string.Empty;
                    if (accountCode.StartsWith("0000000005.0000000001.0000000001", StringComparison.Ordinal) || accountCode.StartsWith("0000000005.0000000001.0000000002", StringComparison.Ordinal))
                        cogs.Add(line);
                    else if (accountCode.StartsWith("0000000005.0000000001.0000000003", StringComparison.Ordinal) || accountCode.StartsWith("0000000005.0000000001.0000000004", StringComparison.Ordinal))
                        operatingExpenses.Add(line);
                    else if (accountCode.StartsWith("0000000005", StringComparison.Ordinal))
                        otherExpenses.Add(line);
                    else
                        operatingExpenses.Add(line);
                }
            }

            var revenueCurrent = revenue.Sum(x => x.CurrentPeriod);
            var revenueCompare = revenue.Sum(x => x.ComparePeriod);
            var cogsCurrent = cogs.Sum(x => x.CurrentPeriod);
            var cogsCompare = cogs.Sum(x => x.ComparePeriod);
            var operatingExpensesCurrent = operatingExpenses.Sum(x => x.CurrentPeriod);
            var operatingExpensesCompare = operatingExpenses.Sum(x => x.ComparePeriod);
            var otherIncomeCurrent = otherIncome.Sum(x => x.CurrentPeriod);
            var otherIncomeCompare = otherIncome.Sum(x => x.ComparePeriod);
            var otherExpensesCurrent = otherExpenses.Sum(x => x.CurrentPeriod);
            var otherExpensesCompare = otherExpenses.Sum(x => x.ComparePeriod);

            var grossProfitCurrent = revenueCurrent - cogsCurrent;
            var grossProfitCompare = revenueCompare - cogsCompare;

            var operatingProfitCurrent = grossProfitCurrent - operatingExpensesCurrent;
            var operatingProfitCompare = grossProfitCompare - operatingExpensesCompare;

            var netProfitCurrent = operatingProfitCurrent + otherIncomeCurrent - otherExpensesCurrent;
            var netProfitCompare = operatingProfitCompare + otherIncomeCompare - otherExpensesCompare;

            return new ProfitAndLossReport
            {
                Revenue = revenue.OrderBy(x => x.AccountCode).ToList(),
                CostOfGoodsSold = cogs.OrderBy(x => x.AccountCode).ToList(),
                GrossProfit = new ReportSummaryLine
                {
                    Name = "Gross Profit",
                    CurrentPeriod = grossProfitCurrent,
                    ComparePeriod = grossProfitCompare,
                    Variance = grossProfitCurrent - grossProfitCompare
                },
                OperatingExpenses = operatingExpenses.OrderBy(x => x.AccountCode).ToList(),
                OperatingProfit = new ReportSummaryLine
                {
                    Name = "Operating Profit",
                    CurrentPeriod = operatingProfitCurrent,
                    ComparePeriod = operatingProfitCompare,
                    Variance = operatingProfitCurrent - operatingProfitCompare
                },
                OtherIncome = otherIncome.OrderBy(x => x.AccountCode).ToList(),
                OtherExpenses = otherExpenses.OrderBy(x => x.AccountCode).ToList(),
                NetProfit = new ReportSummaryLine
                {
                    Name = "Net Profit",
                    CurrentPeriod = netProfitCurrent,
                    ComparePeriod = netProfitCompare,
                    Variance = netProfitCurrent - netProfitCompare
                }
            };
        }
    }
}

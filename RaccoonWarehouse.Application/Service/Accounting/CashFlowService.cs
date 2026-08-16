using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.Accounts;
using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Reports.Financial.Dtos;

namespace RaccoonWarehouse.Application.Service.Accounting
{
    public class CashFlowService
    {
        private readonly ApplicationDbContext _context;
        private readonly ProfitAndLossService _profitAndLossService;

        public CashFlowService(ApplicationDbContext context, ProfitAndLossService profitAndLossService)
        {
            _context = context;
            _profitAndLossService = profitAndLossService;
        }

        public async Task<CashFlowReport> GetReportAsync(int fiscalYearId, DateTime fromDate, DateTime toDate)
        {
            var from = fromDate.Date;
            var to = toDate.Date;
            var openingCutoff = from.AddDays(-1);

            var pnl = await _profitAndLossService.GetReportAsync(fiscalYearId, from, to);
            var netIncome = pnl.NetProfit.CurrentPeriod;

            var allAccounts = await _context.Set<Account>()
                .AsNoTracking()
                .ToListAsync();

            var periodBalances = await GetBalancesAsync(fiscalYearId, from, to);
            var openingBalances = await GetBalancesAsync(fiscalYearId, DateTime.MinValue, openingCutoff);

            var adjustments = BuildAdjustments(allAccounts, periodBalances);
            var workingCapital = BuildWorkingCapitalChanges(allAccounts, openingBalances, periodBalances);
            var investing = BuildTaggedSection(allAccounts, periodBalances, CashFlowCategory.Investing, "Investing");
            var financing = BuildTaggedSection(allAccounts, periodBalances, CashFlowCategory.Financing, "Financing");

            var totalAdjustments = adjustments.Sum(x => x.Amount);
            var totalWorkingCapital = workingCapital.Sum(x => x.Amount);
            var totalInvesting = investing.Sum(x => x.Amount);
            var totalFinancing = financing.Sum(x => x.Amount);

            var cashFromOperating = netIncome + totalAdjustments + totalWorkingCapital;
            var netChange = cashFromOperating + totalInvesting + totalFinancing;

            var cashAccounts = allAccounts.Where(IsCashAccount).ToList();
            var openingCash = SumSectionBalance(cashAccounts, openingBalances);
            var closingCash = openingCash + netChange;

            return new CashFlowReport
            {
                NetIncome = new CashFlowSectionLine { Name = "Net Income", Amount = netIncome },
                Adjustments = adjustments,
                WorkingCapitalChanges = workingCapital,
                CashFromOperatingActivities = new CashFlowSectionLine { Name = "Cash From Operating Activities", Amount = cashFromOperating },
                CashFromInvestingActivities = investing,
                CashFromFinancingActivities = financing,
                NetChangeInCash = new CashFlowSectionLine { Name = "Net Change in Cash", Amount = netChange },
                OpeningPlusNetEqualsClosing = new CashFlowSectionLine
                {
                    Name = "Opening Cash + Net Change = Closing Cash",
                    Amount = closingCash
                },
                OpeningCash = openingCash,
                ClosingCash = closingCash
            };
        }

        private async Task<Dictionary<int, decimal>> GetBalancesAsync(int fiscalYearId, DateTime from, DateTime to)
        {
            if (to < from)
                return new Dictionary<int, decimal>();

            return await _context.JournalEntryLines
                .AsNoTracking()
                .Where(x => x.JournalEntry.FiscalYearId == fiscalYearId)
                .Where(x => x.JournalEntry.Status == JournalEntryStatus.Posted)
                .Where(x => x.JournalEntry.EntryDate.Date >= from && x.JournalEntry.EntryDate.Date <= to)
                .GroupBy(x => x.AccountId)
                .Select(g => new
                {
                    AccountId = g.Key,
                    Debit = g.Sum(x => x.Debit),
                    Credit = g.Sum(x => x.Credit)
                })
                .ToDictionaryAsync(x => x.AccountId, x => x.Debit - x.Credit);
        }

        private static List<CashFlowSectionLine> BuildAdjustments(List<Account> accounts, Dictionary<int, decimal> periodBalances)
        {
            var lines = new List<CashFlowSectionLine>();

            var operatingTagged = accounts
                .Where(x => x.CashFlowCategory == CashFlowCategory.Operating)
                .Where(x => !IsWorkingCapitalAccount(x))
                .Where(x => x.AccountTypeCode != "PL" && x.AccountType != AccountType.Revenue && x.AccountType != AccountType.Expense)
                .ToList();

            foreach (var account in operatingTagged)
            {
                var amount = GetSignedBalance(account, periodBalances);
                if (amount == 0m)
                    continue;

                lines.Add(new CashFlowSectionLine
                {
                    Name = $"{account.Code} - {account.Name}",
                    Amount = amount
                });
            }

            return lines.OrderBy(x => x.Name).ToList();
        }

        private static List<CashFlowSectionLine> BuildWorkingCapitalChanges(
            List<Account> accounts,
            Dictionary<int, decimal> openingBalances,
            Dictionary<int, decimal> periodBalances)
        {
            var lines = new List<CashFlowSectionLine>();
            var workingAccounts = accounts.Where(IsWorkingCapitalAccount).ToList();

            foreach (var account in workingAccounts)
            {
                var open = GetSignedBalance(account, openingBalances);
                var movement = GetSignedBalance(account, periodBalances);
                var close = open + movement;
                var change = close - open;

                var cashImpact = account.AccountType switch
                {
                    AccountType.Asset => -change,
                    AccountType.Liability => change,
                    _ => -change
                };

                if (cashImpact == 0m)
                    continue;

                lines.Add(new CashFlowSectionLine
                {
                    Name = $"{account.Code} - {account.Name}",
                    Amount = cashImpact
                });
            }

            return lines.OrderBy(x => x.Name).ToList();
        }

        private static List<CashFlowSectionLine> BuildTaggedSection(
            List<Account> accounts,
            Dictionary<int, decimal> periodBalances,
            CashFlowCategory category,
            string label)
        {
            var lines = new List<CashFlowSectionLine>();

            foreach (var account in accounts.Where(x => x.CashFlowCategory == category))
            {
                var amount = GetSignedBalance(account, periodBalances);
                if (amount == 0m)
                    continue;

                lines.Add(new CashFlowSectionLine
                {
                    Name = $"{label}: {account.Code} - {account.Name}",
                    Amount = amount
                });
            }

            return lines.OrderBy(x => x.Name).ToList();
        }

        private static decimal SumSectionBalance(IEnumerable<Account> accounts, Dictionary<int, decimal> balances)
        {
            return accounts.Sum(x => GetSignedBalance(x, balances));
        }

        private static decimal GetSignedBalance(Account account, Dictionary<int, decimal> balances)
        {
            if (!balances.TryGetValue(account.Id, out var raw))
                return 0m;

            var isCreditNature = string.Equals(account.AccountNature, "Credit", StringComparison.OrdinalIgnoreCase);
            return isCreditNature ? -raw : raw;
        }

        private static bool IsWorkingCapitalAccount(Account account)
        {
            var code = account.Code ?? string.Empty;
            var name = account.Name ?? string.Empty;

            if (code.StartsWith("0000000001.0000000001.0000000004", StringComparison.Ordinal) || name.Contains("Receivable", StringComparison.OrdinalIgnoreCase) || name.Contains("ذمم مدينة", StringComparison.OrdinalIgnoreCase))
                return true;
            if (code.StartsWith("0000000002.0000000001.0000000001", StringComparison.Ordinal) || name.Contains("Payable", StringComparison.OrdinalIgnoreCase) || name.Contains("ذمم دائنة", StringComparison.OrdinalIgnoreCase))
                return true;
            if (code.StartsWith("0000000001.0000000001.0000000005", StringComparison.Ordinal) || name.Contains("Inventory", StringComparison.OrdinalIgnoreCase) || name.Contains("مخزون", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private static bool IsCashAccount(Account account)
        {
            var code = account.Code ?? string.Empty;
            var name = account.Name ?? string.Empty;
            return code.StartsWith("0000000001.0000000001.0000000001", StringComparison.Ordinal)
                || code.StartsWith("0000000001.0000000001.0000000002", StringComparison.Ordinal)
                || code.StartsWith("0000000001.0000000001.0000000003", StringComparison.Ordinal)
                || name.Contains("Cash", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Bank", StringComparison.OrdinalIgnoreCase)
                || name.Contains("صندوق", StringComparison.OrdinalIgnoreCase)
                || name.Contains("بنك", StringComparison.OrdinalIgnoreCase);
        }
    }
}

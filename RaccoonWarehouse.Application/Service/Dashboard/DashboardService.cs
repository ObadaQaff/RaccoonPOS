using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Reports.Dashboard;

namespace RaccoonWarehouse.Application.Service.Dashboard
{
    public class DashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardSummary> GetSummaryAsync()
        {
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddTicks(-1);

            var postedLines = _context.JournalEntryLines
                .AsNoTracking()
                .Where(x => x.JournalEntry.Status == JournalEntryStatus.Posted);

            var monthLines = postedLines.Where(x => x.JournalEntry.EntryDate >= monthStart && x.JournalEntry.EntryDate <= monthEnd);

            var monthRevenueRaw = await monthLines
                .Where(x => x.Account.AccountType == AccountType.Revenue)
                .SumAsync(x => x.Credit - x.Debit);

            var monthExpenseRaw = await monthLines
                .Where(x => x.Account.AccountType == AccountType.Expense)
                .SumAsync(x => x.Debit - x.Credit);

            var currentMonthRevenue = monthRevenueRaw;
            var currentMonthExpenses = monthExpenseRaw;
            var netProfit = currentMonthRevenue - currentMonthExpenses;
            var profitMarginPct = currentMonthRevenue == 0m ? 0m : Math.Round((netProfit / currentMonthRevenue) * 100m, 2);

            var asOfLines = postedLines.Where(x => x.JournalEntry.EntryDate <= today);

            var totalAssets = await asOfLines
                .Where(x => x.Account.AccountType == AccountType.Asset)
                .SumAsync(x => x.Debit - x.Credit);

            var totalLiabilities = await asOfLines
                .Where(x => x.Account.AccountType == AccountType.Liability)
                .SumAsync(x => x.Credit - x.Debit);

            var totalEquity = await asOfLines
                .Where(x => x.Account.AccountType == AccountType.Equity)
                .SumAsync(x => x.Credit - x.Debit);

            var currentAssets = await asOfLines
                .Where(x => x.Account.AccountType == AccountType.Asset && (x.Account.Level <= 3 || x.Account.Code.StartsWith("11")))
                .SumAsync(x => x.Debit - x.Credit);

            var inventoryBalance = await asOfLines
                .Where(x => x.Account.Code.StartsWith("1105") || x.Account.Name.Contains("Inventory") || x.Account.Name.Contains("مخزون"))
                .SumAsync(x => x.Debit - x.Credit);

            var currentLiabilities = await asOfLines
                .Where(x => x.Account.AccountType == AccountType.Liability && (x.Account.Level <= 3 || x.Account.Code.StartsWith("21")))
                .SumAsync(x => x.Credit - x.Debit);

            var quickAssets = currentAssets - inventoryBalance;
            var currentRatio = currentLiabilities == 0m ? 0m : Math.Round(currentAssets / currentLiabilities, 2);
            var quickRatio = currentLiabilities == 0m ? 0m : Math.Round(quickAssets / currentLiabilities, 2);
            var debtToEquity = totalEquity == 0m ? 0m : Math.Round(totalLiabilities / totalEquity, 2);

            var totalOutstandingAr = await GetOutstandingAsync(isReceivable: true);
            var totalOutstandingAp = await GetOutstandingAsync(isReceivable: false);

            var cashAndBankBalance = await asOfLines
                .Where(x =>
                    x.Account.Code.StartsWith("1101") ||
                    x.Account.Code.StartsWith("1102") ||
                    x.Account.Code.StartsWith("1103") ||
                    x.Account.Name.Contains("Cash") ||
                    x.Account.Name.Contains("Bank") ||
                    x.Account.Name.Contains("صندوق") ||
                    x.Account.Name.Contains("بنك"))
                .SumAsync(x => x.Debit - x.Credit);

            return new DashboardSummary
            {
                CurrentMonthRevenue = currentMonthRevenue,
                CurrentMonthExpenses = currentMonthExpenses,
                NetProfit = netProfit,
                ProfitMarginPct = profitMarginPct,
                TotalAssets = totalAssets,
                TotalLiabilities = totalLiabilities,
                Equity = totalEquity,
                CurrentRatio = currentRatio,
                QuickRatio = quickRatio,
                DebtToEquityRatio = debtToEquity,
                TotalOutstandingAR = totalOutstandingAr,
                TotalOutstandingAP = totalOutstandingAp,
                CashAndBankBalance = cashAndBankBalance
            };
        }

        private async Task<decimal> GetOutstandingAsync(bool isReceivable)
        {
            var invoices = await _context.Set<Domain.Invoices.Invoice>()
                .AsNoTracking()
                .Where(x => x.PaymentType == PaymentType.Credit)
                .Where(x => isReceivable ? x.InvoiceType == InvoiceType.Sale : x.InvoiceType == InvoiceType.Purchase)
                .Select(x => new { x.Id, x.TotalAmount })
                .ToListAsync();

            if (!invoices.Any())
                return 0m;

            var invoiceIds = invoices.Select(x => x.Id).ToList();
            var sourceType = isReceivable ? FinancialSourceType.SaleInvoice : FinancialSourceType.PurchaseInvoice;
            var direction = isReceivable ? TransactionDirection.In : TransactionDirection.Out;

            var paid = await _context.Set<Domain.FinancialTransactions.FinancialTransaction>()
                .AsNoTracking()
                .Where(x => x.SourceId.HasValue && invoiceIds.Contains(x.SourceId.Value))
                .Where(x => x.SourceType == sourceType)
                .Where(x => x.Direction == direction)
                .Where(x => x.Status == FinancialTransactionStatus.Posted)
                .GroupBy(x => x.SourceId!.Value)
                .Select(g => new { InvoiceId = g.Key, Amount = g.Sum(x => Math.Abs(x.Amount)) })
                .ToDictionaryAsync(x => x.InvoiceId, x => x.Amount);

            return invoices.Sum(x =>
            {
                var collected = paid.TryGetValue(x.Id, out var value) ? value : 0m;
                var outstanding = x.TotalAmount - collected;
                return outstanding > 0m ? outstanding : 0m;
            });
        }
    }
}

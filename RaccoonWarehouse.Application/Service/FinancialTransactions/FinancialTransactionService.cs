using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Generic;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.FinancialTransactions;
using RaccoonWarehouse.Domain.FinancialTransactions.DTOs;
using RaccoonWarehouse.Domain.InvoiceLines;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Reports.Financial.Dtos;
using RaccoonWarehouse.Domain.Reports.Financial.Filters;
using RaccoonWarehouse.Domain.Reports.Sales.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Application.Service.FinancialTransactions
{
    public class FinancialTransactionService : GenericService<FinancialTransaction, FinancialTransactionWriteDto, FinancialTransactionReadDto>,
                                                      IFinancialTransactionService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        private IUserSession _userSession;

        public FinancialTransactionService(ApplicationDbContext context, IUOW uow, IMapper mapper, IUserSession userSession) : base(context, uow, mapper)
        {
            _userSession = userSession;
            _uow = uow;
            _mapper = mapper;
        }
        public async Task<Result<FinancialTransactionReadDto>> PostAsync(FinancialPostDto dto)
        {
            // 1) Validations
            if (dto.Amount <= 0)
                return Result<FinancialTransactionReadDto>.Fail("Amount must be greater than zero.");
            /*
                        if (dto.SourceType != FinancialSourceType.Manual && dto.SourceId is null)
                            return Result<FinancialTransactionReadDto>.Fail("SourceId is required for non-manual transactions.");
            */
            // إذا الكاش هو اللي يأثر على الصندوق: لازم يكون فيه Session
            if (dto.Method == PaymentMethod.Cash && dto.CashierSessionId is null)
                return Result<FinancialTransactionReadDto>.Fail("CashierSessionId is required for cash transactions.");

            // 2) Build Entity (لا تعتمد على Generic CreateAsync)
            var entity = _mapper.Map<FinancialTransaction>(dto);

            entity.TransactionNumber = GenerateTransactionNumber(dto); // اعملها برا
            entity.Status = FinancialTransactionStatus.Posted;

            var jordanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, jordanTimeZone);

            entity.CreatedDate = now;
            entity.UpdatedDate = now;

            // 3) Save
            var repo = _uow.GetRepository<FinancialTransaction>();
            await repo.AddAsync(entity);
            await _uow.CommitAsync();

            var readDto = _mapper.Map<FinancialTransactionReadDto>(entity);
            return Result<FinancialTransactionReadDto>.Ok(readDto, "Transaction posted successfully.");
        }

        public async Task<Result> VoidAsync(int transactionId, string reason)
        {
            var repo = _uow.GetRepository<FinancialTransaction>();
            var entity = await repo.GetByIdAsync(transactionId);

            if (entity == null)
                return Result.Fail("Transaction not found.");

            if (entity.Status == FinancialTransactionStatus.Voided)
                return Result.Fail("Transaction already voided.");

            entity.Status = FinancialTransactionStatus.Voided;
            entity.Notes = string.IsNullOrWhiteSpace(entity.Notes)
                ? $"VOID: {reason}"
                : $"{entity.Notes}\nVOID: {reason}";

            var jordanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");
            entity.UpdatedDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, jordanTimeZone);

            await repo.UpdateAsync(entity);
            await _uow.CommitAsync();

            return Result.Ok("Transaction voided successfully.");
        }



        public async Task<Result> VoidBySourceAsync(
    FinancialSourceType sourceType,
    int sourceId,
    string reason)
        {
            var repo = _uow.GetRepository<FinancialTransaction>();

            var transactions = repo
                .GetAllAsQueryable()
                .Where(x =>
                    x.SourceType == sourceType &&
                    x.SourceId == sourceId &&
                    x.Status == FinancialTransactionStatus.Posted)
                .ToList();

            if (!transactions.Any())
                return Result.Ok("No transactions found to void.");

            foreach (var entity in transactions)
            {
                entity.Status = FinancialTransactionStatus.Voided;

                entity.Notes = string.IsNullOrWhiteSpace(entity.Notes)
                    ? $"VOID: {reason}"
                    : $"{entity.Notes}\nVOID: {reason}";

                var jordanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");
                entity.UpdatedDate =
                    TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, jordanTimeZone);

                await repo.UpdateAsync(entity);
            }

            await _uow.CommitAsync();

            return Result.Ok("Transactions voided successfully.");
        }

        private string GenerateTransactionNumber(FinancialPostDto dto)
        {
            // مثال: حسب الاتجاه + المصدر + وقت
            var prefix = dto.Direction == TransactionDirection.In ? "IN" : "OUT";
            return $"{prefix}-{dto.SourceType}-{DateTime.Now:yyyyMMdd-HHmmss}";
        }


        public async Task<decimal> GetExpectedCashForSessionAsync(int cashierSessionId)
        {
            var repo = _uow.GetRepository<FinancialTransaction>();

            var transactions = repo
                .GetAllAsQueryable()
                .Where(t =>
                    t.CashierSessionId == cashierSessionId &&
                    t.Method == PaymentMethod.Cash &&
                    t.Status == FinancialTransactionStatus.Posted);

            var expected = transactions
                .Sum(t => t.Direction == TransactionDirection.In
                    ? t.Amount
                    : -t.Amount);

            return await Task.FromResult(expected);
        }



        // ===================== REPORTS =====================
        public async Task<Result<(FinancialSummaryDto summary, List<SalesReportRowDto> rows)>>
     GetSalesReportAsync(FinancialSummaryFilterDto filter, InvoiceType? type = null)
        {
            if (filter.From > filter.To)
                return Result<(FinancialSummaryDto, List<SalesReportRowDto>)>.Fail("Invalid date range.");

            var invoiceRepo = _uow.GetRepository<Invoice>();
            var lineRepo = _uow.GetRepository<InvoiceLine>();

            var invoicesQ = invoiceRepo.GetAllAsQueryable()
                .Where(x => x.CreatedDate >= filter.From && x.CreatedDate <= filter.To);

            if (filter.CustomerId.HasValue)
                invoicesQ = invoicesQ.Where(x => x.CustomerId == filter.CustomerId.Value);

            if (type.HasValue)
                invoicesQ = invoicesQ.Where(x => x.InvoiceType == type.Value);

            // ✅ rows
            var invoiceIds = await invoicesQ.Select(x => x.Id).ToListAsync();

            var lines = await lineRepo.GetAllAsQueryable()
                .Where(l => invoiceIds.Contains(l.InvoiceId))
                .ToListAsync();

            var linesByInvoice = lines.GroupBy(l => l.InvoiceId)
                .ToDictionary(g => g.Key, g => new
                {
                    Cogs = g.Sum(x => x.Quantity * x.UnitCost),
                    SubTotal = g.Sum(x => x.LineSubTotal),
                    Tax = g.Sum(x => x.TaxAmount)
                });

            var invoices = await invoicesQ
                .Include(x => x.User) // customer
                .ToListAsync();

            var rows = invoices.Select(inv =>
            {
                var discount = inv.DiscountAmount ?? 0m;

                linesByInvoice.TryGetValue(inv.Id, out var agg);
                var subTotal = agg?.SubTotal ?? inv.SubTotal;     // prefer lines, fallback invoice
                var tax = agg?.Tax ?? inv.TotalTax;
                var cogs = agg?.Cogs ?? inv.TotalCOGS;

                var total = subTotal - discount + tax;
                var profit = (subTotal - discount) - cogs;

                return new SalesReportRowDto
                {
                    InvoiceId = inv.Id,
                    InvoiceNumber = inv.InvoiceNumber,
                    Date = inv.CreatedDate,
                    CustomerName = inv.User?.Name ?? "—",

                    SubTotal = subTotal,
                    TotalTax = tax,
                    Discount = discount,
                    Total = total,

                    Cogs = cogs,
                    Profit = profit,

                    InvoiceType = inv.InvoiceType.ToString(),
                    PaymentMethod = inv.PaymentType?.ToString() ?? "—",
                    Status = inv.Status?.ToString() ?? "—"
                };
            }).OrderByDescending(r => r.Date).ToList();

            // ✅ summary
            var totalSales = rows.Where(r => r.InvoiceType == InvoiceType.Sale.ToString()).Sum(r => r.SubTotal);
            var totalTax = rows.Where(r => r.InvoiceType == InvoiceType.Sale.ToString()).Sum(r => r.TotalTax);
            var totalDiscounts = rows.Where(r => r.InvoiceType == InvoiceType.Sale.ToString()).Sum(r => r.Discount);

            var totalReturns = filter.IncludeReturns
                ? rows.Where(r => r.InvoiceType == InvoiceType.Return.ToString()).Sum(r => r.SubTotal)
                : 0m;

            var netSales = (totalSales - totalReturns) - totalDiscounts; // قبل الضريبة
            var totalCogs = rows.Where(r => r.InvoiceType == InvoiceType.Sale.ToString()).Sum(r => r.Cogs);
            var grossProfit = netSales - totalCogs;
            var margin = netSales == 0 ? 0 : Math.Round((grossProfit / netSales) * 100m, 2);

            var countInvoices = rows.Count(r => r.InvoiceType == InvoiceType.Sale.ToString());
            var avg = countInvoices == 0 ? 0 : Math.Round(netSales / countInvoices, 2);

            var summary = new FinancialSummaryDto
            {
                TotalSales = totalSales,
                TotalTax = totalTax,
                TotalDiscounts = totalDiscounts,
                TotalReturns = totalReturns,
                NetSales = netSales,

                TotalCOGS = totalCogs,
                GrossProfit = grossProfit,
                GrossProfitMargin = margin,

                NumberOfInvoices = countInvoices,
                AverageInvoiceValue = avg
            };

            return Result<(FinancialSummaryDto, List<SalesReportRowDto>)>.Ok((summary, rows));
        }



        public async Task<(CashFlowSummaryDto summary, List<CashFlowRowDto> rows)> GetCashFlowAsync(CashFlowFilterDto filter)
        {
            if (filter.From > filter.To)
                throw new ArgumentException("Invalid date range");

            var from = filter.From.Date;
            var to = filter.To.Date.AddDays(1).AddTicks(-1);

            var repo = _uow.GetRepository<FinancialTransaction>();

            var q = repo.GetAllAsQueryable()
                .Where(x => x.TransactionDate >= from && x.TransactionDate <= to);

            // ✅ ignore voided unless included
            if (!filter.IncludeVoided)
                q = q.Where(x => x.Status != FinancialTransactionStatus.Voided); // عدّل اسم الحقل إذا عندك مختلف

            if (filter.CashierId.HasValue)
                q = q.Where(x => x.CashierId == filter.CashierId.Value);

            if (filter.CashierSessionId.HasValue)
                q = q.Where(x => x.CashierSessionId == filter.CashierSessionId.Value);

            if (filter.Method.HasValue)
                q = q.Where(x => x.Method == filter.Method.Value);

            if (filter.Direction.HasValue)
                q = q.Where(x => x.Direction == filter.Direction.Value);

            if (filter.SourceType.HasValue)
                q = q.Where(x => x.SourceType == filter.SourceType.Value);

            // Includes (اختياري) لو عندك علاقات للكاشير
            q = q.Include(x => x.Cashier);

            var list = await q.OrderByDescending(x => x.TransactionDate).ToListAsync();

            var rows = list.Select(x =>
            {
                var amount = x.Amount;

                // ✅ normalize:
                // - لو Amount عندك دائمًا موجب: اتجاه يحدد In/Out
                // - لو Amount عندك ممكن سالب: نعدلها
                var abs = Math.Abs(amount);

                var inAmount = x.Direction == TransactionDirection.In ? abs : 0m;
                var outAmount = x.Direction == TransactionDirection.Out ? abs : 0m;

                return new CashFlowRowDto
                {
                    Id = x.Id,
                    Date = x.TransactionDate,
                    Direction = x.Direction,
                    Method = x.Method,

                    AmountIn = inAmount,
                    AmountOut = outAmount,

                    SourceType = x.SourceType,
                    SourceId = x.SourceId,

                    CashierName = x.Cashier?.Name ?? "—",
                    Notes = x.Notes,
                    IsVoided = x.Status == FinancialTransactionStatus.Voided
                };
            }).ToList();

            var summary = new CashFlowSummaryDto
            {
                TotalIn = rows.Sum(r => r.AmountIn),
                TotalOut = rows.Sum(r => r.AmountOut),

                CountIn = rows.Count(r => r.AmountIn > 0),
                CountOut = rows.Count(r => r.AmountOut > 0),

                CashNet = rows.Where(r => r.Method == PaymentMethod.Cash).Sum(r => r.Net),
                VisaNet = rows.Where(r => r.Method == PaymentMethod.Visa).Sum(r => r.Net),
            };

            return (summary, rows);
        }
    


    public async Task<(ProfitLossSummaryDto summary, List<ProfitLossRowDto> rows)> GetProfitLossAsync(ProfitLossFilterDto filter)
        {
            if (filter.From > filter.To)
                throw new ArgumentException("Invalid date range");

            var from = filter.From.Date;
            var to = filter.To.Date.AddDays(1).AddTicks(-1);

            // =========================
            // 1) Revenue/COGS from Invoices
            // =========================
            var invoiceRepo = _uow.GetRepository<Invoice>();

            var invQ = invoiceRepo.GetAllAsQueryable()
                .Where(x => x.CreatedDate >= from && x.CreatedDate <= to);

            if (filter.CashierId.HasValue)
                invQ = invQ.Where(x => x.CasherId == filter.CashierId.Value);

            if (filter.CashierSessionId.HasValue)
                invQ = invQ.Where(x => x.CashierSessionId == filter.CashierSessionId.Value);

            // Only sale/return invoices affect P&L (adjust if you have other types)
            var salesQ = invQ.Where(x => x.InvoiceType == InvoiceType.Sale);
            var returnsQ = invQ.Where(x => x.InvoiceType == InvoiceType.Return);

            var totalSales = await salesQ.SumAsync(x => (decimal?)x.SubTotal) ?? 0m;
            var totalDiscounts = await salesQ.SumAsync(x => (decimal?)(x.DiscountAmount ?? 0m)) ?? 0m;

            var totalReturns = 0m;
            if (filter.IncludeReturns)
                totalReturns = await returnsQ.SumAsync(x => (decimal?)x.SubTotal) ?? 0m;

            // ✅ NetSales rule (قبل الضريبة)
            var netSales = (totalSales - totalReturns) - totalDiscounts;

            // COGS (نحسبها من Invoice.TotalCOGS لو موجودة ومخزنة)
            var totalCogs = await salesQ.SumAsync(x => (decimal?)x.TotalCOGS) ?? 0m;

            var grossProfit = netSales - totalCogs;

            // =========================
            // 2) Expenses from FinancialTransactions (OUT)
            // =========================
            var ftRepo = _uow.GetRepository<FinancialTransaction>();

            var ftQ = ftRepo.GetAllAsQueryable()
                .Where(x => x.TransactionDate >= from && x.TransactionDate <= to)
                .Where(x => x.Direction == TransactionDirection.Out);

            if (!filter.IncludeVoidedTransactions)
                ftQ = ftQ.Where(x => x.Status != FinancialTransactionStatus.Voided); // عدّل اسم الحقل إذا مختلف

            if (filter.CashierId.HasValue)
                ftQ = ftQ.Where(x => x.CashierId == filter.CashierId.Value);

            if (filter.CashierSessionId.HasValue)
                ftQ = ftQ.Where(x => x.CashierSessionId == filter.CashierSessionId.Value);

            var expensesList = await ftQ.ToListAsync();

            // Normalize expense amounts (لو عندك سالب/موجب)
            decimal totalExpenses = expensesList.Sum(x => Math.Abs(x.Amount));

            var netProfit = grossProfit - totalExpenses;

            decimal grossMargin = netSales == 0 ? 0 : Math.Round((grossProfit / netSales) * 100m, 2);
            decimal netMargin = netSales == 0 ? 0 : Math.Round((netProfit / netSales) * 100m, 2);

            var summary = new ProfitLossSummaryDto
            {
                TotalSales = totalSales,
                TotalReturns = totalReturns,
                TotalDiscounts = totalDiscounts,
                NetSales = netSales,

                TotalCOGS = totalCogs,
                GrossProfit = grossProfit,

                TotalExpenses = totalExpenses,
                NetProfit = netProfit,

                GrossMarginPercent = grossMargin,
                NetMarginPercent = netMargin
            };

            // =========================
            // 3) Rows breakdown
            // =========================
            var rows = new List<ProfitLossRowDto>
            {
                new ProfitLossRowDto{ Section="Revenue", Item="Sales (SubTotal)", Amount=totalSales },
                new ProfitLossRowDto{ Section="Revenue", Item="Returns", Amount= -totalReturns },
                new ProfitLossRowDto{ Section="Revenue", Item="Discounts", Amount= -totalDiscounts },
                new ProfitLossRowDto{ Section="Revenue", Item="Net Sales", Amount= netSales },

                new ProfitLossRowDto{ Section="COGS", Item="COGS", Amount= -totalCogs },
                new ProfitLossRowDto{ Section="COGS", Item="Gross Profit", Amount= grossProfit },
            };

            // Expenses breakdown by SourceType
            var bySource = expensesList
                .GroupBy(x => x.SourceType)
                .Select(g => new
                {
                    SourceType = g.Key.ToString(),
                    Amount = g.Sum(x => Math.Abs(x.Amount))
                })
                .OrderByDescending(x => x.Amount)
                .ToList();

            foreach (var s in bySource)
            {
                rows.Add(new ProfitLossRowDto
                {
                    Section = "Expenses",
                    Item = s.SourceType,
                    Amount = -s.Amount
                });
            }

            rows.Add(new ProfitLossRowDto { Section = "Expenses", Item = "Total Expenses", Amount = -totalExpenses });
            rows.Add(new ProfitLossRowDto { Section = "Other", Item = "Net Profit", Amount = netProfit });

            return (summary, rows);
        }
    }
}


    public interface IFinancialTransactionService : IGenericService<FinancialTransaction, FinancialTransactionWriteDto, FinancialTransactionReadDto>
    {
            Task<Result<FinancialTransactionReadDto>> PostAsync(FinancialPostDto dto);
            Task<Result> VoidAsync(int transactionId, string reason);
            Task<Result> VoidBySourceAsync(
                FinancialSourceType sourceType,
                int sourceId,
                string reason);

            Task<decimal> GetExpectedCashForSessionAsync(int cashierSessionId);
            Task<Result<(FinancialSummaryDto summary, List<SalesReportRowDto> rows)>>
                GetSalesReportAsync(FinancialSummaryFilterDto filter, InvoiceType? type = null);

            Task<(CashFlowSummaryDto summary, List<CashFlowRowDto> rows)> GetCashFlowAsync(CashFlowFilterDto filter);


            Task<(ProfitLossSummaryDto summary, List<ProfitLossRowDto> rows)> GetProfitLossAsync(ProfitLossFilterDto filter);


}



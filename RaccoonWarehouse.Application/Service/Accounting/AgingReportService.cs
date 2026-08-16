using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Reports.Financial.Dtos;
using RaccoonWarehouse.Domain.Reports.Financial.Enums;

namespace RaccoonWarehouse.Application.Service.Accounting
{
    public class AgingReportService
    {
        private readonly ApplicationDbContext _context;

        public AgingReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<AgingRow>> GetAgingReportAsync(AgingType type, DateTime asOfDate)
        {
            var asOf = asOfDate.Date;

            var invoicesQuery = _context.Set<Domain.Invoices.Invoice>()
                .AsNoTracking()
                .Where(x => x.PaymentType == PaymentType.Credit);

            if (type == AgingType.AccountsReceivable)
            {
                invoicesQuery = invoicesQuery.Where(x => x.InvoiceType == InvoiceType.Sale && x.CustomerId.HasValue);
            }
            else
            {
                invoicesQuery = invoicesQuery.Where(x => x.InvoiceType == InvoiceType.Purchase && x.SupplierId.HasValue);
            }

            var invoices = await invoicesQuery
                .Select(x => new
                {
                    x.Id,
                    PartyId = type == AgingType.AccountsReceivable ? x.CustomerId : x.SupplierId,
                    InvoiceDate = x.DocumentDate ?? x.CreatedDate,
                    DueDate = x.DueDate,
                    x.TotalAmount
                })
                .ToListAsync();

            if (!invoices.Any())
                return new List<AgingRow>();

            var invoiceIds = invoices.Select(x => x.Id).ToList();

            var paidByInvoice = await _context.Set<Domain.FinancialTransactions.FinancialTransaction>()
                .AsNoTracking()
                .Where(x => x.SourceId.HasValue && invoiceIds.Contains(x.SourceId.Value))
                .Where(x => x.Status == FinancialTransactionStatus.Posted)
                .Where(x =>
                    type == AgingType.AccountsReceivable
                        ? (x.SourceType == FinancialSourceType.SaleInvoice || x.SourceType == FinancialSourceType.PosSaleInvoice) && x.Direction == TransactionDirection.In
                        : x.SourceType == FinancialSourceType.PurchaseInvoice && x.Direction == TransactionDirection.Out)
                .GroupBy(x => x.SourceId!.Value)
                .Select(g => new
                {
                    InvoiceId = g.Key,
                    Paid = g.Sum(x => Math.Abs(x.Amount))
                })
                .ToDictionaryAsync(x => x.InvoiceId, x => x.Paid);

            var partyIds = invoices
                .Where(x => x.PartyId.HasValue)
                .Select(x => x.PartyId!.Value)
                .Distinct()
                .ToList();

            var partyNames = await _context.Set<Domain.Users.User>()
                .AsNoTracking()
                .Where(x => partyIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name);

            var rowsByParty = new Dictionary<int, AgingRow>();

            foreach (var invoice in invoices)
            {
                if (!invoice.PartyId.HasValue)
                    continue;

                var paid = paidByInvoice.TryGetValue(invoice.Id, out var paidAmount) ? paidAmount : 0m;
                var outstanding = invoice.TotalAmount - paid;
                if (outstanding <= 0m)
                    continue;

                var dueDate = (invoice.DueDate ?? invoice.InvoiceDate.AddDays(30)).Date;
                var age = (asOf - dueDate).Days;

                var partyId = invoice.PartyId.Value;
                if (!rowsByParty.TryGetValue(partyId, out var row))
                {
                    row = new AgingRow
                    {
                        PartyId = partyId,
                        PartyName = partyNames.TryGetValue(partyId, out var name) ? name : "—"
                    };
                    rowsByParty[partyId] = row;
                }

                if (age <= 0)
                    row.Current += outstanding;
                else if (age <= 30)
                    row.Days1to30 += outstanding;
                else if (age <= 60)
                    row.Days31to60 += outstanding;
                else if (age <= 90)
                    row.Days61to90 += outstanding;
                else
                    row.Over90 += outstanding;
            }

            foreach (var row in rowsByParty.Values)
            {
                row.Total = row.Current + row.Days1to30 + row.Days31to60 + row.Days61to90 + row.Over90;
            }

            return rowsByParty.Values
                .OrderBy(x => x.PartyName)
                .ToList();
        }
    }
}

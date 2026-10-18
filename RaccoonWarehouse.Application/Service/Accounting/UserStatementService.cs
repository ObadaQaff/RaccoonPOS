using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.JournalEntries;
using RaccoonWarehouse.Domain.Accounting.Accounts;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.PurchaseOrders;
using RaccoonWarehouse.Domain.Reports.Accounting.Dtos;
using RaccoonWarehouse.Domain.Reports.Accounting.Filters;
using RaccoonWarehouse.Domain.Users;

namespace RaccoonWarehouse.Application.Service.Accounting
{
    public class UserStatementService
    {
        private readonly ApplicationDbContext _context;

        public UserStatementService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<decimal> GetCurrentBalanceAsync(int userId)
        {
            var result = await GetAsync(new UserStatementFilterDto
            {
                UserId = userId,
                From = DateTime.SpecifyKind(new DateTime(1900, 1, 1), DateTimeKind.Unspecified),
                To = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1)
            });

            return result.Success && result.Data != null ? result.Data.ClosingBalance : 0m;
        }

        public async Task<Result> ValidateCreditLimitAsync(int userId, decimal additionalAmount)
        {
            if (additionalAmount <= 0m)
                return Result.Ok();

            var user = await _context.Set<User>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null)
                return Result.Fail("User was not found.");

            if (user.Role != UserRole.Customer)
                return Result.Ok();

            if (user.CreditStatus is CreditStatus.Blocked or CreditStatus.Suspended)
                return Result.Fail("The customer credit account is blocked.");

            var limit = user.CreditLimit;
            if (limit <= 0m)
                return Result.Ok();

            var currentBalance = await GetCurrentBalanceAsync(userId);
            var currentDebt = Math.Max(0m, -currentBalance);
            var projectedBalance = currentDebt + additionalAmount;

            if (projectedBalance > limit)
            {
                return Result.Fail($"The customer credit limit was exceeded. Current balance: {currentBalance:N2}, limit: {limit:N2}, projected: {projectedBalance:N2}.");
            }

            return Result.Ok();
        }

        public async Task<Result<UserStatementReportDto>> GetAsync(UserStatementFilterDto filter)
        {
            if (filter.UserId <= 0)
                return Result<UserStatementReportDto>.Fail("User id is required.");

            if (filter.From > filter.To)
                return Result<UserStatementReportDto>.Fail("Invalid date range.");

            var user = await _context.Set<User>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == filter.UserId);

            if (user == null)
                return Result<UserStatementReportDto>.Fail("User was not found.");

            var statementRole = filter.Role ?? user.Role;
            var customerControlAccountId = await GetPartyControlAccountIdAsync(UserRole.Customer);
            var supplierControlAccountId = await GetPartyControlAccountIdAsync(UserRole.Supplier);

            var baseQuery = _context.Set<JournalEntryLine>()
                .AsNoTracking()
                .Include(x => x.JournalEntry)
                .Where(x => x.JournalEntry.Status == Domain.Accounting.Enums.JournalEntryStatus.Posted);

            baseQuery = baseQuery.Where(x => filter.IncludeAllPartyRelationships
                ? x.CustomerId == filter.UserId || x.SupplierId == filter.UserId || x.PartyUserId == filter.UserId ||
                  (x.CustomerId == null && x.SupplierId == null && x.PartyUserId == null &&
                   x.JournalEntry.ReferenceType == "Invoice" &&
                   x.JournalEntry.ReferenceId.HasValue &&
                   ((x.AccountId == customerControlAccountId &&
                     _context.Set<Invoice>().Any(invoice =>
                         invoice.Id == x.JournalEntry.ReferenceId.Value &&
                         invoice.PaymentType == PaymentType.Credit &&
                         invoice.CustomerId == filter.UserId)) ||
                    (x.AccountId == supplierControlAccountId &&
                     _context.Set<Invoice>().Any(invoice =>
                         invoice.Id == x.JournalEntry.ReferenceId.Value &&
                         invoice.PaymentType == PaymentType.Credit &&
                         invoice.SupplierId == filter.UserId))))
                : statementRole == UserRole.Customer
                ? x.CustomerId == filter.UserId || x.PartyUserId == filter.UserId ||
                  (x.CustomerId == null && x.SupplierId == null && x.PartyUserId == null &&
                   x.AccountId == customerControlAccountId &&
                   x.JournalEntry.ReferenceType == "Invoice" &&
                   x.JournalEntry.ReferenceId.HasValue &&
                   _context.Set<Invoice>().Any(invoice =>
                       invoice.Id == x.JournalEntry.ReferenceId.Value &&
                       invoice.PaymentType == PaymentType.Credit &&
                       invoice.CustomerId == filter.UserId))
                : x.SupplierId == filter.UserId || x.PartyUserId == filter.UserId ||
                  (x.CustomerId == null && x.SupplierId == null && x.PartyUserId == null &&
                   x.AccountId == supplierControlAccountId &&
                   x.JournalEntry.ReferenceType == "Invoice" &&
                   x.JournalEntry.ReferenceId.HasValue &&
                   _context.Set<Invoice>().Any(invoice =>
                       invoice.Id == x.JournalEntry.ReferenceId.Value &&
                       invoice.PaymentType == PaymentType.Credit &&
                       invoice.SupplierId == filter.UserId)));

            var openingBalance = await baseQuery
                .Where(x => x.JournalEntry.EntryDate < filter.From)
                .SumAsync(x => x.Credit - x.Debit);

            var rows = await baseQuery
                .Where(x => x.JournalEntry.EntryDate >= filter.From && x.JournalEntry.EntryDate <= filter.To)
                .OrderBy(x => x.JournalEntry.EntryDate)
                .ThenBy(x => x.JournalEntry.Id)
                .ThenBy(x => x.LineNumber)
                .ThenBy(x => x.Id)
                .Select(x => new
                {
                    x.JournalEntry.EntryDate,
                    x.JournalEntry.EntryNumber,
                    x.JournalEntry.ReferenceNumber,
                    x.JournalEntry.ReferenceType,
                    x.JournalEntry.ReferenceId,
                    EntryDescription = x.JournalEntry.Description,
                    LineDescription = x.Description,
                    x.Debit,
                    x.Credit
                })
                .ToListAsync();

            // Paid invoices usually have no customer/supplier id on their cash or
            // bank settlement line. Keep the accounting balance based on party
            // journal lines, but add a zero-balance activity row for every related
            // invoice that has a posted journal entry.
            var postedInvoiceReferences = await _context.Set<JournalEntry>()
                .AsNoTracking()
                .Where(x => x.Status == Domain.Accounting.Enums.JournalEntryStatus.Posted
                    && x.ReferenceType == "Invoice"
                    && x.ReferenceId.HasValue
                    && x.EntryDate >= filter.From
                    && x.EntryDate <= filter.To)
                .Select(x => new { InvoiceId = x.ReferenceId!.Value, x.EntryNumber, x.EntryDate })
                .ToListAsync();

            var postedInvoiceIds = postedInvoiceReferences
                .Select(x => x.InvoiceId)
                .Distinct()
                .ToList();

            var relatedInvoices = postedInvoiceIds.Count == 0
                ? new List<Invoice>()
                : await _context.Set<Invoice>()
                    .AsNoTracking()
                    .Where(invoice => postedInvoiceIds.Contains(invoice.Id)
                        && (filter.IncludeAllPartyRelationships
                            ? invoice.CustomerId == filter.UserId || invoice.SupplierId == filter.UserId
                            : statementRole == UserRole.Customer
                                ? invoice.CustomerId == filter.UserId
                                : invoice.SupplierId == filter.UserId))
                    .ToListAsync();

            var invoiceById = relatedInvoices.ToDictionary(x => x.Id);
            var invoiceReferenceById = postedInvoiceReferences
                .GroupBy(x => x.InvoiceId)
                .ToDictionary(x => x.Key, x => x.OrderBy(y => y.EntryDate).First());

            var resultRows = new List<UserStatementRowDto>();
            var totalTransactionAmount = relatedInvoices.Sum(invoice => Math.Abs(invoice.TotalAmount));
            var totalPurchaseOrderAmount = 0m;

            foreach (var row in rows)
            {
                invoiceById.TryGetValue(row.ReferenceType?.Equals("Invoice", StringComparison.OrdinalIgnoreCase) == true
                    ? row.ReferenceId ?? 0
                    : 0, out var relatedInvoice);

                resultRows.Add(new UserStatementRowDto
                {
                    EntryDate = row.EntryDate,
                    EntryNumber = row.EntryNumber,
                    Description = AccountingTextLocalizer.ToArabic(string.IsNullOrWhiteSpace(row.LineDescription) ? row.EntryDescription : row.LineDescription!),
                    Reference = string.IsNullOrWhiteSpace(row.ReferenceNumber)
                        ? AccountingTextLocalizer.ReferenceLabel(row.ReferenceType, row.ReferenceId)
                        : AccountingTextLocalizer.ToArabic(row.ReferenceNumber),
                    ReferenceType = row.ReferenceType,
                    ReferenceId = row.ReferenceId,
                    Debit = row.Debit,
                    Credit = row.Credit,
                    DocumentAmount = relatedInvoice == null ? null : Math.Abs(relatedInvoice.TotalAmount),
                    PaymentMethod = relatedInvoice?.PaymentType?.ToString(),
                    DocumentType = relatedInvoice?.InvoiceType.ToString()
                });
            }

            var representedInvoiceIds = resultRows
                .Where(x => x.ReferenceType?.Equals("Invoice", StringComparison.OrdinalIgnoreCase) == true)
                .Select(x => x.ReferenceId)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToHashSet();

            foreach (var invoice in relatedInvoices.Where(x => !representedInvoiceIds.Contains(x.Id)))
            {
                if (!invoiceReferenceById.TryGetValue(invoice.Id, out var journalReference))
                    continue;

                resultRows.Add(new UserStatementRowDto
                {
                    EntryDate = journalReference.EntryDate,
                    EntryNumber = journalReference.EntryNumber,
                    Description = $"Posted {invoice.InvoiceType} invoice activity #{invoice.InvoiceNumber}",
                    Reference = string.IsNullOrWhiteSpace(invoice.ReferenceNumber)
                        ? invoice.InvoiceNumber
                        : invoice.ReferenceNumber!,
                    ReferenceType = "Invoice",
                    ReferenceId = invoice.Id,
                    DocumentAmount = Math.Abs(invoice.TotalAmount),
                    PaymentMethod = invoice.PaymentType?.ToString(),
                    DocumentType = invoice.InvoiceType.ToString(),
                    IsDocumentSummary = true
                });
            }

            // Purchase orders are operational commitments, not accounting entries.
            // Show them in the supplier statement as zero-balance activity so the
            // supplier history is complete without changing receivables/payables.
            if (statementRole == UserRole.Supplier || filter.IncludeAllPartyRelationships)
            {
                var purchaseOrders = await _context.Set<PurchaseOrder>()
                    .AsNoTracking()
                    .Where(order => order.SupplierId == filter.UserId
                        && (order.DocumentDate ?? order.CreatedDate) >= filter.From
                        && (order.DocumentDate ?? order.CreatedDate) <= filter.To)
                    .ToListAsync();

                totalPurchaseOrderAmount = purchaseOrders.Sum(order => Math.Abs(order.TotalAmount));

                foreach (var order in purchaseOrders)
                {
                    var orderDate = order.DocumentDate ?? order.CreatedDate;
                    resultRows.Add(new UserStatementRowDto
                    {
                        EntryDate = orderDate,
                        EntryNumber = order.OrderNumber,
                        Description = $"Purchase order activity #{order.OrderNumber} ({order.Status})",
                        Reference = order.OrderNumber,
                        ReferenceType = "PurchaseOrder",
                        ReferenceId = order.Id,
                        DocumentAmount = Math.Abs(order.TotalAmount),
                        PaymentMethod = order.PaymentType?.ToString(),
                        DocumentType = $"PurchaseOrder ({order.Status})",
                        IsDocumentSummary = true
                    });
                }
            }

            resultRows = resultRows
                .OrderBy(x => x.EntryDate)
                .ThenBy(x => x.EntryNumber)
                .ThenBy(x => x.ReferenceId ?? 0)
                .ToList();

            var runningBalance = openingBalance;
            var totalDebit = 0m;
            var totalCredit = 0m;
            foreach (var resultRow in resultRows)
            {
                runningBalance += resultRow.Credit - resultRow.Debit;
                totalDebit += resultRow.Debit;
                totalCredit += resultRow.Credit;
                resultRow.RunningBalance = runningBalance;
            }

            var report = new UserStatementReportDto
            {
                UserId = user.Id,
                UserName = user.Name,
                Role = user.Role,
                OpeningBalance = openingBalance,
                TotalDebit = totalDebit,
                TotalCredit = totalCredit,
                ClosingBalance = runningBalance,
                TotalTransactionAmount = totalTransactionAmount,
                TotalPurchaseOrderAmount = totalPurchaseOrderAmount,
                Rows = resultRows
            };

            return Result<UserStatementReportDto>.Ok(report);
        }

        private async Task<int> GetPartyControlAccountIdAsync(UserRole role)
        {
            var isSupplier = role == UserRole.Supplier;
            var settingKey = isSupplier
                ? AccountingService.AccountsPayableAccountCodeKey
                : AccountingService.AccountsReceivableAccountCodeKey;
            var defaultCode = isSupplier ? "2110000000" : "1140000000";
            var legacyCode = isSupplier ? "2101" : "1104";
            var configuredCode = await _context.AppSettings
                .AsNoTracking()
                .Where(x => x.Key == settingKey)
                .Select(x => x.Value)
                .FirstOrDefaultAsync();

            var candidates = new[] { configuredCode?.Trim(), defaultCode, legacyCode }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            return await _context.Set<Account>()
                .AsNoTracking()
                .Where(x => candidates.Contains(x.Code))
                .OrderBy(x => x.Code == configuredCode ? 0 : x.Code == defaultCode ? 1 : 2)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.JournalEntries;
using RaccoonWarehouse.Domain.Accounting.Accounts;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Reports.Accounting.Dtos;
using RaccoonWarehouse.Domain.Reports.Accounting.Filters;
using RaccoonWarehouse.Domain.Users;

namespace RaccoonWarehouse.Application.Service.Accounting
{
    public class PartyBalanceReportService
    {
        private readonly ApplicationDbContext _context;

        public PartyBalanceReportService(ApplicationDbContext context) => _context = context;

        public async Task<Result<PartyBalanceReportDto>> GetAsync(PartyBalanceFilterDto filter)
        {
            if (filter.Role is not UserRole.Customer and not UserRole.Supplier)
                return Result<PartyBalanceReportDto>.Fail("Only customers and suppliers are supported.");
            if (filter.AsOfDate == default)
                return Result<PartyBalanceReportDto>.Fail("As-of date is required.");

            var cutoff = filter.AsOfDate.Date.AddDays(1);
            var controlAccountId = await GetPartyControlAccountIdAsync(filter.Role);
            var users = _context.Set<User>().AsNoTracking();
            var search = filter.Search?.Trim();
            if (!string.IsNullOrWhiteSpace(search))
                users = users.Where(x => x.Name.Contains(search) || (x.PhoneNumber != null && x.PhoneNumber.Contains(search)));

            var parties = await users
                .Select(x => new { x.Id, x.Name, x.PhoneNumber })
                .ToListAsync();
            var partyIds = parties.Select(x => x.Id).ToList();

            var movements = await PostedLines(cutoff)
                .Select(line => new
                {
                    UserId = filter.Role == UserRole.Customer
                        ? line.CustomerId ?? line.PartyUserId ?? _context.Set<Invoice>()
                            .Where(invoice => line.CustomerId == null && line.SupplierId == null && line.PartyUserId == null &&
                                              line.AccountId == controlAccountId &&
                                              line.JournalEntry.ReferenceType == "Invoice" &&
                                              line.JournalEntry.ReferenceId.HasValue &&
                                              invoice.Id == line.JournalEntry.ReferenceId.Value &&
                                              invoice.PaymentType == PaymentType.Credit)
                            .Select(invoice => invoice.CustomerId)
                            .FirstOrDefault()
                        : line.SupplierId ?? line.PartyUserId ?? _context.Set<Invoice>()
                            .Where(invoice => line.CustomerId == null && line.SupplierId == null && line.PartyUserId == null &&
                                              line.AccountId == controlAccountId &&
                                              line.JournalEntry.ReferenceType == "Invoice" &&
                                              line.JournalEntry.ReferenceId.HasValue &&
                                              invoice.Id == line.JournalEntry.ReferenceId.Value &&
                                              invoice.PaymentType == PaymentType.Credit)
                            .Select(invoice => invoice.SupplierId)
                            .FirstOrDefault(),
                    line.Debit,
                    line.Credit,
                    line.JournalEntry.EntryDate
                })
                .Where(x => x.UserId.HasValue && partyIds.Contains(x.UserId.Value))
                .GroupBy(x => x.UserId!.Value)
                .Select(group => new
                {
                    UserId = group.Key,
                    TotalDebit = group.Sum(x => x.Debit),
                    TotalCredit = group.Sum(x => x.Credit),
                    LastMovementDate = (DateTime?)group.Max(x => x.EntryDate)
                })
                .ToDictionaryAsync(x => x.UserId);

            var rows = parties.Select(party =>
            {
                movements.TryGetValue(party.Id, out var movement);
                var debit = movement?.TotalDebit ?? 0m;
                var credit = movement?.TotalCredit ?? 0m;
                return new PartyBalanceRowDto
                {
                    UserId = party.Id,
                    Name = party.Name,
                    PhoneNumber = party.PhoneNumber,
                    TotalDebit = debit,
                    TotalCredit = credit,
                    Balance = filter.Role == UserRole.Supplier ? credit - debit : debit - credit,
                    LastMovementDate = movement?.LastMovementDate
                };
            }).ToList();

            if (filter.OutstandingOnly)
                rows = rows.Where(x => x.Balance > 0m).ToList();

            rows = rows.OrderByDescending(x => x.Balance).ThenBy(x => x.Name).ToList();
            return Result<PartyBalanceReportDto>.Ok(new PartyBalanceReportDto
            {
                Role = filter.Role,
                AsOfDate = filter.AsOfDate.Date,
                Rows = rows,
                OutstandingCount = rows.Count(x => x.Balance > 0m),
                TotalOutstanding = rows.Where(x => x.Balance > 0m).Sum(x => x.Balance)
            });
        }

        private IQueryable<JournalEntryLine> PostedLines(DateTime cutoff) => _context.Set<JournalEntryLine>()
            .Where(line => line.JournalEntry.Status == Domain.Accounting.Enums.JournalEntryStatus.Posted && line.JournalEntry.EntryDate < cutoff);

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

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

            // The report role describes the ledger relationship (CustomerId/SupplierId), not the profile role.
            // A party may buy and sell, so do not exclude users based on their profile role.
            var parties = await users
                .Select(x => new { x.Id, x.Name, x.PhoneNumber, x.Role })
                .ToListAsync();
            var partyIds = parties.Select(x => x.Id).ToList();

            var movementRows = await GetPartyMovementsAsync(filter.Role, cutoff, controlAccountId, partyIds);
            var movements = movementRows
                .GroupBy(x => x.UserId!.Value)
                .Select(group => new
                {
                    UserId = group.Key,
                    TotalDebit = group.Sum(x => x.Debit),
                    TotalCredit = group.Sum(x => x.Credit),
                    LastMovementDate = (DateTime?)group.Max(x => x.EntryDate)
                })
                .ToDictionary(x => x.UserId);

            var rows = parties
                .Where(party => party.Role == filter.Role || movements.ContainsKey(party.Id))
                .Select(party =>
            {
                movements.TryGetValue(party.Id, out var movement);
                var debit = movement?.TotalDebit ?? 0m;
                var credit = movement?.TotalCredit ?? 0m;
                return new PartyBalanceRowDto
                {
                    Role = filter.Role,
                    UserId = party.Id,
                    Name = party.Name,
                    PhoneNumber = party.PhoneNumber,
                    TotalDebit = debit,
                    TotalCredit = credit,
                    Balance = credit - debit,
                    LastMovementDate = movement?.LastMovementDate
                };
            }).ToList();

            if (filter.OutstandingOnly)
                rows = rows.Where(x => filter.Role == UserRole.Supplier ? x.Balance > 0m : x.Balance < 0m).ToList();

            rows = filter.BalanceFilter?.Trim().ToLowerInvariant() switch
            {
                "debit" or "negative" => rows.Where(x => x.Balance < 0m).ToList(),
                "credit" or "positive" => rows.Where(x => x.Balance > 0m).ToList(),
                "zero" => rows.Where(x => x.Balance == 0m).ToList(),
                _ => rows
            };

            rows = rows.OrderByDescending(x => x.Balance).ThenBy(x => x.Name).ToList();
            return Result<PartyBalanceReportDto>.Ok(new PartyBalanceReportDto
            {
                Role = filter.Role,
                AsOfDate = filter.AsOfDate.Date,
                Rows = rows,
                OutstandingCount = rows.Count(x => filter.Role == UserRole.Supplier ? x.Balance > 0m : x.Balance < 0m),
                TotalOutstanding = rows.Where(x => filter.Role == UserRole.Supplier ? x.Balance > 0m : x.Balance < 0m).Sum(x => filter.Role == UserRole.Supplier ? x.Balance : -x.Balance)
            });
        }

        public async Task<Result<PartyBalanceReportDto>> GetCombinedAsync(PartyBalanceFilterDto filter)
        {
            if (filter.AsOfDate == default)
                return Result<PartyBalanceReportDto>.Fail("As-of date is required.");

            var customerResult = await GetAsync(new PartyBalanceFilterDto
            {
                Role = UserRole.Customer,
                AsOfDate = filter.AsOfDate,
                Search = filter.Search,
                OutstandingOnly = false,
                BalanceFilter = "all"
            });
            var supplierResult = await GetAsync(new PartyBalanceFilterDto
            {
                Role = UserRole.Supplier,
                AsOfDate = filter.AsOfDate,
                Search = filter.Search,
                OutstandingOnly = false,
                BalanceFilter = "all"
            });

            if (!customerResult.Success || customerResult.Data == null)
                return Result<PartyBalanceReportDto>.Fail(customerResult.Message ?? "Failed to load customer balances.");
            if (!supplierResult.Success || supplierResult.Data == null)
                return Result<PartyBalanceReportDto>.Fail(supplierResult.Message ?? "Failed to load supplier balances.");

            var parties = customerResult.Data.Rows
                .Concat(supplierResult.Data.Rows)
                .GroupBy(x => new { x.UserId, x.Name, x.PhoneNumber });
            var partyIds = parties.Select(x => x.Key.UserId).ToList();
            var cutoff = filter.AsOfDate.Date.AddDays(1);
            var customerMovements = await GetPartyMovementsAsync(
                UserRole.Customer, cutoff, await GetPartyControlAccountIdAsync(UserRole.Customer), partyIds);
            var supplierMovements = await GetPartyMovementsAsync(
                UserRole.Supplier, cutoff, await GetPartyControlAccountIdAsync(UserRole.Supplier), partyIds);

            // A journal line can carry both customer and supplier links (or a shared party link).
            // Deduplicate by journal-line/user before combining the two relationship views.
            var combinedMovements = customerMovements
                .Concat(supplierMovements)
                .GroupBy(x => new { x.UserId, x.LineId })
                .Select(group => group.First())
                .GroupBy(x => x.UserId!.Value)
                .ToDictionary(group => group.Key, group => new
                {
                    TotalDebit = group.Sum(x => x.Debit),
                    TotalCredit = group.Sum(x => x.Credit),
                    LastMovementDate = (DateTime?)group.Max(x => x.EntryDate)
                });

            var rows = parties
                .Select(group =>
                {
                    combinedMovements.TryGetValue(group.Key.UserId, out var movement);
                    var debit = movement?.TotalDebit ?? 0m;
                    var credit = movement?.TotalCredit ?? 0m;
                    return new PartyBalanceRowDto
                    {
                    Role = UserRole.Customer,
                    IsCombined = true,
                    UserId = group.Key.UserId,
                    Name = group.Key.Name,
                    PhoneNumber = group.Key.PhoneNumber,
                    TotalDebit = debit,
                    TotalCredit = credit,
                    Balance = credit - debit,
                    LastMovementDate = movement?.LastMovementDate
                    };
                })
                .ToList();

            rows = filter.OutstandingOnly
                ? rows.Where(x => x.Balance != 0m).ToList()
                : rows;
            rows = filter.BalanceFilter?.Trim().ToLowerInvariant() switch
            {
                "debit" or "negative" => rows.Where(x => x.Balance < 0m).ToList(),
                "credit" or "positive" => rows.Where(x => x.Balance > 0m).ToList(),
                "zero" => rows.Where(x => x.Balance == 0m).ToList(),
                _ => rows
            };
            rows = rows.OrderByDescending(x => x.Balance).ThenBy(x => x.Name).ToList();

            return Result<PartyBalanceReportDto>.Ok(new PartyBalanceReportDto
            {
                Role = UserRole.Customer,
                AsOfDate = filter.AsOfDate.Date,
                Rows = rows,
                OutstandingCount = rows.Count(x => x.Balance != 0m),
                TotalOutstanding = rows.Where(x => x.Balance != 0m).Sum(x => Math.Abs(x.Balance))
            });
        }
        private IQueryable<JournalEntryLine> PostedLines(DateTime cutoff) => _context.Set<JournalEntryLine>()
            .Where(line => line.JournalEntry.Status == Domain.Accounting.Enums.JournalEntryStatus.Posted && line.JournalEntry.EntryDate < cutoff);

        private async Task<List<PartyMovement>> GetPartyMovementsAsync(
            UserRole role,
            DateTime cutoff,
            int controlAccountId,
            IReadOnlyCollection<int> partyIds)
        {
            return await PostedLines(cutoff)
                .Select(line => new PartyMovement
                {
                    LineId = line.Id,
                    UserId = role == UserRole.Customer
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
                    Debit = line.Debit,
                    Credit = line.Credit,
                    EntryDate = line.JournalEntry.EntryDate
                })
                .Where(x => x.UserId.HasValue && partyIds.Contains(x.UserId.Value))
                .ToListAsync();
        }

        private sealed class PartyMovement
        {
            public int LineId { get; set; }
            public int? UserId { get; set; }
            public decimal Debit { get; set; }
            public decimal Credit { get; set; }
            public DateTime EntryDate { get; set; }
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

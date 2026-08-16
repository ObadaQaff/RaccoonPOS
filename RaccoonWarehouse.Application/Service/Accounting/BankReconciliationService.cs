using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.Banks;
using RaccoonWarehouse.Domain.Accounting.Banks.DTOs;

namespace RaccoonWarehouse.Application.Service.Accounting
{
    public class BankReconciliationService
    {
        private readonly ApplicationDbContext _context;

        public BankReconciliationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task ImportStatementAsync(int bankAccountId, List<StatementLineDto> lines)
        {
            if (lines == null || lines.Count == 0)
                throw new InvalidOperationException("Statement lines are required.");

            var bankAccount = await _context.Set<BankAccount>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == bankAccountId && x.IsActive);

            if (bankAccount == null)
                throw new InvalidOperationException("Bank account was not found.");

            var statementDate = lines.Max(x => x.TransactionDate).Date;

            var statement = new BankStatement
            {
                BankAccountId = bankAccountId,
                StatementDate = statementDate,
                OpeningBalance = 0m,
                ClosingBalance = lines.Sum(x => x.Amount),
                ImportedAt = DateTime.UtcNow
            };

            _context.Set<BankStatement>().Add(statement);
            await _context.SaveChangesAsync();

            var entities = lines.Select(x => new BankStatementLine
            {
                BankStatementId = statement.Id,
                TransactionDate = x.TransactionDate,
                Description = x.Description,
                Amount = x.Amount,
                Reference = x.Reference,
                IsReconciled = false
            }).ToList();

            await _context.Set<BankStatementLine>().AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }

        public async Task AutoMatchAsync(int bankAccountId, DateTime statementDate)
        {
            var statement = await _context.Set<BankStatement>()
                .Include(x => x.BankAccount)
                .FirstOrDefaultAsync(x => x.BankAccountId == bankAccountId && x.StatementDate.Date == statementDate.Date);

            if (statement == null)
                throw new InvalidOperationException("Statement not found.");

            var pendingLines = await _context.Set<BankStatementLine>()
                .Where(x => x.BankStatementId == statement.Id && !x.IsReconciled)
                .OrderBy(x => x.TransactionDate)
                .ToListAsync();

            if (pendingLines.Count == 0)
                return;

            var matchedJournalLineIds = await _context.Set<BankStatementLine>()
                .Where(x => x.MatchedJournalEntryLineId.HasValue)
                .Select(x => x.MatchedJournalEntryLineId!.Value)
                .Distinct()
                .ToListAsync();

            foreach (var line in pendingLines)
            {
                var fromDate = line.TransactionDate.Date.AddDays(-3);
                var toDate = line.TransactionDate.Date.AddDays(3);

                var candidate = await _context.JournalEntryLines
                    .Include(x => x.JournalEntry)
                    .Where(x => x.AccountId == statement.BankAccount.GlAccountId)
                    .Where(x => !matchedJournalLineIds.Contains(x.Id))
                    .Where(x => x.JournalEntry.EntryDate.Date >= fromDate && x.JournalEntry.EntryDate.Date <= toDate)
                    .Where(x => (x.Debit - x.Credit) == line.Amount || (x.Credit - x.Debit) == line.Amount)
                    .OrderBy(x => x.JournalEntry.EntryDate)
                    .FirstOrDefaultAsync();

                if (candidate == null)
                    continue;

                line.IsReconciled = true;
                line.MatchedJournalEntryLineId = candidate.Id;
                matchedJournalLineIds.Add(candidate.Id);
            }

            await _context.SaveChangesAsync();
        }

        public async Task ManualMatchAsync(int statementLineId, int journalEntryLineId)
        {
            var statementLine = await _context.Set<BankStatementLine>()
                .Include(x => x.BankStatement)
                .ThenInclude(x => x.BankAccount)
                .FirstOrDefaultAsync(x => x.Id == statementLineId);

            if (statementLine == null)
                throw new InvalidOperationException("Statement line was not found.");

            var alreadyMatched = await _context.Set<BankStatementLine>()
                .AnyAsync(x => x.MatchedJournalEntryLineId == journalEntryLineId && x.Id != statementLineId);
            if (alreadyMatched)
                throw new InvalidOperationException("Journal entry line is already matched.");

            var journalLine = await _context.JournalEntryLines
                .FirstOrDefaultAsync(x => x.Id == journalEntryLineId);
            if (journalLine == null)
                throw new InvalidOperationException("Journal entry line was not found.");

            if (journalLine.AccountId != statementLine.BankStatement.BankAccount.GlAccountId)
                throw new InvalidOperationException("Journal entry line account does not match bank GL account.");

            statementLine.MatchedJournalEntryLineId = journalEntryLineId;
            statementLine.IsReconciled = true;
            await _context.SaveChangesAsync();
        }

        public async Task UnmatchAsync(int statementLineId)
        {
            var statementLine = await _context.Set<BankStatementLine>()
                .FirstOrDefaultAsync(x => x.Id == statementLineId);

            if (statementLine == null)
                throw new InvalidOperationException("Statement line was not found.");

            statementLine.MatchedJournalEntryLineId = null;
            statementLine.IsReconciled = false;
            await _context.SaveChangesAsync();
        }

        public async Task<BankReconciliationSummaryDto> GetSummaryAsync(int bankAccountId, DateTime statementDate)
        {
            var statement = await _context.Set<BankStatement>()
                .FirstOrDefaultAsync(x => x.BankAccountId == bankAccountId && x.StatementDate.Date == statementDate.Date);

            if (statement == null)
                throw new InvalidOperationException("Statement not found.");

            var lines = await _context.Set<BankStatementLine>()
                .Where(x => x.BankStatementId == statement.Id)
                .ToListAsync();

            var matched = lines.Where(x => x.IsReconciled).Sum(x => Math.Abs(x.Amount));
            var unmatched = lines.Where(x => !x.IsReconciled).Sum(x => Math.Abs(x.Amount));

            return new BankReconciliationSummaryDto
            {
                Matched = matched,
                Unmatched = unmatched,
                Difference = unmatched
            };
        }
    }
}

using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.JournalEntries.DTOs;
using RaccoonWarehouse.Domain.Accounting.RecurringJournals;
using RaccoonWarehouse.Domain.Accounting.RecurringJournals.DTOs;
using RaccoonWarehouse.Domain.Accounting.RecurringJournals.Enums;

namespace RaccoonWarehouse.Application.Service.Accounting
{
    public class RecurringJournalService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAccountingService _accountingService;

        public RecurringJournalService(ApplicationDbContext context, IAccountingService accountingService)
        {
            _context = context;
            _accountingService = accountingService;
        }

        public async Task<RecurringJournal> CreateAsync(RecurringJournalUpsertDto dto)
        {
            ValidateDto(dto);

            var entity = new RecurringJournal
            {
                Name = dto.Name,
                Description = dto.Description,
                Frequency = dto.Frequency,
                NextRunDate = dto.NextRunDate.Date,
                EndDate = dto.EndDate?.Date,
                IsActive = dto.IsActive,
                Lines = dto.Lines.Select(x => new RecurringJournalLine
                {
                    AccountId = x.AccountId,
                    CostCenterId = x.CostCenterId,
                    DebitAmount = x.DebitAmount,
                    CreditAmount = x.CreditAmount,
                    Description = x.Description
                }).ToList()
            };

            await _context.Set<RecurringJournal>().AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<RecurringJournal?> GetByIdAsync(int id)
        {
            return await _context.Set<RecurringJournal>()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<RecurringJournal>> GetAllAsync()
        {
            return await _context.Set<RecurringJournal>()
                .Include(x => x.Lines)
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        public async Task UpdateAsync(int id, RecurringJournalUpsertDto dto)
        {
            ValidateDto(dto);

            var entity = await _context.Set<RecurringJournal>()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                throw new InvalidOperationException("Recurring journal was not found.");

            entity.Name = dto.Name;
            entity.Description = dto.Description;
            entity.Frequency = dto.Frequency;
            entity.NextRunDate = dto.NextRunDate.Date;
            entity.EndDate = dto.EndDate?.Date;
            entity.IsActive = dto.IsActive;

            _context.Set<RecurringJournalLine>().RemoveRange(entity.Lines);
            entity.Lines = dto.Lines.Select(x => new RecurringJournalLine
            {
                RecurringJournalId = entity.Id,
                AccountId = x.AccountId,
                CostCenterId = x.CostCenterId,
                DebitAmount = x.DebitAmount,
                CreditAmount = x.CreditAmount,
                Description = x.Description
            }).ToList();

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.Set<RecurringJournal>()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return;

            _context.Set<RecurringJournalLine>().RemoveRange(entity.Lines);
            _context.Set<RecurringJournal>().Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task ExecuteDueAsync(DateTime asOfDate)
        {
            var asOf = asOfDate.Date;

            var dueJournals = await _context.Set<RecurringJournal>()
                .Include(x => x.Lines)
                .Where(x => x.IsActive)
                .Where(x => x.NextRunDate.Date <= asOf)
                .Where(x => !x.EndDate.HasValue || x.NextRunDate.Date <= x.EndDate.Value.Date)
                .OrderBy(x => x.NextRunDate)
                .ToListAsync();

            foreach (var journal in dueJournals)
            {
                if (!journal.Lines.Any())
                    continue;

                var entry = new JournalEntryWriteDto
                {
                    EntryDate = journal.NextRunDate.Date,
                    Description = $"Recurring: {journal.Name}",
                    Lines = journal.Lines.Select((line, index) => new JournalEntryLineWriteDto
                    {
                        Id = index + 1,
                        AccountId = line.AccountId,
                        CostCenterId = line.CostCenterId,
                        Debit = line.DebitAmount,
                        Credit = line.CreditAmount,
                        Description = line.Description
                    }).ToList()
                };

                var result = await _accountingService.PostJournalEntryAsync(entry);
                if (!result.Success)
                    continue;

                var postedDate = journal.NextRunDate.Date;
                journal.LastPostedDate = postedDate;

                var next = AdvanceDate(postedDate, journal.Frequency);
                if (journal.EndDate.HasValue && next.Date > journal.EndDate.Value.Date)
                {
                    journal.IsActive = false;
                }
                else
                {
                    journal.NextRunDate = next.Date;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<RecurringJournal>> GetUpcomingAsync(int days)
        {
            if (days < 0)
                throw new InvalidOperationException("Days must be zero or greater.");

            var today = DateTime.Today;
            var to = today.AddDays(days);

            return await _context.Set<RecurringJournal>()
                .Include(x => x.Lines)
                .Where(x => x.IsActive)
                .Where(x => x.NextRunDate.Date >= today && x.NextRunDate.Date <= to)
                .OrderBy(x => x.NextRunDate)
                .ThenBy(x => x.Name)
                .ToListAsync();
        }

        private static DateTime AdvanceDate(DateTime date, RecurringFrequency frequency)
        {
            return frequency switch
            {
                RecurringFrequency.Daily => date.AddDays(1),
                RecurringFrequency.Weekly => date.AddDays(7),
                RecurringFrequency.Monthly => date.AddMonths(1),
                RecurringFrequency.Quarterly => date.AddMonths(3),
                RecurringFrequency.Yearly => date.AddYears(1),
                _ => date.AddMonths(1)
            };
        }

        private static void ValidateDto(RecurringJournalUpsertDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new InvalidOperationException("Name is required.");

            if (dto.Lines == null || dto.Lines.Count == 0)
                throw new InvalidOperationException("At least one line is required.");

            var totalDebit = dto.Lines.Sum(x => x.DebitAmount);
            var totalCredit = dto.Lines.Sum(x => x.CreditAmount);
            if (totalDebit <= 0m && totalCredit <= 0m)
                throw new InvalidOperationException("Line amounts are required.");

            if (totalDebit != totalCredit)
                throw new InvalidOperationException("Recurring journal must be balanced.");
        }
    }
}

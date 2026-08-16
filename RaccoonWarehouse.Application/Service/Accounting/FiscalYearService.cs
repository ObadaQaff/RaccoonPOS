using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Accounting.Periods;

namespace RaccoonWarehouse.Application.Service.Accounting
{
    public class FiscalYearService
    {
        private readonly ApplicationDbContext _dbContext;

        public FiscalYearService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<FiscalYear> CreateFiscalYear(string name, DateTime startDate, DateTime endDate)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Fiscal year name is required.");
            }

            if (endDate.Date < startDate.Date)
            {
                throw new InvalidOperationException("Fiscal year end date must be on or after start date.");
            }

            var overlaps = await _dbContext.FiscalYears.AnyAsync(x =>
                startDate.Date <= x.EndDate.Date && endDate.Date >= x.StartDate.Date);
            if (overlaps)
            {
                throw new InvalidOperationException("Fiscal year date range overlaps an existing fiscal year.");
            }

            var fiscalYear = new FiscalYear
            {
                Code = BuildCode(startDate),
                Name = name.Trim(),
                StartDate = startDate.Date,
                EndDate = endDate.Date,
                Status = FiscalYearStatus.Open,
                IsClosed = false,
                IsLegacy = false,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            _dbContext.FiscalYears.Add(fiscalYear);
            await _dbContext.SaveChangesAsync();
            return fiscalYear;
        }

        public async Task<List<AccountingPeriod>> GenerateMonthlyPeriods(int fiscalYearId)
        {
            var fiscalYear = await _dbContext.FiscalYears.FirstOrDefaultAsync(x => x.Id == fiscalYearId);
            if (fiscalYear == null)
            {
                throw new InvalidOperationException("Fiscal year was not found.");
            }

            var existing = await _dbContext.AccountingPeriods.CountAsync(x => x.FiscalYearId == fiscalYearId);
            if (existing > 0)
            {
                throw new InvalidOperationException("Fiscal year already has periods.");
            }

            var periods = new List<AccountingPeriod>();
            var cursor = new DateTime(fiscalYear.StartDate.Year, fiscalYear.StartDate.Month, 1);

            for (var i = 1; i <= 12; i++)
            {
                var start = i == 1 ? fiscalYear.StartDate.Date : cursor;
                var monthEnd = new DateTime(cursor.Year, cursor.Month, DateTime.DaysInMonth(cursor.Year, cursor.Month));
                var end = i == 12 ? fiscalYear.EndDate.Date : monthEnd;

                if (end > fiscalYear.EndDate.Date)
                {
                    end = fiscalYear.EndDate.Date;
                }

                periods.Add(new AccountingPeriod
                {
                    FiscalYearId = fiscalYearId,
                    PeriodNumber = i,
                    Name = $"{fiscalYear.Name} - P{i:00}",
                    StartDate = start,
                    EndDate = end,
                    Status = AccountingPeriodStatus.Open,
                    IsClosed = false,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                });

                cursor = cursor.AddMonths(1);
            }

            _dbContext.AccountingPeriods.AddRange(periods);
            await _dbContext.SaveChangesAsync();
            return periods;
        }

        public async Task ClosePeriod(int periodId)
        {
            var period = await _dbContext.AccountingPeriods.FirstOrDefaultAsync(x => x.Id == periodId);
            if (period == null)
            {
                throw new InvalidOperationException("Accounting period was not found.");
            }

            var hasOpenEntries = await _dbContext.JournalEntries.AnyAsync(x =>
                x.AccountingPeriodId == periodId &&
                x.Status != JournalEntryStatus.Posted);

            if (hasOpenEntries)
            {
                throw new InvalidOperationException("Cannot close period while it has non-posted journal entries.");
            }

            period.Status = AccountingPeriodStatus.Closed;
            period.IsClosed = true;
            period.UpdatedDate = DateTime.Now;
            await _dbContext.SaveChangesAsync();
        }

        public async Task<AccountingPeriod?> GetCurrentOpenPeriod()
        {
            var today = DateTime.Today;
            return await _dbContext.AccountingPeriods
                .AsNoTracking()
                .Where(x =>
                    x.Status == AccountingPeriodStatus.Open &&
                    x.StartDate.Date <= today &&
                    x.EndDate.Date >= today)
                .OrderBy(x => x.StartDate)
                .FirstOrDefaultAsync();
        }

        private static string BuildCode(DateTime startDate)
        {
            return $"FY{startDate:yyyy}";
        }
    }
}

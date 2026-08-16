using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Accounting.Periods;

namespace RaccoonWarehouse.Application.Service.Accounting
{
    public static class FiscalYearSeeder
    {
        public static async Task SeedLegacyAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken = default)
        {
            var exists = await dbContext.FiscalYears.AnyAsync(x => x.IsLegacy, cancellationToken);
            if (exists)
            {
                return;
            }

            var minDate = await dbContext.JournalEntries
                .Select(x => (DateTime?)x.EntryDate)
                .MinAsync(cancellationToken);
            var maxDate = await dbContext.JournalEntries
                .Select(x => (DateTime?)x.EntryDate)
                .MaxAsync(cancellationToken);

            if (!minDate.HasValue || !maxDate.HasValue)
            {
                return;
            }

            var startDate = minDate.Value.Date;
            var endDate = maxDate.Value.Date;

            var legacyFiscalYear = new FiscalYear
            {
                Code = "LEGACY",
                Name = "Legacy",
                StartDate = startDate,
                EndDate = endDate,
                Status = FiscalYearStatus.Open,
                IsClosed = false,
                IsLegacy = true,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            dbContext.FiscalYears.Add(legacyFiscalYear);
            await dbContext.SaveChangesAsync(cancellationToken);

            var legacyPeriod = new AccountingPeriod
            {
                FiscalYearId = legacyFiscalYear.Id,
                PeriodNumber = 1,
                Name = "Legacy Period",
                StartDate = startDate,
                EndDate = endDate,
                Status = AccountingPeriodStatus.Open,
                IsClosed = false,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            dbContext.AccountingPeriods.Add(legacyPeriod);
            await dbContext.SaveChangesAsync(cancellationToken);

            await dbContext.JournalEntries
                .Where(x => !x.FiscalYearId.HasValue || !x.AccountingPeriodId.HasValue)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.FiscalYearId, legacyFiscalYear.Id)
                        .SetProperty(x => x.AccountingPeriodId, legacyPeriod.Id),
                    cancellationToken);
        }
    }
}

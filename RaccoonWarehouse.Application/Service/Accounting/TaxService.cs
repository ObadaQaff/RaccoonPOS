using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Accounting.TaxRates;
using RaccoonWarehouse.Domain.Accounting.TaxRates.DTOs;

namespace RaccoonWarehouse.Application.Service.Accounting
{
    public class TaxService
    {
        private readonly ApplicationDbContext _dbContext;

        public TaxService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<TaxRate> CreateAsync(string name, decimal rate, int taxAccountId, TaxType taxType)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Tax rate name is required.");
            }

            if (rate < 0)
            {
                throw new InvalidOperationException("Tax rate cannot be negative.");
            }

            var accountExists = await _dbContext.Accounts.AnyAsync(x => x.Id == taxAccountId);
            if (!accountExists)
            {
                throw new InvalidOperationException("Tax account was not found.");
            }

            var entity = new TaxRate
            {
                Name = name.Trim(),
                Rate = rate,
                TaxAccountId = taxAccountId,
                TaxType = taxType,
                IsActive = true,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            _dbContext.Set<TaxRate>().Add(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(int id, string name, decimal rate, int taxAccountId, TaxType taxType, bool isActive)
        {
            var entity = await _dbContext.Set<TaxRate>().FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                throw new InvalidOperationException("Tax rate was not found.");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Tax rate name is required.");
            }

            if (rate < 0)
            {
                throw new InvalidOperationException("Tax rate cannot be negative.");
            }

            var accountExists = await _dbContext.Accounts.AnyAsync(x => x.Id == taxAccountId);
            if (!accountExists)
            {
                throw new InvalidOperationException("Tax account was not found.");
            }

            entity.Name = name.Trim();
            entity.Rate = rate;
            entity.TaxAccountId = taxAccountId;
            entity.TaxType = taxType;
            entity.IsActive = isActive;
            entity.UpdatedDate = DateTime.Now;
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeactivateAsync(int id)
        {
            var entity = await _dbContext.Set<TaxRate>().FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                throw new InvalidOperationException("Tax rate was not found.");
            }

            entity.IsActive = false;
            entity.UpdatedDate = DateTime.Now;
            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<TaxRate>> GetAllAsync()
        {
            return await _dbContext.Set<TaxRate>()
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<decimal> CalculateTaxAsync(decimal amount, int taxRateId)
        {
            var taxRate = await _dbContext.Set<TaxRate>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == taxRateId && x.IsActive);
            if (taxRate == null)
            {
                throw new InvalidOperationException("Tax rate was not found or inactive.");
            }

            return amount * taxRate.Rate / 100m;
        }

        public async Task<TaxReportDto> GetTaxReportAsync(DateTime fromDate, DateTime toDate)
        {
            var rows = await _dbContext.JournalEntryLines
                .AsNoTracking()
                .Where(x => x.TaxRateId.HasValue && x.TaxAmount.HasValue)
                .Join(
                    _dbContext.JournalEntries.AsNoTracking().Where(e =>
                        e.Status == JournalEntryStatus.Posted &&
                        e.EntryDate.Date >= fromDate.Date &&
                        e.EntryDate.Date <= toDate.Date),
                    line => line.JournalEntryId,
                    entry => entry.Id,
                    (line, entry) => line)
                .Join(
                    _dbContext.Set<TaxRate>().AsNoTracking(),
                    line => line.TaxRateId!.Value,
                    taxRate => taxRate.Id,
                    (line, taxRate) => new { taxRate.TaxType, TaxAmount = line.TaxAmount!.Value })
                .ToListAsync();

            var inputVat = rows
                .Where(x => x.TaxType == TaxType.InputTax)
                .Sum(x => x.TaxAmount);
            var outputVat = rows
                .Where(x => x.TaxType == TaxType.OutputTax)
                .Sum(x => x.TaxAmount);

            return new TaxReportDto
            {
                InputVAT = inputVat,
                OutputVAT = outputVat,
                NetVATPayable = outputVat - inputVat
            };
        }
    }
}

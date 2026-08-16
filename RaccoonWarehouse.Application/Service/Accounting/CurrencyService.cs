using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;

namespace RaccoonWarehouse.Application.Service.Accounting
{
    public class CurrencyService
    {
        private readonly ApplicationDbContext _dbContext;

        public CurrencyService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<decimal> GetRateAsync(int currencyId, DateTime date)
        {
            var currency = await _dbContext.Currencies
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == currencyId);
            if (currency == null)
            {
                throw new InvalidOperationException("Currency was not found.");
            }

            if (currency.IsBaseCurrency)
            {
                return 1m;
            }

            var baseCurrency = await _dbContext.Currencies
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IsBaseCurrency && x.IsActive);
            if (baseCurrency == null)
            {
                throw new InvalidOperationException("Base currency is not configured.");
            }

            var rate = await _dbContext.ExchangeRates
                .AsNoTracking()
                .Where(x =>
                    x.FromCurrencyId == currencyId &&
                    x.ToCurrencyId == baseCurrency.Id &&
                    x.EffectiveDate <= date.Date)
                .OrderByDescending(x => x.EffectiveDate)
                .Select(x => (decimal?)x.Rate)
                .FirstOrDefaultAsync();

            if (!rate.HasValue || rate.Value <= 0)
            {
                throw new InvalidOperationException("No valid exchange rate found on or before the specified date.");
            }

            return rate.Value;
        }

        public async Task<decimal> ConvertToBaseAsync(decimal amount, int currencyId, DateTime date)
        {
            var rate = await GetRateAsync(currencyId, date);
            return amount * rate;
        }
    }
}

using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Currencies;

namespace RaccoonWarehouse.Application.Service.Accounting
{
    public static class CurrencySeeder
    {
        public static async Task SeedBaseCurrencyAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken = default)
        {
            var hasAny = await dbContext.Currencies.AnyAsync(cancellationToken);
            if (hasAny)
            {
                return;
            }

            dbContext.Currencies.Add(new Currency
            {
                Code = "JOD",
                Name = "Jordanian Dinar",
                Symbol = "JD",
                ExchangeRate = 1m,
                IsBaseCurrency = true,
                IsActive = true,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
namespace RaccoonWarehouse.Application.Service.Accounting;
public interface ICurrencyService
{
    Task<decimal> GetRateAsync(int currencyId, DateTime date);
    Task<decimal> ConvertToBaseAsync(decimal amount, int currencyId, DateTime date);
}

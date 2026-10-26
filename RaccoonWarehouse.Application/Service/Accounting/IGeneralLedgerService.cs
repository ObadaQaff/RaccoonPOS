using RaccoonWarehouse.Domain.Reports.Accounting.Dtos;
using RaccoonWarehouse.Domain.Reports.Accounting.Filters;
namespace RaccoonWarehouse.Application.Service.Accounting;
public interface IGeneralLedgerService
{
    Task<List<LedgerLine>> GetAsync(int accountId, DateTime fromDate, DateTime toDate);
}

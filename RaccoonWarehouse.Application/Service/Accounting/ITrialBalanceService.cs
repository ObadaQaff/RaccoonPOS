using RaccoonWarehouse.Domain.Reports.Accounting.Dtos;
using RaccoonWarehouse.Domain.Reports.Accounting.Filters;
namespace RaccoonWarehouse.Application.Service.Accounting;
public interface ITrialBalanceService
{
    Task<List<TrialBalanceLine>> GetAsync(int fiscalYearId, DateTime fromDate, DateTime toDate);
}

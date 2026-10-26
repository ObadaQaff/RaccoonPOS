using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Accounting.Periods;
namespace RaccoonWarehouse.Application.Service.Accounting;
public interface IFiscalYearService
{
    Task<FiscalYear> CreateFiscalYear(string name, DateTime startDate, DateTime endDate);
    Task<List<AccountingPeriod>> GenerateMonthlyPeriods(int fiscalYearId);
    Task ClosePeriod(int periodId);
    Task<AccountingPeriod?> GetCurrentOpenPeriod();
}

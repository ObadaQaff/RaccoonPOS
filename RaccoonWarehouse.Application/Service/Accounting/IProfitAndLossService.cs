using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Reports.Financial.Dtos;
namespace RaccoonWarehouse.Application.Service.Accounting;
public interface IProfitAndLossService
{
    Task<ProfitAndLossReport> GetReportAsync(
            int fiscalYearId,
            DateTime fromDate,
            DateTime toDate,
            DateTime? compareFromDate = null,
            DateTime? compareToDate = null);
}

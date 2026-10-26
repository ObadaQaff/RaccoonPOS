using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.Accounts;
using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Reports.Financial.Dtos;
namespace RaccoonWarehouse.Application.Service.Accounting;
public interface ICashFlowService
{
    Task<CashFlowReport> GetReportAsync(int fiscalYearId, DateTime fromDate, DateTime toDate);
}

using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Reports.Financial.Dtos;
using RaccoonWarehouse.Domain.Reports.Financial.Enums;
namespace RaccoonWarehouse.Application.Service.Accounting;
public interface IAgingReportService
{
    Task<List<AgingRow>> GetAgingReportAsync(AgingType type, DateTime asOfDate);
}

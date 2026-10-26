using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.JournalEntries;
using RaccoonWarehouse.Domain.Accounting.Accounts;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Reports.Accounting.Dtos;
using RaccoonWarehouse.Domain.Reports.Accounting.Filters;
using RaccoonWarehouse.Domain.Users;
namespace RaccoonWarehouse.Application.Service.Accounting;
public interface IPartyBalanceReportService
{
    Task<Result<PartyBalanceReportDto>> GetAsync(PartyBalanceFilterDto filter);
    Task<Result<PartyBalanceReportDto>> GetCombinedAsync(PartyBalanceFilterDto filter);
}

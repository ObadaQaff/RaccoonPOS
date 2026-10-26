using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.JournalEntries;
using RaccoonWarehouse.Domain.Accounting.Accounts;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.PurchaseOrders;
using RaccoonWarehouse.Domain.Reports.Accounting.Dtos;
using RaccoonWarehouse.Domain.Reports.Accounting.Filters;
using RaccoonWarehouse.Domain.Users;
namespace RaccoonWarehouse.Application.Service.Accounting;
public interface IUserStatementService
{
    Task<decimal> GetCurrentBalanceAsync(int userId);
    Task<Result> ValidateCreditLimitAsync(int userId, decimal additionalAmount);
    Task<Result<UserStatementReportDto>> GetAsync(UserStatementFilterDto filter);
}

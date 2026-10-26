using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.AccountOpeningBalances;
using RaccoonWarehouse.Domain.Accounting.AccountOpeningBalances.DTOs;
namespace RaccoonWarehouse.Application.Service.Accounting;
public interface IOpeningBalanceService
{
    Task UpsertAsync(int accountId, int fiscalYearId, decimal debitAmount, decimal creditAmount);
    Task<List<OpeningBalanceDto>> GetAllAsync(int fiscalYearId);
    Task<(bool IsBalanced, decimal Difference)> ValidateBalancedAsync(int fiscalYearId);
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.Accounts;
using RaccoonWarehouse.Domain.Accounting.Accounts.DTOs;
using RaccoonWarehouse.Domain.Accounting.Enums;
namespace RaccoonWarehouse.Application.Service.Accounting;
public interface IAccountService
{
    Task<List<AccountTreeNodeDto>> GetTreeAsync();
    Task<Account> CreateChildAsync(
            int parentId,
            string name,
            string nature,
            string category,
            string? accountCode = null,
            string? accountTypeCode = null,
            bool? isPosting = null,
            string? description = null);
    Task UpdateAsync(int id, string name, bool isActive);
    Task DeactivateAsync(int id);
    Task<AccountBalanceDto> GetBalanceAsync(int id);
}

using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Domain.Accounting.Accounts;
using RaccoonWarehouse.Domain.Accounting.Enums;

namespace RaccoonWarehouse.Core.Interface.Accounting
{
    public interface IAccountRepository : IGenericRepository<Account>
    {
        Task<List<Account>> GetTreeAsync(bool activeOnly = true);

        Task<Account?> GetByCodeAsync(string code, bool activeOnly = true);

        Task<List<Account>> GetLeafAccountsAsync(AccountType? accountType = null, bool activeOnly = true);
    }
}

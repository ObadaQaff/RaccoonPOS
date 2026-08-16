using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Core.Interface.Accounting;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.Accounts;
using RaccoonWarehouse.Domain.Accounting.Enums;

namespace RaccoonWarehouse.Data.Repository
{
    public class AccountRepository : GenericService<Account>, IAccountRepository
    {
        public AccountRepository(ApplicationDbContext context, AutoMapper.IMapper mapper)
            : base(context, mapper)
        {
        }

        public async Task<List<Account>> GetTreeAsync(bool activeOnly = true)
        {
            var query = BuildQuery(activeOnly)
                .OrderBy(x => x.ParentAccountId)
                .ThenBy(x => x.Code)
                .ThenBy(x => x.Name)
                .ThenBy(x => x.Id);

            var accounts = await query.ToListAsync();
            return BuildTree(accounts);
        }

        public async Task<Account?> GetByCodeAsync(string code, bool activeOnly = true)
        {
            var normalized = code?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return null;
            }

            return await BuildQuery(activeOnly)
                .FirstOrDefaultAsync(x => x.Code == normalized || x.AccountCode == normalized);
        }

        public async Task<List<Account>> GetLeafAccountsAsync(AccountType? accountType = null, bool activeOnly = true)
        {
            var query = BuildQuery(activeOnly)
                .Where(x => !x.Children.Any());

            if (accountType.HasValue)
            {
                query = query.Where(x => x.AccountType == accountType.Value);
            }

            return await query
                .OrderBy(x => x.Code)
                .ThenBy(x => x.Name)
                .ToListAsync();
        }

        private IQueryable<Account> BuildQuery(bool activeOnly)
        {
            var query = _context.Set<Account>().AsNoTracking();
            if (activeOnly)
            {
                query = query.Where(x => x.IsActive);
            }

            return query;
        }

        private static List<Account> BuildTree(IReadOnlyCollection<Account> accounts)
        {
            var byId = accounts.ToDictionary(x => x.Id);
            var roots = new List<Account>();

            foreach (var account in accounts)
            {
                account.Children = new List<Account>();
            }

            foreach (var account in accounts)
            {
                if (account.ParentAccountId.HasValue && byId.TryGetValue(account.ParentAccountId.Value, out var parent))
                {
                    parent.Children.Add(account);
                }
                else
                {
                    roots.Add(account);
                }
            }

            return roots
                .OrderBy(x => x.Code)
                .ThenBy(x => x.Name)
                .ToList();
        }
    }
}

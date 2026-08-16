using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.Accounts;

namespace RaccoonWarehouse.Application.Service.Accounting
{
    public static class AccountTreeSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken = default)
        {
            var accounts = await dbContext.Accounts
                .OrderBy(x => x.ParentAccountId)
                .ThenBy(x => x.Code)
                .ThenBy(x => x.Name)
                .ThenBy(x => x.Id)
                .ToListAsync(cancellationToken);

            if (accounts.Count == 0)
            {
                Console.WriteLine("Assigned levels to 0 accounts, skipped 0");
                return;
            }

            var accountById = accounts.ToDictionary(x => x.Id);

            var childrenByParent = accounts
                .Where(x => x.ParentAccountId.HasValue)
                .GroupBy(x => x.ParentAccountId!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g
                        .OrderBy(x => x.Code)
                        .ThenBy(x => x.Name)
                        .ThenBy(x => x.Id)
                        .ToList());

            var assigned = 0;
            var skipped = 0;
            var visited = new HashSet<int>();

            var roots = accounts
                .Where(x => !x.ParentAccountId.HasValue || !accountById.ContainsKey(x.ParentAccountId.Value))
                .OrderBy(x => x.Code)
                .ThenBy(x => x.Name)
                .ThenBy(x => x.Id)
                .ToList();

            foreach (var root in roots)
            {
                var rootCode = AccountCodeHelper.IsFlatAccountCode(root.Code)
                    ? root.Code
                    : AccountCodeHelper.GetRootCode(root.AccountType);
                Traverse(root, rootCode, 1);
            }

            if (assigned > 0)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            Console.WriteLine($"Assigned levels to {assigned} accounts, skipped {skipped}");

            void Traverse(Account account, string code, int depth)
            {
                if (!visited.Add(account.Id))
                {
                    return;
                }

                var hasChildren = childrenByParent.TryGetValue(account.Id, out var children) && children.Count > 0;
                var level = depth;
                if (level > 3)
                {
                    level = 3;
                }

                if (account.AccountLevel.HasValue)
                {
                    skipped++;
                }
                else
                {
                    account.AccountLevel = level;
                    account.AccountCode = code;
                    account.IsPosting = level == 3;

                    assigned++;
                }

                if (!hasChildren || children == null)
                {
                    return;
                }

                var childIndex = 0;
                foreach (var child in children)
                {
                    childIndex++;
                    var nextLevel = depth + 1;
                    var childCode = AccountCodeHelper.GenerateCode(new Account { AccountCode = code, Code = code }, childIndex);
                    Traverse(child, childCode, nextLevel);
                }
            }
        }
    }
}

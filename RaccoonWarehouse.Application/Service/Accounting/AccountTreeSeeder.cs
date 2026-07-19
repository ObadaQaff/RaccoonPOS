using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.Accounts;

namespace RaccoonWarehouse.Application.Service.Accounting
{
    public static class AccountTreeSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger("AccountTreeSeeder");

            var accounts = await dbContext.Accounts
                .OrderBy(x => x.ParentAccountId)
                .ThenBy(x => x.Code)
                .ThenBy(x => x.Name)
                .ThenBy(x => x.Id)
                .ToListAsync(cancellationToken);

            if (accounts.Count == 0)
            {
                logger?.LogInformation("Assigned levels to 0 accounts, skipped 0");
                return;
            }

            var accountById = accounts.ToDictionary(x => x.Id);

            var childrenByParent = accounts
                .GroupBy(x => x.ParentAccountId)
                .ToDictionary(
                    g => g.Key,
                    g => g
                        .OrderBy(x => x.Code)
                        .ThenBy(x => x.Name)
                        .ThenBy(x => x.Id)
                        .ToList());

            var assigned = 0;
            var skipped = 0;
            var rootIndex = 0;
            var visited = new HashSet<int>();

            var roots = accounts
                .Where(x => !x.ParentAccountId.HasValue || !accountById.ContainsKey(x.ParentAccountId.Value))
                .OrderBy(x => x.Code)
                .ThenBy(x => x.Name)
                .ThenBy(x => x.Id)
                .ToList();

            foreach (var root in roots)
            {
                rootIndex++;
                Traverse(root, rootIndex.ToString(), 1);
            }

            if (assigned > 0)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            logger?.LogInformation("Assigned levels to {Assigned} accounts, skipped {Skipped}", assigned, skipped);
            Console.WriteLine($"Assigned levels to {assigned} accounts, skipped {skipped}");

            void Traverse(Account account, string code, int depth)
            {
                if (!visited.Add(account.Id))
                {
                    return;
                }

                var hasChildren = childrenByParent.TryGetValue(account.Id, out var children) && children.Count > 0;
                var level = hasChildren ? Math.Min(depth, 4) : 5;

                if (account.AccountLevel.HasValue)
                {
                    skipped++;
                }
                else
                {
                    account.AccountLevel = level;
                    account.AccountCode = code;
                    if (level == 5)
                    {
                        account.IsPosting = true;
                    }

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
                    var segment = (depth + 1) switch
                    {
                        2 => childIndex.ToString("00"),
                        3 => childIndex.ToString("000"),
                        4 => childIndex.ToString("000"),
                        _ => childIndex.ToString("000")
                    };

                    var childCode = $"{code}-{segment}";
                    Traverse(child, childCode, depth + 1);
                }
            }
        }
    }
}

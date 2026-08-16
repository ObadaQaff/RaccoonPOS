using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.Accounts;
using RaccoonWarehouse.Domain.Accounting.Accounts.DTOs;
using RaccoonWarehouse.Domain.Accounting.Enums;

namespace RaccoonWarehouse.Application.Service.Accounting
{
    public class AccountService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public AccountService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<List<AccountTreeNodeDto>> GetTreeAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var accounts = await dbContext.Accounts
                .AsNoTracking()
                .OrderBy(x => x.ParentAccountId)
                .ThenBy(x => x.AccountCode ?? x.Code)
                .ThenBy(x => x.Name)
                .ThenBy(x => x.Id)
                .ToListAsync();

            var map = accounts.ToDictionary(
                x => x.Id,
                x => new AccountTreeNodeDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.AccountCode ?? x.Code,
                    Level = x.AccountLevel,
                    Nature = x.AccountNature,
                    Category = x.AccountCategory,
                    IsPosting = x.IsPosting,
                    IsActive = x.IsActive
                });

            var roots = new List<AccountTreeNodeDto>();
            foreach (var account in accounts)
            {
                var node = map[account.Id];
                if (account.ParentAccountId.HasValue && map.TryGetValue(account.ParentAccountId.Value, out var parent))
                {
                    parent.Children.Add(node);
                }
                else
                {
                    roots.Add(node);
                }
            }

            return roots;
        }

        public async Task<Account> CreateChildAsync(
            int parentId,
            string name,
            string nature,
            string category,
            string? accountCode = null,
            string? accountTypeCode = null,
            bool? isPosting = null,
            string? description = null)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Account name is required.");
            }

            if (!IsValidNature(nature))
            {
                throw new InvalidOperationException("Account nature must be 'Debit' or 'Credit'.");
            }

            if (!IsValidCategory(category))
            {
                throw new InvalidOperationException("Account category must be Asset, Liability, Equity, Revenue, or Expense.");
            }

            var parent = await dbContext.Accounts.FirstOrDefaultAsync(x => x.Id == parentId);
            if (parent == null)
            {
                throw new InvalidOperationException("Parent account was not found.");
            }

            var parentLevel = parent.AccountLevel ?? 1;
            if (parentLevel >= 3)
            {
                throw new InvalidOperationException("Cannot create child account under level 3 account.");
            }

            var siblingCount = await dbContext.Accounts.CountAsync(x => x.ParentAccountId == parent.Id);
            var level = parentLevel + 1;
            var generatedCode = AccountCodeHelper.GenerateCode(parent, siblingCount + 1);
            var code = string.IsNullOrWhiteSpace(accountCode) ? generatedCode : accountCode.Trim();

            var codeExists = await dbContext.Accounts.AnyAsync(x => x.AccountCode == code);
            if (codeExists)
            {
                throw new InvalidOperationException("Account code already exists.");
            }

            var resolvedTypeCode = string.IsNullOrWhiteSpace(accountTypeCode)
                ? ResolveTypeCode(category)
                : accountTypeCode.Trim().ToUpperInvariant();
            if (resolvedTypeCode is not ("BS" or "PL"))
            {
                throw new InvalidOperationException("Account type must be BS or PL.");
            }

            var account = new Account
            {
                Name = name.Trim(),
                ParentAccountId = parent.Id,
                AccountLevel = level,
                AccountCode = code,
                AccountNature = nature,
                AccountCategory = category,
                AccountTypeCode = resolvedTypeCode,
                IsPosting = level == 3 ? true : (isPosting ?? false),
                IsActive = true,
                Code = code,
                AccountType = ResolveAccountType(category),
                Level = level,
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                NormalBalanceType = nature.Equals("Debit", StringComparison.OrdinalIgnoreCase)
                    ? NormalBalanceType.Debit
                    : NormalBalanceType.Credit,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            dbContext.Accounts.Add(account);
            await dbContext.SaveChangesAsync();
            return account;
        }

        public async Task UpdateAsync(int id, string name, bool isActive)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Account name is required.");
            }

            var account = await dbContext.Accounts.FirstOrDefaultAsync(x => x.Id == id);
            if (account == null)
            {
                throw new InvalidOperationException("Account was not found.");
            }

            account.Name = name.Trim();
            account.IsActive = isActive;
            account.UpdatedDate = DateTime.Now;
            await dbContext.SaveChangesAsync();
        }

        public async Task DeactivateAsync(int id)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var account = await dbContext.Accounts.FirstOrDefaultAsync(x => x.Id == id);
            if (account == null)
            {
                throw new InvalidOperationException("Account was not found.");
            }

            var hasActiveChildren = await dbContext.Accounts
                .AnyAsync(x => x.ParentAccountId == id && x.IsActive);
            if (hasActiveChildren)
            {
                throw new InvalidOperationException("Cannot deactivate account because it has active children.");
            }

            var hasPostedLines = await dbContext.JournalEntryLines
                .Where(x => x.AccountId == id)
                .Join(
                    dbContext.JournalEntries,
                    line => line.JournalEntryId,
                    entry => entry.Id,
                    (line, entry) => entry.Status)
                .AnyAsync(status => status == JournalEntryStatus.Posted);

            if (hasPostedLines)
            {
                throw new InvalidOperationException("Cannot deactivate account because it has posted journal lines.");
            }

            account.IsActive = false;
            account.UpdatedDate = DateTime.Now;
            await dbContext.SaveChangesAsync();
        }

        public async Task<AccountBalanceDto> GetBalanceAsync(int id)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var exists = await dbContext.Accounts.AnyAsync(x => x.Id == id);
            if (!exists)
            {
                throw new InvalidOperationException("Account was not found.");
            }

            var balances = await dbContext.JournalEntryLines
                .Where(x => x.AccountId == id)
                .Join(
                    dbContext.JournalEntries,
                    line => line.JournalEntryId,
                    entry => entry.Id,
                    (line, entry) => new { line.Debit, line.Credit, entry.Status })
                .Where(x => x.Status == JournalEntryStatus.Posted)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Debit = g.Sum(x => x.Debit),
                    Credit = g.Sum(x => x.Credit)
                })
                .FirstOrDefaultAsync();

            var debit = balances?.Debit ?? 0m;
            var credit = balances?.Credit ?? 0m;
            return new AccountBalanceDto
            {
                DebitBalance = debit,
                CreditBalance = credit,
                NetBalance = debit - credit
            };
        }

        private static bool IsValidNature(string? nature)
        {
            return string.Equals(nature, "Debit", StringComparison.OrdinalIgnoreCase)
                || string.Equals(nature, "Credit", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsValidCategory(string? category)
        {
            return string.Equals(category, "Asset", StringComparison.OrdinalIgnoreCase)
                || string.Equals(category, "Liability", StringComparison.OrdinalIgnoreCase)
                || string.Equals(category, "Equity", StringComparison.OrdinalIgnoreCase)
                || string.Equals(category, "Revenue", StringComparison.OrdinalIgnoreCase)
                || string.Equals(category, "Expense", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveTypeCode(string category)
        {
            return category.Equals("Revenue", StringComparison.OrdinalIgnoreCase)
                   || category.Equals("Expense", StringComparison.OrdinalIgnoreCase)
                ? "PL"
                : "BS";
        }

        private static AccountType ResolveAccountType(string category)
        {
            if (category.Equals("Asset", StringComparison.OrdinalIgnoreCase))
                return AccountType.Asset;
            if (category.Equals("Liability", StringComparison.OrdinalIgnoreCase))
                return AccountType.Liability;
            if (category.Equals("Equity", StringComparison.OrdinalIgnoreCase))
                return AccountType.Equity;
            if (category.Equals("Revenue", StringComparison.OrdinalIgnoreCase))
                return AccountType.Revenue;
            return AccountType.Expense;
        }
    }
}

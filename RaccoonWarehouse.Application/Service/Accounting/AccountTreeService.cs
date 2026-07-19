using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Domain.Accounting.Accounts;
using RaccoonWarehouse.Domain.Accounting.Accounts.DTOs;
using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Accounting.JournalEntries;

namespace RaccoonWarehouse.Application.Service.Accounting
{
    /// <summary>
    /// Provides chart-of-accounts tree operations for WPF screens.
    /// </summary>
    public interface IAccountTreeService
    {
        /// <summary>
        /// Returns the full chart of accounts tree.
        /// </summary>
        Task<Result<List<AccountTreeNodeDto>>> GetTreeAsync();

        /// <summary>
        /// Creates a new account node under the given parent.
        /// </summary>
        Task<Result<AccountTreeNodeDto>> CreateAsync(CreateAccountNodeDto dto);

        /// <summary>
        /// Updates account mutable fields only (name and active status).
        /// </summary>
        Task<Result<AccountTreeNodeDto>> UpdateAsync(int id, UpdateAccountNodeDto dto);

        /// <summary>
        /// Soft deletes an account by setting it inactive, with business validations.
        /// </summary>
        Task<Result<bool>> SoftDeleteAsync(int id);

        /// <summary>
        /// Gets the posted debit/credit/net balances for an account.
        /// </summary>
        Task<Result<AccountBalanceDto>> GetBalanceAsync(int id);
    }

    public class AccountTreeService : IAccountTreeService
    {
        private readonly IUOW _uow;

        public AccountTreeService(IUOW uow)
        {
            _uow = uow;
        }

        public async Task<Result<List<AccountTreeNodeDto>>> GetTreeAsync()
        {
            var accounts = await _uow.Accounts.GetAllAsQueryable()
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

            return Result<List<AccountTreeNodeDto>>.Ok(roots);
        }

        public async Task<Result<AccountTreeNodeDto>> CreateAsync(CreateAccountNodeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return Result<AccountTreeNodeDto>.Fail("Account name is required.");
            }

            if (!IsValidNature(dto.Nature))
            {
                return Result<AccountTreeNodeDto>.Fail("Account nature must be 'Debit' or 'Credit'.");
            }

            if (!IsValidCategory(dto.Category))
            {
                return Result<AccountTreeNodeDto>.Fail("Account category must be Asset, Liability, Equity, Revenue, or Expense.");
            }

            Account? parent = null;
            var parentLevel = 0;
            string accountCode;

            if (dto.ParentId.HasValue)
            {
                parent = await _uow.Accounts.GetByIdAsync(dto.ParentId.Value);
                if (parent == null)
                {
                    return Result<AccountTreeNodeDto>.Fail("Parent account was not found.");
                }

                parentLevel = parent.AccountLevel ?? 1;
                if (parentLevel >= 5)
                {
                    return Result<AccountTreeNodeDto>.Fail("Cannot create child account under level 5 posting account.");
                }

                var siblingCount = await _uow.Accounts.GetAllAsQueryable()
                    .Where(x => x.ParentAccountId == parent.Id)
                    .CountAsync();

                accountCode = AccountCodeHelper.GenerateCode(parent, siblingCount + 1);
            }
            else
            {
                var rootCount = await _uow.Accounts.GetAllAsQueryable()
                    .Where(x => x.ParentAccountId == null)
                    .CountAsync();
                accountCode = (rootCount + 1).ToString();
            }

            var level = parentLevel + 1;
            var isPosting = level == 5;

            var account = new Account
            {
                Name = dto.Name.Trim(),
                ParentAccountId = dto.ParentId,
                IsPosting = isPosting,
                IsActive = true,
                AccountCode = accountCode,
                AccountLevel = level,
                AccountNature = dto.Nature,
                AccountCategory = dto.Category,
                AccountTypeCode = ResolveTypeCode(dto.Category),
                // Keep existing required fields consistent with current model.
                Code = accountCode,
                AccountType = ResolveAccountType(dto.Category),
                Level = level,
                NormalBalanceType = dto.Nature.Equals("Debit", StringComparison.OrdinalIgnoreCase)
                    ? NormalBalanceType.Debit
                    : NormalBalanceType.Credit,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            await _uow.Accounts.AddAsync(account);
            await _uow.CommitAsync();

            return Result<AccountTreeNodeDto>.Ok(ToNode(account), "Account created successfully.");
        }

        public async Task<Result<AccountTreeNodeDto>> UpdateAsync(int id, UpdateAccountNodeDto dto)
        {
            var account = await _uow.Accounts.GetByIdAsyncForUpdate(id);
            if (account == null)
            {
                return Result<AccountTreeNodeDto>.Fail("Account was not found.");
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return Result<AccountTreeNodeDto>.Fail("Account name is required.");
            }

            account.Name = dto.Name.Trim();
            account.IsActive = dto.IsActive;
            account.UpdatedDate = DateTime.Now;

            await _uow.CommitAsync();
            return Result<AccountTreeNodeDto>.Ok(ToNode(account), "Account updated successfully.");
        }

        public async Task<Result<bool>> SoftDeleteAsync(int id)
        {
            var account = await _uow.Accounts.GetByIdAsyncForUpdate(id);
            if (account == null)
            {
                return Result<bool>.Fail("Account was not found.");
            }

            var hasActiveChildren = await _uow.Accounts.GetAllAsQueryable()
                .AnyAsync(x => x.ParentAccountId == id && x.IsActive);
            if (hasActiveChildren)
            {
                return Result<bool>.Fail("Cannot deactivate account because it has active children.");
            }

            var postedStatus = JournalEntryStatus.Posted;
            var hasPostedLines = await _uow.GetRepository<JournalEntryLine>()
                .GetAllAsQueryable()
                .Where(x => x.AccountId == id)
                .Join(
                    _uow.JournalEntries.GetAllAsQueryable(),
                    line => line.JournalEntryId,
                    entry => entry.Id,
                    (line, entry) => entry.Status)
                .AnyAsync(status => status == postedStatus);

            if (hasPostedLines)
            {
                return Result<bool>.Fail("Cannot deactivate account because it has posted journal lines.");
            }

            account.IsActive = false;
            account.UpdatedDate = DateTime.Now;
            await _uow.CommitAsync();

            return Result<bool>.Ok(true, "Account deactivated successfully.");
        }

        public async Task<Result<AccountBalanceDto>> GetBalanceAsync(int id)
        {
            var exists = await _uow.Accounts.AnyAsync(x => x.Id == id);
            if (!exists)
            {
                return Result<AccountBalanceDto>.Fail("Account was not found.");
            }

            var postedStatus = JournalEntryStatus.Posted;
            var balances = await _uow.GetRepository<JournalEntryLine>()
                .GetAllAsQueryable()
                .Where(x => x.AccountId == id)
                .Join(
                    _uow.JournalEntries.GetAllAsQueryable(),
                    line => line.JournalEntryId,
                    entry => entry.Id,
                    (line, entry) => new { line.Debit, line.Credit, entry.Status })
                .Where(x => x.Status == postedStatus)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Debit = g.Sum(x => x.Debit),
                    Credit = g.Sum(x => x.Credit)
                })
                .FirstOrDefaultAsync();

            var debit = balances?.Debit ?? 0m;
            var credit = balances?.Credit ?? 0m;
            var dto = new AccountBalanceDto
            {
                DebitBalance = debit,
                CreditBalance = credit,
                NetBalance = debit - credit
            };

            return Result<AccountBalanceDto>.Ok(dto);
        }

        private static AccountTreeNodeDto ToNode(Account account)
        {
            return new AccountTreeNodeDto
            {
                Id = account.Id,
                Name = account.Name,
                Code = account.AccountCode ?? account.Code,
                Level = account.AccountLevel,
                Nature = account.AccountNature,
                Category = account.AccountCategory,
                IsPosting = account.IsPosting,
                IsActive = account.IsActive
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

        private static Domain.Accounting.Enums.AccountType ResolveAccountType(string category)
        {
            if (category.Equals("Asset", StringComparison.OrdinalIgnoreCase))
                return Domain.Accounting.Enums.AccountType.Asset;
            if (category.Equals("Liability", StringComparison.OrdinalIgnoreCase))
                return Domain.Accounting.Enums.AccountType.Liability;
            if (category.Equals("Equity", StringComparison.OrdinalIgnoreCase))
                return Domain.Accounting.Enums.AccountType.Equity;
            if (category.Equals("Revenue", StringComparison.OrdinalIgnoreCase))
                return Domain.Accounting.Enums.AccountType.Revenue;
            return Domain.Accounting.Enums.AccountType.Expense;
        }
    }
}

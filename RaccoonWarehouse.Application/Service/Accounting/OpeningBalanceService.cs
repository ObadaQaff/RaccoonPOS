using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.AccountOpeningBalances;
using RaccoonWarehouse.Domain.Accounting.AccountOpeningBalances.DTOs;

namespace RaccoonWarehouse.Application.Service.Accounting
{
    public class OpeningBalanceService
    {
        private readonly ApplicationDbContext _dbContext;

        public OpeningBalanceService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task UpsertAsync(int accountId, int fiscalYearId, decimal debitAmount, decimal creditAmount)
        {
            if (debitAmount < 0 || creditAmount < 0)
            {
                throw new InvalidOperationException("Debit and credit amounts cannot be negative.");
            }

            var accountExists = await _dbContext.Accounts.AnyAsync(x => x.Id == accountId);
            if (!accountExists)
            {
                throw new InvalidOperationException("Account was not found.");
            }

            var fiscalYearExists = await _dbContext.FiscalYears.AnyAsync(x => x.Id == fiscalYearId);
            if (!fiscalYearExists)
            {
                throw new InvalidOperationException("Fiscal year was not found.");
            }

            var openingBalance = await _dbContext.AccountOpeningBalances.FirstOrDefaultAsync(x =>
                x.AccountId == accountId &&
                x.FiscalYearId == fiscalYearId &&
                x.BranchId == null &&
                x.CostCenterId == null &&
                x.WarehouseId == null &&
                x.PartyUserId == null);

            if (openingBalance == null)
            {
                openingBalance = new AccountOpeningBalance
                {
                    AccountId = accountId,
                    FiscalYearId = fiscalYearId,
                    Debit = debitAmount,
                    Credit = creditAmount,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                };
                _dbContext.AccountOpeningBalances.Add(openingBalance);
            }
            else
            {
                openingBalance.Debit = debitAmount;
                openingBalance.Credit = creditAmount;
                openingBalance.UpdatedDate = DateTime.Now;
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<OpeningBalanceDto>> GetAllAsync(int fiscalYearId)
        {
            return await _dbContext.AccountOpeningBalances
                .AsNoTracking()
                .Where(x =>
                    x.FiscalYearId == fiscalYearId &&
                    x.BranchId == null &&
                    x.CostCenterId == null &&
                    x.WarehouseId == null &&
                    x.PartyUserId == null)
                .Join(
                    _dbContext.Accounts.AsNoTracking(),
                    ob => ob.AccountId,
                    a => a.Id,
                    (ob, a) => new OpeningBalanceDto
                    {
                        AccountId = ob.AccountId,
                        AccountName = a.Name,
                        FiscalYearId = ob.FiscalYearId,
                        DebitAmount = ob.Debit,
                        CreditAmount = ob.Credit
                    })
                .OrderBy(x => x.AccountName)
                .ToListAsync();
        }

        public async Task<(bool IsBalanced, decimal Difference)> ValidateBalancedAsync(int fiscalYearId)
        {
            var totals = await _dbContext.AccountOpeningBalances
                .AsNoTracking()
                .Where(x =>
                    x.FiscalYearId == fiscalYearId &&
                    x.BranchId == null &&
                    x.CostCenterId == null &&
                    x.WarehouseId == null &&
                    x.PartyUserId == null)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Debit = g.Sum(x => x.Debit),
                    Credit = g.Sum(x => x.Credit)
                })
                .FirstOrDefaultAsync();

            var totalDebit = totals?.Debit ?? 0m;
            var totalCredit = totals?.Credit ?? 0m;
            var difference = totalDebit - totalCredit;
            return (difference == 0m, difference);
        }
    }
}

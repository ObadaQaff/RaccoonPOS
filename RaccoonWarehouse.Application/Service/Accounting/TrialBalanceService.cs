using RaccoonWarehouse.Domain.Reports.Accounting.Dtos;
using RaccoonWarehouse.Domain.Reports.Accounting.Filters;

namespace RaccoonWarehouse.Application.Service.Accounting
{
    public class TrialBalanceService
    {
        private readonly IAccountingService _accountingService;

        public TrialBalanceService(IAccountingService accountingService)
        {
            _accountingService = accountingService;
        }

        public async Task<List<TrialBalanceLine>> GetAsync(int fiscalYearId, DateTime fromDate, DateTime toDate)
        {
            _ = fiscalYearId;

            var result = await _accountingService.GetTrialBalanceAsync(new TrialBalanceFilterDto
            {
                From = fromDate.Date,
                To = toDate.Date.AddDays(1).AddTicks(-1),
                IncludePostedOnly = true,
                IncludeZeroBalances = false
            });

            if (!result.Success || result.Data.rows == null)
                return new List<TrialBalanceLine>();

            return result.Data.rows
                .Select(x => new TrialBalanceLine
                {
                    AccountId = x.AccountId,
                    AccountCode = x.AccountCode,
                    AccountName = x.AccountName,
                    DebitBalance = x.ClosingDebit,
                    CreditBalance = x.ClosingCredit
                })
                .ToList();
        }
    }
}

using RaccoonWarehouse.Domain.Reports.Accounting.Dtos;
using RaccoonWarehouse.Domain.Reports.Accounting.Filters;

namespace RaccoonWarehouse.Application.Service.Accounting
{
    public class GeneralLedgerService
    {
        private readonly IAccountingService _accountingService;

        public GeneralLedgerService(IAccountingService accountingService)
        {
            _accountingService = accountingService;
        }

        public async Task<List<LedgerLine>> GetAsync(int accountId, DateTime fromDate, DateTime toDate)
        {
            var result = await _accountingService.GetGeneralLedgerAsync(new GeneralLedgerFilterDto
            {
                AccountId = accountId,
                From = fromDate.Date,
                To = toDate.Date.AddDays(1).AddTicks(-1),
                IncludePostedOnly = true,
                IncludeOpeningBalance = true
            });

            if (!result.Success || result.Data == null)
                return new List<LedgerLine>();

            var accountLedger = result.Data.FirstOrDefault();
            if (accountLedger == null)
                return new List<LedgerLine>();

            return accountLedger.Rows
                .Select(x => new LedgerLine
                {
                    Date = x.EntryDate,
                    JournalEntryNumber = x.EntryNumber,
                    Description = x.Description,
                    Debit = x.Debit,
                    Credit = x.Credit,
                    RunningBalance = x.RunningBalance,
                    ReferenceType = x.ReferenceType,
                    ReferenceId = x.ReferenceId
                })
                .ToList();
        }
    }
}

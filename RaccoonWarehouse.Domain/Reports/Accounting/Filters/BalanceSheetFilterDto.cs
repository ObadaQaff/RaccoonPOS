namespace RaccoonWarehouse.Domain.Reports.Accounting.Filters
{
    public class BalanceSheetFilterDto
    {
        public DateTime AsOfDate { get; set; }
        public bool IncludeZeroBalances { get; set; }
        public bool IncludeInactiveAccounts { get; set; }
        public bool IncludePostedOnly { get; set; } = true;
    }
}

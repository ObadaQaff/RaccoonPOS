namespace RaccoonWarehouse.Domain.Reports.Accounting.Filters
{
    public class TrialBalanceFilterDto
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public bool IncludeZeroBalances { get; set; }
        public bool IncludePostedOnly { get; set; } = true;
    }
}

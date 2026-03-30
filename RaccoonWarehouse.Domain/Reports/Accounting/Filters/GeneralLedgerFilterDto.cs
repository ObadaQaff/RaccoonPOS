namespace RaccoonWarehouse.Domain.Reports.Accounting.Filters
{
    public class GeneralLedgerFilterDto
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public int? AccountId { get; set; }
        public bool IncludeOpeningBalance { get; set; } = true;
        public bool IncludePostedOnly { get; set; } = true;
    }
}

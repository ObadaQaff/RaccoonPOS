namespace RaccoonWarehouse.Domain.Reports.Financial.Dtos
{
    public class ReportLine
    {
        public int AccountId { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal CurrentPeriod { get; set; }
        public decimal ComparePeriod { get; set; }
        public decimal Variance { get; set; }
    }
}

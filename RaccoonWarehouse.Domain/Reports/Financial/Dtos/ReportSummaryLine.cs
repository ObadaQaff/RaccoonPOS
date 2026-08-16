namespace RaccoonWarehouse.Domain.Reports.Financial.Dtos
{
    public class ReportSummaryLine
    {
        public string Name { get; set; } = string.Empty;
        public decimal CurrentPeriod { get; set; }
        public decimal ComparePeriod { get; set; }
        public decimal Variance { get; set; }
    }
}

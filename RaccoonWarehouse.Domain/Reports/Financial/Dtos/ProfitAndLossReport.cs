namespace RaccoonWarehouse.Domain.Reports.Financial.Dtos
{
    public class ProfitAndLossReport
    {
        public List<ReportLine> Revenue { get; set; } = new();
        public List<ReportLine> CostOfGoodsSold { get; set; } = new();
        public ReportSummaryLine GrossProfit { get; set; } = new();
        public List<ReportLine> OperatingExpenses { get; set; } = new();
        public ReportSummaryLine OperatingProfit { get; set; } = new();
        public List<ReportLine> OtherIncome { get; set; } = new();
        public List<ReportLine> OtherExpenses { get; set; } = new();
        public ReportSummaryLine NetProfit { get; set; } = new();
    }
}

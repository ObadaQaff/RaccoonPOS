namespace RaccoonWarehouse.Domain.Reports.Dashboard
{
    public class DashboardSummary
    {
        public decimal CurrentMonthRevenue { get; set; }
        public decimal CurrentMonthExpenses { get; set; }
        public decimal NetProfit { get; set; }
        public decimal ProfitMarginPct { get; set; }
        public decimal TotalAssets { get; set; }
        public decimal TotalLiabilities { get; set; }
        public decimal Equity { get; set; }
        public decimal CurrentRatio { get; set; }
        public decimal QuickRatio { get; set; }
        public decimal DebtToEquityRatio { get; set; }
        public decimal TotalOutstandingAR { get; set; }
        public decimal TotalOutstandingAP { get; set; }
        public decimal CashAndBankBalance { get; set; }
    }
}

namespace RaccoonWarehouse.Domain.Reports.Financial.Dtos
{
    public class CashFlowReport
    {
        public CashFlowSectionLine NetIncome { get; set; } = new();
        public List<CashFlowSectionLine> Adjustments { get; set; } = new();
        public List<CashFlowSectionLine> WorkingCapitalChanges { get; set; } = new();
        public CashFlowSectionLine CashFromOperatingActivities { get; set; } = new();
        public List<CashFlowSectionLine> CashFromInvestingActivities { get; set; } = new();
        public List<CashFlowSectionLine> CashFromFinancingActivities { get; set; } = new();
        public CashFlowSectionLine NetChangeInCash { get; set; } = new();
        public CashFlowSectionLine OpeningPlusNetEqualsClosing { get; set; } = new();
        public decimal OpeningCash { get; set; }
        public decimal ClosingCash { get; set; }
    }
}

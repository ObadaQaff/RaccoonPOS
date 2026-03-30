namespace RaccoonWarehouse.Domain.Reports.Accounting.Dtos
{
    public class BalanceSheetDto
    {
        public DateTime AsOfDate { get; set; }
        public BalanceSheetSectionDto Assets { get; set; } = new();
        public BalanceSheetSectionDto Liabilities { get; set; } = new();
        public BalanceSheetSectionDto Equity { get; set; } = new();
        public decimal TotalLiabilitiesAndEquity => Liabilities.Total + Equity.Total;
    }
}

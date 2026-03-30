namespace RaccoonWarehouse.Domain.Reports.Accounting.Dtos
{
    public class TrialBalanceSummaryDto
    {
        public decimal TotalOpeningBalance { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal TotalClosingDebit { get; set; }
        public decimal TotalClosingCredit { get; set; }
        public bool IsBalanced => TotalClosingDebit == TotalClosingCredit;
    }
}

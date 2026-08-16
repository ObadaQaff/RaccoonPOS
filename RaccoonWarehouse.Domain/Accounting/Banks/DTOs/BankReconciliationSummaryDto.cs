namespace RaccoonWarehouse.Domain.Accounting.Banks.DTOs
{
    public class BankReconciliationSummaryDto
    {
        public decimal Matched { get; set; }
        public decimal Unmatched { get; set; }
        public decimal Difference { get; set; }
    }
}

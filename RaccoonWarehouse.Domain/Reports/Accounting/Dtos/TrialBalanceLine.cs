namespace RaccoonWarehouse.Domain.Reports.Accounting.Dtos
{
    public class TrialBalanceLine
    {
        public int AccountId { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal DebitBalance { get; set; }
        public decimal CreditBalance { get; set; }
    }
}

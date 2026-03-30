using RaccoonWarehouse.Domain.Accounting.Enums;

namespace RaccoonWarehouse.Domain.Reports.Accounting.Dtos
{
    public class TrialBalanceRowDto
    {
        public int AccountId { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public AccountType AccountType { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal ClosingBalance { get; set; }
        public decimal ClosingDebit { get; set; }
        public decimal ClosingCredit { get; set; }
    }
}

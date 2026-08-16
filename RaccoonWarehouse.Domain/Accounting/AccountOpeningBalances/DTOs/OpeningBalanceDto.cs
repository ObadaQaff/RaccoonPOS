namespace RaccoonWarehouse.Domain.Accounting.AccountOpeningBalances.DTOs
{
    public class OpeningBalanceDto
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public int FiscalYearId { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
    }
}

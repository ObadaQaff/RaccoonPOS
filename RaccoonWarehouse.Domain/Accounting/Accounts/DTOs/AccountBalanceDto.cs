namespace RaccoonWarehouse.Domain.Accounting.Accounts.DTOs
{
    /// <summary>
    /// Represents debit, credit and net balance for an account.
    /// </summary>
    public class AccountBalanceDto
    {
        public decimal DebitBalance { get; set; }
        public decimal CreditBalance { get; set; }
        public decimal NetBalance { get; set; }
    }
}

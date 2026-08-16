namespace RaccoonWarehouse.Domain.Accounting.Banks.DTOs
{
    public class StatementLineDto
    {
        public DateTime TransactionDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Reference { get; set; }
    }
}

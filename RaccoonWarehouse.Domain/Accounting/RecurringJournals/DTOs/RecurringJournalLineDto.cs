namespace RaccoonWarehouse.Domain.Accounting.RecurringJournals.DTOs
{
    public class RecurringJournalLineDto
    {
        public int? Id { get; set; }
        public int AccountId { get; set; }
        public int? CostCenterId { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public string? Description { get; set; }
    }
}

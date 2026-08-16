namespace RaccoonWarehouse.Domain.Reports.Accounting.Dtos
{
    public class LedgerLine
    {
        public DateTime Date { get; set; }
        public string JournalEntryNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal RunningBalance { get; set; }
        public string? ReferenceType { get; set; }
        public int? ReferenceId { get; set; }
    }
}

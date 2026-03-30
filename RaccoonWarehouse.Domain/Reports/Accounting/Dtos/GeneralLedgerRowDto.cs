namespace RaccoonWarehouse.Domain.Reports.Accounting.Dtos
{
    public class GeneralLedgerRowDto
    {
        public DateTime EntryDate { get; set; }
        public string EntryNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ReferenceType { get; set; }
        public int? ReferenceId { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal RunningBalance { get; set; }
        public bool IsOpeningBalance { get; set; }
    }
}

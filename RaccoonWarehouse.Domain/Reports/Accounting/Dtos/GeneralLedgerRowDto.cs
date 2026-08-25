namespace RaccoonWarehouse.Domain.Reports.Accounting.Dtos
{
    public class GeneralLedgerRowDto
    {
        public DateTime EntryDate { get; set; }
        public string EntryNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ReferenceType { get; set; }
        public int? ReferenceId { get; set; }
        public string ReferenceLabel => ReferenceType switch
        {
            "Invoice" => $"فاتورة #{ReferenceId}",
            "Voucher" => $"سند #{ReferenceId}",
            "StockDocument" => $"سند مخزون #{ReferenceId}",
            "FinancialTransaction" => $"حركة مالية #{ReferenceId}",
            "StockAdjustment" => $"تسوية مخزون #{ReferenceId}",
            _ => string.IsNullOrWhiteSpace(ReferenceType) ? string.Empty : $"{ReferenceType} #{ReferenceId}"
        };
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal RunningBalance { get; set; }
        public bool IsOpeningBalance { get; set; }
    }
}

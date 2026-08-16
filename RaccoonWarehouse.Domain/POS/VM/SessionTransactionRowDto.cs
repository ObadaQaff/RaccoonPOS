using System;

namespace RaccoonWarehouse.Domain.POS.VM
{
    public class SessionTransactionRowDto
    {
        public DateTime Date { get; set; }
        public string DocumentTypeText { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string? ReferenceText { get; set; }
        public string? DirectionText { get; set; }
        public string? MethodText { get; set; }
        public decimal Amount { get; set; }
        public string? CashierName { get; set; }
        public string? Notes { get; set; }
        public string? StatusText { get; set; }
        public string? ReferenceType { get; set; }
        public int? ReferenceId { get; set; }
        public string SourceKind { get; set; } = string.Empty;

        public bool IsOpenable => !string.IsNullOrWhiteSpace(ReferenceType) && ReferenceId.HasValue && ReferenceId.Value > 0;
    }
}

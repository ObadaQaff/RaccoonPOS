using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Accounting.Periods;
using RaccoonWarehouse.Domain.Base;

namespace RaccoonWarehouse.Domain.Accounting.JournalEntries
{
    public class JournalEntry : BaseEntity
    {
        public string EntryNumber { get; set; } = string.Empty;
        public DateTime EntryDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public JournalEntryStatus Status { get; set; } = JournalEntryStatus.Draft;
        public bool IsDraft { get; set; } = true;
        public AccountingSourceType? SourceType { get; set; }
        public int? SourceId { get; set; }
        public string? ReferenceType { get; set; }
        public int? ReferenceId { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }
        public int? FiscalYearId { get; set; }
        public FiscalYear? FiscalYear { get; set; }
        public int? AccountingPeriodId { get; set; }
        public AccountingPeriod? AccountingPeriod { get; set; }
        public int? BranchId { get; set; }
        public int? WarehouseId { get; set; }
        public int? CashierSessionId { get; set; }
        public int? CurrencyId { get; set; }
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public ICollection<JournalEntryLine> Lines { get; set; } = new List<JournalEntryLine>();
    }
}

using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Base;

namespace RaccoonWarehouse.Domain.Accounting.JournalEntries
{
    public class JournalEntry : BaseEntity
    {
        public string EntryNumber { get; set; } = string.Empty;
        public DateTime EntryDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public JournalEntryStatus Status { get; set; } = JournalEntryStatus.Draft;
        public string? ReferenceType { get; set; }
        public int? ReferenceId { get; set; }
        public ICollection<JournalEntryLine> Lines { get; set; } = new List<JournalEntryLine>();
    }
}

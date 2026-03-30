using RaccoonWarehouse.Domain.Accounting.Accounts;
using RaccoonWarehouse.Domain.Base;

namespace RaccoonWarehouse.Domain.Accounting.JournalEntries
{
    public class JournalEntryLine : BaseEntity
    {
        public int JournalEntryId { get; set; }
        public JournalEntry JournalEntry { get; set; } = null!;
        public int AccountId { get; set; }
        public Account Account { get; set; } = null!;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string? Description { get; set; }
    }
}

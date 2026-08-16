using RaccoonWarehouse.Domain.Accounting.RecurringJournals.Enums;
using RaccoonWarehouse.Domain.Base;

namespace RaccoonWarehouse.Domain.Accounting.RecurringJournals
{
    public class RecurringJournal : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public RecurringFrequency Frequency { get; set; }
        public DateTime NextRunDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? LastPostedDate { get; set; }
        public ICollection<RecurringJournalLine> Lines { get; set; } = new List<RecurringJournalLine>();
    }
}

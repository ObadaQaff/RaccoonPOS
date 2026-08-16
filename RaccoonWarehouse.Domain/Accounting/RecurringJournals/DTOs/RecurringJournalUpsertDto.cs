using RaccoonWarehouse.Domain.Accounting.RecurringJournals.Enums;

namespace RaccoonWarehouse.Domain.Accounting.RecurringJournals.DTOs
{
    public class RecurringJournalUpsertDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public RecurringFrequency Frequency { get; set; }
        public DateTime NextRunDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public List<RecurringJournalLineDto> Lines { get; set; } = new();
    }
}

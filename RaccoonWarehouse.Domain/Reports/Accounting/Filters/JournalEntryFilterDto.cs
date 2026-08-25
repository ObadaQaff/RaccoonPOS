using RaccoonWarehouse.Domain.Accounting.Enums;

namespace RaccoonWarehouse.Domain.Reports.Accounting.Filters
{
    public class JournalEntryFilterDto
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public JournalEntryStatus? Status { get; set; }
        public string? ReferenceType { get; set; }
        public string? ReferenceSearch { get; set; }
        public string? AccountSearch { get; set; }
    }
}

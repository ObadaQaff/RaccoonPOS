using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Accounting.Enums;

namespace RaccoonWarehouse.Domain.Accounting.JournalEntries.DTOs
{
    public class JournalEntryReadDto : IBaseDto
    {
        public int Id { get; set; }
        public string EntryNumber { get; set; } = string.Empty;
        public DateTime EntryDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public JournalEntryStatus Status { get; set; }
        public string? ReferenceType { get; set; }
        public int? ReferenceId { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public List<JournalEntryLineReadDto> Lines { get; set; } = new();
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}

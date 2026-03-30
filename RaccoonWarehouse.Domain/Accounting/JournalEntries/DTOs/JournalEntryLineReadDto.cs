using RaccoonWarehouse.Core.EntityAndDtoStructure;

namespace RaccoonWarehouse.Domain.Accounting.JournalEntries.DTOs
{
    public class JournalEntryLineReadDto : IBaseDto
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}

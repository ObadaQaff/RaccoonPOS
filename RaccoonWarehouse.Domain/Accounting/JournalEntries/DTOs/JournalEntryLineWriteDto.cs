using RaccoonWarehouse.Core.EntityAndDtoStructure;

namespace RaccoonWarehouse.Domain.Accounting.JournalEntries.DTOs
{
    public class JournalEntryLineWriteDto : IBaseDto
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}

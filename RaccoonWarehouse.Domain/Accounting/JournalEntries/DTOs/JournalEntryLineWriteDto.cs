using RaccoonWarehouse.Core.EntityAndDtoStructure;

namespace RaccoonWarehouse.Domain.Accounting.JournalEntries.DTOs
{
    public class JournalEntryLineWriteDto : IBaseDto
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public int? PartyUserId { get; set; }
        public int? CustomerId { get; set; }
        public int? SupplierId { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public int? CostCenterId { get; set; }
        public int? TaxRateId { get; set; }
        public decimal? TaxAmount { get; set; }
        public int? CurrencyId { get; set; }
        public decimal? ForeignAmount { get; set; }
        public decimal? ExchangeRate { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}

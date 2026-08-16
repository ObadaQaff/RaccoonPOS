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
        public int LineNumber { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string? Description { get; set; }
        public int? PartyUserId { get; set; }
        public int? CustomerId { get; set; }
        public int? SupplierId { get; set; }
        public int? CashierId { get; set; }
        public int? WarehouseId { get; set; }
        public int? CostCenterId { get; set; }
        public int? TaxRateId { get; set; }
        public decimal? TaxAmount { get; set; }
        public int? CurrencyId { get; set; }
        public decimal? ForeignAmount { get; set; }
        public decimal? ExchangeRate { get; set; }
        public int? BranchId { get; set; }
        public int? InvoiceId { get; set; }
        public int? VoucherId { get; set; }
        public int? StockDocumentId { get; set; }
        public int? FinancialTransactionId { get; set; }
        public string? ReferenceType { get; set; }
        public int? ReferenceId { get; set; }
    }
}

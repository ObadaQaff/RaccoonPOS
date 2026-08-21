using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Checks;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.StockItems;
using RaccoonWarehouse.Domain.Users;

namespace RaccoonWarehouse.Domain.StockDocuments
{
    public class StockDocument : BaseEntity
    {
        public string DocumentNumber { get; set; } = string.Empty;
        public StockVoucherType Type { get; set; }
        public DateTime? DocumentDate { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }
        public int? SupplierId { get; set; }
        public User? Supplier { get; set; }
        public int? WarehouseId { get; set; }
        public int? BranchId { get; set; }
        public AccountingPostingStatus PostingStatus { get; set; } = AccountingPostingStatus.NotPosted;
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public decimal? DiscountAmount { get; set; }
        public PaymentType? PaymentType { get; set; }
        public ICollection<Check>? Checks { get; set; } = new List<Check>();
        public List<StockItem> Items { get; set; } = new();
    }
}

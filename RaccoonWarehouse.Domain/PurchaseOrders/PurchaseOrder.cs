using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users;

namespace RaccoonWarehouse.Domain.PurchaseOrders
{
    public class PurchaseOrder : BaseEntity
    {
        public string OrderNumber { get; set; } = string.Empty;
        public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Submitted;
        public int? SupplierId { get; set; }
        public User? Supplier { get; set; }
        public int? WarehouseId { get; set; }
        public int? CurrencyId { get; set; }
        public PaymentType? PaymentType { get; set; }
        public DateTime? DocumentDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Notes { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalAmount { get; set; }
        public bool CostPriceIncludesTax { get; set; } = true;
        public int? CreatedBy { get; set; }
        public int? SubmittedBy { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public int? RejectedBy { get; set; }
        public DateTime? RejectedDate { get; set; }
        public string? RejectionReason { get; set; }
        public int? ConvertedInvoiceId { get; set; }
        public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
    }

    public class PurchaseOrderLine : BaseEntity
    {
        public int PurchaseOrderId { get; set; }
        public PurchaseOrder PurchaseOrder { get; set; } = null!;
        public int? PurchaseRequestLineId { get; set; }
        public int ProductId { get; set; }
        public int ProductUnitId { get; set; }
        public decimal Quantity { get; set; }
        public decimal QuantityPerUnitSnapshot { get; set; } = 1m;
        public decimal BaseQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal UnitCost { get; set; }
        public decimal LineDiscountAmount { get; set; }
        public decimal FreeQuantity { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool TaxExempt { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal LineSubTotal { get; set; }
    }
}

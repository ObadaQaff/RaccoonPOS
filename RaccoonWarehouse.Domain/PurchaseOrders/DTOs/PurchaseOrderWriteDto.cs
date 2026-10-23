using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.PurchaseOrders.DTOs
{
    public class PurchaseOrderWriteDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string? SupplierName { get; set; }
        public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Submitted;
        public PurchaseOrderPaymentStatus PaymentStatus { get; set; } = PurchaseOrderPaymentStatus.Unpaid;
        public int? SupplierId { get; set; }
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
        public int? PaidBy { get; set; }
        public DateTime? PaidDate { get; set; }
        public string? PaymentNote { get; set; }
        public int? RejectedBy { get; set; }
        public DateTime? RejectedDate { get; set; }
        public string? RejectionReason { get; set; }
        public int? ConvertedInvoiceId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public ICollection<PurchaseOrderLineWriteDto> Lines { get; set; } = new List<PurchaseOrderLineWriteDto>();
    }
}

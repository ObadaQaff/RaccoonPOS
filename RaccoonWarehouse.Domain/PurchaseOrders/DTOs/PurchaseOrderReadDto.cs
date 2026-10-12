using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.PurchaseOrders.DTOs
{
    public class PurchaseOrderReadDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public PurchaseOrderStatus Status { get; set; }
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public DateTime? DocumentDate { get; set; }
        public decimal TotalAmount { get; set; }
        public int LineCount { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public int? ConvertedInvoiceId { get; set; }
    }
}

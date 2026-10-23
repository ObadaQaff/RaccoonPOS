using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.PurchaseOrders.DTOs
{
    public class PurchaseOrderReadDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public PurchaseOrderStatus Status { get; set; }
        public PurchaseOrderPaymentStatus PaymentStatus { get; set; }
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public DateTime? DocumentDate { get; set; }
        public decimal TotalAmount { get; set; }
        public int LineCount { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public int? ConvertedInvoiceId { get; set; }
        public IReadOnlyList<PurchaseOrderFilterLineDto> FilterLines => _filterLines;
        private readonly List<PurchaseOrderFilterLineDto> _filterLines = new();
        public void SetFilterLines(IEnumerable<PurchaseOrderFilterLineDto> lines)
        {
            _filterLines.Clear();
            _filterLines.AddRange(lines);
        }
    }

    public sealed class PurchaseOrderFilterLineDto
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? Barcode { get; set; }
    }

}

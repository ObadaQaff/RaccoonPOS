namespace RaccoonWarehouse.Domain.PurchaseOrders.DTOs
{
    public class PurchaseOrderLineWriteDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int ProductUnitId { get; set; }
        public string? ProductName { get; set; }
        public string? UnitName { get; set; }
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

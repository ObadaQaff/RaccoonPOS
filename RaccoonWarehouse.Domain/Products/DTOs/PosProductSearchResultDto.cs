namespace RaccoonWarehouse.Domain.Products.DTOs
{
    public sealed class PosProductSearchResultDto
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public long? ItemCode { get; set; }
        public bool? TaxExempt { get; set; }
        public decimal? TaxRate { get; set; }
        public int ProductUnitId { get; set; }
        public string? AlternateBarcode { get; set; }
        public string? UnitName { get; set; }
        public decimal UnitSalePrice { get; set; }
        public decimal UnitPurchasePrice { get; set; }
        public decimal QuantityPerUnit { get; set; }
        public bool IsBaseUnit { get; set; }
        public bool IsDefaultSaleUnit { get; set; }
        public bool IsDefaultPurchaseUnit { get; set; }
        public decimal StockQuantity { get; set; }
        public decimal StockPurchasePrice { get; set; }
        public decimal StockSalePrice { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}

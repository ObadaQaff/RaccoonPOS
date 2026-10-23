namespace RaccoonWarehouse.Domain.Products.DTOs
{
    public sealed class PosProductSearchResultDto
    {
        public int ProductId { get; set; }
        public int SubCategoryId { get; set; }
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
        public bool AllowFractionalSale { get; set; }
        public decimal FractionalPartsPerUnit { get; set; }
        public string? FractionalPartName { get; set; }
        public decimal? FractionalSalePrice { get; set; }
        public decimal? FractionalSaleCost { get; set; }
        public bool IsBaseUnit { get; set; }
        public bool IsDefaultSaleUnit { get; set; }
        public bool IsDefaultPurchaseUnit { get; set; }
        public decimal StockQuantity { get; set; }
        public decimal StockPurchasePrice { get; set; }
        public decimal StockSalePrice { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool AvailableToSale { get; set; }
        public string StockStatus { get; set; } = "OutOfStock";
        public bool HasStock => StockQuantity > 0m;
        public string? SupplierName { get; set; }
        public int? SupplierId { get; set; }
    }
}

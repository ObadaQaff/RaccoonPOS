namespace RaccoonWarehouse.Domain.Reports.Stocks.Dtos
{
    public class PriceListRowDto
    {
        public int ProductId { get; set; }
        public string ItemID { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public bool IsDefaultSaleUnit { get; set; }
        public bool IsDefaultPurchaseUnit { get; set; }
    }
}

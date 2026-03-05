namespace RaccoonWarehouse.Domain.Reports.Stocks.Dtos
{
    public class ItemCostDetailRowDto
    {
        public int ProductId { get; set; }
        public string ItemID { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Cost { get; set; }
        public decimal Total { get; set; }
        public decimal MinimumQuantity { get; set; }
    }
}

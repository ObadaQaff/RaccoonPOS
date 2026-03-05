namespace RaccoonWarehouse.Domain.Reports.Stocks.Dtos
{
    public class StockValuationRowDto
    {
        public int ProductId { get; set; }
        public string ITEMCODE { get; set; } = string.Empty;
        public string? ProductName { get; set; }
        public string? UnitName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }
        public decimal MinimumQuantity { get; set; }
    }
}

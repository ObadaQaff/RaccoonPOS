namespace RaccoonWarehouse.Domain.Reports.Stocks.Dtos
{
    public class StockVarianceRowDto
    {
        public int ProductId { get; set; }
        public string ITEMCODE { get; set; } = string.Empty;
        public string? ProductName { get; set; }
        public string? UnitName { get; set; }
        public decimal CurrentQuantity { get; set; }
        public decimal MinimumQuantity { get; set; }
        public decimal VarianceQuantity { get; set; }
        public string StatusText { get; set; } = string.Empty;
    }
}

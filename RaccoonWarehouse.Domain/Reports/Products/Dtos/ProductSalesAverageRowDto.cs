namespace RaccoonWarehouse.Domain.Reports.Products.Dtos;

public sealed class ProductSalesAverageRowDto
{
    public int ProductId { get; set; }
    public long? ItemCode { get; set; }
    public string? ProductName { get; set; }
    public string? Barcode { get; set; }
    public string? UnitName { get; set; }
    public decimal TotalSoldQuantity { get; set; }
    public decimal AverageMonthlyQuantity { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public DateTime? LastSaleDate { get; set; }
}

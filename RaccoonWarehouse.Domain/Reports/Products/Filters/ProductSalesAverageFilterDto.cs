namespace RaccoonWarehouse.Domain.Reports.Products.Filters;

public sealed class ProductSalesAverageFilterDto
{
    public int Months { get; set; } = 1;
    public string? SearchText { get; set; }
    public int? SupplierId { get; set; }
    public string? ProductName { get; set; }
    public string? ItemCode { get; set; }
}

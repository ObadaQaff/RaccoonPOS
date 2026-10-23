namespace RaccoonWarehouse.Domain.Products.DTOs;

public sealed class ProductColorSelectorItem
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public long? Barcode { get; set; }
}

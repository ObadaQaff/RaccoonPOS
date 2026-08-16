namespace RaccoonWarehouse.Domain.POS.DTOs
{
    public class PosBrowseItemDto
    {
        public int ProductId { get; set; }
        public string? Name { get; set; }
        public long? ItemCode { get; set; }
        public int SubCategoryId { get; set; }
        public decimal CurrentSalePrice { get; set; }
        public decimal AvailableQuantity { get; set; }
        public bool? TaxExempt { get; set; }
        public decimal? TaxRate { get; set; }
    }
}

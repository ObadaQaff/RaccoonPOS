namespace RaccoonWarehouse.Domain.Reports.Financial.Dtos
{
    public class DiscountSummaryRowDto
    {
        public int ProductId { get; set; }
        public string ItemID { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public decimal QuantitySold { get; set; }
        public decimal TotalDiscount { get; set; }
    }
}

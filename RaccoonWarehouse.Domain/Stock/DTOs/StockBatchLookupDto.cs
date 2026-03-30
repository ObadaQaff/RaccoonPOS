using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.Stock.DTOs
{
    public class StockBatchLookupDto
    {
        public int StockLotId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int ProductUnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public decimal OriginalQuantity { get; set; }
        public decimal RemainingQuantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public BatchStatus Status { get; set; }
        public bool IsUsed { get; set; }
        public string DisplayName =>
            $"دفعة #{StockLotId} | {ProductName} | {UnitName} | الكمية {RemainingQuantity:0.###}/{OriginalQuantity:0.###} | شراء {PurchasePrice:0.##} | بيع {SalePrice:0.##}";
    }
}

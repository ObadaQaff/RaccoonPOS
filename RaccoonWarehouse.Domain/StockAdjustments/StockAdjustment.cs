using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.StockLots;
using RaccoonWarehouse.Domain.Users;

namespace RaccoonWarehouse.Domain.StockAdjustments
{
    public class StockAdjustment : BaseEntity
    {
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public int ProductUnitId { get; set; }
        public ProductUnit? ProductUnit { get; set; }
        public int StockLotId { get; set; }
        public StockLot? StockLot { get; set; }
        public int? NewStockLotId { get; set; }
        public StockLot? NewStockLot { get; set; }
        public StockAdjustmentType AdjustmentType { get; set; }
        public decimal QuantityDelta { get; set; }
        public decimal QuantityPerUnitSnapshot { get; set; } = 1m;
        public decimal BaseQuantityDelta { get; set; }
        public decimal? PurchasePrice { get; set; }
        public decimal? SalePrice { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public DateTime AdjustmentDate { get; set; }
        public int? UserId { get; set; }
        public User? User { get; set; }
    }
}

using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.StockAdjustments;
using System;

namespace RaccoonWarehouse.Domain.StockLots
{
    public class StockLot : BaseEntity
    {
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public int ProductUnitId { get; set; }
        public ProductUnit? ProductUnit { get; set; }
        public decimal Quantity { get; set; }
        public decimal RemainingQuantity { get; set; }
        public decimal QuantityPerUnitSnapshot { get; set; } = 1m;
        public decimal BaseQuantity { get; set; }
        public decimal RemainingBaseQuantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? Notes { get; set; }
        public BatchStatus Status { get; set; } = BatchStatus.Active;
        public DateTime? ClosedDate { get; set; }
        public string? ClosedReason { get; set; }
        public int? ClosedByUserId { get; set; }
        public int? ReplacesStockLotId { get; set; }
        public StockLot? ReplacesStockLot { get; set; }
        public int? ReplacedByStockLotId { get; set; }
        public StockLot? ReplacedByStockLot { get; set; }
        public List<StockAdjustment> StockAdjustments { get; set; } = new();
    }
}

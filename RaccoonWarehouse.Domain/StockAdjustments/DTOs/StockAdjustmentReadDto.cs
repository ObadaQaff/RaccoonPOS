using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;

namespace RaccoonWarehouse.Domain.StockAdjustments.DTOs
{
    public class StockAdjustmentReadDto : IBaseDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public ProductReadDto? Product { get; set; }
        public int ProductUnitId { get; set; }
        public ProductUnitReadDto? ProductUnit { get; set; }
        public int StockLotId { get; set; }
        public int? NewStockLotId { get; set; }
        public StockAdjustmentType AdjustmentType { get; set; }
        public decimal QuantityDelta { get; set; }
        public decimal QuantityPerUnitSnapshot { get; set; }
        public decimal BaseQuantityDelta { get; set; }
        public decimal? PurchasePrice { get; set; }
        public decimal? SalePrice { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public DateTime AdjustmentDate { get; set; }
        public int? UserId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}

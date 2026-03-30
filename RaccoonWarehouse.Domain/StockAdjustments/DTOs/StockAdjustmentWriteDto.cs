using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.StockAdjustments.DTOs
{
    public class StockAdjustmentWriteDto : IBaseDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int ProductUnitId { get; set; }
        public int StockLotId { get; set; }
        public int? NewStockLotId { get; set; }
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
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}

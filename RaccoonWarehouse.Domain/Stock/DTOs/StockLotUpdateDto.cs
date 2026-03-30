using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.Stock.DTOs
{
    public class StockLotUpdateDto
    {
        public int StockLotId { get; set; }
        public decimal? SalePrice { get; set; }
        public decimal? PurchasePrice { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? Notes { get; set; }
        public BatchStatus? Status { get; set; }
    }
}

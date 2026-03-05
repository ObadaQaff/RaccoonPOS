using System;

namespace RaccoonWarehouse.Domain.Reports.Stocks.Dtos
{
    public class StockAdjustmentRowDto
    {
        public int TransactionId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string ITEMCODE { get; set; } = string.Empty;
        public string? ProductName { get; set; }
        public string? UnitName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Notes { get; set; }
        public string? CreatedBy { get; set; }
        public string SourceReference { get; set; } = string.Empty;
    }
}

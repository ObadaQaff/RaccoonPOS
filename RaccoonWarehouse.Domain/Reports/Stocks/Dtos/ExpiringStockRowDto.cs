using System;

namespace RaccoonWarehouse.Domain.Reports.Stocks.Dtos
{
    public class ExpiringStockRowDto
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ITEMCODE { get; set; }
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string? UnitName { get; set; }
        public decimal RemainingQuantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal TotalCost { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int DaysRemaining { get; set; }
    }
}

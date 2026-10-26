using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Stock.DTOs
{
    public class CurrentStockLotDto
    {
        public int StockLotId { get; set; }
        public decimal CurrentQuantity { get; set; }
        public decimal SalePrice { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    public class CurrentStockDto
    {
        public int ProductId { get; set; }
        public string ITEMCODE { get; set; } 
        public string? ProductName { get; set; }
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string? UnitName { get; set; }

        public decimal Quantity { get; set; }
        public decimal? MinimumQuantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public DateTime? NearestExpiryDate { get; set; }
        public List<CurrentStockLotDto> Lots { get; set; } = new();

        // Optional: Useful for UI
        public bool IsLowStock => MinimumQuantity.HasValue && Quantity <= MinimumQuantity;
    }
}

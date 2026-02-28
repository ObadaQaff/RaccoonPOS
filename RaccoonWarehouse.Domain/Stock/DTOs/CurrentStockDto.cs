using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Stock.DTOs
{
    public class CurrentStockDto
    {
        public int ProductId { get; set; }
        public string ITEMCODE { get; set; } 
        public string? ProductName { get; set; }
        public string? UnitName { get; set; }

        public decimal Quantity { get; set; }
        public decimal? MinimumQuantity { get; set; }

        // Optional: Useful for UI
        public bool IsLowStock => MinimumQuantity.HasValue && Quantity <= MinimumQuantity;
    }
}

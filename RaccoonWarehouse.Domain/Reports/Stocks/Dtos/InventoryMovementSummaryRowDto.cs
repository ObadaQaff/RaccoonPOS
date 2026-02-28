using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace RaccoonWarehouse.Domain.Stock.DTOs
{
    public class InventoryMovementSummaryRowDto
    {
        public int ProductId { get; set; }
        public int ProductUnitId { get; set; }

        public string? ITEMCODE { get; set; }
        public string? ProductName { get; set; }
        public string? UnitName { get; set; }

        public decimal InQty { get; set; }     // داخل
        public decimal OutQty { get; set; }    // خارج
        public decimal NetQty => InQty - OutQty; // صافي
        public decimal MinimumQuantity { get; set; }
        public string StatusText { get; set; } = "—";
    }
}
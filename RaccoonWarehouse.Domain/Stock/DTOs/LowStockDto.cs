using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Stock.DTOs
{
    public class LowStockDto
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ITEMCODE { get; set; }
        public string? UnitName { get; set; }
        public decimal CurrentQuantity { get; set; }
        public decimal MinimumQuantity { get; set; }

        // Optional: Extra info for UI or reports
        public decimal StockGap => MinimumQuantity - CurrentQuantity;
        public decimal Shortage => MinimumQuantity - CurrentQuantity; // ✅ النقص

    }
}

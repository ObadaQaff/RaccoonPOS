using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.Reports.Stocks.Filters
{
    public class InventoryMovementSummaryFilterDto
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        public int? ProductId { get; set; }   // optional
        public string? ProductSearchText { get; set; } // optional product name or barcode
        public int? SupplierId { get; set; }  // optional
        public bool IncludeInvoices { get; set; } = true; // فواتير كمصدر حركة
        public StockMovementSource Source { get; set; } = StockMovementSource.All;
    }
}

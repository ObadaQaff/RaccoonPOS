using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Reports.Stocks.Filters
{
    public class InventoryMovementSummaryFilterDto
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        public int? ProductId { get; set; }   // optional
        public bool IncludeInvoices { get; set; } = true; // فواتير كمصدر حركة
    }
}

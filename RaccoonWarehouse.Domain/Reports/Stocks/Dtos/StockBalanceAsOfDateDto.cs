using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Reports.Stocks.Dtos
{
        public class StockBalanceByDateDto
        {
            public int ProductId { get; set; }
            public int ProductUnitId { get; set; }

            public string? ITEMCODE { get; set; }
            public string? ProductName { get; set; }
            public string? UnitName { get; set; }

            public decimal Quantity { get; set; }
            public decimal MinimumQuantity { get; set; }

            public string StatusText { get; set; } = "طبيعي";
        }
}

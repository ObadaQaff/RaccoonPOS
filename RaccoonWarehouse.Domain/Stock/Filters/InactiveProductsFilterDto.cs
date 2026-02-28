using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Stock.Filters
{
    public class InactiveProductsFilterDto
    {
        public int DaysWithoutMovement { get; set; } = 30;

        public bool IncludeZeroStockOnly { get; set; } = false;

        public DateTime? AsOfDate { get; set; } // optional (default = Today)
    }
}

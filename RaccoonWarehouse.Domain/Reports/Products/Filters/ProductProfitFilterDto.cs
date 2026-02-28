using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Reports.Products.Filters
{
    public class ProductProfitFilterDto
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        public int? ProductId { get; set; }
        public bool IncludeReturns { get; set; } = true;

        // إذا بدك لاحقاً التقرير حسب الوحدة كمان:
        public bool GroupByUnit { get; set; } = false;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Reports.Financial.Dtos
{
    public class ProfitLossRowDto
    {
        public string Section { get; set; } = "";   // Revenue / COGS / Expenses / Other
        public string Item { get; set; } = "";      // e.g. Sales, Returns, SourceType, etc.
        public decimal Amount { get; set; }
    }
}

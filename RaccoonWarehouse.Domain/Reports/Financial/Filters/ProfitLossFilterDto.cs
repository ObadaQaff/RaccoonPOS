using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Reports.Financial.Filters
{
    public class ProfitLossFilterDto
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public bool IncludeReturns { get; set; } = true;

        // Expenses
        public bool IncludeVoidedTransactions { get; set; } = false;
        // optional
        public int? CashierId { get; set; }
        public int? CashierSessionId { get; set; }
    }
}

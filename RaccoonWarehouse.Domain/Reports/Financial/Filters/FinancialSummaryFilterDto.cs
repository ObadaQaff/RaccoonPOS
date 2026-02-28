using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Reports.Financial.Filters
{
    public class FinancialSummaryFilterDto
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        // optional filters
        public int? BranchId { get; set; }
        public int? CustomerId { get; set; }
        public bool IncludeReturns { get; set; } = true;
    }
}

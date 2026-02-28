using RaccoonWarehouse.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Reports.Financial.Filters
{
    public class CashFlowFilterDto
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        public int? CashierId { get; set; }
        public int? CashierSessionId { get; set; }

        public PaymentMethod? Method { get; set; }
        public TransactionDirection? Direction { get; set; }
        public FinancialSourceType? SourceType { get; set; }

        public bool IncludeVoided { get; set; } = false;
    }
}

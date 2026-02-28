using RaccoonWarehouse.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Reports.Financial.Dtos
{
    public class CashFlowRowDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }

        public TransactionDirection Direction { get; set; }
        public PaymentMethod Method { get; set; }

        public decimal AmountIn { get; set; }
        public decimal AmountOut { get; set; }
        public decimal Net => AmountIn - AmountOut;

        public FinancialSourceType SourceType { get; set; }
        public int? SourceId { get; set; }

        public string? CashierName { get; set; }
        public string? Notes { get; set; }

        public bool IsVoided { get; set; }
        public string StatusText => IsVoided ? "ملغي" : "فعال";
    }
}

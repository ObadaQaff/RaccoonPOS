using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Reports.Financial.Dtos
{
    public class CashFlowSummaryDto
    {
        public decimal TotalIn { get; set; }
        public decimal TotalOut { get; set; }
        public decimal Net => TotalIn - TotalOut;

        public int CountIn { get; set; }
        public int CountOut { get; set; }
        public int CountAll => CountIn + CountOut;

        // Optional breakdown
        public decimal CashNet { get; set; }
        public decimal VisaNet { get; set; }
    }
}

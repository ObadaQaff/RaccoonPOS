using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Reports.Financial.Dtos
{
    public class ProfitLossSummaryDto
    {
        // Revenue side
        public decimal TotalSales { get; set; }        // SubTotal (Sales)
        public decimal TotalReturns { get; set; }      // SubTotal (Returns) if enabled
        public decimal TotalDiscounts { get; set; }    // DiscountAmount (Sales)
        public decimal NetSales { get; set; }          // (Sales - Returns) - Discounts

        // Cost
        public decimal TotalCOGS { get; set; }         // COGS for sales (optionally net returns later)
        public decimal GrossProfit { get; set; }       // NetSales - COGS

        // Expenses
        public decimal TotalExpenses { get; set; }     // Financial OUT (non-voided)
        public decimal NetProfit { get; set; }         // GrossProfit - Expenses

        public decimal GrossMarginPercent { get; set; } // GrossProfit / NetSales * 100
        public decimal NetMarginPercent { get; set; }   // NetProfit / NetSales * 100
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Reports.Financial.Dtos
{
    public class FinancialSummaryDto
    {
        // Sales
        public decimal TotalSales { get; set; }          // إجمالي المبيعات (بعد الخصم وقبل المرتجعات حسب منطقك)
        public decimal TotalTax { get; set; }            // إجمالي الضريبة
        public decimal TotalDiscounts { get; set; }      // إجمالي الخصومات
        public decimal TotalReturns { get; set; }        // إجمالي المرتجعات
        public decimal NetSales { get; set; }            // صافي المبيعات

        // COGS & Profit
        public decimal TotalCOGS { get; set; }           // تكلفة البضاعة المباعة
        public decimal GrossProfit { get; set; }         // NetSales - COGS
        public decimal GrossProfitMargin { get; set; }   // (GrossProfit / NetSales) * 100

        // Invoices
        public int NumberOfInvoices { get; set; }
        public decimal AverageInvoiceValue { get; set; } // NetSales / NumberOfInvoices
    }
}

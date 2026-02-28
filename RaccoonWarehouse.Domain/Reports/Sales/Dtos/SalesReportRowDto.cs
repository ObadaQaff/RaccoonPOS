using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Reports.Sales.Dtos
{
    public class SalesReportRowDto
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime Date { get; set; }
        public string CustomerName { get; set; }

        public decimal SubTotal { get; set; }       // قبل الضريبة
        public decimal TotalTax { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }          // NetTotal = SubTotal - Discount + Tax

        public decimal Cogs { get; set; }           // ✅ Sum(qty*unitCost)
        public decimal Profit { get; set; }         // ✅ (SubTotal-Discount) - Cogs

        public string InvoiceType { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; }
    }
}

using RaccoonWarehouse.Domain.Reports.Sales.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RaccoonWarehouse.Helpers.Pdf.Reports
{
    public class SalesSummaryReportDocument : BaseReportDocument
    {
        private readonly IReadOnlyCollection<SalesReportRowDto> _rows;
        private readonly DateTime? _from;
        private readonly DateTime? _to;
        private readonly string _customerName;

        public SalesSummaryReportDocument(IEnumerable<SalesReportRowDto> rows, DateTime? from, DateTime? to, string customerName)
        {
            _rows = rows.ToList();
            _from = from;
            _to = to;
            _customerName = string.IsNullOrWhiteSpace(customerName) ? "الكل" : customerName;
        }

        public override string ArabicTitle => "تقرير المبيعات";
        public override string EnglishTitle => "Sales Report";

        public override Dictionary<string, string> InfoFields =>
            new()
            {
                { "من تاريخ", _from?.ToString("yyyy/MM/dd") ?? "-" },
                { "إلى تاريخ", _to?.ToString("yyyy/MM/dd") ?? "-" },
                { "الزبون", _customerName },
                { "عدد السجلات", _rows.Count.ToString() },
                { "إجمالي الفواتير", _rows.Sum(x => x.Total).ToString("N2") },
                { "إجمالي الربح", _rows.Sum(x => x.Profit).ToString("N2") }
            };

        public override List<string> TableHeaders =>
            new()
            {
                "رقم الفاتورة",
                "التاريخ",
                "الزبون",
                "الإجمالي",
                "الربح",
                "طريقة الدفع",
                "الحالة"
            };

        public override List<List<string>> TableRows =>
            _rows.Select(x => new List<string>
            {
                x.InvoiceNumber ?? "-",
                x.Date.ToString("yyyy/MM/dd"),
                x.CustomerName ?? "-",
                x.Total.ToString("N2"),
                x.Profit.ToString("N2"),
                x.PaymentMethod ?? "-",
                x.Status ?? "-"
            }).ToList();
    }
}

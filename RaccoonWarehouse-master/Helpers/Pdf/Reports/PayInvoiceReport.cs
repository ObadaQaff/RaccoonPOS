using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using System.Collections.Generic;
using System.Linq;

namespace RaccoonWarehouse.Helpers.Pdf
{
    public class PayInvoiceReport : BaseReportDocument
    {
        private readonly InvoiceReadDto _invoice;

        public PayInvoiceReport(InvoiceReadDto invoice)
        {
            _invoice = invoice;
        }

        public override string ArabicTitle => "فاتورة مشتريات";
        public override string EnglishTitle => "purchases Invoice";

        public override Dictionary<string, string> InfoFields =>
            new Dictionary<string, string>
            {
                { "رقم الفاتورة", _invoice.InvoiceNumber },
                { "التاريخ", _invoice.CreatedDate.ToString("yyyy/MM/dd") },
                { "اسم المورد", _invoice.User?.Name ?? "-" },
                { "طريقة الدفع", _invoice.PaymentType.ToString() ?? "-" },
                { "المبلغ الإجمالي", _invoice.TotalAmount.ToString("N2") }
            };

        public override List<string> TableHeaders =>
            new List<string>
            {
                "المنتج",
                "الوحدة",
                "الكمية",
                "سعر البيع",
                "الإجمالي",
                "تاريخ الانتهاء"
            };

        public override List<List<string>> TableRows =>
            _invoice.InvoiceLines.Select(line => new List<string>
            {
                line.Product?.Name ?? "",
                line.ProductUnit?.Unit?.Name ?? "",
                line.Quantity.ToString(),
                line.UnitPrice.ToString("N2"),
                line.LineTotal.ToString("N2"),
                line.ExpiryDate.ToString("yyyy/MM/dd") ?? "-"
            }).ToList();
    }
}

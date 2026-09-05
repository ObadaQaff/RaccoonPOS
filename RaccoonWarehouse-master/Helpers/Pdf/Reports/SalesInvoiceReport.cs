using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using System.Collections.Generic;
using System.Linq;

namespace RaccoonWarehouse.Helpers.Pdf
{
    public class SalesInvoiceReport : BaseReportDocument
    {
        private readonly InvoiceReadDto _invoice;

        public SalesInvoiceReport(InvoiceReadDto invoice) => _invoice = invoice;

        public override string ArabicTitle => "فاتورة مبيعات";
        public override string EnglishTitle => "Sales Invoice";

        public override Dictionary<string, string> InfoFields => new()
        {
            { "رقم الفاتورة", _invoice.InvoiceNumber },
            { "التاريخ", _invoice.CreatedDate.ToString("yyyy/MM/dd") },
            { "اسم الزبون", _invoice.User?.Name ?? "-" },
            { "طريقة الدفع", _invoice.PaymentType.ToString() ?? "-" },
            { "المبلغ الإجمالي", _invoice.TotalAmount.ToString("N5") }
        };

        public override List<string> TableHeaders => new()
        {
            "المنتج", "الوحدة", "الكمية", "سعر البيع", "الإجمالي", "الباركود"
        };

        public override List<List<string>> TableRows =>
            (_invoice.InvoiceLines ?? Enumerable.Empty<InvoiceLineReadDto>()).Select(line => new List<string>
            {
                line.Product?.Name ?? "",
                line.ProductUnit?.Unit?.Name ?? "",
                line.Quantity.ToString(),
                line.UnitPrice.ToString("N5"),
                line.LineTotal.ToString("N5"),
                GetBarcode(line)
            }).ToList();

        protected override void ComposeDataCell(IContainer container, string value, int columnIndex)
        {
            if (columnIndex == 5)
            {
                var barcode = Code39BarcodeRenderer.CreatePng(value);
                if (barcode != null)
                {
                    container.Column(column =>
                    {
                        // Keep the barcode inside the cell's available area so long
                        // values cannot create conflicting row constraints.
                        column.Item().Height(24).AlignCenter().Image(barcode).FitArea();
                        column.Item().AlignCenter().Text(value).FontSize(8);
                    });
                    return;
                }
            }

            container.Text(RaccoonWarehouse.Helpers.Localization.UiText.Translate(value)).FontSize(9);
        }

        protected override float GetColumnRelativeWidth(int columnIndex) =>
            columnIndex switch
            {
                0 => 4f,
                5 => 4f,
                _ => 2f
            };

        private static string GetBarcode(InvoiceLineReadDto line) =>
            !string.IsNullOrWhiteSpace(line.ProductUnit?.AlternateBarcode)
                ? line.ProductUnit.AlternateBarcode!
                : line.Product?.ITEMCODE?.ToString() ?? string.Empty;
    }
}

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System.Linq;

namespace RaccoonWarehouse.Helpers.Pdf.Reports
{
    public sealed class SmallSalesInvoiceReport : IReportDocument
    {
        private readonly InvoiceReadDto _invoice;

        public SmallSalesInvoiceReport(InvoiceReadDto invoice)
        {
            _invoice = invoice;
        }

        public string FileName => $"POS_{_invoice.InvoiceNumber}";

        public DocumentMetadata GetMetadata()
            => new() { Title = UiText.IsEnglish ? "POS Invoice" : "فاتورة نقطة بيع" };

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.ContinuousSize(80, Unit.Millimetre);
                page.MarginHorizontal(4, Unit.Millimetre);
                page.MarginVertical(4, Unit.Millimetre);
                page.DefaultTextStyle(text => text
                    .FontFamily("Arial")
                    .FontSize(9)
                    .DirectionFromRightToLeft());

                page.Content().Column(column =>
                {
                    column.Spacing(4);
                    column.Item().AlignCenter().Text("Raccoon POS").Bold().FontSize(14);
                    column.Item().AlignCenter().Text(UiText.T("فاتورة مبيعات", "Sales Invoice")).Bold().FontSize(11);

                    column.Item().LineHorizontal(1);
                    column.Item().Text(UiText.T("رقم الفاتورة", "Invoice") + $": {_invoice.InvoiceNumber}");
                    column.Item().Text(UiText.T("التاريخ", "Date") + $": {_invoice.CreatedDate:yyyy/MM/dd HH:mm}");
                    column.Item().Text(UiText.T("العميل", "Customer") + $": {_invoice.Customer?.Name ?? "-"}");
                    column.Item().Text(UiText.T("الدفع", "Payment") + $": {_invoice.PaymentType?.ToString() ?? "-"}");
                    column.Item().LineHorizontal(1);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                        });

                        table.Header(header =>
                        {
                            HeaderCell(header.Cell(), UiText.T("الصنف", "Item"));
                            HeaderCell(header.Cell(), UiText.T("الكمية", "Qty"));
                            HeaderCell(header.Cell(), UiText.T("السعر", "Price"));
                            HeaderCell(header.Cell(), UiText.T("الإجمالي", "Total"));
                        });

                        foreach (var line in _invoice.InvoiceLines ?? Enumerable.Empty<InvoiceLineReadDto>())
                        {
                            Cell(table.Cell(), line.Product?.Name ?? "-");
                            Cell(table.Cell(), line.Quantity.ToString("0.00000"));
                            Cell(table.Cell(), line.UnitPrice.ToString("N5"));
                            Cell(table.Cell(), line.LineTotal.ToString("N5"));
                        }
                    });

                    column.Item().LineHorizontal(1);
                    column.Item().AlignRight().Text(UiText.T("الإجمالي", "Total") + $": {_invoice.TotalAmount:N5}").Bold().FontSize(12);
                    column.Item().AlignCenter().Text(UiText.T("شكراً لزيارتكم", "Thank you for your visit")).FontSize(9);
                });
            });
        }

        private static void HeaderCell(IContainer container, string text)
        {
            container
                .Background("#E8E8E8")
                .PaddingVertical(3)
                .PaddingHorizontal(2)
                .AlignCenter()
                .Text(text)
                .Bold()
                .FontSize(8);
        }

        private static void Cell(IContainer container, string text)
        {
            container
                .BorderBottom(0.5f)
                .BorderColor("#BBBBBB")
                .PaddingVertical(3)
                .PaddingHorizontal(2)
                .AlignCenter()
                .Text(text)
                .FontSize(8);
        }
    }
}

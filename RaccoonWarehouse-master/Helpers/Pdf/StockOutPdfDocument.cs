using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using RaccoonWarehouse.Domain.StockDocuments.DTOs;

namespace RaccoonWarehouse.Helpers.pdf
{
    public class StockOutPdfDocument : IDocument
    {
        private readonly StockDocumentReadDto _doc;

        public StockOutPdfDocument(StockDocumentReadDto doc)
        {
            _doc = doc;
        }

        public DocumentMetadata GetMetadata() => new DocumentMetadata { Title = "Stock Out Document" };

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(20);

                page.DefaultTextStyle(t =>
                         t.FontSize(14)
                          .FontFamily("Arial")
                          .DirectionFromRightToLeft()
                     );


                // HEADER
                page.Header().AlignCenter().Text("سند إخراج بضاعة")
                    .FontSize(24)
                    .Bold();

                page.Content().Column(col =>
                {
                    // Document info
                    col.Item().Text(txt =>
                    {
                        txt.Span($"رقم السند: {_doc.DocumentNumber}");
                        txt.Line("");
                        txt.Span($"التاريخ: {_doc.CreatedDate:yyyy/MM/dd}");
                        txt.Line("");
                        txt.Span($"المورد: {_doc.Supplier?.Name ?? "غير محدد"}");
                    });

                    col.Item().PaddingVertical(10).LineHorizontal(1);

                    // Table
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(4);  // المنتج
                            cols.RelativeColumn(2);  // الوحدة
                            cols.RelativeColumn(1);  // الكمية
                            cols.RelativeColumn(2);  // تاريخ الانتهاء
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("المنتج").Bold();
                            header.Cell().Element(CellStyle).Text("الوحدة").Bold();
                            header.Cell().Element(CellStyle).Text("الكمية").Bold();
                            header.Cell().Element(CellStyle).Text("تاريخ الانتهاء").Bold();
                        });

                        foreach (var item in _doc.Items)
                        {
                            table.Cell().Element(CellStyle).Text(item.Product?.Name ?? "");
                            table.Cell().Element(CellStyle).Text(item.ProductUnit?.Unit?.Name ?? "");
                            table.Cell().Element(CellStyle).Text(item.Quantity.ToString("0.##"));
                            table.Cell().Element(CellStyle).Text(item.ExpiryDate?.ToString("yyyy/MM/dd") ?? "");
                        }
                    });

                    col.Item().PaddingVertical(20).LineHorizontal(1);

                    col.Item().Text("توقيع الموظف: __________________________").FontSize(16);
                });
            });
        }


        private static IContainer CellStyle(IContainer container)
        {
            return container
                .Border(1)
                .BorderColor("#CCCCCC")
                .Padding(5);
        }
    }
}

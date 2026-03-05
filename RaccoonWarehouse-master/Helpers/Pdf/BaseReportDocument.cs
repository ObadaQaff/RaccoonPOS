using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Collections.Generic;

namespace RaccoonWarehouse.Helpers.Pdf
{
    public abstract class BaseReportDocument : IReportDocument
    {
        public abstract string ArabicTitle { get; }
        public abstract string EnglishTitle { get; }
        public virtual string FileName => EnglishTitle.Replace(" ", "_");

        // Dictionary for info box (label → value)
        public abstract Dictionary<string, string> InfoFields { get; }

        // Table header labels
        public abstract List<string> TableHeaders { get; }

        // Rows → each row list of strings
        public abstract List<List<string>> TableRows { get; }

        public DocumentMetadata GetMetadata() =>
            new DocumentMetadata { Title = EnglishTitle };

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(40);

                // Global Arabic RTL setup
                page.DefaultTextStyle(t => t
                    .FontFamily("Arial")
                    .FontSize(14)
                    .DirectionFromRightToLeft()
                    .FontColor("#000000")
                );

                // ====================== HEADER ============================
                page.Header().Column(col =>
                {
                    col.Spacing(4);

                    col.Item().AlignCenter().Text("Raccoon System")
                        .FontSize(22)
                        .Bold()
                        .FontColor("#000000");

                    col.Item().AlignCenter().Text(ArabicTitle)
                        .FontSize(20)
                        .Bold();
                });

                // ====================== CONTENT ============================
                page.Content().Column(col =>
                {
                    col.Spacing(18);

                    // -------- INFO BOX ----------
                    col.Item().Border(1)
                        .BorderColor("#000000")
                        .Padding(14)
                        .Column(info =>
                        {
                            info.Spacing(6);
                            foreach (var f in InfoFields)
                                info.Item().Text($"{f.Key}: {f.Value}")
                                    .FontSize(14)
                                    .Bold();
                        });

                    // -------- TABLE ----------
                    col.Item().Table(table =>
                    {
                        // Column widths
                        table.ColumnsDefinition(cols =>
                        {
                            // 🟦 First column = product name = WIDE
                            cols.RelativeColumn(4);   // 4x width

                            // 🟩 All other columns = normal size
                            for (int i = 1; i < TableHeaders.Count; i++)
                                cols.RelativeColumn(2);
                        });


                        // ----- HEADER -----
                        table.Header(header =>
                        {
                            foreach (var h in TableHeaders)
                            {
                                header.Cell().Element(HeaderCell).Text(h);
                            }
                        });

                        // ----- ROWS -----
                        foreach (var rowData in TableRows)
                        {
                            var fixedRow = new List<string>(rowData);

                            // pad or trim
                            while (fixedRow.Count < TableHeaders.Count)
                                fixedRow.Add("");

                            if (fixedRow.Count > TableHeaders.Count)
                                fixedRow = fixedRow.GetRange(0, TableHeaders.Count);

                            foreach (var cell in fixedRow)
                                table.Cell().Element(DataCell).Text(cell);
                        }
                    });

                    // -------- FOOTER SIGNATURE ----------
                    col.Item().PaddingTop(20)
                        .Text("توقيع الموظف: __________________________")
                        .FontSize(16)
                        .Bold();
                });

                // ====================== FOOTER ============================
                page.Footer().AlignCenter().Element(footer =>
                {
                    footer.Column(col =>
                    {
                        col.Item().DefaultTextStyle(t => t
                            .FontSize(12)
                            .FontColor("#444444")
                            .DirectionFromRightToLeft()
                        );

                        col.Item().Text(txt =>
                        {
                            txt.Span("الصفحة ");
                            txt.CurrentPageNumber();
                            txt.Span(" من ");
                            txt.TotalPages();
                        });
                    });
                });

            });
        }

        // --------------------- STYLING HELPERS ---------------------

        private static IContainer HeaderCell(IContainer container)
        {
            return container
                .Background("#E8E8E8")      // Light gray (good in B&W)
                .BorderBottom(2)
                .BorderColor("#000000")
                .PaddingVertical(10)
                .PaddingHorizontal(6)
                .AlignCenter()
                .DefaultTextStyle(t => t
                    .FontSize(15)
                    .Bold()
                    .FontColor("#000000")   // Black text
                );
        }


        private static IContainer DataCell(IContainer container)
        {
            return container
                .Border(1)
                .BorderColor("#000000")
                .PaddingVertical(8)
                .PaddingHorizontal(5)
                .AlignRight()
                .DefaultTextStyle(t => t.FontSize(14));
        }
    }
}

using Microsoft.Win32;
using QuestPDF.Fluent;
using RaccoonWarehouse.Helpers.Localization;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using RaccoonWarehouse.Domain.Invoices.DTOs;

namespace RaccoonWarehouse.Helpers.Pdf
{
    public static class ReportPrintService
    {
        public static void ExportPdf(IReportDocument document, Window owner)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = $"{document.FileName}_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
                AddExtension = true,
                DefaultExt = ".pdf"
            };

            if (dialog.ShowDialog(owner) != true)
                return;

            document.GeneratePdf(dialog.FileName);
            MessageBox.Show(owner, UiText.Translate("تم تصدير التقرير بنجاح."), UiText.Translate("PDF"), MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void Print(IReportDocument document, Window owner)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"{document.FileName}_{Guid.NewGuid():N}.pdf");
            document.GeneratePdf(tempPath);

            Process.Start(new ProcessStartInfo
            {
                FileName = tempPath,
                Verb = "print",
                UseShellExecute = true,
                CreateNoWindow = true
            });
        }

        public static void PrintSmallInvoice(InvoiceReadDto invoice, Window owner)
        {
            var printDialog = new PrintDialog
            {
                PrintQueue = System.Printing.LocalPrintServer.GetDefaultPrintQueue()
            };

            var document = BuildSmallInvoiceDocument(invoice);
            var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
            printDialog.PrintDocument(paginator, $"POS Invoice {invoice.InvoiceNumber}");
            SendXpS200mFeedAndCut(printDialog.PrintQueue.FullName, 6);
        }

        public static void PreviewSmallInvoice(InvoiceReadDto invoice, Window owner)
        {
            var preview = new Window
            {
                Owner = owner,
                Title = UiText.T("معاينة الفاتورة الصغيرة", "Small invoice preview"),
                Width = 390,
                Height = 720,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = Brushes.White,
                FlowDirection = FlowDirection.LeftToRight
            };

            var document = BuildSmallInvoiceDocument(invoice);
            var viewer = new FlowDocumentScrollViewer
            {
                Document = document,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(12)
            };

            var printButton = new Button
            {
                Content = UiText.T("طباعة", "Print"),
                Width = 100,
                Height = 34,
                Margin = new Thickness(0, 0, 8, 0)
            };
            printButton.Click += (_, _) =>
            {
                try
                {
                    PrintSmallInvoice(invoice, preview);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        preview,
                        $"{UiText.T("تعذر الطباعة", "Could not print")}: {ex.Message}",
                        UiText.T("خطأ", "Error"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            };

            var closeButton = new Button
            {
                Content = UiText.T("إغلاق", "Close"),
                Width = 100,
                Height = 34
            };
            closeButton.Click += (_, _) => preview.Close();

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            };
            buttons.Children.Add(printButton);
            buttons.Children.Add(closeButton);

            var layout = new DockPanel();
            DockPanel.SetDock(buttons, Dock.Bottom);
            layout.Children.Add(buttons);
            layout.Children.Add(viewer);

            preview.Content = layout;
            preview.ShowDialog();
        }

        private static FlowDocument BuildSmallInvoiceDocument(InvoiceReadDto invoice)
        {
            var document = new FlowDocument
            {
                PageWidth = 302,
                PagePadding = new Thickness(6),
                FontFamily = new FontFamily("Arial"),
                FontSize = 11,
                FlowDirection = UiText.IsEnglish ? FlowDirection.LeftToRight : FlowDirection.RightToLeft
            };

            document.Blocks.Add(new Paragraph(new Run("Raccoon POS"))
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 2)
            });
            document.Blocks.Add(new Paragraph(new Run(UiText.T("فاتورة مبيعات", "Sales Invoice")))
            {
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 4)
            });

            var info = new Paragraph
            {
                Margin = new Thickness(0, 0, 0, 4)
            };
            info.Inlines.Add(new Run($"{UiText.T("رقم الفاتورة", "Invoice")}: {invoice.InvoiceNumber}\n"));
            info.Inlines.Add(new Run($"{UiText.T("التاريخ", "Date")}: {invoice.CreatedDate:yyyy/MM/dd HH:mm}\n"));
            info.Inlines.Add(new Run($"{UiText.T("العميل", "Customer")}: {invoice.Customer?.Name ?? "-"}\n"));
            info.Inlines.Add(new Run($"{UiText.T("الدفع", "Payment")}: {invoice.PaymentType?.ToString() ?? "-"}"));
            document.Blocks.Add(info);

            var table = new Table { CellSpacing = 0 };
            // Keep the complete table inside the 80mm receipt width. A star-sized
            // column can otherwise overflow the preview and make the item column
            // appear missing in RTL mode.
            table.Columns.Add(new TableColumn { Width = new GridLength(112) });
            table.Columns.Add(new TableColumn { Width = new GridLength(34) });
            table.Columns.Add(new TableColumn { Width = new GridLength(54) });
            table.Columns.Add(new TableColumn { Width = new GridLength(62) });

            var headerGroup = new TableRowGroup();
            var header = new TableRow();
            AddCell(header, UiText.T("الصنف", "Item"), true);
            AddCell(header, UiText.T("الكمية", "Qty"), true);
            AddCell(header, UiText.T("السعر", "Price"), true);
            AddCell(header, UiText.T("الإجمالي", "Total"), true);
            headerGroup.Rows.Add(header);
            table.RowGroups.Add(headerGroup);

            var rows = new TableRowGroup();
            foreach (var line in invoice.InvoiceLines ?? Enumerable.Empty<Domain.InvoiceLines.DTOs.InvoiceLineReadDto>())
            {
                var row = new TableRow();
                AddCell(row, string.IsNullOrWhiteSpace(line.ProductName) ? line.Product?.Name ?? "-" : line.ProductName);
                AddCell(row, line.Quantity.ToString("0.###"));
                AddCell(row, line.UnitPrice.ToString("N2"));
                AddCell(row, line.LineTotal.ToString("N2"));
                rows.Rows.Add(row);
            }
            table.RowGroups.Add(rows);
            document.Blocks.Add(table);

            document.Blocks.Add(new Paragraph(new Run(
                $"{UiText.T("الإجمالي", "Total")}: {invoice.TotalAmount:N2}"))
            {
                TextAlignment = TextAlignment.Right,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 4, 0, 0)
            });

            // Feed 130 mm of blank paper after the total so thermal printers
            // fully advance the receipt before cutting it.
            document.Blocks.Add(new Paragraph(new Run(" "))
            {
                // WPF uses device-independent units: 130 mm ~= 491 px at 96 DPI.
                Margin = new Thickness(0)
            });

            return document;
        }

        private static void SendXpS200mFeedAndCut(string printerName, int feedLines)
        {
            var lineCount = (byte)Math.Clamp(feedLines, 0, 255);
            var command = new byte[]
            {
                0x1B, 0x64, lineCount, // ESC d n: feed n lines
                0x1D, 0x56, 0x00       // GS V 0: full cut
            };

            if (!OpenPrinter(printerName, out var printerHandle, IntPtr.Zero))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());

            var documentStarted = false;
            var pageStarted = false;
            try
            {
                var documentInfo = new DocInfo
                {
                    DocumentName = "POS Receipt Feed and Cut",
                    DataType = "RAW"
                };

                if (StartDocPrinter(printerHandle, 1, documentInfo) == 0)
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());

                documentStarted = true;

                if (!StartPagePrinter(printerHandle))
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());

                pageStarted = true;

                if (!WritePrinter(printerHandle, command, command.Length, out var written) || written != command.Length)
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                if (pageStarted)
                    EndPagePrinter(printerHandle);
                if (documentStarted)
                    EndDocPrinter(printerHandle);
                ClosePrinter(printerHandle);
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private sealed class DocInfo
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string DocumentName = string.Empty;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string? OutputFile;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string DataType = "RAW";
        }

        [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool OpenPrinter(string printerName, out IntPtr printerHandle, IntPtr defaults);

        [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int StartDocPrinter(IntPtr printerHandle, int level, [In] DocInfo documentInfo);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool StartPagePrinter(IntPtr printerHandle);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool WritePrinter(IntPtr printerHandle, byte[] buffer, int bufferLength, out int bytesWritten);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndPagePrinter(IntPtr printerHandle);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndDocPrinter(IntPtr printerHandle);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr printerHandle);

        private static void AddCell(TableRow row, string text, bool header = false)
        {
            row.Cells.Add(new TableCell(new Paragraph(new Run(text))
            {
                Margin = new Thickness(0),
                LineHeight = 15
            })
            {
                Padding = new Thickness(1),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0, 0, 0, 0.5),
                TextAlignment = TextAlignment.Center,
                FontWeight = header ? FontWeights.Bold : FontWeights.Normal
            });
        }
    }
}

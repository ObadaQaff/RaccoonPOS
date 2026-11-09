using Microsoft.Win32;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Media.Imaging;
using RaccoonWarehouse.Domain.Settings;
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
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;

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

            var branding=DocumentPrintRuntime.Branding(invoiceId:invoice.Id);
            var ticket=printDialog.PrintTicket;
            ticket.PageMediaSize=new System.Printing.PageMediaSize(branding.ThermalPaperWidthMm*96d/25.4,14000);
            printDialog.PrintTicket=ticket;
            var capabilities=printDialog.PrintQueue.GetPrintCapabilities(ticket);
            var paperWidth=branding.ThermalPaperWidthMm*96d/25.4;
            var area=capabilities.PageImageableArea;
            var printable=area?.ExtentWidth??paperWidth;
            if(printable<100)throw new InvalidOperationException(UiText.T("عرض الطابعة غير صالح؛ راجع إعدادات الورق.","Invalid printer width; check paper settings."));
            var document = BuildSmallInvoiceDocument(invoice,branding,Math.Min(paperWidth,printable));
            document.PageHeight=MeasureReceiptHeight(document);
            var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
            ticket.PageMediaSize=new System.Printing.PageMediaSize(paperWidth,document.PageHeight);
            printDialog.PrintTicket=ticket;
            printDialog.PrintDocument(paginator, $"POS Invoice {invoice.InvoiceNumber}");
            // RAW feed/cut is opt-in for ESC/POS printers only, never PDF printers.
            if(branding.EscPosCutEnabled)SendXpS200mFeedAndCut(printDialog.PrintQueue.FullName, 3);
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

            var branding=DocumentPrintRuntime.Branding(invoiceId:invoice.Id);
            var document = BuildSmallInvoiceDocument(invoice,branding);
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
            RaccoonWarehouse.Security.PermissionWindowGuard.ShowDialog(preview, "internal");
        }

        internal static FlowDocument BuildSmallInvoiceDocument(InvoiceReadDto invoice,DocumentBranding branding,double? printableWidth=null)
        {
            var width=printableWidth??branding.ThermalPaperWidthMm*96d/25.4;
            var document=new FlowDocument { PageWidth=width,PagePadding=new Thickness(6),ColumnWidth=double.PositiveInfinity,
                FontFamily=new FontFamily("Arial"),FontSize=11,FlowDirection=UiText.CurrentFlowDirection };
            void Text(string value,bool bold=false,bool center=false)
            {
                document.Blocks.Add(new Paragraph(new Run(value)){Margin=new Thickness(0,2,0,2),TextAlignment=center?TextAlignment.Center:TextAlignment.Right,FontWeight=bold?FontWeights.Bold:FontWeights.Normal});
            }
            if(!string.IsNullOrWhiteSpace(branding.LogoBase64))
            {
                var logo=ImageSource(Convert.FromBase64String(branding.LogoBase64));
                document.Blocks.Add(new BlockUIContainer(new System.Windows.Controls.Image{Source=logo,Height=48,MaxWidth=width-12,Stretch=Stretch.Uniform}));
            }
            Text(branding.DisplayName,true,true);
            foreach(var value in new[]{branding.Address,branding.Phone,branding.AdditionalInformation})if(!string.IsNullOrWhiteSpace(value))Text(value,false,true);
            if(!string.IsNullOrWhiteSpace(branding.TaxNumber))Text(UiText.T("الرقم الضريبي: ","Tax number: ")+branding.TaxNumber,false,true);
            Text(UiText.T("فاتورة مبيعات","Sales Invoice"),true,true);
            Text(UiText.T("رقم الفاتورة: ","Invoice: ")+invoice.InvoiceNumber);
            Text(invoice.CreatedDate.ToString("yyyy/MM/dd HH:mm"));
            Text(UiText.T("العميل: ","Customer: ")+(invoice.Customer?.Name??"—"));
            Text(UiText.T("الدفع: ","Payment: ")+UiText.Translate(invoice.PaymentType?.ToString()??"—"));
            foreach(var line in invoice.InvoiceLines??Enumerable.Empty<InvoiceLineReadDto>())
            {
                // Full-width item name followed by numeric details avoids tiny clipped columns on 58mm.
                Text(string.IsNullOrWhiteSpace(line.ProductName)?line.Product?.Name??"—":line.ProductName,true);
                Text(UiText.T("باركود: ","Barcode: ")+GetBarcode(line));
                Text($"{UiText.T("الكمية","Qty")}: {line.Quantity:0.###} × {line.UnitPrice:0.000}");
                if(line.FreeQuantity!=0)Text($"{UiText.T("مجاني","Free")}: {line.FreeQuantity:0.###}");
                if(line.LineDiscountAmount!=0)Text($"{UiText.T("خصم السطر","Line discount")}: {line.LineDiscountAmount:0.000}");
                Text($"{UiText.T("الضريبة","Tax")}: {line.TaxAmount:0.000}   {UiText.T("إجمالي السطر","Line total")}: {line.LineTotal:0.000}");
                Text("────────────────────");
            }
            Text($"{UiText.T("قبل الضريبة","Subtotal")}: {invoice.SubTotal:0.000}");
            Text($"{UiText.T("خصم الفاتورة","Invoice discount")}: {invoice.DiscountAmount??0:0.000}");
            Text($"{UiText.T("الضريبة","Tax")}: {invoice.TotalTax:0.000}");
            Text($"{UiText.T("الإجمالي المستحق (د.أ)","Amount due (JOD)")}: {invoice.TotalAmount:0.000}",true);
            foreach(var payment in invoice.Payments??Enumerable.Empty<InvoicePaymentReadDto>())Text($"{UiText.T("دفعة","Payment")}: {UiText.Translate(payment.PaymentType.ToString())} {payment.Amount:0.000}");
            if(!string.IsNullOrWhiteSpace(invoice.Notes))Text(invoice.Notes);
            var provider=((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<IReceiptQrProvider>();
            var qr=provider.Get(invoice);
            Text(qr.IsOfficial?UiText.T("رمز الفاتورة الإلكترونية","Electronic invoice QR"):UiText.T("تجريبي — TEST — ليس رمزاً رسمياً","TEST — Not an official QR"),true,true);
            var qrImage=new System.Windows.Controls.Image{Source=ImageSource(ReceiptQrService.Image(qr)),Width=Math.Min(132,width-16),Height=Math.Min(132,width-16),Stretch=Stretch.Uniform,SnapsToDevicePixels=true};
            RenderOptions.SetBitmapScalingMode(qrImage,BitmapScalingMode.NearestNeighbor);
            document.Blocks.Add(new BlockUIContainer(qrImage));
            if(!string.IsNullOrWhiteSpace(branding.Footer))Text(branding.Footer,false,true);
            return document;
        }
        internal static double MeasureReceiptHeight(FlowDocument document)
        {
            var width=document.PageWidth-document.PagePadding.Left-document.PagePadding.Right;
            double height=document.PagePadding.Top+document.PagePadding.Bottom+16;
            foreach(var block in document.Blocks)
            {
                if(block is Paragraph p)
                {
                    var text=new TextBlock{Text=new TextRange(p.ContentStart,p.ContentEnd).Text.TrimEnd('\r','\n'),
                        FontFamily=document.FontFamily,FontSize=p.FontSize,FontWeight=p.FontWeight,
                        TextWrapping=TextWrapping.Wrap,FlowDirection=document.FlowDirection};
                    text.Measure(new Size(width,double.PositiveInfinity));
                    height+=text.DesiredSize.Height+p.Margin.Top+p.Margin.Bottom;
                }
                else if(block is BlockUIContainer ui && ui.Child!=null)
                {
                    ui.Child.Measure(new Size(width,double.PositiveInfinity));
                    height+=ui.Child.DesiredSize.Height+ui.Margin.Top+ui.Margin.Bottom;
                }
            }
            return Math.Max(200,Math.Ceiling(height));
        }

        private static BitmapSource ImageSource(byte[] data)
        {
            using var stream=new MemoryStream(data);var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.StreamSource=stream;image.EndInit();image.Freeze();return image;
        }

        private static string GetBarcode(InvoiceLineReadDto line) =>
            !string.IsNullOrWhiteSpace(line.ProductUnit?.AlternateBarcode)
                ? line.ProductUnit.AlternateBarcode!
                : line.Product?.ITEMCODE?.ToString() ?? string.Empty;

        private static void AddBarcodeCell(TableRow row, string barcode)
        {
            var cell = new TableCell
            {
                Padding = new Thickness(1),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0, 0, 0, 0.5),
                TextAlignment = TextAlignment.Center
            };
            var panel = new StackPanel { Orientation = Orientation.Vertical };
            var image = Code39BarcodeRenderer.CreateWpfImage(barcode);
            if (image != null)
                panel.Children.Add(new System.Windows.Controls.Image { Source = image, Height = 25, Stretch = Stretch.Fill });
            panel.Children.Add(new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(barcode) ? "-" : barcode,
                FontSize = 7,
                TextAlignment = TextAlignment.Center
            });
            cell.Blocks.Add(new BlockUIContainer(panel));
            row.Cells.Add(cell);
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

        private static void AddCell(TableRow row, string text, bool header = false, double? fontSize = null)
        {
            row.Cells.Add(new TableCell(new Paragraph(new Run(text))
            {
                Margin = new Thickness(0),
                LineHeight = header ? 15 : 11,
                FontSize = fontSize ?? (header ? 11 : 9)
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

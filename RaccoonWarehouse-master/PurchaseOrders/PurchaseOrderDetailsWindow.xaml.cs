using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.PurchaseOrders.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace RaccoonWarehouse.PurchaseOrders
{
    public partial class PurchaseOrderDetailsWindow : Window
    {
        private readonly PurchaseOrderWriteDto _order;

        public PurchaseOrderDetailsWindow(PurchaseOrderWriteDto order)
        {
            _order = order;
            InitializeComponent();
            UiText.ApplyWindow(this);
            OrderSummaryText.Text = $"{order.OrderNumber} | {Format(order.TotalAmount)}";
            SupplierText.Text = order.SupplierName ?? order.SupplierId?.ToString() ?? "—";
            DateText.Text = order.DocumentDate?.ToString("dd/MM/yyyy") ?? "—";
            PaymentText.Text = FormatPaymentType(order.PaymentType);
            StatusText.Text = FormatOrderStatus(order.Status);
            PaymentStatusText.Text = FormatPaymentStatus(order.PaymentStatus);
            PaidDateText.Text = order.PaidDate?.ToString("dd/MM/yyyy HH:mm") ?? "—";
            NotesText.Text = string.IsNullOrWhiteSpace(order.Notes)
                ? UiText.T("لا توجد ملاحظات.", "No notes.")
                : UiText.T($"ملاحظات: {order.Notes}", $"Notes: {order.Notes}");
            PaymentNoteText.Text = string.IsNullOrWhiteSpace(order.PaymentNote)
                ? UiText.T("لا توجد ملاحظة دفع.", "No payment note.")
                : UiText.T($"ملاحظة الدفع: {order.PaymentNote}", $"Payment note: {order.PaymentNote}");
            AuditText.Text = UiText.T(
                $"تاريخ الإنشاء: {order.CreatedDate:dd/MM/yyyy HH:mm} | آخر تحديث: {order.UpdatedDate:dd/MM/yyyy HH:mm} | رقم الفاتورة: {order.ConvertedInvoiceId?.ToString() ?? "—"}",
                $"Created: {order.CreatedDate:dd/MM/yyyy HH:mm} | Updated: {order.UpdatedDate:dd/MM/yyyy HH:mm} | Invoice: {order.ConvertedInvoiceId?.ToString() ?? "—"}");
            SubtotalText.Text = Format(order.SubTotal);
            DiscountText.Text = Format(order.DiscountAmount ?? 0m);
            TaxText.Text = Format(order.TotalTax);
            GrandTotalText.Text = Format(order.TotalAmount);
            ItemCountText.Text = order.Lines.Count.ToString(CultureInfo.CurrentCulture);
            TaxBasisText.Text = order.CostPriceIncludesTax
                ? UiText.T("التكلفة تشمل الضريبة", "Unit costs include tax")
                : UiText.T("التكلفة لا تشمل الضريبة", "Unit costs exclude tax");
            LinesGrid.ItemsSource = order.Lines;
        }

        private static string FormatOrderStatus(PurchaseOrderStatus status) => status switch
        {
            PurchaseOrderStatus.Draft => UiText.T("مسودة", "Draft"),
            PurchaseOrderStatus.Submitted => UiText.T("مقدم", "Submitted"),
            PurchaseOrderStatus.Approved => UiText.T("معتمد", "Approved"),
            PurchaseOrderStatus.Rejected => UiText.T("مرفوض", "Rejected"),
            PurchaseOrderStatus.Cancelled => UiText.T("ملغي", "Cancelled"),
            PurchaseOrderStatus.Converted => UiText.T("محول", "Converted"),
            _ => status.ToString()
        };

        private static string FormatPaymentStatus(PurchaseOrderPaymentStatus status) =>
            status == PurchaseOrderPaymentStatus.Paid ? UiText.T("مدفوع", "Paid") : UiText.T("غير مدفوع", "Unpaid");

        private static string Format(decimal value) => value.ToString("N3", CultureInfo.CurrentCulture);

        private static string FormatPaymentType(PaymentType? paymentType) => paymentType switch
        {
            PaymentType.Cash => UiText.T("نقدي", "Cash"),
            PaymentType.Credit => UiText.T("آجل", "Credit"),
            PaymentType.Debit => UiText.T("شبكة", "Debit"),
            PaymentType.Check => UiText.T("شيك", "Check"),
            PaymentType.MobilePayment => UiText.T("دفع إلكتروني", "Mobile payment"),
            PaymentType.Master => "Mastercard",
            PaymentType.Visa => "Visa",
            _ => UiText.T("غير محدد", "Not specified")
        };

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() != true)
                    return;

                var document = BuildPrintDocument();
                printDialog.PrintDocument(
                    ((IDocumentPaginatorSource)document).DocumentPaginator,
                    $"Purchase Order {_order.OrderNumber}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("تعذرت طباعة أمر الشراء", "Could not print the purchase order")}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private FlowDocument BuildPrintDocument()
        {
            var document = new FlowDocument
            {
                PagePadding = new Thickness(36),
                ColumnWidth = double.PositiveInfinity,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 10
            };

            document.Blocks.Add(new Paragraph(new Run($"{UiText.T("أمر شراء", "PURCHASE ORDER")}\n{_order.OrderNumber}"))
            {
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center
            });

            document.Blocks.Add(new Paragraph(new Run(
                $"{UiText.T("المورد", "Supplier")}: {_order.SupplierName ?? _order.SupplierId?.ToString() ?? "—"}    " +
                $"{UiText.T("التاريخ", "Date")}: {_order.DocumentDate?.ToString("dd/MM/yyyy") ?? "—"}    " +
                $"{UiText.T("حالة الأمر", "Order status")}: {FormatOrderStatus(_order.Status)}    " +
                $"{UiText.T("حالة الدفع", "Payment status")}: {FormatPaymentStatus(_order.PaymentStatus)}")));

            document.Blocks.Add(new Paragraph(new Run(
                $"{UiText.T("طريقة الدفع", "Payment term")}: {FormatPaymentType(_order.PaymentType)}    " +
                $"{UiText.T("تاريخ الدفع", "Paid date")}: {_order.PaidDate?.ToString("dd/MM/yyyy HH:mm") ?? "—"}")));
            document.Blocks.Add(new Paragraph(new Run(TaxBasisText.Text)));

            var table = new Table { CellSpacing = 0 };
            for (var i = 0; i < 8; i++)
                table.Columns.Add(new TableColumn { Width = new GridLength(i == 0 ? 3 : 1, GridUnitType.Star) });

            var headerGroup = new TableRowGroup();
            headerGroup.Rows.Add(CreateRow(new[]
            {
                UiText.T("المنتج", "Product"), UiText.T("الوحدة", "Unit"), UiText.T("الكمية", "Qty"),
                UiText.T("التكلفة", "Unit cost"), UiText.T("الخصم", "Discount"), UiText.T("الضريبة", "Tax"),
                UiText.T("إجمالي السطر شامل الضريبة", "Line total incl. tax"), UiText.T("الانتهاء", "Expiry")
            }, true));
            table.RowGroups.Add(headerGroup);

            var bodyGroup = new TableRowGroup();
            foreach (var line in _order.Lines ?? new List<PurchaseOrderLineWriteDto>())
            {
                bodyGroup.Rows.Add(CreateRow(new[]
                {
                    line.ProductName ?? line.ProductId.ToString(),
                    line.UnitName ?? line.ProductUnitId.ToString(),
                    Format(line.Quantity),
                    Format(line.UnitCost),
                    Format(line.LineDiscountAmount),
                    Format(line.TaxAmount),
                    Format(line.LineTotal),
                    line.ExpiryDate?.ToString("dd/MM/yyyy") ?? "—"
                }, false));
            }
            table.RowGroups.Add(bodyGroup);
            document.Blocks.Add(table);

            document.Blocks.Add(new Paragraph(new Run(
                $"{UiText.T("قبل الضريبة", "Subtotal")}: {Format(_order.SubTotal)} | " +
                $"{UiText.T("الضريبة", "Tax")}: {Format(_order.TotalTax)} | " +
                $"{UiText.T("خصم أمر الشراء", "Order discount")}: {Format(_order.DiscountAmount ?? 0m)} | " +
                $"{UiText.T("الإجمالي المستحق", "Grand total")}: {Format(_order.TotalAmount)}"))
            {
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 14, 0, 0)
            });

            if (!string.IsNullOrWhiteSpace(_order.Notes))
                document.Blocks.Add(new Paragraph(new Run($"{UiText.T("ملاحظات", "Notes")}: {_order.Notes}")));
            if (!string.IsNullOrWhiteSpace(_order.PaymentNote))
                document.Blocks.Add(new Paragraph(new Run($"{UiText.T("ملاحظة الدفع", "Payment note")}: {_order.PaymentNote}")));

            UiText.ApplyDocument(document);
            return document;
        }

        private static TableRow CreateRow(IEnumerable<string> values, bool header)
        {
            var row = new TableRow();
            foreach (var value in values)
            {
                var cell = new TableCell(new Paragraph(new Run(value)))
                {
                    Padding = new Thickness(5),
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(0.5),
                    Background = header ? new SolidColorBrush(Color.FromRgb(225, 237, 245)) : Brushes.White
                };
                row.Cells.Add(cell);
            }
            return row;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}

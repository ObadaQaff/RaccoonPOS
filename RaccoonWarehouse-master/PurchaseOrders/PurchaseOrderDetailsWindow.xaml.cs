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

        private async void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saved=await RaccoonWarehouse.Helpers.Pdf.DocumentPrintRuntime.SavedOrderAsync(_order.Id);
                RaccoonWarehouse.Helpers.Pdf.DocumentPrintRuntime.Preview(RaccoonWarehouse.Helpers.Pdf.PurchasingDocument.Order(saved, RaccoonWarehouse.Helpers.Pdf.DocumentPrintRuntime.Branding(warehouseId:saved.WarehouseId)), this);

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

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}

using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.PurchaseOrders.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System.Globalization;
using System.Windows;

namespace RaccoonWarehouse.PurchaseOrders
{
    public partial class PurchaseOrderDetailsWindow : Window
    {
        public PurchaseOrderDetailsWindow(PurchaseOrderWriteDto order)
        {
            InitializeComponent();
            UiText.ApplyWindow(this);
            OrderSummaryText.Text = $"{order.OrderNumber} | {Format(order.TotalAmount)}";
            SupplierText.Text = order.SupplierName ?? order.SupplierId?.ToString() ?? "—";
            DateText.Text = order.DocumentDate?.ToString("dd/MM/yyyy") ?? "—";
            PaymentText.Text = FormatPaymentType(order.PaymentType);
            StatusText.Text = order.Status.ToString();
            NotesText.Text = string.IsNullOrWhiteSpace(order.Notes)
                ? UiText.T("لا توجد ملاحظات.", "No notes.")
                : UiText.T($"ملاحظات: {order.Notes}", $"Notes: {order.Notes}");
            AuditText.Text = UiText.T(
                $"تاريخ الإنشاء: {order.CreatedDate:dd/MM/yyyy HH:mm} | رقم الفاتورة: {order.ConvertedInvoiceId?.ToString() ?? "—"}",
                $"Created: {order.CreatedDate:dd/MM/yyyy HH:mm} | Invoice: {order.ConvertedInvoiceId?.ToString() ?? "—"}");
            TotalsText.Text = UiText.T(
                $"قبل الضريبة: {Format(order.SubTotal)} | الضريبة: {Format(order.TotalTax)} | الخصم: {Format(order.DiscountAmount ?? 0m)} | الإجمالي: {Format(order.TotalAmount)}",
                $"Subtotal: {Format(order.SubTotal)} | Tax: {Format(order.TotalTax)} | Discount: {Format(order.DiscountAmount ?? 0m)} | Total: {Format(order.TotalAmount)}");
            LinesGrid.ItemsSource = order.Lines;
        }

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

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}

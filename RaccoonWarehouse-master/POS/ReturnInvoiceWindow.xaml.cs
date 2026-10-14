using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System.Collections.ObjectModel;
using System.Windows;

namespace RaccoonWarehouse.POS
{
    public partial class ReturnInvoiceWindow : Window
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ObservableCollection<ReturnInvoiceLookupRow> _rows = new();
        private bool _isBusy;

        public string OriginalInvoiceId { get; private set; } = string.Empty;

        public ReturnInvoiceWindow(IInvoiceService invoiceService)
        {
            InitializeComponent();
            UiText.ApplyWindow(this);
            _invoiceService = invoiceService;
            ResultsGrid.ItemsSource = _rows;
            Loaded += async (_, _) => await SearchAsync();
        }

        private async void Search_Click(object sender, RoutedEventArgs e) => await SearchAsync();

        private async Task SearchAsync()
        {
            if (_isBusy)
                return;

            try
            {
                _isBusy = true;
                var result = await _invoiceService.SearchReturnInvoiceHeadersAsync(
                    DateFrom.SelectedDate,
                    DateTo.SelectedDate,
                    InvoiceNumberTextBox.Text?.Trim());

                if (!result.Success)
                {
                    MessageBox.Show(
                        result.Message ?? UiText.T("تعذر تحميل الفواتير.", "Failed to load invoices."),
                        UiText.T("خطأ", "Error"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                _rows.Clear();
                foreach (var header in result.Data ?? new List<ReturnInvoiceLookupDto>())
                    _rows.Add(new ReturnInvoiceLookupRow(header));

                UiText.ApplyTranslations(ResultsGrid);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    UiText.T($"تعذر تحميل الفواتير: {ex.Message}", $"Failed to load invoices: {ex.Message}"),
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _isBusy = false;
            }
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy || ResultsGrid.SelectedItem is not ReturnInvoiceLookupRow selected)
                return;

            try
            {
                _isBusy = true;
                if (selected.IsFullyReturned)
                {
                    MessageBox.Show(
                        UiText.T("هذه الفاتورة تم إرجاعها بالكامل ولا توجد كميات متبقية للإرجاع.", "This invoice has already been fully returned and has no remaining quantity to return."),
                        UiText.T("الفاتورة مرتجعة بالكامل", "Invoice fully returned"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                // The POS return flow loads the full invoice once after this dialog closes.
                OriginalInvoiceId = selected.InvoiceNumber;
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    UiText.T($"تعذر تحميل تفاصيل الفاتورة: {ex.Message}", $"Failed to load invoice details: {ex.Message}"),
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _isBusy = false;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

        public sealed class ReturnInvoiceLookupRow
        {
            private readonly ReturnInvoiceLookupDto _source;

            public int Id => _source.Id;
            public string InvoiceNumber => _source.InvoiceNumber;
            public DateTime CreatedDate => _source.CreatedDate;
            public string CustomerName => string.IsNullOrWhiteSpace(_source.CustomerName) ? "—" : _source.CustomerName;
            public decimal TotalAmount => _source.TotalAmount;
            public string ReturnStatus => _source.ReturnStatus;
            public bool IsFullyReturned => _source.IsFullyReturned;
            public string PaymentMethodDisplay => _source.PaymentTypes.Count == 0
                ? UiText.T("غير محدد", "Not specified")
                : string.Join(" / ", _source.PaymentTypes.Select(GetPaymentName));

            public ReturnInvoiceLookupRow(ReturnInvoiceLookupDto source) => _source = source;

            private static string GetPaymentName(PaymentType paymentType) => paymentType switch
            {
                PaymentType.Cash => UiText.T("نقدي", "Cash"),
                PaymentType.Visa => "Visa",
                PaymentType.Master => "Master",
                PaymentType.Debit => UiText.T("تحويل بنكي", "Bank transfer"),
                PaymentType.Check => UiText.T("شيك", "Check"),
                PaymentType.MobilePayment => UiText.T("دفع إلكتروني", "Mobile payment"),
                PaymentType.Credit => UiText.T("آجل", "Credit"),
                _ => paymentType.ToString()
            };
        }
    }
}

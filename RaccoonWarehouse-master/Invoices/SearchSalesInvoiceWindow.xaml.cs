using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System.Windows;

namespace RaccoonWarehouse.Invoices
{
    public partial class SearchSalesInvoiceWindow : Window
    {
        private readonly IInvoiceService _invoiceService;
        private readonly bool? _isSal = true;
        public InvoiceReadDto? Result { get; private set; }

        public SearchSalesInvoiceWindow(IInvoiceService invoiceService, bool? isSal)
        {
            InitializeComponent();
            UiText.ApplyWindow(this);
            _invoiceService = invoiceService;
            _isSal = isSal;
        }

        private async void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = await _invoiceService.SearchSalesInvoicesAsync(
                InvoiceNumberTxt.Text,
                CustomerTxt.Text,
                DateFrom.SelectedDate,
                DateTo.SelectedDate,
                _isSal
            );

            if (result.Success)
            {
                ResultsGrid.ItemsSource = result.Data;
                UiText.ApplyTranslations(ResultsGrid);
            }
            else
                MessageBox.Show(result.Message ?? UiText.T("تعذر تحميل نتائج البحث.", "Failed to load search results."), UiText.T("خطأ", "Error"));
        }

        private async void SelectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ResultsGrid.SelectedItem is InvoiceReadDto invoice)
            {
                try
                {
                    var fullInvoice = await _invoiceService.GetFullInvoiceByIdAsync(invoice.Id);
                    Result = fullInvoice ?? invoice;
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        UiText.T($"تعذر تحميل الفاتورة الكاملة: {ex.Message}", $"Failed to load full invoice: {ex.Message}"),
                        UiText.T("خطأ", "Error"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }
    }
}

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

        private void SelectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ResultsGrid.SelectedItem is InvoiceReadDto invoice)
            {
                Result = invoice;
                DialogResult = true;
                Close();
            }
        }
    }
}

using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System.Windows;
using System.Windows.Input;

namespace RaccoonWarehouse.POS
{
    public partial class ResumeHeldInvoiceWindow : Window
    {
        private readonly IInvoiceService _invoiceService;

        public InvoiceReadDto? SelectedInvoice { get; private set; }

        public ResumeHeldInvoiceWindow(IInvoiceService invoiceService)
        {
            InitializeComponent();
            _invoiceService = invoiceService;
            UiText.ApplyWindow(this);
            Loaded += ResumeHeldInvoiceWindow_Loaded;
        }

        private async void ResumeHeldInvoiceWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var result = await _invoiceService.GetHeldPOSInvoicesAsync();
            if (result.Success)
            {
                HeldInvoicesGrid.ItemsSource = result.Data;
                UiText.ApplyTranslations(this);
            }
            else
            {
                MessageBox.Show(result.Message ?? UiText.T("تعذر تحميل الفواتير المعلقة.", "Failed to load held invoices."));
            }
        }

        private void Grid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectedInvoice = HeldInvoicesGrid.SelectedItem as InvoiceReadDto;
            if (SelectedInvoice != null)
            {
                DialogResult = true;
                Close();
            }
        }
    }
}

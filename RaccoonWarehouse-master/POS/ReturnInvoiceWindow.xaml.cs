using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Helpers.Localization;
using System.Linq;
using System.Windows;

namespace RaccoonWarehouse.POS
{
    public partial class ReturnInvoiceWindow : Window
    {
        public string OriginalInvoiceId { get; private set; }
        private readonly IInvoiceService _invoiceService;

        public ReturnInvoiceWindow(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
            InitializeComponent();
            UiText.ApplyWindow(this);
        }

        private async void Confirm_Click(object sender, RoutedEventArgs e)
        {
            var invoiceNumber = InvoiceNumberTextBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(invoiceNumber))
            {
                MessageBox.Show(UiText.T("يرجى إدخال رقم الفاتورة.", "Please enter the invoice number."));
                return;
            }

            var result = await _invoiceService.GetAllWriteDtoWithFilteringAndIncludeAsync(
                invoice => invoice.InvoiceNumber == invoiceNumber,
                invoice => invoice.InvoiceLines);

            var invoiceData = result.Data?.FirstOrDefault();
            if (invoiceData == null)
            {
                MessageBox.Show(UiText.T("الفاتورة غير موجودة.", "The invoice was not found."));
                return;
            }

            if (invoiceData.InvoiceLines == null || !invoiceData.InvoiceLines.Any())
            {
                MessageBox.Show(UiText.T("لا يمكن إرجاع أو استبدال فاتورة بدون مواد.", "You cannot return or exchange an invoice with no items."));
                return;
            }

            OriginalInvoiceId = invoiceData.InvoiceNumber;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

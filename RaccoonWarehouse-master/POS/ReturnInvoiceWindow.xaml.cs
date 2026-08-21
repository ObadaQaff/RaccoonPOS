using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Helpers.Localization;
using System.Windows;

namespace RaccoonWarehouse.POS
{
    public partial class ReturnInvoiceWindow : Window
    {
        public string OriginalInvoiceId { get; private set; } = string.Empty;

        public ReturnInvoiceWindow(IInvoiceService invoiceService)
        {
            InitializeComponent();
            UiText.ApplyWindow(this);
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            var invoiceNumber = InvoiceNumberTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(invoiceNumber))
            {
                MessageBox.Show(
                    UiText.T("يرجى إدخال رقم الفاتورة.", "Please enter the invoice number."),
                    UiText.T("تنبيه", "Notice"));
                return;
            }

            OriginalInvoiceId = invoiceNumber;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

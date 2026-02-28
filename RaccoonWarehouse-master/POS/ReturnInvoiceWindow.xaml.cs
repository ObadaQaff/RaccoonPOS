using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using System.Windows;
using RaccoonWarehouse.Application.Service.Invoices;

namespace RaccoonWarehouse.POS
{
    public partial class ReturnInvoiceWindow : Window
    {
        public string OriginalInvoiceId { get; private set; }
        private readonly IInvoiceService _invoiceService;

        public ReturnInvoiceWindow(
            IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
            InitializeComponent();
        }

        private async void Confirm_Click(object sender, RoutedEventArgs e)
        {
            var invoiceNumber = InvoiceNumberTextBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(invoiceNumber))
            {
                MessageBox.Show("يرجى إدخال رقم الفاتورة");
                return;
            }

            var result = await _invoiceService
                .GetAllWriteDtoWithFilteringAndIncludeAsync(
                    i => i.InvoiceNumber == invoiceNumber,
                    i => i.InvoiceLines
                );

            var invoice = result.Data?.FirstOrDefault();

            // 1️⃣ الفاتورة غير موجودة
            if (invoice == null)
            {
                MessageBox.Show("الفاتورة غير موجودة");
                return;
            }

            // 2️⃣ الفاتورة فارغة
            if (invoice.InvoiceLines == null || !invoice.InvoiceLines.Any())
            {
                MessageBox.Show("لا يمكن إرجاع أو استبدال فاتورة بدون مواد");
                return;
            }

            // 3️⃣ حفظ رقم الفاتورة الأصلية
            OriginalInvoiceId = invoice.InvoiceNumber;

            DialogResult = true;
            Close();
        }


        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}


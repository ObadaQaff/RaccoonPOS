using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Domain.POS.VM;
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

namespace RaccoonWarehouse.POS
{
    /// <summary>
    /// Interaction logic for DailySalesReport.xaml
    /// </summary>
    public partial class DailySalesReport : Window
    {
        private readonly IInvoiceService _invoiceService;
        private readonly DailySalesReportViewModel _vm;

        public DailySalesReport(IInvoiceService invoiceService)
        {
            InitializeComponent();
            _invoiceService = invoiceService;
            _vm = new DailySalesReportViewModel();
            DataContext = _vm;
        }

        private async void LoadReportBtn_Click(object sender, RoutedEventArgs e)
        {
           var date = _vm.ReportDate.Date;

            var result = await _invoiceService.SearchSalesInvoicesAsync(
                invoiceNumber: null,
                customerName: null,
                dateFrom: date,
                dateTo: date.AddDays(1),
                isSal: null,
                isPOS: true,
                status: InvoiceStatus.Completed
            );

            _vm.Invoices.Clear();

            foreach (var invoice in result.Data)
                _vm.Invoices.Add(invoice);

            _vm.TotalInvoices = _vm.Invoices.Count;
            _vm.TotalSales = _vm.Invoices.Sum(i => i.TotalAmount);
            _vm.TotalDiscount = _vm.Invoices.Sum(i => i.DiscountAmount ?? 0);
        }
    }

}

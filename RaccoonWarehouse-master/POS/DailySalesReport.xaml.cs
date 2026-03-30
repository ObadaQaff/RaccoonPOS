using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.POS.VM;
using RaccoonWarehouse.Helpers.Localization;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RaccoonWarehouse.POS
{
    public partial class DailySalesReport : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly DailySalesReportViewModel _vm;
        private readonly int _cashierSessionId;
        private readonly int _cashierId;
        private bool _isLoading;

        public DailySalesReport(IServiceProvider serviceProvider, int cashierSessionId, int cashierId)
        {
            InitializeComponent();
            UiText.ApplyWindow(this);
            _serviceProvider = serviceProvider;
            _cashierSessionId = cashierSessionId;
            _cashierId = cashierId;
            _vm = new DailySalesReportViewModel();
            DataContext = _vm;
            ContentRendered += DailySalesReport_ContentRendered;
        }

        private async void DailySalesReport_ContentRendered(object? sender, EventArgs e)
        {
            ContentRendered -= DailySalesReport_ContentRendered;
            await LoadReportAsync();
        }

        private async Task LoadReportAsync()
        {
            if (_isLoading)
                return;

            _isLoading = true;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();

                var date = _vm.ReportDate.Date;
                var result = await invoiceService.SearchSalesInvoicesAsync(
                    invoiceNumber: null,
                    customerName: null,
                    dateFrom: date,
                    dateTo: date.AddDays(1).AddTicks(-1),
                    isSal: null,
                    isPOS: true,
                    status: InvoiceStatus.Completed);

                if (!result.Success || result.Data == null)
                {
                    MessageBox.Show(
                        result.Message ?? UiText.T("تعذر تحميل التقرير اليومي.", "Failed to load the daily report."),
                        UiText.T("خطأ", "Error"));
                    return;
                }

                var sessionInvoices = result.Data
                    .Where(i => i.CashierSessionId == _cashierSessionId || (!i.CashierSessionId.HasValue && i.CasherId == _cashierId))
                    .OrderBy(i => i.ClosedAt ?? i.CreatedDate)
                    .ToList();

                _vm.Invoices.Clear();
                foreach (var invoice in sessionInvoices)
                    _vm.Invoices.Add(invoice);

                _vm.TotalInvoices = _vm.Invoices.Count;
                _vm.TotalSales = _vm.Invoices.Sum(i => i.TotalAmount);
                _vm.TotalDiscount = _vm.Invoices.Sum(i => i.DiscountAmount ?? 0m);
                UiText.ApplyTranslations(this);

                if (_vm.TotalInvoices == 0)
                {
                    MessageBox.Show(
                        UiText.T("لا توجد فواتير مبيعات اليوم للجلسة الحالية.", "There are no sales invoices for today's session."),
                        UiText.T("تنبيه", "Notice"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("تعذر تحميل التقرير اليومي", "Failed to load the daily report")}: {ex.Message}",
                    UiText.T("خطأ", "Error"));
            }
            finally
            {
                _isLoading = false;
            }
        }
    }
}

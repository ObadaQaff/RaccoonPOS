using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Reports.Financial.Filters;
using RaccoonWarehouse.Domain.Reports.Sales.Dtos;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Helpers.Pdf;
using RaccoonWarehouse.Helpers.Pdf.Reports;
using RaccoonWarehouse.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.Reports
{
    public partial class SalesReport : Window
    {
        private readonly IInvoiceService _invoiceService;   // ✅ real invoices query
        private readonly IUserService _userService;
        private readonly ILoadingService _loadingService;
        private readonly SourceDocumentNavigationService _sourceDocumentNavigationService;

        private List<UserReadDto> _customers = new();
        private List<UserReadDto> _cashiers = new();
        private List<SalesReportRowDto> _currentRows = new();

        public SalesReport(
            IInvoiceService invoiceService,
            IUserService userService,
            ILoadingService loadingService,
            SourceDocumentNavigationService sourceDocumentNavigationService)
        {
            InitializeComponent();
            UiText.ApplyWindow(this);

            _invoiceService = invoiceService;
            _userService = userService;
            _loadingService = loadingService;
            _sourceDocumentNavigationService = sourceDocumentNavigationService;

            Loaded += SalesReport_Loaded;
        }

        private async void SalesReportGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (SalesReportGrid.SelectedItem is not SalesReportRowDto row || row.InvoiceId <= 0)
                return;

            await _sourceDocumentNavigationService.OpenSourceDocument("Invoice", row.InvoiceId);
        }

        private async void SalesReport_Loaded(object sender, RoutedEventArgs e)
        {
            string? errorMessage = null;

            try
            {
                _loadingService.Show();

                InvoiceTypeComboBox.Items.Clear();
                InvoiceTypeComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("الكل", "All"), Tag = (InvoiceType?)null });
                InvoiceTypeComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("مبيعات", "Sales"), Tag = (InvoiceType?)InvoiceType.Sale });
                InvoiceTypeComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("مرتجع", "Return"), Tag = (InvoiceType?)InvoiceType.Return });
                InvoiceTypeComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("طلبات API", "Endpoint orders"), Tag = (InvoiceType?)InvoiceType.EndpointOrder });
                InvoiceTypeComboBox.SelectedIndex = 0;

                PosFilterComboBox.Items.Clear();
                PosFilterComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("الكل", "All"), Tag = null });
                PosFilterComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("فواتير POS فقط", "POS invoices only"), Tag = true });
                PosFilterComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("فواتير غير POS", "Non-POS invoices"), Tag = false });
                PosFilterComboBox.SelectedIndex = 0;

                PaymentMethodComboBox.Items.Clear();
                PaymentMethodComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("الكل", "All"), Tag = null });
                PaymentMethodComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("نقدي", "Cash"), Tag = PaymentType.Cash });
                PaymentMethodComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("آجل", "Credit"), Tag = PaymentType.Credit });
                PaymentMethodComboBox.Items.Add(new ComboBoxItem { Content = "Debit", Tag = PaymentType.Debit });
                PaymentMethodComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("شيك", "Check"), Tag = PaymentType.Check });
                PaymentMethodComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("موبايل", "Mobile"), Tag = PaymentType.MobilePayment });
                PaymentMethodComboBox.Items.Add(new ComboBoxItem { Content = "MasterCard", Tag = PaymentType.Master });
                PaymentMethodComboBox.Items.Add(new ComboBoxItem { Content = "Visa", Tag = PaymentType.Visa });
                PaymentMethodComboBox.SelectedIndex = 0;

                var rangeResult = await _invoiceService.GetSalesReportDateRangeAsync();
                if (!rangeResult.Success)
                    errorMessage = rangeResult.Message;

                FromDatePicker.SelectedDate = rangeResult.Success
                    ? rangeResult.Data.from?.Date ?? DateTime.Today
                    : DateTime.Today;
                ToDatePicker.SelectedDate = rangeResult.Success
                    ? rangeResult.Data.to?.Date ?? DateTime.Today
                    : DateTime.Today;

                var usersRes = await _userService.GetAllAsync();
                if (!usersRes.Success)
                    errorMessage ??= usersRes.Message;

                var allUsers = usersRes.Data ?? new List<UserReadDto>();
                _customers = allUsers
                    .Where(x => x.Role == UserRole.Customer)
                    .OrderBy(x => x.Name)
                    .ToList();
                _cashiers = allUsers
                    .Where(x => x.Role == UserRole.Casher)
                    .OrderBy(x => x.Name)
                    .ToList();

                CustomerComboBox.ItemsSource = new List<UserReadDto>
                {
                    new() { Id = 0, Name = UiText.T("الكل", "All") }
                }.Concat(_customers).ToList();
                CustomerComboBox.SelectedValue = 0;

                CashierComboBox.ItemsSource = new List<UserReadDto>
                {
                    new() { Id = 0, Name = UiText.T("الكل", "All") }
                }.Concat(_cashiers).ToList();
                CashierComboBox.SelectedValue = 0;

                UiText.ApplyTranslations(this);
                ClearSummary();
            }
            catch (Exception ex)
            {
                errorMessage = $"{UiText.T("تعذر تهيئة تقرير المبيعات", "Failed to initialize the sales report")}: {ex.Message}";
            }
            finally
            {
                _loadingService.Hide();
            }

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                MessageBox.Show(errorMessage, UiText.T("خطأ", "Error"));
                return;
            }

            await LoadReportAsync();
        }

        private async void GenerateReportBtn_Click(object sender, RoutedEventArgs e)
        {
            await LoadReportAsync();
        }

        private async System.Threading.Tasks.Task LoadReportAsync()
        {
            string? errorMessage = null;

            try
            {
                if (FromDatePicker.SelectedDate == null || ToDatePicker.SelectedDate == null)
                {
                    MessageBox.Show(UiText.T("يرجى اختيار تاريخ البداية والنهاية.", "Please choose the start and end dates."));
                    return;
                }

                _loadingService.Show();

                var from = FromDatePicker.SelectedDate.Value.Date;
                var to = ToDatePicker.SelectedDate.Value.Date.AddDays(1).AddTicks(-1); // include full day

                int? customerId = null;
                if (CustomerComboBox.SelectedValue is int cid && cid != 0)
                    customerId = cid;

                int? cashierId = null;
                if (CashierComboBox.SelectedValue is int cashierValue && cashierValue != 0)
                    cashierId = cashierValue;

                // ✅ get selected invoice type from Tag
                InvoiceType? invoiceType = null;

                if (InvoiceTypeComboBox.SelectedItem is ComboBoxItem it)
                {
                    if (it.Tag != null)
                        invoiceType = (InvoiceType)it.Tag;
                }

                bool? isPOS = null;
                if (PosFilterComboBox.SelectedItem is ComboBoxItem posItem && posItem.Tag != null)
                    isPOS = (bool)posItem.Tag;

                PaymentType? paymentType = null;
                if (PaymentMethodComboBox.SelectedItem is ComboBoxItem paymentItem && paymentItem.Tag is PaymentType selectedPaymentType)
                    paymentType = selectedPaymentType;

                var filter = new FinancialSummaryFilterDto
                {
                    From = from,
                    To = to,
                    CustomerId = customerId,
                    CashierId = cashierId,
                    PaymentType = paymentType,
                    IncludeReturns = true
                };

                var res = await _invoiceService.GetSalesReportAsync(filter, invoiceType, isPOS);

                if (!res.Success)
                {
                    errorMessage = res.Message ?? UiText.T("فشل تحميل التقرير", "Failed to load the report.");
                }
                else
                {
                    var rows = res.Data.rows ?? new List<SalesReportRowDto>();
                    _currentRows = rows;
                    SalesReportGrid.ItemsSource = rows;
                    FillSummary(rows);
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"{UiText.T("تعذر تحميل تقرير المبيعات", "Failed to load the sales report")}: {ex.Message}";
            }
            finally
            {
                _loadingService.Hide();
            }

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                MessageBox.Show(errorMessage, UiText.T("خطأ", "Error"));
            }
        }

        private void FillSummary(List<SalesReportRowDto> data)
        {
            bool IsReturn(SalesReportRowDto x)
            {
                var t = x.InvoiceType ?? "";
                return t.Contains("Return", StringComparison.OrdinalIgnoreCase)
                       || t.Contains("مرت", StringComparison.OrdinalIgnoreCase);
            }

            bool IsCountedSale(SalesReportRowDto x)
            {
                if (string.Equals(x.InvoiceType, InvoiceType.Sale.ToString(), StringComparison.OrdinalIgnoreCase))
                    return true;

                return string.Equals(x.InvoiceType, InvoiceType.EndpointOrder.ToString(), StringComparison.OrdinalIgnoreCase) &&
                       (string.Equals(x.Status, InvoiceStatus.Completed.ToString(), StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(x.Status, InvoiceStatus.Posted.ToString(), StringComparison.OrdinalIgnoreCase));
            }

            var countedSales = data.Where(IsCountedSale).ToList();
            decimal totalSales = countedSales.Sum(x => x.SubTotal);
            decimal totalReturns = data.Where(IsReturn).Sum(x => x.SubTotal);
            decimal totalTax = countedSales.Sum(x => x.TotalTax);
            decimal totalDiscount = countedSales.Sum(x => x.Discount);
            decimal totalCogs = countedSales.Sum(x => x.Cogs);

            decimal netSales = (totalSales - totalReturns) - totalDiscount;
            decimal grossProfit = netSales - totalCogs;

            decimal PaymentTotal(PaymentType paymentType) => data
                .Where(x => string.Equals(x.PaymentMethod, paymentType.ToString(), StringComparison.OrdinalIgnoreCase))
                .Sum(x => x.Total);

            CashTotalText.Text = PaymentTotal(PaymentType.Cash).ToString("0.00000");
            VisaTotalText.Text = PaymentTotal(PaymentType.Visa).ToString("0.00000");
            MasterTotalText.Text = PaymentTotal(PaymentType.Master).ToString("0.00000");
            DebitTotalText.Text = PaymentTotal(PaymentType.Debit).ToString("0.00000");
            CheckTotalText.Text = PaymentTotal(PaymentType.Check).ToString("0.00000");
            MobileTotalText.Text = PaymentTotal(PaymentType.MobilePayment).ToString("0.00000");
            CreditTotalText.Text = PaymentTotal(PaymentType.Credit).ToString("0.00000");

            TotalSalesText.Text = totalSales.ToString("0.00000");
            TotalReturnsText.Text = totalReturns.ToString("0.00000");
            TotalTaxText.Text = totalTax.ToString("0.00000");
            TotalDiscountText.Text = totalDiscount.ToString("0.00000");
            TotalCogsText.Text = totalCogs.ToString("0.00000");
            GrossProfitText.Text = grossProfit.ToString("0.00000");
        }

        private void ClearSummary()
        {
            TotalSalesText.Text = "0";
            TotalReturnsText.Text = "0";
            TotalTaxText.Text = "0";
            TotalDiscountText.Text = "0";
            TotalCogsText.Text = "0";
            GrossProfitText.Text = "0";
            CashTotalText.Text = "0";
            VisaTotalText.Text = "0";
            MasterTotalText.Text = "0";
            DebitTotalText.Text = "0";
            CheckTotalText.Text = "0";
            MobileTotalText.Text = "0";
            CreditTotalText.Text = "0";
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ExportPdfBtn_Click(object sender, RoutedEventArgs e)
        {
            var document = BuildPdfDocument();
            if (document == null)
                return;

            try
            {
                ReportPrintService.ExportPdf(document, this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("خطأ في تصدير التقرير", "Error exporting report")}: {ex.Message}");
            }
        }

        private void PrintBtn_Click(object sender, RoutedEventArgs e)
        {
            var document = BuildPdfDocument();
            if (document == null)
                return;

            try
            {
                ReportPrintService.Print(document, this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("خطأ في طباعة التقرير", "Error printing report")}: {ex.Message}");
            }
        }

        private SalesSummaryReportDocument? BuildPdfDocument()
        {
            if (_currentRows.Count == 0)
            {
                MessageBox.Show(UiText.T("اعرض التقرير أولاً قبل التصدير أو الطباعة.", "Generate the report before exporting or printing."));
                return null;
            }

            var customerName = (CustomerComboBox.SelectedItem as UserReadDto)?.Name ?? UiText.T("الكل", "All");
            return new SalesSummaryReportDocument(_currentRows, FromDatePicker.SelectedDate, ToDatePicker.SelectedDate, customerName);
        }
    }
}

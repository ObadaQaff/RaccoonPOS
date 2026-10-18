using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Reports.Financial.Filters;
using RaccoonWarehouse.Domain.Reports.Financial.Dtos;
using RaccoonWarehouse.Domain.Reports.Sales.Dtos;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Helpers;
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
        private readonly IFinancialTransactionService _financialTransactionService;
        private readonly SourceDocumentNavigationService _sourceDocumentNavigationService;

        private List<UserReadDto> _customers = new();
        private List<UserReadDto> _cashiers = new();
        private List<UserReadDto> _suppliers = new();
        private List<SalesReportRowDto> _currentRows = new();
        private bool _purchaseReportMode;
        private bool _isFilteringSuppliers;
        private bool _isNavigatingSuppliers;
        private string _supplierSearchText = string.Empty;

        public SalesReport(
            IInvoiceService invoiceService,
            IUserService userService,
            ILoadingService loadingService,
            SourceDocumentNavigationService sourceDocumentNavigationService,
            IFinancialTransactionService financialTransactionService)
        {
            InitializeComponent();
            UiText.ApplyWindow(this);

            _invoiceService = invoiceService;
            _userService = userService;
            _loadingService = loadingService;
            _financialTransactionService = financialTransactionService;
            _sourceDocumentNavigationService = sourceDocumentNavigationService;

            Loaded += SalesReport_Loaded;
        }

        private void SupplierComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (SupplierComboBox.Template.FindName("PART_EditableTextBox", SupplierComboBox) is TextBox textBox)
                textBox.TextChanged += SupplierComboBox_TextChanged;
        }

        private void SupplierComboBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key is not (System.Windows.Input.Key.Down or System.Windows.Input.Key.Up or System.Windows.Input.Key.Enter)
                || SupplierComboBox.Items.Count == 0)
                return;

            _isNavigatingSuppliers = true;
            try
            {
                if (e.Key == System.Windows.Input.Key.Down)
                    SupplierComboBox.SelectedIndex = Math.Min(SupplierComboBox.SelectedIndex + 1, SupplierComboBox.Items.Count - 1);
                else if (e.Key == System.Windows.Input.Key.Up)
                    SupplierComboBox.SelectedIndex = Math.Max(SupplierComboBox.SelectedIndex - 1, 0);
                else
                {
                    SupplierComboBox.IsDropDownOpen = false;
                    e.Handled = true;
                    return;
                }

                RestoreSupplierSearchText();
                e.Handled = true;
            }
            finally
            {
                _isNavigatingSuppliers = false;
            }
        }

        private void RestoreSupplierSearchText()
        {
            if (SupplierComboBox.Template.FindName("PART_EditableTextBox", SupplierComboBox) is not TextBox textBox)
                return;

            textBox.Text = _supplierSearchText;
            textBox.CaretIndex = textBox.Text.Length;
            SupplierComboBox.IsDropDownOpen = true;
        }

        private void SupplierComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFilteringSuppliers || _isNavigatingSuppliers || !SupplierComboBox.IsKeyboardFocusWithin)
                return;

            if (SupplierComboBox.Template.FindName("PART_EditableTextBox", SupplierComboBox) is not TextBox textBox)
                return;

            _supplierSearchText = textBox.Text.Trim();
            _isFilteringSuppliers = true;
            try
            {
                SupplierComboBox.SelectedValue = null;
                SupplierComboBox.ItemsSource = string.IsNullOrWhiteSpace(_supplierSearchText)
                    ? _suppliers.ToList()
                    : _suppliers.Where(user =>
                            (user.Name ?? string.Empty).Contains(_supplierSearchText, StringComparison.CurrentCultureIgnoreCase)
                            || (user.PhoneNumber ?? string.Empty).Contains(_supplierSearchText, StringComparison.CurrentCultureIgnoreCase))
                        .ToList();
                textBox.Text = _supplierSearchText;
                textBox.CaretIndex = textBox.Text.Length;
                SupplierComboBox.IsDropDownOpen = true;
            }
            finally
            {
                _isFilteringSuppliers = false;
            }
        }

        public void ConfigureAsPurchaseReport()
        {
            _purchaseReportMode = true;
            Title = UiText.T("تقرير المشتريات", "Purchase Report");
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
                if (_purchaseReportMode)
                {
                    InvoiceTypeComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("فواتير شراء", "Purchase invoices"), Tag = (InvoiceType?)InvoiceType.Purchase });
                    InvoiceTypeComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("مرتجع شراء", "Purchase returns"), Tag = (InvoiceType?)InvoiceType.PurchaseReturn });
                    CustomerFilterPanel.Visibility = Visibility.Collapsed;
                    SupplierFilterPanel.Visibility = Visibility.Visible;
                    PosFilterPanel.Visibility = Visibility.Collapsed;
                    VoucherSummaryGrid.Visibility = Visibility.Collapsed;
                    TotalDisplayedAmountCard.Visibility = Visibility.Visible;
                    SummaryCardsGrid.Columns = 5;
                    TotalCogsCard.Visibility = Visibility.Collapsed;
                    GrossProfitCard.Visibility = Visibility.Collapsed;
                    if (SalesReportGrid.Columns.Count > 9)
                    {
                        SalesReportGrid.Columns[8].Visibility = Visibility.Collapsed;
                        SalesReportGrid.Columns[9].Visibility = Visibility.Collapsed;
                    }
                    ReportHeaderText.Text = UiText.T("تقرير المشتريات", "Purchase Report");
                    TotalSalesLabel.Text = UiText.T("إجمالي الشراء", "Total purchases");
                    TotalReturnsLabel.Text = UiText.T("مرتجعات الشراء", "Purchase returns");
                }
                else
                {
                    InvoiceTypeComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("مبيعات", "Sales"), Tag = (InvoiceType?)InvoiceType.Sale });
                    InvoiceTypeComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("مرتجع", "Return"), Tag = (InvoiceType?)InvoiceType.Return });
                    InvoiceTypeComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("طلبات API", "Endpoint orders"), Tag = (InvoiceType?)InvoiceType.EndpointOrder });
                }
                if (!_purchaseReportMode)
                {
                    CustomerFilterPanel.Visibility = Visibility.Visible;
                    SupplierFilterPanel.Visibility = Visibility.Collapsed;
                    PosFilterPanel.Visibility = Visibility.Visible;
                    VoucherSummaryGrid.Visibility = Visibility.Visible;
                    TotalDisplayedAmountCard.Visibility = Visibility.Collapsed;
                    SummaryCardsGrid.Columns = 7;
                }

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

                var rangeResult = await _invoiceService.GetSalesReportDateRangeAsync(_purchaseReportMode);
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
                    .OrderBy(x => x.Name)
                    .ToList();
                _cashiers = allUsers
                    .Where(x => x.Role == UserRole.Casher)
                    .OrderBy(x => x.Name)
                    .ToList();
                _suppliers = allUsers
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

                SupplierComboBox.ItemsSource = new List<UserReadDto>
                {
                    new() { Id = 0, Name = UiText.T("الكل", "All") }
                }.Concat(_suppliers).ToList();
                SupplierComboBox.SelectedValue = 0;

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
                if (!_purchaseReportMode && CustomerComboBox.SelectedValue is int cid && cid != 0)
                    customerId = cid;

                int? supplierId = null;
                if (_purchaseReportMode && SupplierComboBox.SelectedValue is int sid && sid != 0)
                    supplierId = sid;

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
                    SupplierId = supplierId,
                    CashierId = cashierId,
                    PaymentType = paymentType,
                    IncludeReturns = true
                };

                var res = await _invoiceService.GetSalesReportAsync(filter, invoiceType, isPOS, _purchaseReportMode);

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

                    if (!_purchaseReportMode)
                    {
                        var voucherReport = await _financialTransactionService.GetVoucherPaymentsReceiptsAsync(new CashFlowFilterDto
                        {
                            From = from,
                            To = ToDatePicker.SelectedDate.Value.Date,
                            IncludeVoided = false
                        });
                        FillVoucherSummary(voucherReport.summary);
                    }
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
            if (_purchaseReportMode)
            {
                var purchases = data.Where(x => string.Equals(x.InvoiceType, InvoiceType.Purchase.ToString(), StringComparison.OrdinalIgnoreCase)).ToList();
                var purchaseReturns = data.Where(x => string.Equals(x.InvoiceType, InvoiceType.PurchaseReturn.ToString(), StringComparison.OrdinalIgnoreCase)).ToList();
                var totalPurchases = purchases.Sum(x => Math.Abs(x.Total));
                var totalPurchaseReturns = purchaseReturns.Sum(x => Math.Abs(x.Total));
                var totalPurchaseTax = purchases.Sum(x => Math.Abs(x.TotalTax));
                var totalPurchaseDiscount = purchases.Sum(x => Math.Abs(x.Discount));
                var totalDisplayedAmount = data.Sum(x => x.Total);

                TotalSalesText.Text = totalPurchases.ToString("0.00000");
                TotalReturnsText.Text = totalPurchaseReturns.ToString("0.00000");
                TotalTaxText.Text = totalPurchaseTax.ToString("0.00000");
                TotalDiscountText.Text = totalPurchaseDiscount.ToString("0.00000");
                TotalDisplayedAmountText.Text = totalDisplayedAmount.ToString("0.00000");
                TotalCogsText.Text = "0";
                GrossProfitText.Text = "0";

                decimal PurchasePaymentTotal(PaymentType paymentType) => data
                    .Sum(x => x.PaymentAmounts.TryGetValue(paymentType, out var amount) ? Math.Abs(amount) : 0m);

                CashTotalText.Text = PurchasePaymentTotal(PaymentType.Cash).ToString("0.00000");
                VisaTotalText.Text = PurchasePaymentTotal(PaymentType.Visa).ToString("0.00000");
                MasterTotalText.Text = PurchasePaymentTotal(PaymentType.Master).ToString("0.00000");
                DebitTotalText.Text = PurchasePaymentTotal(PaymentType.Debit).ToString("0.00000");
                CheckTotalText.Text = PurchasePaymentTotal(PaymentType.Check).ToString("0.00000");
                MobileTotalText.Text = PurchasePaymentTotal(PaymentType.MobilePayment).ToString("0.00000");
                CreditTotalText.Text = PurchasePaymentTotal(PaymentType.Credit).ToString("0.00000");
                return;
            }

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
            decimal totalReturns = data.Where(IsReturn).Sum(x => x.ReturnAmount);
            decimal totalTax = countedSales.Sum(x => x.TotalTax);
            decimal totalDiscount = countedSales.Sum(x => x.Discount);
            decimal totalCogs = countedSales.Sum(x => x.Cogs);

            decimal netSales = (totalSales - totalReturns) - totalDiscount;
            decimal grossProfit = netSales - totalCogs;

            decimal PaymentTotal(PaymentType paymentType) => data
                .Sum(x => x.PaymentAmounts.TryGetValue(paymentType, out var amount) ? amount : 0m);

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
            TotalDisplayedAmountText.Text = "0";
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
            VoucherReceiptsText.Text = "0";
            VoucherPaymentsText.Text = "0";
            VoucherNetText.Text = "0";
        }

        private void FillVoucherSummary(CashFlowSummaryDto summary)
        {
            VoucherReceiptsText.Text = summary.TotalIn.ToString("0.00000");
            VoucherPaymentsText.Text = summary.TotalOut.ToString("0.00000");
            VoucherNetText.Text = summary.Net.ToString("0.00000");
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

            var customerName = _purchaseReportMode
                ? (SupplierComboBox.SelectedItem as UserReadDto)?.Name ?? UiText.T("الكل", "All")
                : (CustomerComboBox.SelectedItem as UserReadDto)?.Name ?? UiText.T("الكل", "All");
            return new SalesSummaryReportDocument(_currentRows, FromDatePicker.SelectedDate, ToDatePicker.SelectedDate, customerName);
        }
    }
}

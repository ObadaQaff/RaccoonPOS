using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Reports.Financial.Filters;
using RaccoonWarehouse.Domain.Reports.Sales.Dtos;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Helpers.Pdf;
using RaccoonWarehouse.Helpers.Pdf.Reports;
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

        private List<UserReadDto> _customers = new();
        private List<UserReadDto> _cashiers = new();
        private List<SalesReportRowDto> _currentRows = new();

        public SalesReport(IInvoiceService invoiceService, IUserService userService)
        {
            InitializeComponent();
            UiText.ApplyWindow(this);

            _invoiceService = invoiceService;
            _userService = userService;

            Loaded += SalesReport_Loaded;
        }

        private async void SalesReport_Loaded(object sender, RoutedEventArgs e)
        {
            // ✅ default date range
            FromDatePicker.SelectedDate = DateTime.Today;
            ToDatePicker.SelectedDate = DateTime.Today;

            // ✅ invoice type filter
            // مهم: نخزن القيمة كـ enum داخل ComboBoxItem.Tag حتى ما نعتمد على string
            InvoiceTypeComboBox.Items.Clear();
            InvoiceTypeComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = UiText.T("الكل", "All"), Tag = (InvoiceType?)null });
            InvoiceTypeComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = UiText.T("مبيعات", "Sales"), Tag = (InvoiceType?)InvoiceType.Sale });
            InvoiceTypeComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = UiText.T("مرتجع", "Return"), Tag = (InvoiceType?)InvoiceType.Return });
            InvoiceTypeComboBox.SelectedIndex = 0;

            PosFilterComboBox.Items.Clear();
            PosFilterComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("الكل", "All"), Tag = null });
            PosFilterComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("فواتير POS فقط", "POS invoices only"), Tag = true });
            PosFilterComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("فواتير غير POS", "Non-POS invoices"), Tag = false });
            PosFilterComboBox.SelectedIndex = 0;

            // ✅ load report users
            var usersRes = await _userService.GetAllAsync();
            var allUsers = usersRes.Data ?? new List<UserReadDto>();

            _customers = allUsers
                .Where(x => x.Role == UserRole.Customer)
                .OrderBy(x => x.Name)
                .ToList();

            _cashiers = allUsers
                .Where(x => x.Role == UserRole.Casher)
                .OrderBy(x => x.Name)
                .ToList();

            var customerList = new List<UserReadDto>();
            customerList.Add(new UserReadDto { Id = 0, Name = UiText.T("الكل", "All") });
            customerList.AddRange(_customers);

            var cashierList = new List<UserReadDto>();
            cashierList.Add(new UserReadDto { Id = 0, Name = UiText.T("الكل", "All") });
            cashierList.AddRange(_cashiers);

            CustomerComboBox.ItemsSource = customerList;
            CustomerComboBox.SelectedValue = 0;
            CashierComboBox.ItemsSource = cashierList;
            CashierComboBox.SelectedValue = 0;
            UiText.ApplyTranslations(this);

            // ✅ init cards
            ClearSummary();
        }

        private async void GenerateReportBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FromDatePicker.SelectedDate == null || ToDatePicker.SelectedDate == null)
                {
                    MessageBox.Show(UiText.T("يرجى اختيار تاريخ البداية والنهاية.", "Please choose the start and end dates."));
                    return;
                }

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

                var filter = new FinancialSummaryFilterDto
                {
                    From = from,
                    To = to,
                    CustomerId = customerId,
                    CashierId = cashierId,
                    IncludeReturns = true
                };

                var res = await _invoiceService.GetSalesReportAsync(filter, invoiceType, isPOS);

                if (!res.Success)
                {
                    MessageBox.Show(res.Message ?? UiText.T("فشل تحميل التقرير", "Failed to load the report."));
                    return;
                }

                // ✅ res.Data هو (summary, rows)
                var rows = res.Data.rows ?? new List<SalesReportRowDto>();
                _currentRows = rows;
                SalesReportGrid.ItemsSource = rows;

                // لو عندك Summary Cards
                FillSummary(rows); // أو FillSummary(res.Data.summary) حسب طريقتك
                if (!res.Success)
                {
                    MessageBox.Show(res.Message ?? UiText.T("فشل تحميل التقرير.", "Failed to load the report."));
                    return;
                }

               
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("خطأ", "Error")}: {ex.Message}");
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

            decimal totalSales = data.Where(x => !IsReturn(x)).Sum(x => x.SubTotal);
            decimal totalReturns = data.Where(IsReturn).Sum(x => x.SubTotal);
            decimal totalTax = data.Where(x => !IsReturn(x)).Sum(x => x.TotalTax);
            decimal totalDiscount = data.Where(x => !IsReturn(x)).Sum(x => x.Discount);
            decimal totalCogs = data.Where(x => !IsReturn(x)).Sum(x => x.Cogs);

            decimal netSales = (totalSales - totalReturns) - totalDiscount;
            decimal grossProfit = netSales - totalCogs;

            TotalSalesText.Text = totalSales.ToString("0.##");
            TotalReturnsText.Text = totalReturns.ToString("0.##");
            TotalTaxText.Text = totalTax.ToString("0.##");
            TotalDiscountText.Text = totalDiscount.ToString("0.##");
            TotalCogsText.Text = totalCogs.ToString("0.##");
            GrossProfitText.Text = grossProfit.ToString("0.##");
        }

        private void ClearSummary()
        {
            TotalSalesText.Text = "0";
            TotalReturnsText.Text = "0";
            TotalTaxText.Text = "0";
            TotalDiscountText.Text = "0";
            TotalCogsText.Text = "0";
            GrossProfitText.Text = "0";
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

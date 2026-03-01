using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Reports.Financial.Filters;
using RaccoonWarehouse.Domain.Reports.Sales.Dtos;
using RaccoonWarehouse.Domain.Users.DTOs;
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
        private readonly IUserService _userService;         // ✅ customers

        private List<UserReadDto> _customers = new();

        public SalesReport(IInvoiceService invoiceService, IUserService userService)
        {
            InitializeComponent();

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
            InvoiceTypeComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "الكل", Tag = (InvoiceType?)null });
            InvoiceTypeComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "مبيعات", Tag = (InvoiceType?)InvoiceType.Sale });
            InvoiceTypeComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "مرتجع", Tag = (InvoiceType?)InvoiceType.Return });
            InvoiceTypeComboBox.SelectedIndex = 0;

            PosFilterComboBox.Items.Clear();
            PosFilterComboBox.Items.Add(new ComboBoxItem { Content = "الكل", Tag = null });
            PosFilterComboBox.Items.Add(new ComboBoxItem { Content = "فواتير POS فقط", Tag = true });
            PosFilterComboBox.Items.Add(new ComboBoxItem { Content = "فواتير غير POS", Tag = false });
            PosFilterComboBox.SelectedIndex = 0;

            // ✅ load customers
            var usersRes = await _userService.GetAllAsync();
            _customers = usersRes.Data ?? new List<UserReadDto>();

            var list = new List<UserReadDto>();
            list.Add(new UserReadDto { Id = 0, Name = "الكل" });
            list.AddRange(_customers);

            CustomerComboBox.ItemsSource = list;
            CustomerComboBox.SelectedValue = 0;

            // ✅ init cards
            ClearSummary();
        }

        private async void GenerateReportBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FromDatePicker.SelectedDate == null || ToDatePicker.SelectedDate == null)
                {
                    MessageBox.Show("يرجى اختيار تاريخ البداية والنهاية.");
                    return;
                }

                var from = FromDatePicker.SelectedDate.Value.Date;
                var to = ToDatePicker.SelectedDate.Value.Date.AddDays(1).AddTicks(-1); // include full day

                int? customerId = null;
                if (CustomerComboBox.SelectedValue is int cid && cid != 0)
                    customerId = cid;

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
                    IncludeReturns = true
                };

                var res = await _invoiceService.GetSalesReportAsync(filter, invoiceType, isPOS);

                if (!res.Success)
                {
                    MessageBox.Show(res.Message ?? "فشل تحميل التقرير.");
                    return;
                }

                // ✅ res.Data هو (summary, rows)
                var rows = res.Data.rows ?? new List<SalesReportRowDto>();
                SalesReportGrid.ItemsSource = rows;

                // لو عندك Summary Cards
                FillSummary(rows); // أو FillSummary(res.Data.summary) حسب طريقتك
                if (!res.Success)
                {
                    MessageBox.Show(res.Message ?? "فشل تحميل التقرير.");
                    return;
                }

               
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ: {ex.Message}");
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
    }
}

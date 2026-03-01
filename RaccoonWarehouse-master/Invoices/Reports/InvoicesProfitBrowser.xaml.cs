using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Users.DTOs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.Invoices.Reports
{
    public partial class InvoicesProfitBrowser : Window
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IUserService _userService;

        private ObservableCollection<UserReadDto> _customers = new();
        private ObservableCollection<InvoiceHeaderVm> _invoices = new();
        private ObservableCollection<InvoiceLineVm> _lines = new();

        public InvoicesProfitBrowser(IInvoiceService invoiceService, IUserService userService)
        {
            _invoiceService = invoiceService;
            _userService = userService;

            InitializeComponent();

            InvoicesGrid.ItemsSource = _invoices;
            LinesGrid.ItemsSource = _lines;

            Loaded += InvoicesProfitBrowser_Loaded;
        }

        private async void InvoicesProfitBrowser_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                FromDatePicker.SelectedDate = DateTime.Now.Date;
                ToDatePicker.SelectedDate = DateTime.Now.Date;

                // Customers
                var users = await _userService.GetAllAsync();
                _customers = new ObservableCollection<UserReadDto>(users?.Data ?? new List<UserReadDto>());

                // add "All"
                CustomerComboBox.ItemsSource = _customers;
                CustomerComboBox.SelectedIndex = -1;

                // Invoice Types (Tag = enum)
                InvoiceTypeComboBox.Items.Clear();

                InvoiceTypeComboBox.Items.Add(new ComboBoxItem { Content = "الكل", Tag = null });
                InvoiceTypeComboBox.Items.Add(new ComboBoxItem { Content = "مبيعات", Tag = InvoiceType.Sale });
                InvoiceTypeComboBox.Items.Add(new ComboBoxItem { Content = "مرتجعات", Tag = InvoiceType.Return });

                InvoiceTypeComboBox.SelectedIndex = 0;

                // Initial load
                await LoadInvoicesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء التحميل: {ex.Message}", "خطأ");
            }
        }

        private async void LoadBtn_Click(object sender, RoutedEventArgs e)
        {
            await LoadInvoicesAsync();
        }

        private async Task LoadInvoicesAsync()
        {
            if (FromDatePicker.SelectedDate == null || ToDatePicker.SelectedDate == null)
            {
                MessageBox.Show("يرجى اختيار تاريخ البداية والنهاية.");
                return;
            }

            var from = FromDatePicker.SelectedDate.Value.Date;
            var to = ToDatePicker.SelectedDate.Value.Date.AddDays(1).AddTicks(-1);

            int? customerId = null;
            if (CustomerComboBox.SelectedValue is int cid)
                customerId = cid;

            InvoiceType? invoiceType = null;
            if (InvoiceTypeComboBox.SelectedItem is ComboBoxItem it && it.Tag is InvoiceType t)
                invoiceType = t;

            try
            {
                _invoices.Clear();
                _lines.Clear();
                ClearSelectedSummary();

                // ✅ IMPORTANT:
                // Here we load invoice headers (fast).
                // Use your existing method - adjust name if different in your IInvoiceService.
                // Option A: if you already have GetAllWithFilteringAndIncludeAsync(...)
                var res = await _invoiceService.GetAllWithFilteringAndIncludeAsync(
                    x => x.CreatedDate >= from && x.CreatedDate <= to
                      && (!customerId.HasValue || x.CustomerId == customerId.Value)
                      && (invoiceType.HasValue
                            ? x.InvoiceType == invoiceType.Value
                            : x.InvoiceType == InvoiceType.Sale),
                    x => x.User);

                if (!res.Success)
                {
                    MessageBox.Show(res.Message ?? "فشل تحميل الفواتير");
                    return;
                }

                var list = res.Data ?? new List<InvoiceReadDto>();

                foreach (var inv in list.OrderByDescending(x => x.CreatedDate))
                {
                    var discount = inv.DiscountAmount ?? 0m;
                    var subTotal = inv.SubTotal;
                    var tax = inv.TotalTax;
                    var cogs = inv.TotalCOGS;

                    var netProfit = (subTotal - discount) - cogs;

                    _invoices.Add(new InvoiceHeaderVm
                    {
                        Id = inv.Id,
                        InvoiceNumber = inv.InvoiceNumber,
                        Date = inv.CreatedDate,
                        CustomerName = inv.User?.Name ?? "—",

                        SubTotal = subTotal,
                        TotalTax = tax,
                        Discount = discount,
                        TotalCOGS = cogs,

                        TotalAmount = inv.TotalAmount,
                        NetProfit = netProfit,

                        InvoiceType = inv.InvoiceType.ToString(),
                        PaymentMethod = inv.PaymentType?.ToString() ?? "—",
                        Status = inv.Status?.ToString() ?? "—",
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء تحميل الفواتير: {ex.Message}");
            }
        }

        private async void InvoicesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (InvoicesGrid.SelectedItem is not InvoiceHeaderVm header)
                return;

            try
            {
                _lines.Clear();

                // Load full invoice with lines
                var full = await _invoiceService.GetFullInvoiceByIdAsync(header.Id);

                if (full == null)
                {
                    MessageBox.Show("الفاتورة غير موجودة");
                    return;
                }

                var discount = full.DiscountAmount ?? 0m;
                var subTotal = full.SubTotal;
                var tax = full.TotalTax;

                // If you already save TotalCOGS / GrossProfit, use them.
                // If not, compute from lines:
                var lines = full.InvoiceLines?.ToList() ?? new List<InvoiceLineReadDto>();
                var cogs = lines.Sum(l => l.Quantity * l.UnitCost);

                var grossProfit = (subTotal - discount) - cogs;
                var netProfit = grossProfit;

                // Fill summary UI
                SelSubTotalText.Text = subTotal.ToString("0.###");
                SelTaxText.Text = tax.ToString("0.###");
                SelDiscountText.Text = discount.ToString("0.###");
                SelCogsText.Text = cogs.ToString("0.###");
                SelGrossProfitText.Text = grossProfit.ToString("0.###");
                SelNetProfitText.Text = netProfit.ToString("0.###");

                foreach (var l in lines)
                {
                    var qty = l.Quantity;
                    var unitCost = l.UnitCost;

                    var lineSub = l.LineSubTotal > 0 ? l.LineSubTotal : (qty * l.UnitPrice);
                    var costTotal = qty * unitCost;
                    var taxAmount = l.TaxAmount;

                    var profitBeforeTax = lineSub - costTotal;

                    _lines.Add(new InvoiceLineVm
                    {
                        ProductName = l.Product?.Name ?? l.ProductName ?? "—",
                        UnitName = l.ProductUnit?.Unit?.Name  ?? "—",
                        Quantity = qty,
                        UnitPrice = l.UnitPrice,

                        LineSubTotal = lineSub,
                        TaxAmount = taxAmount,

                        UnitCost = unitCost,
                        CostTotal = costTotal,

                        ProfitBeforeTax = profitBeforeTax,
                        Profit = profitBeforeTax
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء تحميل تفاصيل الفاتورة: {ex.Message}");
            }
        }

        private void ClearSelectedSummary()
        {
            SelSubTotalText.Text = "—";
            SelTaxText.Text = "—";
            SelDiscountText.Text = "—";
            SelCogsText.Text = "—";
            SelGrossProfitText.Text = "—";
            SelNetProfitText.Text = "—";
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // ---------------- ViewModels ----------------

        public class InvoiceHeaderVm
        {
            public int Id { get; set; }
            public string InvoiceNumber { get; set; }
            public DateTime Date { get; set; }
            public string CustomerName { get; set; }

            public decimal SubTotal { get; set; }
            public decimal TotalTax { get; set; }
            public decimal Discount { get; set; }
            public decimal TotalCOGS { get; set; }

            public decimal TotalAmount { get; set; }
            public decimal NetProfit { get; set; }

            public string InvoiceType { get; set; }
            public string PaymentMethod { get; set; }
            public string Status { get; set; }
        }

        public class InvoiceLineVm
        {
            public string ProductName { get; set; }
            public string UnitName { get; set; }
            public decimal Quantity { get; set; }

            public decimal UnitPrice { get; set; }
            public decimal LineSubTotal { get; set; }

            public decimal TaxAmount { get; set; }

            public decimal UnitCost { get; set; }
            public decimal CostTotal { get; set; }

            public decimal ProfitBeforeTax { get; set; }
            public decimal Profit { get; set; }
        }
    }
}

using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Helpers.Localization;
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
            UiText.ApplyWindow(this);

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

                var users = await _userService.GetAllAsync();
                _customers = new ObservableCollection<UserReadDto>(users?.Data ?? new List<UserReadDto>());
                CustomerComboBox.ItemsSource = _customers;
                CustomerComboBox.SelectedIndex = -1;

                InvoiceTypeComboBox.Items.Clear();
                InvoiceTypeComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("الكل", "All"), Tag = null });
                InvoiceTypeComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("مبيعات", "Sales"), Tag = InvoiceType.Sale });
                InvoiceTypeComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("مرتجعات", "Returns"), Tag = InvoiceType.Return });
                InvoiceTypeComboBox.SelectedIndex = 0;

                await LoadInvoicesAsync();
                UiText.ApplyTranslations(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("خطأ أثناء التحميل", "Loading error")}: {ex.Message}", UiText.T("خطأ", "Error"));
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
                MessageBox.Show(UiText.T("يرجى اختيار تاريخ البداية والنهاية.", "Please choose the start and end dates."));
                return;
            }

            var from = FromDatePicker.SelectedDate.Value.Date;
            var to = ToDatePicker.SelectedDate.Value.Date.AddDays(1).AddTicks(-1);

            int? customerId = null;
            if (CustomerComboBox.SelectedValue is int cid)
            {
                customerId = cid;
            }

            InvoiceType? invoiceType = null;
            if (InvoiceTypeComboBox.SelectedItem is ComboBoxItem item && item.Tag is InvoiceType type)
            {
                invoiceType = type;
            }

            try
            {
                _invoices.Clear();
                _lines.Clear();
                ClearSelectedSummary();

                var result = await _invoiceService.GetAllWithFilteringAndIncludeAsync(
                    invoice => invoice.CreatedDate >= from && invoice.CreatedDate <= to
                        && (!customerId.HasValue || invoice.CustomerId == customerId.Value)
                        && (invoiceType.HasValue ? invoice.InvoiceType == invoiceType.Value : invoice.InvoiceType == InvoiceType.Sale),
                    invoice => invoice.User);

                if (!result.Success)
                {
                    MessageBox.Show(result.Message ?? UiText.T("فشل تحميل الفواتير.", "Failed to load invoices."));
                    return;
                }

                var list = result.Data ?? new List<InvoiceReadDto>();
                foreach (var invoice in list.OrderByDescending(x => x.CreatedDate))
                {
                    var discount = invoice.DiscountAmount ?? 0m;
                    var subTotal = invoice.SubTotal;
                    var tax = invoice.TotalTax;
                    var cogs = invoice.TotalCOGS;
                    var netProfit = (subTotal - discount) - cogs;

                    _invoices.Add(new InvoiceHeaderVm
                    {
                        Id = invoice.Id,
                        InvoiceNumber = invoice.InvoiceNumber,
                        Date = invoice.CreatedDate,
                        CustomerName = invoice.User?.Name ?? "—",
                        DelegateName = invoice.DelegateName ?? "—",
                        SubTotal = subTotal,
                        TotalTax = tax,
                        Discount = discount,
                        TotalCOGS = cogs,
                        TotalAmount = invoice.TotalAmount,
                        NetProfit = netProfit,
                        InvoiceType = invoice.InvoiceType.ToString(),
                        PaymentMethod = invoice.PaymentType?.ToString() ?? "—",
                        Status = invoice.Status?.ToString() ?? "—",
                    });
                }

                UiText.ApplyTranslations(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("خطأ أثناء تحميل الفواتير", "Error while loading invoices")}: {ex.Message}");
            }
        }

        private async void InvoicesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (InvoicesGrid.SelectedItem is not InvoiceHeaderVm header)
            {
                return;
            }

            try
            {
                _lines.Clear();

                var fullInvoice = await _invoiceService.GetFullInvoiceByIdAsync(header.Id);
                if (fullInvoice == null)
                {
                    MessageBox.Show(UiText.T("الفاتورة غير موجودة.", "The invoice was not found."));
                    return;
                }

                var discount = fullInvoice.DiscountAmount ?? 0m;
                var subTotal = fullInvoice.SubTotal;
                var tax = fullInvoice.TotalTax;

                var lines = fullInvoice.InvoiceLines?.ToList() ?? new List<InvoiceLineReadDto>();
                var cogs = lines.Sum(line => line.Quantity * line.UnitCost);
                var grossProfit = (subTotal - discount) - cogs;
                var netProfit = grossProfit;

                SelSubTotalText.Text = subTotal.ToString("0.###");
                SelTaxText.Text = tax.ToString("0.###");
                SelDiscountText.Text = discount.ToString("0.###");
                SelCogsText.Text = cogs.ToString("0.###");
                SelGrossProfitText.Text = grossProfit.ToString("0.###");
                SelNetProfitText.Text = netProfit.ToString("0.###");

                foreach (var line in lines)
                {
                    var quantity = line.Quantity;
                    var unitCost = line.UnitCost;
                    var lineSubTotal = line.LineSubTotal > 0 ? line.LineSubTotal : quantity * line.UnitPrice;
                    var costTotal = quantity * unitCost;
                    var taxAmount = line.TaxAmount;
                    var profitBeforeTax = lineSubTotal - costTotal;

                    _lines.Add(new InvoiceLineVm
                    {
                        ProductName = line.Product?.Name ?? line.ProductName ?? "—",
                        UnitName = line.ProductUnit?.Unit?.Name ?? "—",
                        Quantity = quantity,
                        UnitPrice = line.UnitPrice,
                        LineSubTotal = lineSubTotal,
                        TaxAmount = taxAmount,
                        UnitCost = unitCost,
                        CostTotal = costTotal,
                        ProfitBeforeTax = profitBeforeTax,
                        Profit = profitBeforeTax
                    });
                }

                UiText.ApplyTranslations(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("خطأ أثناء تحميل تفاصيل الفاتورة", "Error while loading invoice details")}: {ex.Message}");
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

        public class InvoiceHeaderVm
        {
            public int Id { get; set; }
            public string InvoiceNumber { get; set; }
            public DateTime Date { get; set; }
            public string CustomerName { get; set; }
            public string DelegateName { get; set; }
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

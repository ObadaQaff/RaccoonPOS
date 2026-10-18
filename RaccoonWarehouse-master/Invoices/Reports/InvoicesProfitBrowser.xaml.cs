using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Helpers;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Products;
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
        private sealed class InvoiceMetrics
        {
            public decimal SubTotal { get; init; }
            public decimal TotalTax { get; init; }
            public decimal Discount { get; init; }
            public decimal TotalCOGS { get; init; }
            public decimal TotalAmount { get; init; }
            public decimal Profit { get; init; }
        }

        private readonly IInvoiceService _invoiceService;
        private readonly IUserService _userService;
        private readonly ILoadingService _loadingService;
        private readonly SourceDocumentNavigationService _sourceDocumentNavigationService;

        private readonly ObservableCollection<UserReadDto> _customers = new();
        private readonly ObservableCollection<InvoiceHeaderVm> _invoices = new();
        private readonly ObservableCollection<InvoiceLineVm> _lines = new();
        private readonly Dictionary<int, UserReadDto> _supplierLookup = new();
        private bool _purchaseReportMode;

        public InvoicesProfitBrowser(
            IInvoiceService invoiceService,
            IUserService userService,
            ILoadingService loadingService,
            SourceDocumentNavigationService sourceDocumentNavigationService)
        {
            _invoiceService = invoiceService;
            _userService = userService;
            _loadingService = loadingService;
            _sourceDocumentNavigationService = sourceDocumentNavigationService;

            InitializeComponent();
            UiText.ApplyWindow(this);

            InvoicesGrid.ItemsSource = _invoices;
            LinesGrid.ItemsSource = _lines;
            SupplierComboBoxSearch.Attach(SupplierComboBox);

            Loaded += InvoicesProfitBrowser_Loaded;
        }

        public void ConfigureAsPurchaseReport()
        {
            _purchaseReportMode = true;
            Title = UiText.T("تقرير المشتريات", "Purchase Report");
        }

        private async void InvoicesGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (InvoicesGrid.SelectedItem is not InvoiceHeaderVm invoice || invoice.Id <= 0)
                return;

            await _sourceDocumentNavigationService.OpenSourceDocument("Invoice", invoice.Id);
        }

        private void LinesGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (LinesGrid.SelectedItem is not InvoiceLineVm line || line.ProductId <= 0)
                return;

            WindowManager.ShowDialog<UpdateProduct>(WindowSizeType.MediumRectangle, window => window.Initialize(line.ProductId));
        }

        private async void InvoicesProfitBrowser_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _loadingService.Show();

                var rangeResult = await _invoiceService.GetSalesReportDateRangeAsync();
                FromDatePicker.SelectedDate = rangeResult.Success
                    ? rangeResult.Data.from?.Date ?? DateTime.Now.Date
                    : DateTime.Now.Date;
                ToDatePicker.SelectedDate = rangeResult.Success
                    ? rangeResult.Data.to?.Date ?? DateTime.Now.Date
                    : DateTime.Now.Date;

                var users = await _userService.GetAllAsync();
                _customers.Clear();
                foreach (var user in users?.Data ?? new List<UserReadDto>())
                    _customers.Add(user);

                CustomerComboBox.ItemsSource = _customers;
                CustomerComboBox.SelectedIndex = -1;

                var suppliers = (users?.Data ?? new List<UserReadDto>())
                    .OrderBy(x => x.Name).ToList();
                suppliers.Insert(0, new UserReadDto { Id = 0, Name = "All suppliers" });
                _supplierLookup.Clear();
                foreach (var supplier in suppliers.Where(x => x.Id > 0))
                    _supplierLookup[supplier.Id] = supplier;
                SupplierComboBoxSearch.SetItems(SupplierComboBox, suppliers);
                SupplierComboBox.SelectedIndex = 0;

                InvoiceTypeComboBox.Items.Clear();
                InvoiceTypeComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("الكل", "All"), Tag = null });
                InvoiceTypeComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("مبيعات", "Sales"), Tag = InvoiceType.Sale });
                InvoiceTypeComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("مرتجعات", "Returns"), Tag = InvoiceType.Return });
                InvoiceTypeComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("طلبات API", "Endpoint orders"), Tag = InvoiceType.EndpointOrder });
                InvoiceTypeComboBox.Items.Add(new ComboBoxItem { Content = "Purchase invoices", Tag = InvoiceType.Purchase });
                InvoiceTypeComboBox.Items.Add(new ComboBoxItem { Content = "Purchase returns", Tag = InvoiceType.PurchaseReturn });
                InvoiceTypeComboBox.SelectedIndex = _purchaseReportMode ? 4 : 0;

                await LoadInvoicesAsync();
                UiText.ApplyTranslations(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("خطأ أثناء التحميل", "Loading error")}: {ex.Message}", UiText.T("خطأ", "Error"));
            }
            finally
            {
                _loadingService.Hide();
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
                customerId = cid;

            InvoiceType? invoiceType = null;
            if (InvoiceTypeComboBox.SelectedItem is ComboBoxItem item && item.Tag is InvoiceType type)
                invoiceType = type;

            int? supplierId = SupplierComboBox.SelectedValue is int sid && sid > 0 ? sid : null;

            try
            {
                _loadingService.Show();
                _invoices.Clear();
                _lines.Clear();
                ClearSelectedSummary();

                var result = await _invoiceService.GetAllWithFilteringAndIncludeAsync(
                    invoice => invoice.CreatedDate >= from && invoice.CreatedDate <= to
                        && (invoice.Status == null || invoice.Status == InvoiceStatus.Completed || invoice.Status == InvoiceStatus.Posted || invoice.Status == InvoiceStatus.Returned)
                        && (!customerId.HasValue || invoice.CustomerId == customerId.Value)
                        && (!supplierId.HasValue || invoice.SupplierId == supplierId.Value)
                        && (
                            !invoiceType.HasValue
                            || invoice.InvoiceType == invoiceType.Value
                            || (invoiceType.Value == InvoiceType.Sale && invoice.InvoiceType == InvoiceType.EndpointOrder)
                        ),
                    invoice => invoice.User,
                    invoice => invoice.InvoiceLines);

                if (!result.Success)
                {
                    MessageBox.Show(result.Message ?? UiText.T("فشل تحميل الفواتير.", "Failed to load invoices."));
                    return;
                }

                foreach (var invoice in (result.Data ?? new List<InvoiceReadDto>()).OrderByDescending(x => x.CreatedDate))
                {
                    var metrics = CalculateInvoiceMetrics(invoice);
                    var supplierName = invoice.SupplierId.HasValue && _supplierLookup.TryGetValue(invoice.SupplierId.Value, out var supplier)
                        ? supplier.Name
                        : null;

                    _invoices.Add(new InvoiceHeaderVm
                    {
                        Id = invoice.Id,
                        InvoiceNumber = invoice.InvoiceNumber,
                        Date = invoice.CreatedDate,
                        CustomerName = supplierName ?? invoice.User?.Name ?? invoice.Customer?.Name ?? "-",
                        PartyName = supplierName ?? invoice.Customer?.Name ?? invoice.User?.Name ?? "-",
                        DelegateName = invoice.DelegateName ?? "-",
                        SubTotal = metrics.SubTotal,
                        TotalTax = metrics.TotalTax,
                        Discount = metrics.Discount,
                        TotalCOGS = metrics.TotalCOGS,
                        TotalAmount = metrics.TotalAmount,
                        NetProfit = metrics.Profit,
                        InvoiceType = invoice.InvoiceType.ToString(),
                        PaymentMethod = invoice.PaymentType?.ToString() ?? "-",
                        Status = invoice.Status?.ToString() ?? "-"
                    });
                }

                UiText.ApplyTranslations(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("خطأ أثناء تحميل الفواتير", "Error while loading invoices")}: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private async void InvoicesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (InvoicesGrid.SelectedItem is not InvoiceHeaderVm header)
                return;

            try
            {
                _lines.Clear();

                var fullInvoice = await _invoiceService.GetFullInvoiceByIdAsync(header.Id);
                if (fullInvoice == null)
                {
                    MessageBox.Show(UiText.T("الفاتورة غير موجودة.", "The invoice was not found."));
                    return;
                }

                var metrics = CalculateInvoiceMetrics(fullInvoice);
                var lines = fullInvoice.InvoiceLines?.ToList() ?? new List<InvoiceLineReadDto>();

                SelSubTotalText.Text = metrics.SubTotal.ToString("0.000");
                SelTaxText.Text = metrics.TotalTax.ToString("0.000");
                SelDiscountText.Text = metrics.Discount.ToString("0.000");
                SelCogsText.Text = metrics.TotalCOGS.ToString("0.000");
                SelGrossProfitText.Text = metrics.Profit.ToString("0.000");
                SelNetProfitText.Text = metrics.Profit.ToString("0.000");

                foreach (var line in lines)
                {
                    var quantity = line.Quantity;
                    var unitCost = line.UnitCost;
                    var lineSubTotal = line.LineSubTotal != 0m ? line.LineSubTotal : quantity * line.UnitPrice;
                    var costTotal = quantity * unitCost;
                    var taxAmount = line.TaxAmount;
                    var profitBeforeTax = lineSubTotal - costTotal;

                    _lines.Add(new InvoiceLineVm
                    {
                        ProductId = line.ProductId,
                        ProductName = line.Product?.Name ?? line.ProductName ?? "-",
                        UnitName = line.ProductUnit?.Unit?.Name ?? "-",
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
            SelSubTotalText.Text = "-";
            SelTaxText.Text = "-";
            SelDiscountText.Text = "-";
            SelCogsText.Text = "-";
            SelGrossProfitText.Text = "-";
            SelNetProfitText.Text = "-";
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static InvoiceMetrics CalculateInvoiceMetrics(InvoiceReadDto invoice)
        {
            var lines = invoice.InvoiceLines?.ToList() ?? new List<InvoiceLineReadDto>();
            var hasLines = lines.Count > 0;
            var countForProfit = ShouldCountForProfit(invoice);

            var subTotal = hasLines
                ? lines.Sum(line => line.LineSubTotal != 0m ? line.LineSubTotal : line.Quantity * line.UnitPrice)
                : invoice.SubTotal;

            var tax = hasLines
                ? lines.Sum(line => line.TaxAmount)
                : invoice.TotalTax;

            var cogs = hasLines
                ? lines.Sum(line => line.Quantity * line.UnitCost)
                : invoice.TotalCOGS;

            var invoiceDiscount = invoice.DiscountAmount ?? 0m;
            var lineDiscount = lines.Sum(line => line.LineDiscountAmount);
            var discount = invoiceDiscount + lineDiscount;
            var totalAmount = subTotal + tax - invoiceDiscount;
            var profit = countForProfit ? (subTotal - invoiceDiscount) - cogs : 0m;
            var displayedCogs = countForProfit ? cogs : 0m;

            return new InvoiceMetrics
            {
                SubTotal = subTotal,
                TotalTax = tax,
                Discount = discount,
                TotalCOGS = displayedCogs,
                TotalAmount = totalAmount,
                Profit = profit
            };
        }

        private static bool ShouldCountForProfit(InvoiceReadDto invoice)
        {
            if (invoice.InvoiceType == InvoiceType.Sale || invoice.InvoiceType == InvoiceType.Return)
                return true;

            if (invoice.InvoiceType != InvoiceType.EndpointOrder)
                return false;

            return invoice.Status is InvoiceStatus.Completed or InvoiceStatus.Posted;
        }

        public class InvoiceHeaderVm
        {
            public int Id { get; set; }
            public string InvoiceNumber { get; set; }
            public DateTime Date { get; set; }
            public string CustomerName { get; set; }
            public string PartyName { get; set; }
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
            public int ProductId { get; set; }
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

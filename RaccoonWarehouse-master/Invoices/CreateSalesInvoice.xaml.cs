using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Application.Service.Delegates;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.ProductUnits;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.StockTransactions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Common;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Domain.Cashiers.DTOs;
using RaccoonWarehouse.Domain.Delegates.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.FinancialTransactions.DTOs;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using RaccoonWarehouse.Domain.Stock;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Domain.StockTransactions.DTOs;
using RaccoonWarehouse.Domain.Units.DTOs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using RaccoonWarehouse.Domain.Stock.DTOs;
using RaccoonWarehouse.Helpers.Localization;

namespace RaccoonWarehouse.Invoices
{
    public partial class CreateSalesInvoice : Window
    {
        private const decimal MinimumSellableQuantity = 10m;

        // ====== مجموعات للـ Binding ======
        public ObservableCollection<ProductReadDto> Products { get; set; } = new();
        private Dictionary<StockItemWriteDto, int> _itemUnits = new();

        public ObservableCollection<InvoiceLineWriteDto> InvoiceLines { get; set; } = new();

        private ObservableCollection<UserReadDto> _allCustomers;
        private ObservableCollection<DelegateReadDto> _allDelegates = new();
        private List<InvoiceLineReadDto> _originalLines = new(); // to restore stock on update


        private readonly IInvoiceService _invoicesService;
        private readonly IUserService _userService;
        private readonly IProductService _productService;
        private readonly IProductUnitService _productUnitService;
        private readonly IStockService _stockService;
        private readonly IStockTransactionService _stockTransactionService;
        private readonly IFinancialTransactionService _financialService; // لو عندك خدمة مالية 
        private readonly IDelegateService _delegateService;
        private readonly IDelegateFeatureService _delegateFeatureService;
        private readonly IUserSession _userSession; // لو عندك جلسة مستخدم

        private bool _isLoadingUnits = false;
        private int? _currentInvoiceId = null;   // لتحديث الفاتورة بعد الحفظ الأول

        public CreateSalesInvoice(
            IStockService stockService,
            IInvoiceService invoiceService,
            IUserService userService,
            IDelegateService delegateService,
            IDelegateFeatureService delegateFeatureService,
            IProductService productService,
            IProductUnitService productUnitService,
            IUserSession userSession,
            IStockTransactionService stockTransactionService,
            IFinancialTransactionService
            financialService)
        {
            _stockService = stockService;
            _productService = productService;
            _delegateService = delegateService;
            _delegateFeatureService = delegateFeatureService;
            _productUnitService = productUnitService;
            _stockTransactionService = stockTransactionService;
            _invoicesService = invoiceService;
            _userService = userService;
            _userSession = userSession;
            _financialService = financialService;

            InitializeComponent();
            UiText.ApplyWindow(this);

            DataContext = this;

            // رقم الفاتورة
            InvoiceNumberTextBox.Text = GenerateInvoiceNumber();

            // ربط الـ Grid
            ProductsGrid.ItemsSource = InvoiceLines;

            Loaded += CreateSalesInvoice_Loaded;
            Closed += CreateSalesInvoice_Closed;
            CatalogRefreshNotifier.CatalogChanged += CatalogRefreshNotifier_CatalogChanged;
            _userSession = userSession;
        }

        private string GenerateInvoiceNumber()
        {
            string prefix = "INV";
            string datePart = DateTime.Now.ToString("yyyyMMddHHmmss");
            return $"{prefix}-{datePart}";
        }

        // ===================== LOAD DATA =====================
        private async void CreateSalesInvoice_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                UiText.ApplyTranslations(this);
                // العملاء
                var result = await _userService.GetAllAsync();
                _allCustomers = new ObservableCollection<UserReadDto>(result?.Data ?? new List<UserReadDto>());
                CustomerComboBox.ItemsSource = _allCustomers;
                CustomerComboBox.SelectedIndex = -1;

                var delegateEnabled = await _delegateFeatureService.IsEnabledAsync();
                DelegatePanel.Visibility = delegateEnabled ? Visibility.Visible : Visibility.Collapsed;
                if (delegateEnabled)
                {
                    var delegatesResult = await _delegateService.GetActiveDelegatesAsync();
                    _allDelegates = new ObservableCollection<DelegateReadDto>(delegatesResult.Data ?? new List<DelegateReadDto>());
                    DelegateComboBox.ItemsSource = _allDelegates;
                    DelegateComboBox.SelectedIndex = -1;
                }

                InvoiceDatePicker.SelectedDate = DateTime.Now;

                PaymentMethodComboBox.SelectedIndex = 0;
                UiText.ApplyTranslations(PaymentMethodComboBox);

                await LoadProductsAsync();
                UiText.ApplyTranslations(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء تحميل البيانات", "An error occurred while loading data")}: {ex.Message}", UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                var stockedProducts = await _stockService.GetAllWithFilteringAndIncludeAsync(
                            s => s.Quantity > 0,
                            new Expression<Func<Stock, object>>[]
                            {
                        s => s.Product,
                        s => s.Product.SubCategory,
                        s => s.Product.Brand,
                        s => s.Product.ProductUnits
                            });

                Products.Clear();

                foreach (var product in stockedProducts.Data
                    .Where(stock => stock.Product != null)
                    .GroupBy(stock => stock.ProductId)
                    .Where(group => group.Sum(stock => stock.Quantity) > MinimumSellableQuantity)
                    .Select(group => group.First().Product!))
                {
                    Products.Add(product);
                }

                ProductBox.ItemsSource = Products;
                ProductBox.DisplayMemberPath = "Name";
                ProductBox.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("خطأ عند تحميل المنتجات", "Error loading products")}: {ex.Message}", UiText.T("خطأ", "Error"));
            }
        }

        private async void CatalogRefreshNotifier_CatalogChanged(object? sender, EventArgs e)
        {
            if (!IsLoaded)
                return;

            await LoadProductsAsync();
        }

        private void CreateSalesInvoice_Closed(object? sender, EventArgs e)
        {
            CatalogRefreshNotifier.CatalogChanged -= CatalogRefreshNotifier_CatalogChanged;
        }
        private bool TryGetActiveCashierSession(out CashierSessionReadDto? session)
        {
            session = _userSession.CurrentCashierSession;
            if (session != null)
                return true;

            MessageBox.Show(UiText.T("لا توجد جلسة كاشير مفتوحة. الرجاء فتح جلسة أولاً.", "There is no open cashier session. Please open a session first."), UiText.T("خطأ", "Error"));
            return false;
        }
        private IEnumerable<StockMovementPostDto> BuildInvoiceStockMovements(
            IEnumerable<InvoiceLineWriteDto> lines,
            TransactionType transactionType,
            int? invoiceId,
            int? cashierId,
            int? cashierSessionId,
            string notes,
            decimal multiplier)
        {
            return lines
                .Where(line => line.ProductId > 0 && line.ProductUnitId > 0 && line.Quantity != 0)
                .Select(line =>
                {
                    var quantityPerUnit = line.QuantityPerUnitSnapshot > 0 ? line.QuantityPerUnitSnapshot : 1m;
                    var baseQuantity = line.BaseQuantity != 0 ? line.BaseQuantity : line.Quantity * quantityPerUnit;

                    return new StockMovementPostDto
                    {
                        ProductId = line.ProductId,
                        ProductUnitId = line.ProductUnitId,
                        Quantity = line.Quantity * multiplier,
                        QuantityPerUnitSnapshot = quantityPerUnit,
                        BaseQuantity = baseQuantity * multiplier,
                        UnitPrice = line.UnitPrice,
                        PurchasePrice = line.UnitCost,
                        SalePrice = line.UnitPrice,
                        ExpiryDate = line.ExpiryDate,
                        TransactionType = transactionType,
                        InvoiceId = invoiceId,
                        CasherId = cashierId,
                        CashierSessionId = cashierSessionId,
                        TransactionDate = DateTime.Now,
                        Notes = notes
                    };
                });
        }



        // ===================== CUSTOMER SEARCH =====================
        private void CustomerComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            CustomerComboBox.DisplayMemberPath = "Name";
            CustomerComboBox.SelectedValuePath = "Id";
        }

        private void CustomerComboBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            FilterCustomerList(CustomerComboBox.Text + e.Text);
        }

        private void CustomerComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back || e.Key == Key.Delete)
                FilterCustomerList(CustomerComboBox.Text);
        }

        private void FilterCustomerList(string text)
        {
            if (_allCustomers == null) return;

            var filtered = _allCustomers
                .Where(c => c.Name != null &&
                            c.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
                .ToList();

            CustomerComboBox.ItemsSource = filtered;
            CustomerComboBox.IsDropDownOpen = true;
        }


       
        private async void Product_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is ComboBox cb && cb.SelectedValue is int selectedProductId && selectedProductId > 0)
                {
                    // Get the bound row item (StockItemWriteDto)
                    if (cb.DataContext is not StockItemWriteDto item)
                        return;

                    // Cancel any previous unit load operation (prevent thread overlap)
                    // Optional: use a CancellationTokenSource if you already have one

                    item.ProductId = selectedProductId;
                    item.ProductUnitId = 0; // Reset the unit selection

                    var availableUnits = await GetAvailableUnitsForProductAsync(selectedProductId);

                    item.Units.Clear();
                    foreach (var unit in availableUnits)
                    {
                        item.Units.Add(unit);
                    }

                    var defaultUnit = ProductUnitSelector.GetDefaultSaleUnit(item.Units) ?? item.Units.FirstOrDefault();
                    if (defaultUnit != null)
                    {
                        item.ProductUnitId = defaultUnit.Id;
                        item.PurchasePrice = defaultUnit.PurchasePrice;
                        item.SalePrice = defaultUnit.SalePrice;

                        // Map the selected unit for saving
                        _itemUnits[item] = defaultUnit.Id;
                    }

                    // ✅ Set default quantity = 1 if it's 0 or less
                    if (item.Quantity <= 0)
                        item.Quantity = 1;

                    // ✅ Force UI refresh safely on the main thread
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        cb.Items.Refresh();
                    }, System.Windows.Threading.DispatcherPriority.Background);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء تحديث الوحدات", "An error occurred while updating units")}: {ex.Message}",
                                UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearProductInputs()
        {
            ProductBox.SelectedIndex = -1;
            UnitBox.ItemsSource = null;
            QtyBox.Text = "";
         
            ExpiryBox.SelectedDate = null;
        }

        private void UnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UnitBox.SelectedItem is not ProductUnitWriteDto unit)
                return;

            PurchaseBox.Text = unit.PurchasePrice.ToString();
            SaleBox.Text = unit.SalePrice.ToString();
        }

        private async void ProductBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_isLoadingUnits)
                    return;

                if (ProductBox.SelectedValue is not int productId || productId <= 0)
                    return;

                _isLoadingUnits = true;

                var availableUnits = await GetAvailableUnitsForProductAsync(productId);

                UnitBox.ItemsSource = availableUnits;

                // 🔷 Auto-select first unit if exists
                var firstUnit = availableUnits.FirstOrDefault();
                if (firstUnit != null)
                {
                    UnitBox.SelectedValue = firstUnit.Id;
                    PurchaseBox.Text = firstUnit.PurchasePrice.ToString();
                    SaleBox.Text = firstUnit.SalePrice.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("خطأ أثناء تحميل الوحدات", "Error while loading units")}: {ex.Message}", UiText.T("خطأ", "Error"));
            }
            finally
            {
                _isLoadingUnits = false;
            }
        }

        // ===================== ADD PRODUCT LINE =====================
        /*private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            if (ProductBox.SelectedItem is not ProductReadDto product)
            {
                MessageBox.Show(UiText.T("يرجى اختيار منتج.", "Please choose a product."), UiText.T("تنبيه", "Notice"));
                return;
            }

            if (UnitBox.SelectedItem is not ProductUnitWriteDto unit)
            {
                MessageBox.Show(UiText.T("يرجى اختيار وحدة.", "Please choose a unit."), UiText.T("تنبيه", "Notice"));
                return;
            }

            if (!TryParseDecimalInput(QtyBox.Text, out decimal qty) || qty <= 0)
            {
                MessageBox.Show(UiText.T("الكمية غير صحيحة.", "The quantity is invalid."), UiText.T("تنبيه", "Notice"));
                return;
            }

            // 🔥 إضافة منتج مع كل البيانات (نفس StockOut)
            var line = new InvoiceLineWriteDto
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductUnitId = unit.Id,
                UnitName = unit.Unit?.Name,
                Quantity = qty,
                UnitPrice = unit.SalePrice,
                ExpiryDate = ExpiryBox.SelectedDate ?? DateTime.Now.AddMonths(6),
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            InvoiceLines.Add(line);
            UpdateTotal();
            ClearProductInputs();
        }*/
        private async void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            if (ProductBox.SelectedItem is not ProductReadDto product)
            {
                MessageBox.Show(UiText.T("يرجى اختيار منتج.", "Please choose a product."), UiText.T("تنبيه", "Notice"));
                return;
            }

            if (UnitBox.SelectedItem is not ProductUnitWriteDto unit)
            {
                MessageBox.Show(UiText.T("يرجى اختيار وحدة.", "Please choose a unit."), UiText.T("تنبيه", "Notice"));
                return;
            }

            if (!TryParseDecimalInput(QtyBox.Text, out decimal qty) || qty <= 0)
            {
                MessageBox.Show(UiText.T("الكمية غير صحيحة.", "The quantity is invalid."), UiText.T("تنبيه", "Notice"));
                return;
            }

            // ✅ ADD: Snapshot tax info from product at time of invoice
            bool taxExempt = product.TaxExempt ?? false;
            decimal taxRate = taxExempt ? 0m : (product.TaxRate ?? 0m);
            decimal unitPrice = unit.SalePrice;
            decimal lineTotal = qty * unitPrice;
            decimal divisor = 1m + (taxRate / 100m);
            decimal lineSubTotal = taxExempt || divisor <= 0m
                ? lineTotal
                : Math.Round(lineTotal / divisor, 3);
            decimal taxAmount = Math.Round(lineTotal - lineSubTotal, 3);

            var line = new InvoiceLineWriteDto
            {
                ProductId = product.Id,
                ProductName = product.Name,

                ProductUnitId = unit.Id,
                QuantityPerUnitSnapshot = unit.QuantityPerUnit > 0 ? unit.QuantityPerUnit : 1m,
                BaseQuantity = qty * (unit.QuantityPerUnit > 0 ? unit.QuantityPerUnit : 1m),
                UnitName = unit.Unit?.Name,

                Quantity = qty,

                // ✅ sale price stored on product already includes tax
                UnitPrice = unitPrice,

                // ✅ ADD: store purchase cost used (snapshot)
                UnitCost = unit.PurchasePrice,

                // ✅ ADD: tax snapshot fields
                TaxExempt = taxExempt,
                TaxRate = taxRate,
                TaxAmount = taxAmount,

                // ✅ ADD: store subtotal before tax
                LineSubTotal = lineSubTotal,

                ExpiryDate = ExpiryBox.SelectedDate ?? DateTime.Now.AddMonths(6),
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            var expandDraftResult = await ExpandInvoiceLinesByFefoAsync(new[] { line });
            if (!expandDraftResult.Success || expandDraftResult.Data == null)
            {
                MessageBox.Show(
                    expandDraftResult.Message ?? UiText.T("تعذر تخصيص المخزون للصنف المحدد.", "Could not allocate stock for the selected item."),
                    UiText.T("تنبيه", "Notice"));
                return;
            }

            foreach (var expandedLine in expandDraftResult.Data)
            {
                InvoiceLines.Add(expandedLine);
            }

            UpdateTotals();   // ✅ ADD: new totals method
            ClearProductInputs();
        }


        private void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is InvoiceLineWriteDto line)
            {
                InvoiceLines.Remove(line);
                UpdateTotals();
            }
        }

        /* private void UpdateTotal()
         {
             TotalAmountTextBox.Text =
                 InvoiceLines.Sum(x => x.LineTotal).ToString("0.###");
         }
            */
        // ✅ ADD: Calculates invoice summary fields needed for reports
        private void UpdateTotals()
        {
            decimal subTotal = InvoiceLines.Sum(l => l.LineSubTotal);   // قبل الضريبة
            decimal taxTotal = InvoiceLines.Sum(l => l.TaxAmount);      // الضريبة
            decimal grossSales = InvoiceLines.Sum(l => l.Quantity * l.UnitPrice);
            decimal discount = 0m;

            // ✅ ADD: (optional) if you later add Discount UI textbox
            // decimal.TryParse(DiscountTextBox.Text, out discount);

            decimal netTotal = grossSales - discount;

            // ✅ Existing UI field shows final
            TotalAmountTextBox.Text = netTotal.ToString("0.###");

            // ✅ Optional: if you want show subtotal/tax in UI, bind to labels/textboxes
             SubTotalTextBox.Text = subTotal.ToString("0.###");
             TaxTotalTextBox.Text = taxTotal.ToString("0.###");
        }

        private static void RecalculateLineAmounts(InvoiceLineWriteDto line)
        {
            var lineTotal = line.Quantity * line.UnitPrice;
            var divisor = 1m + (line.TaxRate / 100m);

            line.LineSubTotal = line.TaxExempt || divisor <= 0m
                ? lineTotal
                : Math.Round(lineTotal / divisor, 3);
            line.TaxAmount = Math.Round(lineTotal - line.LineSubTotal, 3);
            line.ProfitBeforeTax = line.LineSubTotal - (line.Quantity * line.UnitCost);
            line.Profit = line.ProfitBeforeTax;
        }

        private async void SaveReceiptBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!InvoiceLines.Any())
                {
                    MessageBox.Show(UiText.T("يرجى إضافة منتج واحد على الأقل.", "Please add at least one product."), UiText.T("تنبيه", "Notice"));
                    return;
                }

                if (CustomerComboBox.SelectedItem == null)
                {
                    MessageBox.Show(UiText.T("يرجى اختيار الزبون.", "Please choose the customer."), UiText.T("تنبيه", "Notice"));
                    return;
                }

                if (!await ValidateInvoiceStockAvailabilityAsync())
                    return;

                var expandInvoiceResult = await ExpandInvoiceLinesByFefoAsync(InvoiceLines);
                if (!expandInvoiceResult.Success || expandInvoiceResult.Data == null)
                {
                    MessageBox.Show(
                        expandInvoiceResult.Message ?? UiText.T("تعذر تخصيص المخزون لبعض الأصناف.", "Could not allocate stock for some items."),
                        UiText.T("تنبيه", "Notice"));
                    return;
                }

                var expandedInvoiceLines = expandInvoiceResult.Data;
                var customer = CustomerComboBox.SelectedItem as UserReadDto;
                //decimal totalAmount = InvoiceLines.Sum(l => l.LineTotal);
                // ✅ ADD: invoice totals required for reporting
                var selectedPaymentType = GetSelectedPaymentType();
                decimal subTotal = expandedInvoiceLines.Sum(l => l.LineSubTotal);
                decimal totalTax = expandedInvoiceLines.Sum(l => l.TaxAmount);
                decimal grossSales = expandedInvoiceLines.Sum(l => l.Quantity * l.UnitPrice);
                decimal discount = 0m; // ✅ ADD: later from UI if needed

                decimal totalAmount = grossSales - discount;
                bool isUpdate = _currentInvoiceId != null;
                if (!TryGetActiveCashierSession(out var session))
                    return;

                var invoiceDto = new InvoiceWriteDto
                {
                    Id = _currentInvoiceId ?? 0,
                    InvoiceNumber = InvoiceNumberTextBox.Text,
                    CustomerId = customer?.Id,
                    DelegateId = DelegateComboBox.SelectedValue is int delegateId ? delegateId : null,
                    InvoiceType = InvoiceType.Sale,
                    TotalAmount = totalAmount,
                    CreatedDate = InvoiceDatePicker.SelectedDate.Value,
                    UpdatedDate = DateTime.Now,
                    InvoiceLines = expandedInvoiceLines,
                    CasherId = session.CashierId,
                    PaymentType = selectedPaymentType,
                    SubTotal = subTotal,        // ✅ ADD
                    TotalTax = totalTax,        // ✅ ADD
                    DiscountAmount = discount,  // ✅ ADD (or keep your existing)
                    Status = InvoiceStatus.Completed
                };


                int savedInvoiceId;

                if (!isUpdate)
                {
                    // ============ CREATE ============
                    var result = await _invoicesService.CreateAsync(invoiceDto);

                    if (!result.Success)
                    {
                        MessageBox.Show(result.Message ?? UiText.T("فشل إنشاء الفاتورة.", "Failed to create the invoice."), UiText.T("خطأ", "Error"));
                        return;
                    }

                    _currentInvoiceId = result.Data.Id;
                    savedInvoiceId = result.Data.Id;

                    // 🔥 طرح الكميات من المخزون
                    var movementResult = await _stockService.PostMovementsAsync(
                        BuildInvoiceStockMovements(
                            expandedInvoiceLines,
                            TransactionType.Sale,
                            savedInvoiceId,
                            session.CashierId,
                            session.Id,
                            $"Sale invoice #{invoiceDto.InvoiceNumber}",
                            -1m));
                    if (!movementResult.Success)
                    {
                        MessageBox.Show(movementResult.Message ?? UiText.T("فشل تحديث المخزون.", "Failed to update stock."), UiText.T("خطأ", "Error"));
                        return;
                    }

                    // ✅ POST financial (Sale Invoice)
                    if (selectedPaymentType != PaymentType.Credit)
                    {
                        var postDto = new FinancialPostDto
                        {
                            Direction = TransactionDirection.In,
                            Method = MapPaymentMethod(selectedPaymentType),
                            Amount = totalAmount,
                            TransactionDate = DateTime.Now,

                            SourceType = FinancialSourceType.SaleInvoice,
                            SourceId = savedInvoiceId,

                            CashierSessionId = session.Id,
                            CashierId = session.CashierId,

                            Notes = $"Sale Invoice #{invoiceDto.InvoiceNumber}"
                        };

                        var postResult = await _financialService.PostAsync(postDto);
                        if (!postResult.Success)
                        {
                            MessageBox.Show(postResult.Message ?? UiText.T("تم حفظ الفاتورة لكن فشل تسجيل الحركة المالية.", "The invoice was saved, but posting the financial transaction failed."), UiText.T("تحذير", "Warning"));
                            return;
                        }
                    }

                MessageBox.Show(
                    selectedPaymentType == PaymentType.Credit
                        ? UiText.T("تم إنشاء الفاتورة الآجلة بنجاح.", "The credit invoice was created successfully.")
                        : UiText.T("تم إنشاء الفاتورة وتسجيل الحركة المالية.", "The invoice and financial transaction were saved successfully."),
                    UiText.T("نجاح", "Success"));
                UiText.ApplyTranslations(this);
            }
                else
                {
                    savedInvoiceId = _currentInvoiceId.Value;

                    // ============ UPDATE ============

                    // 0) Void old financial transactions for this invoice
                    var voidResult = await _financialService.VoidBySourceAsync(
                        FinancialSourceType.SaleInvoice,
                        savedInvoiceId,
                        "Invoice updated"
                    );

                    if (!voidResult.Success)
                    {
                        MessageBox.Show(voidResult.Message ?? UiText.T("فشل إلغاء الحركة المالية السابقة.", "Failed to void the previous financial transaction."), UiText.T("خطأ", "Error"));
                        return;
                    }

                    // 1️⃣ إعادة كميات الفاتورة القديمة إلى المخزون
                    var reverseResult = await _stockService.PostMovementsAsync(
                        BuildInvoiceStockMovements(
                            _originalLines.Select(line => new InvoiceLineWriteDto
                            {
                                ProductId = line.ProductId,
                                ProductUnitId = line.ProductUnitId,
                                Quantity = line.Quantity,
                                QuantityPerUnitSnapshot = line.QuantityPerUnitSnapshot,
                                BaseQuantity = line.BaseQuantity,
                                UnitPrice = line.UnitPrice,
                                UnitCost = line.UnitCost,
                                ExpiryDate = line.ExpiryDate
                            }),
                            TransactionType.Sale,
                            savedInvoiceId,
                            session.CashierId,
                            session.Id,
                            $"Reverse sale invoice #{invoiceDto.InvoiceNumber}",
                            1m));
                    if (!reverseResult.Success)
                    {
                        MessageBox.Show(reverseResult.Message ?? UiText.T("فشل عكس حركة المخزون.", "Failed to reverse the stock movement."), UiText.T("خطأ", "Error"));
                        return;
                    }

                    // 2️⃣ طرح كميات الفاتورة الجديدة
                    var applyResult = await _stockService.PostMovementsAsync(
                        BuildInvoiceStockMovements(
                            expandedInvoiceLines,
                            TransactionType.Sale,
                            savedInvoiceId,
                            session.CashierId,
                            session.Id,
                            $"Update sale invoice #{invoiceDto.InvoiceNumber}",
                            -1m));
                    if (!applyResult.Success)
                    {
                        MessageBox.Show(applyResult.Message ?? UiText.T("فشل تحديث حركة المخزون.", "Failed to update the stock movement."), UiText.T("خطأ", "Error"));
                        return;
                    }

                    // 3️⃣ Update invoice
                    var result = await _invoicesService.UpdateAsync(invoiceDto);

                    if (!result.Success)
                    {
                        MessageBox.Show(result.Message ?? UiText.T("فشل تحديث الفاتورة", "Failed to update the invoice."), UiText.T("خطأ", "Error"));
                        return;
                    }

                    // 4️⃣ Post new financial transaction with new amount
                    if (selectedPaymentType != PaymentType.Credit)
                    {
                        var postDto = new FinancialPostDto
                        {
                            Direction = TransactionDirection.In,
                            Method = MapPaymentMethod(selectedPaymentType),
                            Amount = totalAmount,
                            TransactionDate = DateTime.Now,

                            SourceType = FinancialSourceType.SaleInvoice,
                            SourceId = savedInvoiceId,

                            CashierSessionId = session.Id,
                            CashierId = session.CashierId,

                            Notes = $"Sale Invoice UPDATED #{invoiceDto.InvoiceNumber}"
                        };

                        var postResult = await _financialService.PostAsync(postDto);
                        if (!postResult.Success)
                        {
                            MessageBox.Show(postResult.Message ?? UiText.T("تم تحديث الفاتورة لكن فشل تسجيل الحركة المالية الجديدة", "The invoice was updated, but posting the new financial transaction failed."), UiText.T("تحذير", "Warning"));
                            return;
                        }
                    }

                MessageBox.Show(
                    selectedPaymentType == PaymentType.Credit
                        ? UiText.T("تم تحديث الفاتورة الآجلة بنجاح ✅", "The credit invoice was updated successfully.")
                        : UiText.T("تم تحديث الفاتورة وتسجيل الحركة المالية ✅", "The invoice was updated and the financial transaction was posted successfully."),
                    UiText.T("نجاح", "Success"));
                UiText.ApplyTranslations(this);
            }
                PrintBtn.Visibility = Visibility.Visible;
                NewInvoiceBtn.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                var details = ex.Message;
                var inner = ex.InnerException;
                while (inner != null)
                {
                    details += Environment.NewLine + inner.Message;
                    inner = inner.InnerException;
                }

                MessageBox.Show($"{UiText.T("خطأ أثناء حفظ الفاتورة", "An error occurred while saving the invoice")}:\n{details}", UiText.T("خطأ", "Error"));
            }
        }
        private PaymentMethod MapPaymentMethod(PaymentType paymentType)
        {
            return paymentType switch
            {
                PaymentType.Cash => PaymentMethod.Cash,
                PaymentType.Visa => PaymentMethod.Visa,
                PaymentType.Master => PaymentMethod.Master,
                PaymentType.Debit => PaymentMethod.BankTransfer,
                PaymentType.Check => PaymentMethod.Check,
                PaymentType.MobilePayment => PaymentMethod.MobilePayment,
                PaymentType.Credit => PaymentMethod.Credit,
                _ => PaymentMethod.Cash
            };
        }

        private PaymentType GetSelectedPaymentType()
        {
            if (PaymentMethodComboBox.SelectedItem is ComboBoxItem item &&
                int.TryParse(item.Tag?.ToString(), out var value))
            {
                return (PaymentType)value;
            }

            return PaymentType.Cash;
        }


        /*// ===================== SAVE / UPDATE / PRINT =====================
        private async void SaveReceiptBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!InvoiceLines.Any())
                {
                    MessageBox.Show("يرجى إضافة منتج واحد على الأقل.", "تنبيه");
                    return;
                }

                if (CustomerComboBox.SelectedItem == null)
                {
                    MessageBox.Show("❌ يرجى اختيار الزبون.", "تنبيه");
                    return;
                }

                var customer = CustomerComboBox.SelectedItem as UserReadDto;
                decimal totalAmount = InvoiceLines.Sum(l => l.LineTotal);

                bool isUpdate = _currentInvoiceId != null;

                var invoiceDto = new InvoiceWriteDto
                {
                    Id = _currentInvoiceId ?? 0,
                    InvoiceNumber = InvoiceNumberTextBox.Text,
                    CustomerId = customer?.Id,
                    InvoiceType = InvoiceType.Sale,
                    TotalAmount = totalAmount,
                    CreatedDate = InvoiceDatePicker.SelectedDate.Value,
                    UpdatedDate = DateTime.Now,
                    InvoiceLines = InvoiceLines.ToList()
                };

                if (!isUpdate)
                {
                    // ============ CREATE ============
                    var result = await _invoicesService.CreateAsync(invoiceDto);

                    if (result.Success)
                    {
                        _currentInvoiceId = result.Data.Id;

                        // 🔥 طرح الكميات من المخزون
                        foreach (var line in InvoiceLines)
                            await UpdateStockQuantity(line.ProductId, line.ProductUnitId, line.Quantity);

                        MessageBox.Show("تم إنشاء الفاتورة بنجاح!");
                    }
                }
                else
                {
                    // ============ UPDATE ============

                    // 1️⃣ إعادة كميات الفاتورة القديمة إلى المخزون
                    foreach (var old in _originalLines)
                        await UpdateStockQuantity(old.ProductId, old.ProductUnitId, -old.Quantity);

                    // 2️⃣ طرح كميات الفاتورة الجديدة
                    foreach (var line in InvoiceLines)
                        await UpdateStockQuantity(line.ProductId, line.ProductUnitId, line.Quantity);

                    var result = await _invoicesService.UpdateAsync(invoiceDto);

                    if (result.Success)
                        MessageBox.Show("تم تحديث الفاتورة بنجاح!");
                }

                PrintBtn.Visibility = Visibility.Visible;
                NewInvoiceBtn.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء حفظ الفاتورة:\n{ex.Message}");
            }
        }


     
        }*/
        private async void UpdateInvoiceBtn_Click(object sender, RoutedEventArgs e)
        {

            SaveReceiptBtn_Click(sender, e);

            /* if (_currentInvoiceId == null)
            {
                MessageBox.Show("لا توجد فاتورة محفوظة لتحديثها.", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (!InvoiceLines.Any())
                {
                    MessageBox.Show("يرجى إضافة منتج واحد على الأقل.", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var customer = CustomerComboBox.SelectedItem as UserReadDto;

                decimal totalAmount = InvoiceLines.Sum(l => l.LineTotal);

                var invoiceDto = new InvoiceWriteDto
                {
                    Id = _currentInvoiceId.Value,
                    InvoiceType = InvoiceType.Sale,
                    CustomerId = customer?.Id,
                    CasherId = null,
                    VoucherId = null,
                    TotalAmount = totalAmount,
                    CreatedDate = InvoiceDatePicker.SelectedDate ?? DateTime.Now,
                    InvoiceLines = InvoiceLines.ToList(),
                    UpdatedDate = DateTime.Now
                };

                var result = await _invoicesService.UpdateAsync(invoiceDto);

                if (result.Success)
                {
                    MessageBox.Show("✅ تم تحديث الفاتورة بنجاح!", "نجاح",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"❌ فشل تحديث الفاتورة: {result.Message}", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ حدث خطأ أثناء تحديث الفاتورة:\n{ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }*/
        }
        private async void PrintBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentInvoiceId == null)
            {
                MessageBox.Show(UiText.T("لا توجد فاتورة للطباعة.", "There is no invoice to print."));
                return;
            }

            var invoice = await _invoicesService.GetFullInvoiceByIdAsync(_currentInvoiceId.Value);

            if (invoice == null)
            {
                MessageBox.Show(UiText.T("الفاتورة غير موجودة.", "The invoice was not found."), UiText.T("خطأ", "Error"));
                return;
            }

            SaveSalesInvoicePdf(invoice);
        }



        private void SaveSalesInvoicePdf(InvoiceReadDto invoice)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF File (*.pdf)|*.pdf",
                FileName = $"Invoice_{invoice.InvoiceNumber}.pdf"
            };

            if (dialog.ShowDialog() == true)
            {
                var path = dialog.FileName;

                PdfGenerator.SalesInvoice(invoice, path);

                MessageBox.Show(UiText.T("تم حفظ ملف PDF بنجاح.", "The PDF file was saved successfully."), UiText.T("تم الحفظ", "Saved"), MessageBoxButton.OK, MessageBoxImage.Information);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
        }


        private void SearchInvoiceBtn_Click(object sender, RoutedEventArgs e)
        {
            var searchWindow = new SearchSalesInvoiceWindow(_invoicesService,true)
            {
                Owner = this
            };

            if (searchWindow.ShowDialog() == true)
            {
                LoadSelectedInvoice(searchWindow.Result);
            }
        }
        private void LoadSelectedInvoice(InvoiceReadDto invoice)
        {
            if (invoice == null) return;

            _currentInvoiceId = invoice.Id;

            _originalLines = invoice.InvoiceLines.ToList();   // 🔥 مهم جداً

            InvoiceNumberTextBox.Text = invoice.InvoiceNumber;
            InvoiceDatePicker.SelectedDate = invoice.CreatedDate;

            CustomerComboBox.SelectedItem =
                _allCustomers.FirstOrDefault(c => c.Id == invoice.CustomerId);
            if (DelegatePanel.Visibility == Visibility.Visible)
                DelegateComboBox.SelectedItem = _allDelegates.FirstOrDefault(d => d.Id == invoice.DelegateId);
            UiText.ApplyTranslations(PaymentMethodComboBox);

            InvoiceLines.Clear();

            foreach (var line in invoice.InvoiceLines)
            {
                InvoiceLines.Add(new InvoiceLineWriteDto
                {
                    Id = line.Id,
                    ProductId = line.ProductId,
                    ProductName = line.Product?.Name,
                    ProductUnitId = line.ProductUnitId,
                    QuantityPerUnitSnapshot = line.QuantityPerUnitSnapshot,
                    BaseQuantity = line.BaseQuantity,
                    UnitName = line.ProductUnit?.Unit?.Name,
                    Quantity = line.Quantity,
                    ExpiryDate = line.ExpiryDate,
                    CreatedDate = line.CreatedDate,
                    UpdatedDate = line.UpdatedDate,
                    UnitPrice = line.UnitPrice,
                    UnitCost = line.UnitCost,          // ✅ ADD
                    TaxExempt = line.TaxExempt,        // ✅ ADD
                    TaxRate = line.TaxRate,            // ✅ ADD
                    TaxAmount = line.TaxAmount,        // ✅ ADD
                    LineSubTotal = line.LineSubTotal,  // ✅ ADD

                });
            }

            UpdateTotals();
            UiText.ApplyTranslations(ProductsGrid);

            PrintBtn.Visibility = Visibility.Visible;
        }

        private void NewInvoiceBtn_Click(object sender, RoutedEventArgs e)
        {
            _currentInvoiceId = null;
            _originalLines.Clear();

            InvoiceLines.Clear();
            ProductsGrid.Items.Refresh();

            InvoiceNumberTextBox.Text = GenerateInvoiceNumber();
            CustomerComboBox.SelectedIndex = -1;
            DelegateComboBox.SelectedIndex = -1;
            InvoiceDatePicker.SelectedDate = DateTime.Now;

            TotalAmountTextBox.Text = "0";
            PrintBtn.Visibility = Visibility.Collapsed;
            NewInvoiceBtn.Visibility = Visibility.Collapsed;
        }



        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ClearProductBtn_Click(object sender, RoutedEventArgs e)
        {
            ProductBox.Text = "";
            ProductBox.SelectedIndex = -1;
            ProductBox.ItemsSource = Products;

            UnitBox.ItemsSource = null;

            QtyBox.Text = "";
          
            ExpiryBox.SelectedDate = null;

            ProductBox.IsDropDownOpen = false;
        }


        private void SearchProductBtn_Click(object sender, RoutedEventArgs e)
        {
            string search = ProductBox.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(search))
            {
                ProductBox.ItemsSource = Products;
                ProductBox.IsDropDownOpen = true;
                return;
            }

            var filtered = Products.Where(p =>
                (!string.IsNullOrEmpty(p.Name) &&
                 p.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                ||
                p.ITEMCODE.ToString().Contains(search)
            ).ToList();

            ProductBox.ItemsSource = filtered;
            ProductBox.IsDropDownOpen = true;

            if (filtered.Count == 1)
                ProductBox.SelectedItem = filtered.First();
        }

        private async Task<List<StockReadDto>> GetAvailableStocksForProductAsync(int productId)
        {
            var result = await _stockService.GetAllWithFilteringAndIncludeAsync(
                s => s.ProductId == productId && s.Quantity > 0,
                new Expression<Func<Stock, object>>[]
                {
                    s => s.Product,
                    s => s.Product.ProductUnits,
                    s => s.ProductUnit,
                    s => s.ProductUnit.Unit
                });

            return result?.Data?
                .Where(stock => stock.Product != null && stock.ProductUnit != null && stock.Quantity > 0)
                .OrderBy(stock => stock.ProductUnit?.Unit?.Name)
                .ToList()
                ?? new List<StockReadDto>();
        }

        private static ProductUnitWriteDto MapAvailableUnit(StockReadDto stock)
        {
            return new ProductUnitWriteDto
            {
                Id = stock.ProductUnitId,
                ProductId = stock.ProductId,
                UnitId = stock.ProductUnit?.UnitId ?? 0,
                Unit = stock.ProductUnit?.Unit == null
                    ? null
                    : new UnitWriteDto
                    {
                        Id = stock.ProductUnit.Unit.Id,
                        Name = stock.ProductUnit.Unit.Name ?? string.Empty,
                        CreatedDate = stock.ProductUnit.Unit.CreatedDate,
                        UpdatedDate = stock.ProductUnit.Unit.UpdatedDate
                    },
                QuantityPerUnit = stock.ProductUnit?.QuantityPerUnit ?? 1m,
                PurchasePrice = stock.PurchasePrice,
                SalePrice = stock.SalePrice,
                IsDefaultSaleUnit = stock.ProductUnit?.IsDefaultSaleUnit ?? false,
                IsDefaultPurchaseUnit = stock.ProductUnit?.IsDefaultPurchaseUnit ?? false
            };
        }

        private async Task<List<ProductUnitWriteDto>> GetAvailableUnitsForProductAsync(int productId)
        {
            var stocks = await GetAvailableStocksForProductAsync(productId);
            if (stocks.Sum(stock => stock.Quantity) <= MinimumSellableQuantity)
                return new List<ProductUnitWriteDto>();

            return stocks
                .GroupBy(stock => stock.ProductUnitId)
                .Select(group => MapAvailableUnit(group.First()))
                .OrderByDescending(unit => unit.IsDefaultSaleUnit)
                .ThenBy(unit => unit.Unit?.Name)
                .ToList();
        }

        private async Task<bool> ValidateInvoiceStockAvailabilityAsync()
        {
            foreach (var line in InvoiceLines.Where(l => l.Quantity > 0).ToList())
            {
                var stockResult = await _stockService.GetAllWriteDtoWithFilteringAndIncludeAsync(
                    s => s.ProductId == line.ProductId && s.ProductUnitId == line.ProductUnitId);

                if (stockResult?.Data == null || stockResult.Data.Count == 0)
                {
                    MessageBox.Show(
                        UiText.T(
                            $"الصنف {line.ProductName} غير موجود في المخزون. لن يتم حفظ الفاتورة.",
                            $"The item {line.ProductName} was not found in stock. The invoice will not be saved."),
                        UiText.T("تنبيه", "Notice"));
                    return false;
                }

                var stock = stockResult.Data.First();
                if (stock.Quantity >= line.Quantity)
                    continue;

                var availableQuantity = Math.Max(stock.Quantity, 0m);
                if (availableQuantity > 0)
                {
                    line.Quantity = availableQuantity;
                    line.BaseQuantity = availableQuantity * (line.QuantityPerUnitSnapshot > 0 ? line.QuantityPerUnitSnapshot : 1m);
                    RecalculateLineAmounts(line);
                    MessageBox.Show(
                        UiText.T(
                            $"الكمية المطلوبة للصنف {line.ProductName} غير متوفرة. تم تعديل الكمية إلى الحد الأقصى المتاح: {availableQuantity:0.###}",
                            $"The requested quantity for {line.ProductName} is not available. The quantity was adjusted to the maximum available: {availableQuantity:0.###}"),
                        UiText.T("تنبيه", "Notice"));
                }
                else
                {
                    InvoiceLines.Remove(line);
                    MessageBox.Show(
                        UiText.T(
                            $"الصنف {line.ProductName} غير متوفر حالياً في المخزون، وتمت إزالته من الفاتورة.",
                            $"The item {line.ProductName} is currently unavailable in stock and was removed from the invoice."),
                        UiText.T("تنبيه", "Notice"));
                }

                UpdateTotals();
                ProductsGrid.Items.Refresh();
                return false;
            }

            return true;
        }

        private async Task<Result<List<InvoiceLineWriteDto>>> ExpandInvoiceLinesByFefoAsync(IEnumerable<InvoiceLineWriteDto> sourceLines)
        {
            var expandedLines = new List<InvoiceLineWriteDto>();

            foreach (var sourceLine in sourceLines.Where(line => line.Quantity > 0))
            {
                var allocationResult = await _stockService.AllocateOutgoingAsync(new[]
                {
                    new StockAllocationRequestDto
                    {
                        ProductId = sourceLine.ProductId,
                        ProductUnitId = sourceLine.ProductUnitId,
                        Quantity = sourceLine.Quantity
                    }
                });

                if (!allocationResult.Success || allocationResult.Data == null || allocationResult.Data.Count == 0)
                {
                    var fallbackMessage = UiText.T(
                        $"تعذر تخصيص المخزون للصنف {sourceLine.ProductName}.",
                        $"Could not allocate stock for item {sourceLine.ProductName}.");
                    return Result<List<InvoiceLineWriteDto>>.Fail(allocationResult.Message ?? fallbackMessage);
                }

                foreach (var allocation in allocationResult.Data)
                {
                    var splitLine = new InvoiceLineWriteDto
                    {
                        Id = sourceLine.Id,
                        InvoiceId = sourceLine.InvoiceId,
                        OriginalInvoiceId = sourceLine.OriginalInvoiceId,
                        ProductId = sourceLine.ProductId,
                        ProductName = sourceLine.ProductName,
                        ProductUnitId = sourceLine.ProductUnitId,
                        UnitName = sourceLine.UnitName,
                        Quantity = allocation.Quantity,
                        QuantityPerUnitSnapshot = allocation.QuantityPerUnitSnapshot,
                        BaseQuantity = allocation.BaseQuantity,
                        UnitPrice = allocation.SalePrice,
                        UnitCost = allocation.PurchasePrice,
                        TaxExempt = sourceLine.TaxExempt,
                        TaxRate = sourceLine.TaxRate,
                        ExpiryDate = allocation.ExpiryDate ?? sourceLine.ExpiryDate,
                        CreatedDate = sourceLine.CreatedDate,
                        UpdatedDate = DateTime.Now
                    };

                    RecalculateLineAmounts(splitLine);
                    expandedLines.Add(splitLine);
                }
            }

            return Result<List<InvoiceLineWriteDto>>.Ok(expandedLines);
        }

        private static bool TryParseDecimalInput(string? text, out decimal value)
        {
            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
                || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }



    }
}

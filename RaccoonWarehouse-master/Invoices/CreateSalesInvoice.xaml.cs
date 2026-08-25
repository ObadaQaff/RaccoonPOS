using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Application.Service.Delegates;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.ProductUnits;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.StockTransactions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Common;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Domain.Cashiers.DTOs;
using RaccoonWarehouse.Domain.Checks.DTOs;
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
using RaccoonWarehouse;
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
using System.Windows.Data;
using System.Windows.Input;
using RaccoonWarehouse.Domain.Stock.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Products;
using RaccoonWarehouse.Domain.Users;

namespace RaccoonWarehouse.Invoices
{
    public partial class CreateSalesInvoice : Window
    {
        private const decimal MinimumSellableQuantity = 10m;
        private const int ProductSearchDelayMs = 300;
        private const int ProductSelectionDelayMs = 150;

        // ====== مجموعات للـ Binding ======
        public ObservableCollection<ProductReadDto> Products { get; set; } = new();
        private Dictionary<StockItemWriteDto, int> _itemUnits = new();

        public ObservableCollection<InvoiceLineWriteDto> InvoiceLines { get; set; } = new();

        private ObservableCollection<UserReadDto> _allCustomers;
        private bool _isCompletingCustomerName;
        private ObservableCollection<DelegateReadDto> _allDelegates = new();
        private List<InvoiceLineReadDto> _originalLines = new(); // to restore stock on update
        private List<CheckWriteDto> _currentChecks = new();


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
        private readonly ILoadingService _loadingService;
        private readonly UserStatementService _userStatementService;

        private bool _isLoadingUnits = false;
        private bool _isApplyingProductSelection = false;
        private bool _isUpdatingInvoiceLine = false;
        private bool _isSaving = false;
        private int _productSearchVersion;
        private int _productSelectionVersion;
        private string _productSearchText = string.Empty;
        private bool _isRestoringProductSearchText;
        private bool _isNavigatingProductResults;
        private readonly System.Threading.SemaphoreSlim _productSelectionSemaphore = new(1, 1);
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
            financialService,
            UserStatementService userStatementService,
            ILoadingService loadingService)
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
            _userStatementService = userStatementService;
            _loadingService = loadingService;

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
            return (DateTime.Now.Ticks % 90000 + 10000).ToString();
        }
        #region
        // ===================== LOAD DATA =====================
        private async void CreateSalesInvoice_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _loadingService.Show();
                UiText.ApplyTranslations(this);
                await LoadCustomersAsync();

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
            finally
            {
                _loadingService.Hide();
            }
        }

        private async Task LoadCustomersAsync(int? selectedCustomerId = null)
        {
            var result = await _userService.GetAllAsync();
            _allCustomers = new ObservableCollection<UserReadDto>(result?.Data ?? new List<UserReadDto>());
            CustomerComboBox.ItemsSource = _allCustomers;
            CustomerComboBox.SelectedIndex = -1;

            if (selectedCustomerId.HasValue)
            {
                CustomerComboBox.SelectedItem = _allCustomers.FirstOrDefault(customer => customer.Id == selectedCustomerId.Value);
            }
        }
        #endregion
        #region Checks Handling 
        private static List<CheckWriteDto> CloneChecks(IEnumerable<CheckWriteDto>? checks)
        {
            return checks?.Select(check => new CheckWriteDto
            {
                Id = check.Id,
                CheckNumber = check.CheckNumber,
                BankName = check.BankName,
                DueDate = check.DueDate,
                Amount = check.Amount,
                Status = check.Status,
                Notes = check.Notes,
                VoucherId = check.VoucherId,
                InvoiceId = check.InvoiceId,
                CreatedDate = check.CreatedDate,
                UpdatedDate = check.UpdatedDate
            }).ToList() ?? new List<CheckWriteDto>();
        }

        private static List<CheckWriteDto> CloneChecks(IEnumerable<CheckReadDto>? checks)
        {
            return checks?.Select(check => new CheckWriteDto
            {
                Id = check.Id,
                CheckNumber = check.CheckNumber,
                BankName = check.BankName,
                DueDate = check.DueDate,
                Amount = check.Amount,
                Status = check.Status,
                Notes = check.Notes,
                VoucherId = check.VoucherId,
                InvoiceId = check.InvoiceId,
                CreatedDate = check.CreatedDate,
                UpdatedDate = check.UpdatedDate
            }).ToList() ?? new List<CheckWriteDto>();
        }




        private bool EditCurrentInvoiceChecks(decimal invoiceAmount)
        {
            var dialog = new CheckDetailsWindow(invoiceAmount, _currentChecks);
            if (dialog.ShowDialog() != true)
                return false;

            _currentChecks = CloneChecks(dialog.ResultChecks);
            UpdateChecksButtonVisibility();
            return true;
        }

        private void ChecksBtn_Click(object sender, RoutedEventArgs e)
        {
            var invoiceAmount = InvoiceLines.Sum(line => line.LineTotal);
            if (!EditCurrentInvoiceChecks(invoiceAmount) && !_currentChecks.Any())
            {
                MessageBox.Show(
                    UiText.T("لا توجد شيكات لعرضها أو تعديلها.", "There are no checks to view or edit."),
                    UiText.T("تنبيه", "Notice"));
            }
        }
        private void UpdateChecksButtonVisibility()
        {
            ChecksBtn.Visibility = GetSelectedPaymentType() == PaymentType.Check || _currentChecks.Any()
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        #endregion



        private void PaymentMethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GetSelectedPaymentType() != PaymentType.Check && _currentChecks.Any())
            {
                _currentChecks.Clear();
            }

            UpdateChecksButtonVisibility();
        }

        private async Task<decimal> GetCustomerCurrentBalanceAsync(int customerId)
        {
            return await _userStatementService.GetCurrentBalanceAsync(customerId);
        }

        private async Task<bool> EnsureCustomerCreditAllowedAsync(decimal invoiceAmount, decimal previousCreditAmount = 0m)
        {
            if (CustomerComboBox.SelectedItem is not UserReadDto customer)
            {
                MessageBox.Show(UiText.T("يرجى اختيار الزبون.", "Please choose the customer."), UiText.T("تنبيه", "Notice"));
                return false;
            }

            if (customer.Role != UserRole.Customer)
                return true;

            if (customer.CreditStatus is CreditStatus.Blocked or CreditStatus.Suspended)
            {
                MessageBox.Show(
                    UiText.T(
                        "حساب هذا الزبون الائتماني موقوف. لا يمكن إنشاء فاتورة آجل له.",
                        "This customer credit account is blocked. A credit invoice cannot be created."),
                    UiText.T("تنبيه", "Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            if (customer.CreditLimit <= 0m)
                return true;

            var currentBalance = await GetCustomerCurrentBalanceAsync(customer.Id);
            var projectedBalance = currentBalance + invoiceAmount + previousCreditAmount;

            if (projectedBalance > customer.CreditLimit)
            {
                MessageBox.Show(
                    UiText.T(
                        $"الحد الائتماني للزبون تم تجاوزه.\nالرصيد الحالي: {currentBalance:N5}\nالحد الائتماني: {customer.CreditLimit:N5}\nالرصيد المتوقع بعد الفاتورة: {projectedBalance:N5}",
                        $"The customer credit limit was exceeded.\nCurrent balance: {currentBalance:N5}\nCredit limit: {customer.CreditLimit:N5}\nProjected balance after invoice: {projectedBalance:N5}"),
                    UiText.T("تنبيه", "Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            return true;
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

        private async void CreateProductBtn_Click(object sender, RoutedEventArgs e)
        {
            var existingProductIds = Products.Select(product => product.Id).ToHashSet();

            WindowManager.ShowDialog<CreateProduct>();

            await LoadProductsAsync();

            var createdProduct = Products.FirstOrDefault(product => !existingProductIds.Contains(product.Id));
            if (createdProduct != null)
                ProductBox.SelectedItem = createdProduct;
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



        #region ===================== CUSTOMER SEARCH =====================
        private void CustomerComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            CustomerComboBox.DisplayMemberPath = "Name";
            CustomerComboBox.SelectedValuePath = "Id";
            CustomerComboBox.IsTextSearchEnabled = false;
            CustomerComboBox.AddHandler(TextBox.TextChangedEvent, new TextChangedEventHandler(CustomerComboBox_TextChanged));
        }

        private void CustomerComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isCompletingCustomerName || e.OriginalSource is not TextBox)
                return;

            FilterCustomerList(CustomerComboBox.Text);
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
            CustomerComboBox.IsDropDownOpen = filtered.Count > 0;

            var query = text.Trim();
            if (query.Length < 2)
                return;

            var firstPrefixMatch = filtered.FirstOrDefault(customer =>
                customer.Name!.StartsWith(query, StringComparison.OrdinalIgnoreCase));

            if (firstPrefixMatch?.Name == null ||
                string.Equals(query, firstPrefixMatch.Name, StringComparison.OrdinalIgnoreCase))
                return;

            _isCompletingCustomerName = true;
            try
            {
                CustomerComboBox.SelectedItem = firstPrefixMatch;
                CustomerComboBox.Text = firstPrefixMatch.Name;
                if (CustomerComboBox.Template.FindName("PART_EditableTextBox", CustomerComboBox) is TextBox customerTextBox)
                {
                    customerTextBox.SelectionStart = query.Length;
                    customerTextBox.SelectionLength = firstPrefixMatch.Name.Length - query.Length;
                }
                CustomerComboBox.IsDropDownOpen = true;
            }
            finally
            {
                _isCompletingCustomerName = false;
            }
        }

        private async void AddCustomerBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int? createdCustomerId = null;
                var initialName = CustomerComboBox.Text?.Trim();

                WindowManager.ShowDialog<CreateUser>(WindowSizeType.MediumRectangle, window =>
                {
                    window.InitializeForCustomerQuickCreate(initialName, null);
                    window.Closed += (_, __) => createdCustomerId = window.CreatedUserId;
                });

                if (createdCustomerId.HasValue)
                {
                    await LoadCustomersAsync(createdCustomerId);
                }

                CustomerComboBox.Focus();
                CustomerComboBox.IsDropDownOpen = createdCustomerId.HasValue;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("تعذر إضافة الزبون", "Could not add the customer")}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                CustomerComboBox.Focus();
            }
        }
        #endregion



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
                        item.ExpiryDate = (await GetPreferredAvailableStockAsync(selectedProductId, defaultUnit.Id))?.ExpiryDate;

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
            SaleBox.Text = "";
            PurchaseBox.Text = "";
         
            ExpiryBox.SelectedDate = null;
        }

        private static bool HasHydratedUnits(ProductReadDto? product)
        {
            return product?.ProductUnits?.Any(unit => unit.Unit != null && !string.IsNullOrWhiteSpace(unit.Unit.Name)) == true;
        }

        private ProductReadDto? ResolveProductForUnits(int productId)
        {
            var product = Products.FirstOrDefault(p => p.Id == productId);
            if (HasHydratedUnits(product))
                return product;

            return null;
        }

        private async Task<List<ProductUnitReadDto>> LoadHydratedUnitsAsync(int productId)
        {
            var result = await _productUnitService.GetAllWithFilteringAndIncludeAsync(
                unit => unit.ProductId == productId,
                unit => unit.Unit);

            return result.Data ?? new List<ProductUnitReadDto>();
        }

        private async Task<ProductReadDto?> ResolveProductForUnitsAsync(int productId)
        {
            var cachedProduct = ResolveProductForUnits(productId);
            if (cachedProduct != null)
                return cachedProduct;

            if (productId <= 0)
                return null;

            try
            {
                var result = await _productService.GetByIdAsync(productId);
                var product = result?.Data;
                if (product == null)
                    return null;

                if (!HasHydratedUnits(product))
                    product.ProductUnits = await LoadHydratedUnitsAsync(productId);

                return product;
            }
            catch
            {
                return null;
            }
        }

        //private async void UnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (_isApplyingProductSelection)
        //        return;

        //    if (UnitBox.SelectedItem is not ProductUnitWriteDto unit)
        //        return;

        //    UnitBox.Text = unit.Unit?.Name ?? string.Empty;
        //    PurchaseBox.Text = unit.PurchasePrice.ToString();
        //    SaleBox.Text = unit.SalePrice.ToString();

        //    if (!TryParseDecimalInput(QtyBox.Text, out var qty) || qty <= 0)
        //        QtyBox.Text = "1";

        //    if (ProductBox.SelectedValue is int productId && productId > 0)
        //        await LoadSelectedStockExpiryAsync(productId, unit.Id);
        //}
        private async void UnitBox_SelectionChanged(
    object sender,
    SelectionChangedEventArgs e)
        {
            if (_isApplyingProductSelection)
                return;

            if (UnitBox.SelectedItem is not ProductUnitWriteDto unit)
                return;

            // Unit
            UnitBox.Text = unit.Unit?.Name ?? "";

            // Purchase price
            PurchaseBox.Text =
                unit.PurchasePrice.ToString("0.00000");

            // Sale price
            SaleBox.Text =
                unit.SalePrice.ToString("0.00000");

            // Quantity
            if (!TryParseDecimalInput(QtyBox.Text, out var qty) || qty <= 0)
                QtyBox.Text = "1";

            // Expiry from stock
            if (ProductBox.SelectedValue is int productId && productId > 0)
            {
                await LoadSelectedStockExpiryAsync(
                    productId,
                    unit.Id);
            }
        }

        private async void ProductBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Product data is loaded only after an explicit Enter or mouse choice.
            // SelectionChanged also fires while navigating/searching the list.
        }

        private async void ProductBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isApplyingProductSelection || !ProductBox.IsDropDownOpen)
                return;

            await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Background);

            var product = ResolveProductChoice();

            if (product == null)
                return;

            _isApplyingProductSelection = true;
            try
            {
                ProductBox.Text = product.Name ?? string.Empty;
                _productSearchText = ProductBox.Text;
                ProductBox.IsDropDownOpen = false;

                await LoadProductUnitsIntoInputAsync(product.Id);
                QtyBox.Focus();
                QtyBox.SelectAll();
            }
            finally
            {
                _isApplyingProductSelection = false;
            }
        }

        //private void ProductBox_PreviewKeyDown(object sender, KeyEventArgs e)
        //{
        //    if (e.Key is Key.Up or Key.Down && ProductBox.IsDropDownOpen)
        //    {
        //        RestoreTypedProductSearchTextAfterNavigation();
        //        return;
        //    }

        //    if (e.Key != Key.Enter || ProductBox.SelectedItem is not ProductReadDto product)
        //        return;

        //    e.Handled = true;
        //    _isRestoringProductSearchText = true;
        //    ProductBox.Text = product.Name ?? string.Empty;
        //    _productSearchText = ProductBox.Text;
        //    _isRestoringProductSearchText = false;
        //    ProductBox.IsDropDownOpen = false;
        //    QueueProductSelection(product.Id);
        //}
        private async void ProductBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Up or Key.Down && ProductBox.IsDropDownOpen)
            {
                RestoreTypedProductSearchTextAfterNavigation();
                return;
            }

            if (e.Key is not Key.Enter)

                return;

            await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Background);

            var product = ResolveProductChoice();
            if (product == null)
                return;

            e.Handled = true;

            _isApplyingProductSelection = true;

            try
            {
                ProductBox.Text = product.Name ?? string.Empty;
                _productSearchText = ProductBox.Text;

                ProductBox.IsDropDownOpen = false;

                // Load unit + purchase price + sale price + expiry
                await LoadProductUnitsIntoInputAsync(product.Id);

                // Focus quantity
                QtyBox.Focus();
                QtyBox.SelectAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T(
                        "حدث خطأ أثناء اختيار المنتج",
                        "Error while selecting product"
                    )}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _isApplyingProductSelection = false;
            }
        }

        private ProductReadDto? ResolveProductChoice()
        {
            if (ProductBox.SelectedItem is ProductReadDto selectedProduct)
                return selectedProduct;

            var searchText = ProductBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(searchText))
                return null;

            var exactMatch = Products.FirstOrDefault(item =>
                string.Equals(item.Name?.Trim(), searchText, StringComparison.OrdinalIgnoreCase) ||
                item.ITEMCODE.ToString() == searchText);

            if (exactMatch != null)
                return exactMatch;

            return ProductBox.ItemsSource is IEnumerable<ProductReadDto> filteredProducts
                ? filteredProducts.FirstOrDefault()
                : null;
        }

        private void RestoreTypedProductSearchTextAfterNavigation()
        {
            var typedText = _productSearchText;
            _isNavigatingProductResults = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _isRestoringProductSearchText = true;
                ProductBox.Text = typedText;
                _isRestoringProductSearchText = false;
                _isNavigatingProductResults = false;
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void QueueProductSelection(int productId)
        {
            if (productId <= 0)
                return;

            var selectionVersion = System.Threading.Interlocked.Increment(ref _productSelectionVersion);
            _ = LoadSelectedProductAsync(productId, selectionVersion);
        }

        //private async Task LoadSelectedProductAsync(int productId, int selectionVersion)
        //{
        //    await Task.Delay(ProductSelectionDelayMs);
        //    if (selectionVersion != _productSelectionVersion)
        //        return;

        //    await _productSelectionSemaphore.WaitAsync();
        //    try
        //    {
        //        if (selectionVersion != _productSelectionVersion)
        //            return;

        //        _isLoadingUnits = true;
        //        _isApplyingProductSelection = true;
        //        await LoadProductUnitsIntoInputAsync(productId);
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"{UiText.T("خطأ أثناء تحميل الوحدات", "Error while loading units")}: {ex.Message}", UiText.T("خطأ", "Error"));
        //    }
        //    finally
        //    {
        //        _isLoadingUnits = false;
        //        _isApplyingProductSelection = false;
        //        _productSelectionSemaphore.Release();
        //    }
        //}



        private async Task LoadSelectedProductAsync(int productId, int selectionVersion)
        {
            await Task.Delay(ProductSelectionDelayMs);

            if (selectionVersion != _productSelectionVersion)
                return;

            await _productSelectionSemaphore.WaitAsync();

            try
            {
                if (selectionVersion != _productSelectionVersion)
                    return;

                _isLoadingUnits = true;
                _isApplyingProductSelection = true;

                // =====================================================
                // 1. Load product units
                // =====================================================
                var unitsResult = await _productUnitService
                    .GetAllWriteDtoWithFilteringAndIncludeAsync(
                        pu => pu.ProductId == productId,
                        pu => pu.Unit);

                var units = unitsResult?.Data?.ToList()
                            ?? new List<ProductUnitWriteDto>();

                if (units.Count == 0)
                {
                    UnitBox.ItemsSource = null;
                    UnitBox.SelectedIndex = -1;

                    PurchaseBox.Text = "";
                    SaleBox.Text = "";
                    ExpiryBox.SelectedDate = null;

                    return;
                }

                // =====================================================
                // 2. Put units into UnitBox
                // =====================================================
                UnitBox.ItemsSource = units;

                // =====================================================
                // 3. Select default SALE unit
                // =====================================================
                var defaultUnit =
                    ProductUnitSelector.GetDefaultSaleUnit(units)
                    ?? units.FirstOrDefault();

                if (defaultUnit == null)
                    return;

                // =====================================================
                // 4. Select the unit
                // =====================================================
                UnitBox.SelectedItem = defaultUnit;
                UnitBox.SelectedValue = defaultUnit.Id;

                // =====================================================
                // 5. Fill Unit
                // =====================================================
                UnitBox.Text = defaultUnit.Unit?.Name ?? "";

                // =====================================================
                // 6. Fill Purchase Price
                // =====================================================
                PurchaseBox.Text =
                    defaultUnit.PurchasePrice.ToString("0.00000");

                // =====================================================
                // 7. Fill Sale Price
                // =====================================================
                SaleBox.Text =
                    defaultUnit.SalePrice.ToString("0.00000");

                // =====================================================
                // 8. Get expiry from available stock
                // =====================================================
                var stock = await GetPreferredAvailableStockAsync(
                    productId,
                    defaultUnit.Id);

                ExpiryBox.SelectedDate =
                    stock?.ExpiryDate ?? DateTime.Now.AddMonths(6);

                // =====================================================
                // 9. Default quantity
                // =====================================================
                if (!TryParseDecimalInput(QtyBox.Text, out var qty) || qty <= 0)
                    QtyBox.Text = "1";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T(
                        "خطأ أثناء تحميل الوحدات",
                        "Error while loading units"
                    )}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _isLoadingUnits = false;
                _isApplyingProductSelection = false;

                _productSelectionSemaphore.Release();
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
                unit = await LoadProductUnitsIntoInputAsync(product.Id);
                if (unit == null)
                {
                    MessageBox.Show(UiText.T("يرجى اختيار وحدة.", "Please choose a unit."), UiText.T("تنبيه", "Notice"));
                    return;
                }
            }

            if (!TryParseDecimalInput(QtyBox.Text, out decimal qty) || qty <= 0)
            {
                MessageBox.Show(UiText.T("الكمية غير صحيحة.", "The quantity is invalid."), UiText.T("تنبيه", "Notice"));
                return;
            }

            var unitCost = TryParseDecimalInput(PurchaseBox.Text, out var parsedCost) && parsedCost > 0m
                ? parsedCost
                : unit.PurchasePrice > 0m
                    ? unit.PurchasePrice
                    : product.DefaultPurchasePrice;
            var unitPrice = TryParseDecimalInput(SaleBox.Text, out var parsedSalePrice) && parsedSalePrice > 0m
                ? parsedSalePrice
                : unit.SalePrice > 0m
                    ? unit.SalePrice
                    : product.DefaultSalePrice;

            // ✅ ADD: Snapshot tax info from product at time of invoice
            bool taxExempt = product.TaxExempt ?? false;
            decimal taxRate = taxExempt ? 0m : (product.TaxRate ?? 0m);
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
                UnitNameSnapshot = unit.Unit?.Name,

                Quantity = qty,

                // ✅ sale price stored on product already includes tax
                UnitPrice = unitPrice,

                // ✅ ADD: store purchase cost used (snapshot)
                UnitCost = unitCost,

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

            if (!await AddOrResplitDraftInvoiceLineAsync(line))
                return;

            UpdateTotals();   // ✅ ADD: new totals method
            ClearProductInputs();
        }

        private Task<bool> AddOrResplitDraftInvoiceLineAsync(InvoiceLineWriteDto newLine)
        {
            var matchingLines = InvoiceLines
                .Where(l => l.ProductId == newLine.ProductId && l.ProductUnitId == newLine.ProductUnitId && l.Quantity > 0)
                .ToList();

            var templateLine = matchingLines.FirstOrDefault() ?? newLine;
            var totalQuantity = matchingLines.Sum(l => l.Quantity) + newLine.Quantity;
            var insertIndex = matchingLines.Any() ? InvoiceLines.IndexOf(matchingLines.First()) : InvoiceLines.Count;

            var aggregateLine = new InvoiceLineWriteDto
            {
                Id = templateLine.Id,
                InvoiceId = templateLine.InvoiceId,
                OriginalInvoiceId = templateLine.OriginalInvoiceId,
                ProductId = newLine.ProductId,
                ProductName = newLine.ProductName,
                ProductUnitId = newLine.ProductUnitId,
                UnitName = newLine.UnitName,
                UnitNameSnapshot = newLine.UnitNameSnapshot ?? newLine.UnitName,
                Quantity = totalQuantity,
                QuantityPerUnitSnapshot = newLine.QuantityPerUnitSnapshot,
                BaseQuantity = totalQuantity * (newLine.QuantityPerUnitSnapshot > 0 ? newLine.QuantityPerUnitSnapshot : 1m),
                UnitPrice = newLine.UnitPrice,
                UnitCost = newLine.UnitCost,
                TaxExempt = newLine.TaxExempt,
                TaxRate = newLine.TaxRate,
                ExpiryDate = newLine.ExpiryDate,
                CreatedDate = templateLine.CreatedDate == default ? DateTime.Now : templateLine.CreatedDate,
                UpdatedDate = DateTime.Now
            };

            foreach (var line in matchingLines)
                InvoiceLines.Remove(line);

            InvoiceLines.Insert(insertIndex, aggregateLine);

            ProductsGrid.Items.Refresh();
            return Task.FromResult(true);
        }


        private void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is InvoiceLineWriteDto line)
            {
                InvoiceLines.Remove(line);
                UpdateTotals();
            }
        }

        private async void LineUnitBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ComboBox combo || combo.DataContext is not InvoiceLineWriteDto line || line.ProductId <= 0)
                return;

            try
            {
                _isUpdatingInvoiceLine = true;
                var units = await GetAvailableUnitsForProductAsync(line.ProductId);
                combo.ItemsSource = units;
                combo.SelectedValue = line.ProductUnitId;
            }
            finally
            {
                _isUpdatingInvoiceLine = false;
            }
        }

        private async void LineUnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingInvoiceLine)
                return;

            if (sender is not ComboBox combo ||
                combo.DataContext is not InvoiceLineWriteDto line ||
                combo.SelectedItem is not ProductUnitWriteDto unit)
                return;

            await ApplyUnitToInvoiceLineAsync(line, unit);
        }

        private async void LineNumberBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox textBox || textBox.DataContext is not InvoiceLineWriteDto line)
                return;

            textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();

            if (!TryParseDecimalInput(textBox.Text, out var value))
            {
                ProductsGrid.Items.Refresh();
                return;
            }

            switch (textBox.Tag?.ToString())
            {
                case "Quantity":
                    await UpdateInvoiceLineQuantityAsync(line, value);
                    textBox.Text = line.Quantity.ToString("0.00000");
                    break;

                case "UnitCost":
                    line.UnitCost = Math.Max(value, 0m);
                    RecalculateLineAmounts(line);
                    break;

                case "UnitPrice":
                    line.UnitPrice = Math.Max(value, 0m);
                    RecalculateLineAmounts(line);
                    textBox.Text = line.UnitPrice.ToString("0.00000");
                    break;
            }

            UpdateTotals();
            ProductsGrid.Items.Refresh();
        }

        /* private void UpdateTotal()
         {
             TotalAmountTextBox.Text =
                 InvoiceLines.Sum(x => x.LineTotal).ToString("0.00000");
         }
            */
        // ✅ ADD: Calculates invoice summary fields needed for reports
        private void UpdateTotals()
        {
            EnsureInvoiceLineDisplaySnapshots();

            decimal subTotal = InvoiceLines.Sum(l => l.LineSubTotal);   // قبل الضريبة
            decimal taxTotal = InvoiceLines.Sum(l => l.TaxAmount);      // الضريبة
            decimal grossSales = InvoiceLines.Sum(l => l.Quantity * l.UnitPrice);
            decimal.TryParse(DiscountTextBox.Text, out var discount);
            discount = Math.Clamp(discount, 0m, grossSales);

            decimal netTotal = grossSales - discount;

            // ✅ Existing UI field shows final
            TotalAmountTextBox.Text = netTotal.ToString("0.00000");

            // ✅ Optional: if you want show subtotal/tax in UI, bind to labels/textboxes
             SubTotalTextBox.Text = subTotal.ToString("0.00000");
             TaxTotalTextBox.Text = taxTotal.ToString("0.00000");
         }

        private void DiscountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded)
                UpdateTotals();
        }

        private void EnsureInvoiceLineDisplaySnapshots()
        {
            foreach (var line in InvoiceLines)
            {
                if (!string.IsNullOrWhiteSpace(line.UnitNameSnapshot))
                    continue;

                var unitName =
                    line.UnitName
                    ?? line.ProductUnit?.Unit?.Name;

                if (!string.IsNullOrWhiteSpace(unitName))
                    line.UnitNameSnapshot = unitName;
            }
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
            line.BaseQuantity = line.Quantity * (line.QuantityPerUnitSnapshot > 0 ? line.QuantityPerUnitSnapshot : 1m);
        }

        private async Task ApplyUnitToInvoiceLineAsync(InvoiceLineWriteDto line, ProductUnitWriteDto unit)
        {
            var previousUnitId = line.ProductUnitId;
            line.ProductUnitId = unit.Id;
            line.ProductUnit = unit;
            line.UnitName = unit.Unit?.Name;
            line.UnitNameSnapshot = unit.Unit?.Name;
            line.QuantityPerUnitSnapshot = unit.QuantityPerUnit > 0 ? unit.QuantityPerUnit : 1m;

            var preferredStock = await GetPreferredAvailableStockAsync(line.ProductId, unit.Id);
            line.UnitCost = preferredStock?.PurchasePrice ?? unit.PurchasePrice;
            line.UnitPrice = preferredStock?.SalePrice ?? unit.SalePrice;
            line.ExpiryDate = preferredStock?.ExpiryDate ?? DateTime.Now.AddMonths(6);

            if (line.Quantity <= 0)
                line.Quantity = 1;

            await UpdateInvoiceLineQuantityAsync(line, line.Quantity, previousUnitId == unit.Id ? line : null);
            RecalculateLineAmounts(line);
            UpdateTotals();
            ProductsGrid.Items.Refresh();
        }

        private async Task UpdateInvoiceLineQuantityAsync(
            InvoiceLineWriteDto line,
            decimal requestedQuantity,
            InvoiceLineWriteDto? excludedLine = null)
        {
            var quantity = requestedQuantity <= 0 ? 1m : requestedQuantity;
            var availableQuantity = await GetAvailableQuantityForProductUnitAsync(line.ProductId, line.ProductUnitId);
            var otherRequestedQuantity = InvoiceLines
                .Where(other =>
                    !ReferenceEquals(other, excludedLine ?? line) &&
                    other.ProductId == line.ProductId &&
                    other.ProductUnitId == line.ProductUnitId &&
                    other.Quantity > 0)
                .Sum(other => other.Quantity);

            var remainingQuantity = Math.Max(availableQuantity - otherRequestedQuantity, 0m);
            if (quantity > remainingQuantity)
            {
                quantity = remainingQuantity;
                MessageBox.Show(
                    UiText.T(
                        $"الكمية المطلوبة للصنف {line.ProductName} أكبر من الكمية المتوفرة. تم تعديل الكمية إلى: {quantity:0.00000}.",
                        $"The requested quantity for {line.ProductName} is greater than available stock. Quantity was adjusted to: {quantity:0.00000}."),
                    UiText.T("تنبيه", "Notice"));
            }

            line.Quantity = quantity;
            RecalculateLineAmounts(line);
        }

        private async Task ResetLineSalePriceBelowCostAsync(InvoiceLineWriteDto line)
        {
            var units = await GetAvailableUnitsForProductAsync(line.ProductId);
            var defaultPrice = units.FirstOrDefault(unit => unit.Id == line.ProductUnitId)?.SalePrice ?? line.UnitPrice;

            MessageBox.Show(
                UiText.T(
                    $"لا يمكن بيع الصنف {line.ProductName} بسعر أقل من التكلفة. سيتم إعادة سعر البيع إلى: {defaultPrice:0.00000}.",
                    $"Cannot sell {line.ProductName} below cost. Sale price will be restored to: {defaultPrice:0.00000}."),
                UiText.T("تنبيه", "Notice"));

            line.UnitPrice = defaultPrice;
        }

        private async void SaveReceiptBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isSaving)
                return;

            var loadingShown = false;

            void HideLoadingIfShown()
            {
                if (!loadingShown)
                    return;

                _loadingService.Hide();
                loadingShown = false;
            }

            try
            {
                _isSaving = true;
                SaveReceiptBtn.IsEnabled = false;

                if (!InvoiceLines.Any())
                {
                    MessageBox.Show(UiText.T("يرجى إضافة منتج واحد على الأقل.", "Please add at least one product."), UiText.T("تنبيه", "Notice"));
                    return;
                }

                var customer = CustomerComboBox.SelectedItem as UserReadDto;
                var selectedPaymentType = GetSelectedPaymentType();

                if (selectedPaymentType == PaymentType.Credit && customer == null)
                {
                    MessageBox.Show(
                        UiText.T("يرجى اختيار الزبون قبل إنشاء فاتورة آجل.", "Please select a customer before creating a credit invoice."),
                        UiText.T("تنبيه", "Notice"));
                    return;
                }

                if (MessageBox.Show(
                        UiText.T("هل تريد حفظ فاتورة البيع؟", "Do you want to save the sales invoice?"),
                        UiText.T("تأكيد الحفظ", "Confirm save"),
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

                _loadingService.Show();
                loadingShown = true;

                if (!await ValidateInvoiceStockAvailabilityAsync())
                {
                    HideLoadingIfShown();
                    return;
                }

                var expandInvoiceResult = await ExpandInvoiceLinesByFefoAsync(InvoiceLines);
                if (!expandInvoiceResult.Success || expandInvoiceResult.Data == null)
                {
                    HideLoadingIfShown();
                    MessageBox.Show(
                        expandInvoiceResult.Message ?? UiText.T("تعذر تخصيص المخزون لبعض الأصناف.", "Could not allocate stock for some items."),
                        UiText.T("تنبيه", "Notice"));
                    return;
                }

                var expandedInvoiceLines = expandInvoiceResult.Data;
                //decimal totalAmount = InvoiceLines.Sum(l => l.LineTotal);
                // ✅ ADD: invoice totals required for reporting
                decimal subTotal = expandedInvoiceLines.Sum(l => l.LineSubTotal);
                decimal totalTax = expandedInvoiceLines.Sum(l => l.TaxAmount);
                decimal grossSales = expandedInvoiceLines.Sum(l => l.Quantity * l.UnitPrice);
                decimal.TryParse(DiscountTextBox.Text, out var discount);
                discount = Math.Clamp(discount, 0m, grossSales);

                decimal totalAmount = grossSales - discount;
                bool isUpdate = _currentInvoiceId != null;
                decimal previousCreditAmount = 0m;

                if (isUpdate && _currentInvoiceId.HasValue)
                {
                    var existingInvoice = await _invoicesService.GetFullInvoiceByIdAsync(_currentInvoiceId.Value);
                    if (existingInvoice?.PaymentType == PaymentType.Credit)
                        previousCreditAmount = -existingInvoice.TotalAmount;
                }

                if (selectedPaymentType == PaymentType.Credit)
                {
                    if (!await EnsureCustomerCreditAllowedAsync(totalAmount, previousCreditAmount))
                    {
                        HideLoadingIfShown();
                        return;
                    }
                }

                if (selectedPaymentType == PaymentType.Check)
                {
                    var currentCheckTotal = Math.Round(_currentChecks.Sum(check => check.Amount), 3);
                    var expectedCheckTotal = Math.Round(totalAmount, 3);

                    if (_currentChecks.Count == 0 || currentCheckTotal != expectedCheckTotal)
                    {
                        HideLoadingIfShown();
                        if (!EditCurrentInvoiceChecks(totalAmount))
                            return;
                    }
                }
                else if (_currentChecks.Any())
                {
                    _currentChecks.Clear();
                    UpdateChecksButtonVisibility();
                }

                if (selectedPaymentType == PaymentType.Check && !_currentChecks.Any())
                {
                    HideLoadingIfShown();
                    MessageBox.Show(
                        UiText.T("يرجى إدخال شيك واحد على الأقل.", "Please enter at least one check."),
                        UiText.T("تنبيه", "Notice"));
                    return;
                }

                if (selectedPaymentType == PaymentType.Check)
                {
                    var checkTotal = Math.Round(_currentChecks.Sum(check => check.Amount), 3);
                    if (checkTotal != Math.Round(totalAmount, 3))
                    {
                        HideLoadingIfShown();
                        MessageBox.Show(
                            UiText.T(
                                $"مجموع الشيكات ({checkTotal:0.00000}) يجب أن يساوي إجمالي الفاتورة ({totalAmount:0.00000}).",
                                $"The total check amount ({checkTotal:0.00000}) must equal the invoice total ({totalAmount:0.00000})."),
                            UiText.T("تنبيه", "Notice"));
                        return;
                    }
                }

                if (!TryGetActiveCashierSession(out var session))
                {
                    HideLoadingIfShown();
                    return;
                }

                var invoiceDto = new InvoiceWriteDto
                {
                    Id = _currentInvoiceId ?? 0,
                    InvoiceNumber = InvoiceNumberTextBox.Text,
                    FalconInvoiceNumber = FalconInvoiceNumberTextBox.Text.Trim(),
                    CustomerId = customer?.Id,
                    DelegateId = DelegateComboBox.SelectedValue is int delegateId ? delegateId : null,
                    InvoiceType = InvoiceType.Sale,
                    TotalAmount = totalAmount,
                    CreatedDate = InvoiceDatePicker.SelectedDate.Value,
                    UpdatedDate = DateTime.Now,
                    InvoiceLines = expandedInvoiceLines,
                    CasherId = session.CashierId,
                    PaymentType = selectedPaymentType,
                    Checks = selectedPaymentType == PaymentType.Check ? _currentChecks : null,
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
                        HideLoadingIfShown();
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
                        HideLoadingIfShown();
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
                            HideLoadingIfShown();
                            MessageBox.Show(postResult.Message ?? UiText.T("تم حفظ الفاتورة لكن فشل تسجيل الحركة المالية.", "The invoice was saved, but posting the financial transaction failed."), UiText.T("تحذير", "Warning"));
                            return;
                        }
                    }

                HideLoadingIfShown();
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
                        HideLoadingIfShown();
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
                        HideLoadingIfShown();
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
                        HideLoadingIfShown();
                        MessageBox.Show(applyResult.Message ?? UiText.T("فشل تحديث حركة المخزون.", "Failed to update the stock movement."), UiText.T("خطأ", "Error"));
                        return;
                    }

                    // 3️⃣ Update invoice
                    var result = await _invoicesService.UpdateAsync(invoiceDto);

                    if (!result.Success)
                    {
                        HideLoadingIfShown();
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
                            HideLoadingIfShown();
                            MessageBox.Show(postResult.Message ?? UiText.T("تم تحديث الفاتورة لكن فشل تسجيل الحركة المالية الجديدة", "The invoice was updated, but posting the new financial transaction failed."), UiText.T("تحذير", "Warning"));
                            return;
                        }
                    }

                HideLoadingIfShown();
                MessageBox.Show(
                    selectedPaymentType == PaymentType.Credit
                        ? UiText.T("تم تحديث الفاتورة الآجلة بنجاح ✅", "The credit invoice was updated successfully.")
                        : UiText.T("تم تحديث الفاتورة وتسجيل الحركة المالية ✅", "The invoice was updated and the financial transaction was posted successfully."),
                    UiText.T("نجاح", "Success"));
                UiText.ApplyTranslations(this);
            }

                NewInvoiceBtn_Click(this, new RoutedEventArgs());
            }
            catch (Exception ex)
            {
                HideLoadingIfShown();
                var details = ex.Message;
                var inner = ex.InnerException;
                while (inner != null)
                {
                    details += Environment.NewLine + inner.Message;
                    inner = inner.InnerException;
                }

                MessageBox.Show($"{UiText.T("خطأ أثناء حفظ الفاتورة", "An error occurred while saving the invoice")}:\n{details}", UiText.T("خطأ", "Error"));
            }
            finally
            {
                HideLoadingIfShown();
                SaveReceiptBtn.IsEnabled = true;
                _isSaving = false;
            }
        }
        private void CreateSalesInvoice_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2)
            {
                e.Handled = true;
                SearchProductBtn_Click(this, new RoutedEventArgs());
                return;
            }

            if (e.Key != Key.F1)
                return;

            e.Handled = true;
            SaveReceiptBtn_Click(SaveReceiptBtn, new RoutedEventArgs());
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

        private void SetSelectedPaymentType(PaymentType paymentType)
        {
            foreach (var item in PaymentMethodComboBox.Items.OfType<ComboBoxItem>())
            {
                if (int.TryParse(item.Tag?.ToString(), out var value) && (PaymentType)value == paymentType)
                {
                    PaymentMethodComboBox.SelectedItem = item;
                    return;
                }
            }

            PaymentMethodComboBox.SelectedIndex = 0;
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

                NewInvoiceBtn_Click(this, new RoutedEventArgs());
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


        private async void SearchInvoiceBtn_Click(object sender, RoutedEventArgs e)
        {
            var searchWindow = new SearchSalesInvoiceWindow(_invoicesService, _allCustomers ?? Enumerable.Empty<UserReadDto>(), true)
            {
                Owner = this
            };

            if (searchWindow.ShowDialog() == true)
            {
                await LoadSelectedInvoiceWithLoadingAsync(searchWindow.Result);
            }
        }
        private async Task LoadSelectedInvoiceWithLoadingAsync(InvoiceReadDto invoice)
        {
            _loadingService.Show();
            try
            {
                await Task.Delay(1);
                LoadSelectedInvoice(invoice);
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private void LoadSelectedInvoice(InvoiceReadDto invoice)
        {
            if (invoice == null) return;

            _currentInvoiceId = invoice.Id;

            _originalLines = invoice.InvoiceLines.ToList();   // 🔥 مهم جداً

            InvoiceNumberTextBox.Text = invoice.InvoiceNumber;
            FalconInvoiceNumberTextBox.Text = invoice.FalconInvoiceNumber ?? string.Empty;
            InvoiceDatePicker.SelectedDate = invoice.CreatedDate;

            CustomerComboBox.SelectedItem =
                _allCustomers.FirstOrDefault(c => c.Id == invoice.CustomerId);
            if (DelegatePanel.Visibility == Visibility.Visible)
                DelegateComboBox.SelectedItem = _allDelegates.FirstOrDefault(d => d.Id == invoice.DelegateId);
            if (invoice.PaymentType.HasValue)
                SetSelectedPaymentType(invoice.PaymentType.Value);
            DiscountTextBox.Text = (invoice.DiscountAmount ?? 0m).ToString("0.00000");
            _currentChecks = CloneChecks(invoice.Checks);
            UpdateChecksButtonVisibility();
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
                    UnitNameSnapshot = line.ProductUnit?.Unit?.Name,
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
            UpdateChecksButtonVisibility();

            PrintBtn.Visibility = Visibility.Visible;
        }

        private void NewInvoiceBtn_Click(object sender, RoutedEventArgs e)
        {
            _currentInvoiceId = null;
            _originalLines.Clear();

            InvoiceLines.Clear();
            ProductsGrid.Items.Refresh();

            InvoiceNumberTextBox.Text = GenerateInvoiceNumber();
            FalconInvoiceNumberTextBox.Clear();
            CustomerComboBox.SelectedIndex = -1;
            DelegateComboBox.SelectedIndex = -1;
            InvoiceDatePicker.SelectedDate = DateTime.Now;
            _currentChecks.Clear();
            UpdateChecksButtonVisibility();

            TotalAmountTextBox.Text = "0";
            DiscountTextBox.Text = "0";
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


        private void ProductBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            _productSearchText = ProductBox.Text + e.Text;
            FilterProducts(_productSearchText);
        }

        private void ProductBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key is not (Key.Back or Key.Delete))
                return;

            _productSearchText = ProductBox.Text ?? string.Empty;
            FilterProducts(_productSearchText);
        }

        private async Task DebouncedFilterProductsAsync(int searchVersion)
        {
            await Task.Delay(ProductSearchDelayMs);
            if (searchVersion == _productSearchVersion)
                FilterProducts();
        }

        private async Task<bool> AddProductFromSearchAsync(PurchaseProductSearchWindow.PurchaseProductSearchRow row)
        {
            if (row.ProductUnitId <= 0)
            {
                MessageBox.Show(
                    UiText.T("لا توجد وحدة معرفة لهذا المنتج.", "No unit is defined for this product."),
                    UiText.T("تنبيه", "Notice"));
                return false;
            }

            var unit = row.Product.ProductUnits?.FirstOrDefault(x => x.Id == row.ProductUnitId);
            if (unit == null)
            {
                MessageBox.Show(
                    UiText.T("تعذر تحميل وحدة المنتج.", "The product unit could not be loaded."),
                    UiText.T("تنبيه", "Notice"));
                return false;
            }

            var unitPrice = unit.SalePrice > 0m ? unit.SalePrice : row.Product.DefaultSalePrice;
            var unitCost = unit.PurchasePrice > 0m ? unit.PurchasePrice : row.Product.DefaultPurchasePrice;
            var quantityPerUnit = unit.QuantityPerUnit > 0m ? unit.QuantityPerUnit : 1m;
            var taxExempt = row.Product.TaxExempt ?? false;
            var taxRate = taxExempt ? 0m : row.Product.TaxRate ?? 0m;
            var lineTotal = row.Quantity * unitPrice;
            var divisor = 1m + taxRate / 100m;
            var lineSubTotal = taxExempt || divisor <= 0m
                ? lineTotal
                : Math.Round(lineTotal / divisor, 3);

            var line = new InvoiceLineWriteDto
            {
                ProductId = row.Product.Id,
                ProductName = row.Product.Name,
                ProductUnitId = row.ProductUnitId,
                QuantityPerUnitSnapshot = quantityPerUnit,
                BaseQuantity = row.Quantity * quantityPerUnit,
                UnitName = unit.Unit?.Name,
                UnitNameSnapshot = unit.Unit?.Name,
                Quantity = row.Quantity,
                UnitPrice = unitPrice,
                UnitCost = unitCost,
                TaxExempt = taxExempt,
                TaxRate = taxRate,
                TaxAmount = Math.Round(lineTotal - lineSubTotal, 3),
                LineSubTotal = lineSubTotal,
                ExpiryDate = row.ExpiryDate,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            if (!await AddOrResplitDraftInvoiceLineAsync(line))
                return false;

            UpdateTotals();
            return true;
        }

        private async Task CreateProductFromSearchAsync(string searchText)
        {
            WindowManager.ShowDialog<CreateProduct>(WindowSizeType.LargeRectangle, window =>
            {
                if (long.TryParse(searchText, out var barcode))
                    window.InitialItemCode = barcode.ToString();
            });

            await LoadProductsAsync();
        }

        private async void SearchProductBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var searchWindow = new PurchaseProductSearchWindow(
                    _productService,
                    async row => await AddProductFromSearchAsync(row),
                    async search => await CreateProductFromSearchAsync(search))
                {
                    Owner = this
                };

                searchWindow.ShowDialog();
                ProductBox.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("تعذر فتح نافذة بحث المنتجات", "Could not open the product search window")}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void FilterProducts(string? searchText = null)
        {
            string search = (searchText ?? ProductBox.Text)?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(search))
            {
                ProductBox.ItemsSource = null;
                ProductBox.IsDropDownOpen = false;
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
                .ThenBy(stock => stock.ExpiryDate == null ? 1 : 0)
                .ThenBy(stock => stock.ExpiryDate)
                .ToList()
                ?? new List<StockReadDto>();
        }

        private async Task<StockReadDto?> GetPreferredAvailableStockAsync(int productId, int productUnitId)
        {
            var stocks = await GetAvailableStocksForProductAsync(productId);
            return stocks
                .Where(stock => stock.ProductUnitId == productUnitId)
                .OrderBy(stock => stock.ExpiryDate == null ? 1 : 0)
                .ThenBy(stock => stock.ExpiryDate)
                .FirstOrDefault();
        }

        private async Task LoadSelectedStockExpiryAsync(int productId, int productUnitId)
        {
            var stock = await GetPreferredAvailableStockAsync(productId, productUnitId);
            ExpiryBox.SelectedDate = stock?.ExpiryDate ?? DateTime.Now.AddMonths(6);
        }

        private async Task<ProductUnitWriteDto?> LoadProductUnitsIntoInputAsync(int productId)
        {
            if (productId <= 0)
                return null;

            try
            {
                // Get available stock including:
                // ProductUnit
                // Unit
                // PurchasePrice
                // SalePrice
                // ExpiryDate
                var stocks = await GetAvailableStocksForProductAsync(productId);

                if (stocks == null || stocks.Count == 0)
                {
                    UnitBox.ItemsSource = null;
                    UnitBox.SelectedIndex = -1;

                    PurchaseBox.Text = "";
                    SaleBox.Text = "";
                    ExpiryBox.SelectedDate = null;

                    return null;
                }

                // Convert stock records to units
                var availableUnits = stocks
                    .GroupBy(s => s.ProductUnitId)
                    .Select(g => g
                        .OrderBy(s => s.ExpiryDate == null ? 1 : 0)
                        .ThenBy(s => s.ExpiryDate)
                        .First())
                    .Select(MapAvailableUnit)
                    .OrderByDescending(u => u.IsDefaultSaleUnit)
                    .ThenBy(u => u.DisplayName)
                    .ToList();

                UnitBox.ItemsSource = availableUnits;

                var firstUnit = availableUnits.FirstOrDefault();

                if (firstUnit == null)
                {
                    UnitBox.SelectedIndex = -1;
                    PurchaseBox.Text = "";
                    SaleBox.Text = "";
                    ExpiryBox.SelectedDate = null;

                    return null;
                }

                // Select first/default unit
                UnitBox.SelectedItem = firstUnit;
                UnitBox.SelectedValue = firstUnit.Id;

                // Unit
                UnitBox.Text = firstUnit.Unit?.Name ?? firstUnit.DisplayName;

                // Purchase price
                PurchaseBox.Text = firstUnit.PurchasePrice
                    .ToString("0.00000", CultureInfo.InvariantCulture);

                // Sale price
                SaleBox.Text = firstUnit.SalePrice
                    .ToString("0.00000", CultureInfo.InvariantCulture);

                // Get the stock batch for this unit
                var preferredStock = await GetPreferredAvailableStockAsync(
                    productId,
                    firstUnit.Id);

                // Expiry
                ExpiryBox.SelectedDate =
                    preferredStock?.ExpiryDate
                    ?? DateTime.Now.AddMonths(6);

                // Default quantity
                if (!TryParseDecimalInput(QtyBox.Text, out var qty) || qty <= 0)
                    QtyBox.Text = "1";

                return firstUnit;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T(
                        "حدث خطأ أثناء تحميل بيانات المنتج",
                        "Error while loading product data"
                    )}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return null;
            }
        }

        private async Task<decimal> GetAvailableQuantityForProductUnitAsync(int productId, int productUnitId)
        {
            if (productId <= 0 || productUnitId <= 0)
                return 0m;

            var result = await _stockService.GetAvailableQuantityInUnitAsync(productId, productUnitId);
            return result.Success ? result.Data : 0m;
        }

        private void ResetSalePriceBelowCost(string? productName, decimal unitCost, decimal enteredPrice, decimal defaultPrice)
        {
            MessageBox.Show(
                UiText.T(
                    $"لا يمكن بيع الصنف {productName} بسعر أقل من التكلفة. السعر المدخل: {enteredPrice:0.00000}، التكلفة: {unitCost:0.00000}. سيتم إعادة السعر الافتراضي: {defaultPrice:0.00000}.",
                    $"Cannot sell {productName} below cost. Entered price: {enteredPrice:0.00000}, cost: {unitCost:0.00000}. The default price will be restored: {defaultPrice:0.00000}."),
                UiText.T("تنبيه", "Notice"));

            SaleBox.Text = defaultPrice.ToString("0.00000");
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

        private static ProductUnitWriteDto MapProductUnit(RaccoonWarehouse.Domain.ProductUnits.DTOs.ProductUnitReadDto unit)
        {
            return new ProductUnitWriteDto
            {
                Id = unit.Id,
                ProductId = unit.ProductId,
                UnitId = unit.UnitId,
                Unit = unit.Unit == null
                    ? null
                    : new UnitWriteDto
                    {
                        Id = unit.Unit.Id,
                        Name = unit.Unit.Name ?? string.Empty,
                        CreatedDate = unit.Unit.CreatedDate,
                        UpdatedDate = unit.Unit.UpdatedDate
                    },
                QuantityPerUnit = unit.QuantityPerUnit,
                PurchasePrice = unit.PurchasePrice,
                SalePrice = unit.SalePrice,
                IsBaseUnit = unit.IsBaseUnit,
                IsDefaultSaleUnit = unit.IsDefaultSaleUnit,
                IsDefaultPurchaseUnit = unit.IsDefaultPurchaseUnit
            };
        }

        private async Task<List<ProductUnitWriteDto>> GetAvailableUnitsForProductAsync(int productId)
        {
            var product = await ResolveProductForUnitsAsync(productId);
            var fallbackPurchasePrice = product?.DefaultPurchasePrice ?? 0m;
            var fallbackSalePrice = product?.DefaultSalePrice ?? 0m;

            return (await LoadHydratedUnitsAsync(productId))
                .Select(MapProductUnit)
                .Select(unit =>
                {
                    if (unit.PurchasePrice <= 0m)
                        unit.PurchasePrice = fallbackPurchasePrice;

                    if (unit.SalePrice <= 0m)
                        unit.SalePrice = fallbackSalePrice;

                    return unit;
                })
                .OrderByDescending(unit => unit.IsDefaultSaleUnit)
                .ThenBy(unit => unit.DisplayName)
                .ToList()
                ?? new List<ProductUnitWriteDto>();
        }

        private async Task<bool> ValidateInvoiceStockAvailabilityAsync()
        {
            foreach (var line in InvoiceLines.Where(l => l.Quantity > 0).ToList())
            {
                var stockResult = await _stockService.GetAllWriteDtoWithFilteringAndIncludeAsync(
                    s => s.ProductId == line.ProductId && s.ProductUnitId == line.ProductUnitId);

                var stock = stockResult?.Data?.FirstOrDefault();
                if (stock != null)
                    line.UnitCost = stock.PurchasePrice;

                var availableQuantity = await GetAvailableQuantityForProductUnitAsync(line.ProductId, line.ProductUnitId);
                if (availableQuantity <= 0)
                {
                    MessageBox.Show(
                        UiText.T(
                            $"الصنف {line.ProductName} غير موجود في المخزون. لن يتم حفظ الفاتورة.",
                            $"The item {line.ProductName} was not found in stock. The invoice will not be saved."),
                        UiText.T("تنبيه", "Notice"));
                    return false;
                }

                if (availableQuantity >= line.Quantity)
                {
                    RecalculateLineAmounts(line);
                    continue;
                }

                if (availableQuantity > 0)
                {
                    line.Quantity = availableQuantity;
                    line.BaseQuantity = availableQuantity * (line.QuantityPerUnitSnapshot > 0 ? line.QuantityPerUnitSnapshot : 1m);
                    RecalculateLineAmounts(line);
                    MessageBox.Show(
                        UiText.T(
                            $"الكمية المطلوبة للصنف {line.ProductName} غير متوفرة. تم تعديل الكمية إلى الحد الأقصى المتاح: {availableQuantity:0.00000}",
                            $"The requested quantity for {line.ProductName} is not available. The quantity was adjusted to the maximum available: {availableQuantity:0.00000}"),
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
                        UnitNameSnapshot = sourceLine.UnitNameSnapshot ?? sourceLine.UnitName,
                        Quantity = allocation.Quantity,
                        QuantityPerUnitSnapshot = allocation.QuantityPerUnitSnapshot,
                        BaseQuantity = allocation.BaseQuantity,
                        UnitPrice = sourceLine.UnitPrice,
                        UnitCost = allocation.PurchasePrice > 0m
                            ? allocation.PurchasePrice
                            : sourceLine.UnitCost,
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

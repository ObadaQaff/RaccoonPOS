using RaccoonWarehouse.Data;
using RaccoonWarehouse.Application.Service.Purchases;
using RaccoonWarehouse.Core.Interface;
#region Usings
using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.ProductUnits;
using RaccoonWarehouse.Application.Service.PurchaseOrders;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.StockTransactions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Common;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Cashiers.DTOs;
using RaccoonWarehouse.Domain.Checks.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.InvoiceLines;
using RaccoonWarehouse.Domain.FinancialTransactions.DTOs;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.PurchaseOrders.DTOs;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.Stock;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Domain.StockTransactions.DTOs;
using RaccoonWarehouse.Helpers.Pdf;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Products;
using Microsoft.Extensions.DependencyInjection;
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
using System.Windows.Media;
#endregion
namespace RaccoonWarehouse.Invoices
{
    public partial class PayInvoice : Window
    {
        private const int ProductSelectionDelayMs = 150;
        // ====== مجموعات للـ Binding ======
        public ObservableCollection<ProductReadDto> Products { get; set; } = new();
        public ObservableCollection<InvoiceLineWriteDto> InvoiceLines { get; set; } = new();

        private ObservableCollection<UserReadDto> _allSuppliers;
        private bool _isFilteringSuppliers;
        private List<InvoiceLineReadDto> _originalLines = new(); // to restore stock on update
        private List<CheckWriteDto> _currentChecks = new();

        private readonly IInvoiceService _invoicesService;
        private readonly IUserService _userService;
        private readonly IProductService _productService;
        private readonly IProductUnitService _productUnitService;
        private readonly IStockService _stockService;
        private readonly IStockTransactionService _stockTransactionService;
        private readonly IFinancialTransactionService _financialService;
        private readonly IUserSession _userSession;
        private readonly ILoadingService _loadingService;
        private readonly IPurchaseOrderService _purchaseOrderService;
        private bool _isPurchaseOrderMode;
        private bool _isLoadingUnits = false;
        private int _productSelectionVersion;
        private string _productSearchText = string.Empty;
        private bool _isRestoringProductSearchText;
        private readonly System.Threading.SemaphoreSlim _productSelectionSemaphore = new(1, 1);
        private readonly System.Threading.SemaphoreSlim _productLoadSemaphore = new(1, 1);
        private int? _currentInvoiceId = null;   // لتحديث الفاتورة بعد الحفظ الأول
        private bool _originalCostPriceIncludesTax;
        private readonly IUOW _purchaseUow;
        private readonly ApplicationDbContext _purchaseDb;
        private bool _isSavingPurchase;
        private bool _isLoadingProducts;
        private bool _closeRequestedWhileLoading;
        private bool _isRefreshingPurchaseTotals;

        public PayInvoice(
            IStockService stockService,
            IInvoiceService invoiceService,
            IUserService userService,
            IProductService productService,
            IProductUnitService productUnitService,
            IUserSession userSession,
            IStockTransactionService stockTransactionService,
            IFinancialTransactionService financialService,
            ILoadingService loadingService,
            IPurchaseOrderService purchaseOrderService,
            IUOW purchaseUow, ApplicationDbContext purchaseDb)
        {
            _purchaseDb = purchaseDb;
            _purchaseUow = purchaseUow;
            _stockService = stockService;
            _productService = productService;
            _productUnitService = productUnitService;
            _stockTransactionService = stockTransactionService;
            _invoicesService = invoiceService;
            _userService = userService;
            _userSession = userSession;
            _financialService = financialService;
            _loadingService = loadingService;
            _purchaseOrderService = purchaseOrderService;
            _isLoadingUnits = false;

            RaccoonWarehouse.Security.PermissionWindowGuard.Demand(GetType());
            InitializeComponent();
            // Keep legacy 1+2 bonus support in code/data, but use percentage bonus in purchase invoices.
            BonusRatioColumn.Visibility = Visibility.Collapsed;
            BonusPercentageColumn.Visibility = Visibility.Visible;
            RaccoonWarehouse.Security.PermissionWindowGuard.SetKeys(this, "PurchaseInvoice.View");
            UiText.ApplyWindow(this);
            DataContext = this;

            // Purchase invoices use the supplier's invoice number entered by the user.
            // Purchase orders keep their automatic numbering and set it in ConfigureAsPurchaseOrder().
            InvoiceNumberTextBox.Clear();

            // ربط الـ Grid
            ProductsGrid.ItemsSource = InvoiceLines;

            Loaded += PayInvoice_Loaded;
            Closing += PayInvoice_Closing;
            Closed += PayInvoice_Closed;
            CatalogRefreshNotifier.CatalogChanged += CatalogRefreshNotifier_CatalogChanged;
        }

        public void ConfigureAsPurchaseOrder()
        {
            RaccoonWarehouse.Security.PermissionWindowGuard.SetKeys(this, "PurchaseOrder.View");
            RaccoonWarehouse.Security.PermissionWindowGuard.SetKeys(SaveReceiptBtn, "PurchaseOrder.Create|PurchaseOrder.Edit");
            _isPurchaseOrderMode = true;
            InvoiceNumberTextBox.Text = GenerateInvoiceNumber();
            BonusRatioColumn.Visibility = Visibility.Collapsed;
            BonusPercentageColumn.Visibility = Visibility.Collapsed;
            ReceivedQuantityColumn.Visibility = Visibility.Collapsed;
            EffectiveCostColumn.Visibility = Visibility.Collapsed;
            SalePriceInputPanel.Visibility = Visibility.Collapsed;
            SalePriceColumn.Visibility = Visibility.Collapsed;
            var purchaseOrderTitle = UiText.T("المشتريات", "Purchases");
            Title = purchaseOrderTitle;
            if (HeaderBorder.Child is TextBlock headerText)
                headerText.Text = purchaseOrderTitle;
            InvoiceNumberLabel.Text = UiText.T("رقم المشتريات", "Purchase number");

            Title = UiText.T("المشتريات", "Purchases");
            SaveReceiptBtn.ToolTip = UiText.T("حفظ المشتريات", "Save purchases");
            if (SaveReceiptBtn.Content is Panel savePanel && savePanel.Children.OfType<TextBlock>().FirstOrDefault() is TextBlock saveText)
                saveText.Text = UiText.T("حفظ المشتريات", "Save purchases");
            if (FalconInvoiceNumberTextBox.Parent is Panel falconPanel)
                falconPanel.Visibility = Visibility.Collapsed;
            ChecksBtn.Visibility = Visibility.Collapsed;
            SearchInvoiceBtn.Visibility = Visibility.Collapsed;
            PrintBtn.Visibility = Visibility.Collapsed;
            NewInvoiceBtn.Visibility = Visibility.Collapsed;
        }

        private string GenerateInvoiceNumber()
        {
            return (DateTime.Now.Ticks % 90000 + 10000).ToString();
        }

        // ===================== LOAD DATA =====================
        private async void PayInvoice_Loaded(object sender, RoutedEventArgs e)
        {
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
                _loadingService.Show();
                loadingShown = true;

                UiText.ApplyTranslations(this);
                // The catalog uses an isolated scope, so warm it in parallel
                // with the legacy product dropdown load.
                var catalogWarmTask = ProductSearchWindow.WarmCatalogCacheAsync(_stockService);
                // المورّدين (نفس users لكن أنت تختار اللي يمثل الموردين)
                var result = await _userService.GetAllAsync();
                _allSuppliers = new ObservableCollection<UserReadDto>((result?.Data ?? new List<UserReadDto>()).GroupBy(user => user.Id).Select(group => group.First()));
                SupplierComboBox.ItemsSource = _allSuppliers;
                SupplierComboBox.SelectedIndex = -1;

                InvoiceDatePicker.SelectedDate = DateTime.Now;

                PaymentMethodComboBox.SelectedIndex = _isPurchaseOrderMode ? 3 : 0;
                UiText.ApplyTranslations(PaymentMethodComboBox);

                // Warm the shared product catalog while the invoice loading
                // window is visible, so the first product search is instant.
                // Keep loading visible until both product sources are ready.
                await Task.WhenAll(catalogWarmTask, LoadProductsAsync());
                UiText.ApplyTranslations(this);
            }
            catch (Exception ex)
            {
                HideLoadingIfShown();
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء تحميل البيانات", "An error occurred while loading data")}: {ex.Message}", UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                HideLoadingIfShown();
            }
        }

        private async Task LoadProductsAsync()
        {
            if (_isLoadingProducts)
                return;

            _isLoadingProducts = true;
            await _productLoadSemaphore.WaitAsync();
            try
            {
                if (_closeRequestedWhileLoading)
                    return;

                var result = await _productService.GetReadDtoPagedListAsync(
                  pageNumber: 1,
                  pageSize: 3000,
                  orderBy: q => q.OrderBy(p => p.Name),
                  includes: new Expression<Func<Product, object>>[]
                  {
                        p => p.ProductUnits,
                        p => p.Brand,
                        p => p.SubCategory
                  });

                Products.Clear();

                foreach (var stock in result.Items)
                {
                    if (stock != null)
                        Products.Add(stock);
                }

                ProductBox.ItemsSource = Products;
                ProductBox.DisplayMemberPath = "Name";
                ProductBox.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("خطأ عند تحميل المنتجات", "Error loading products")}: {ex.Message}", UiText.T("خطأ", "Error"));
            }
            finally
            {
                _productLoadSemaphore.Release();
                _isLoadingProducts = false;

                if (_closeRequestedWhileLoading && IsLoaded)
                {
                    Dispatcher.BeginInvoke(
                        new Action(Close),
                        System.Windows.Threading.DispatcherPriority.Background);
                }
            }
        }

        private async void CreateProductBtn_Click(object sender, RoutedEventArgs e)
        {
            var existingProductIds = Products.Select(product => product.Id).ToHashSet();

            CreateProduct? createWindow = null;
            using (CatalogRefreshNotifier.SuppressNotifications())
            {
                WindowManager.ShowDialog<CreateProduct>(WindowSizeType.LargeRectangle, window => createWindow = window);
            }

            await LoadProductsAsync();

            var createdProduct = Products.FirstOrDefault(product => !existingProductIds.Contains(product.Id));
            if (createdProduct != null)
                ProductBox.SelectedItem = createdProduct;

            if (createWindow?.CreatedProductId is int createdProductId)
            {
                ProductBox.SelectedValue = createdProductId;
                await QueueProductSelectionAsync(createdProductId);
            }
        }

        private async void CatalogRefreshNotifier_CatalogChanged(object? sender, EventArgs e)
        {
            if (!IsLoaded)
                return;

            await LoadProductsAsync();
        }

        private void PayInvoice_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1)
            {
                SearchProductBtn_Click(this, new RoutedEventArgs());
                e.Handled = true;
                return;
            }

            if (e.Key == Key.F2)
            {
                SaveReceiptBtn_Click(SaveReceiptBtn, new RoutedEventArgs());
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Delete)
            {
                DeleteProduct_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void ExpiryDatePicker_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is DatePicker datePicker)
                QueueDatePickerTextSelection(datePicker, System.Windows.Threading.DispatcherPriority.ContextIdle);
        }

        private void ExpiryDatePicker_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is DatePicker datePicker)
                QueueDatePickerTextSelection(datePicker, System.Windows.Threading.DispatcherPriority.Input);
        }

        private void QueueDatePickerTextSelection(DatePicker datePicker, System.Windows.Threading.DispatcherPriority priority)
        {
            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    datePicker.ApplyTemplate();
                    if (datePicker.Template.FindName("PART_TextBox", datePicker) is TextBoxBase textBox)
                    {
                        if (!textBox.IsKeyboardFocusWithin)
                            textBox.Focus();

                        textBox.SelectAll();
                    }
                }),
                priority);
        }

        private void PayInvoice_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isLoadingProducts)
                return;

            _closeRequestedWhileLoading = true;
            e.Cancel = true;
        }

        private void PayInvoice_Closed(object? sender, EventArgs e)
        {
            CatalogRefreshNotifier.CatalogChanged -= CatalogRefreshNotifier_CatalogChanged;
        }
        private bool TryGetActiveCashierSession(out CashierSessionReadDto? session)
        {
            session = _userSession.CurrentCashierSession;
            if (session?.Status == SessionStatus.Open)
                return true;

            MessageBox.Show(UiText.T("لا توجد جلسة كاشير مفتوحة. الرجاء فتح جلسة أولاً.", "There is no open cashier session. Please open a session first."), UiText.T("خطأ", "Error"));
            return false;
        }

        private bool CostPriceIncludesTax => CostPriceIncludesTaxCheckBox.IsChecked == true;

        private static decimal GetEffectivePurchaseCost(InvoiceLineWriteDto line, bool includesTax)
        {
            if (!includesTax || line.TaxExempt || line.TaxRate <= 0m)
                return line.UnitCost;

            return Math.Round(line.UnitCost / (1m + line.TaxRate / 100m), 3);
        }

        private void PreparePurchasePricesForSave()
        {
            if (CostPriceIncludesTax)
                return;

            foreach (var line in InvoiceLines)
            {
                if (line.TaxExempt || line.TaxRate <= 0m)
                    continue;

                var factor = 1m + line.TaxRate / 100m;
                line.UnitPrice = Math.Round(line.UnitPrice * factor, 3);
                line.UnitCost = Math.Round(line.UnitCost * factor, 3);
                line.LineDiscountAmount = Math.Round(line.LineDiscountAmount * factor, 3);
            }

            CostPriceIncludesTaxCheckBox.IsChecked = true;
        }

        private IEnumerable<StockMovementPostDto> BuildInvoiceStockMovements(
            IEnumerable<InvoiceLineWriteDto> lines,
            TransactionType transactionType,
            int? invoiceId,
            int? cashierId,
            int? cashierSessionId,
            string notes,
            decimal multiplier,
            bool costPriceIncludesTax = false)
        {
            return lines
                .Where(line => line.ProductId > 0 && line.ProductUnitId > 0
                    && (line.Quantity != 0 || (line.FreeQuantity != 0 && !line.BonusPercentage.HasValue)))
                .SelectMany(line =>
                {
                    var quantityPerUnit = line.QuantityPerUnitSnapshot > 0 ? line.QuantityPerUnitSnapshot : 1m;
                    var paidBaseQuantity = line.BaseQuantity != 0 ? line.BaseQuantity : line.Quantity * quantityPerUnit;
                    var freeQuantity = line.BonusPercentage.HasValue ? 0m : line.FreeQuantity;
                    var profileSalePrice = GetProductUnitSalePrice(line.ProductId, line.ProductUnitId);
                    var salePrice = line.SalePrice > 0m ? line.SalePrice : profileSalePrice;
                    var movements = new List<StockMovementPostDto>();
                    if (line.PurchaseAcquisitionCost.HasValue)
                    {
                        var received = line.Quantity + freeQuantity;
                        movements.Add(new StockMovementPostDto
                        {
                            ProductId = line.ProductId, ProductUnitId = line.ProductUnitId,
                            Quantity = received * multiplier, QuantityPerUnitSnapshot = quantityPerUnit,
                            BaseQuantity = received * quantityPerUnit * multiplier,
                            UnitPrice = line.UnitPrice, PurchaseAcquisitionCost = line.PurchaseAcquisitionCost,
                            PurchasePrice = PurchaseBonusCosting.EffectiveUnitCost(line.PurchaseAcquisitionCost.Value, received),
                            SourcePurchaseLineId = line.Id, SalePrice = salePrice,
                            ExpiryDate = line.ExpiryDate, TransactionType = transactionType,
                            UpdateCatalogAverageCost = false, InvoiceId = invoiceId,
                            CasherId = cashierId, CashierSessionId = cashierSessionId,
                            TransactionDate = DateTime.Now, Notes = notes
                        });
                        return movements;
                    }


                    if (line.Quantity != 0)
                    {
                        movements.Add(new StockMovementPostDto
                        {
                            ProductId = line.ProductId,
                            ProductUnitId = line.ProductUnitId,
                            Quantity = line.Quantity * multiplier,
                            QuantityPerUnitSnapshot = quantityPerUnit,
                            BaseQuantity = paidBaseQuantity * multiplier,
                            UnitPrice = line.UnitPrice,
                            PurchasePrice = GetEffectivePurchaseCost(line, costPriceIncludesTax),
                            SalePrice = salePrice,
                            ExpiryDate = line.ExpiryDate,
                            TransactionType = transactionType,
                            UpdateCatalogAverageCost = true,
                            InvoiceId = invoiceId,
                            CasherId = cashierId,
                            CashierSessionId = cashierSessionId,
                            TransactionDate = DateTime.Now,
                            Notes = notes
                        });
                    }

                    if (freeQuantity > 0)
                    {
                        movements.Add(new StockMovementPostDto
                        {
                            ProductId = line.ProductId,
                            ProductUnitId = line.ProductUnitId,
                            Quantity = freeQuantity * multiplier,
                            QuantityPerUnitSnapshot = quantityPerUnit,
                            BaseQuantity = freeQuantity * quantityPerUnit * multiplier,
                            UnitPrice = 0m,
                            PurchasePrice = 0m,
                            SalePrice = salePrice,
                            ExpiryDate = line.ExpiryDate,
                            TransactionType = transactionType,
                            UpdateCatalogAverageCost = false,
                            InvoiceId = invoiceId,
                            CasherId = cashierId,
                            CashierSessionId = cashierSessionId,
                            TransactionDate = DateTime.Now,
                            Notes = notes + " (free quantity / كمية مجانية)"
                        });
                    }

                    return movements;
                });
        }

        private decimal GetProductUnitSalePrice(int productId, int productUnitId)
        {
            var product = Products.FirstOrDefault(item => item.Id == productId);
            var unit = product?.ProductUnits?.FirstOrDefault(item => item.Id == productUnitId);
            return unit?.SalePrice ?? product?.DefaultSalePrice ?? 0m;
        }

        // ===================== SUPPLIER SEARCH =====================
        private void SupplierComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            SupplierComboBox.DisplayMemberPath = "Name";
            SupplierComboBox.SelectedValuePath = "Id";
        }

        private async void AddSupplierBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WindowManager.ShowDialog<CreateUser>(WindowSizeType.MediumRectangle, window => window.InitializeForSupplierQuickCreate(SupplierComboBox.Text));
                var result = await _userService.GetAllAsync();
                _allSuppliers = new ObservableCollection<UserReadDto>((result?.Data ?? new List<UserReadDto>()).GroupBy(user => user.Id).Select(group => group.First()));
                SupplierComboBox.ItemsSource = _allSuppliers;
                SupplierComboBox.SelectedItem = null;
                SupplierComboBox.SelectedIndex = -1;
                SupplierComboBox.Text = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("تعذر إضافة المورد", "Could not add the supplier")}: {ex.Message}", UiText.T("خطأ", "Error"));
            }
        }
        private void SupplierComboBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            SupplierComboBox.SelectedItem = null;
            Dispatcher.BeginInvoke(new Action(() => FilterSupplierList(SupplierComboBox.Text)), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void SupplierComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back || e.Key == Key.Delete)
            {
                SupplierComboBox.SelectedItem = null;
                FilterSupplierList(SupplierComboBox.Text);
            }
        }

        private void FilterSupplierList(string text)
        {
            if (_allSuppliers == null) return;

            var filtered = _allSuppliers
                .Where(c => c.Name != null &&
                            c.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
                .GroupBy(c => c.Id)
                .Select(group => group.First())
                .ToList();

            _isFilteringSuppliers = true;
            try
            {
                SupplierComboBox.ItemsSource = filtered;
                SupplierComboBox.SelectedItem = null;
                SupplierComboBox.SelectedIndex = -1;
                SupplierComboBox.Text = text;
                SupplierComboBox.IsDropDownOpen = filtered.Count > 0;
            }
            finally
            {
                _isFilteringSuppliers = false;
            }
        }

        // ===================== PRODUCT & UNIT =====================
        private void ProductBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Selection changes while the user navigates the result list are not confirmed until Enter is pressed.
        }

        private async void ProductBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Up or Key.Down && ProductBox.IsDropDownOpen)
            {
                RestoreTypedProductSearchTextAfterNavigation();
                return;
            }

            if (e.Key != Key.Enter)
                return;

            await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);

            var product = ResolveProductChoice();
            if (product == null)
                return;

            e.Handled = true;
            _isRestoringProductSearchText = true;
            ProductBox.Text = product.Name ?? string.Empty;
            _productSearchText = ProductBox.Text;
            _isRestoringProductSearchText = false;
            ProductBox.IsDropDownOpen = false;
            await QueueProductSelectionAsync(product.Id);
        }

        private async void ProductBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isLoadingUnits || !ProductBox.IsDropDownOpen)
                return;

            await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Background);

            var product = ResolveProductChoice();
            if (product == null)
                return;

            _isRestoringProductSearchText = true;
            ProductBox.Text = product.Name ?? string.Empty;
            _productSearchText = ProductBox.Text;
            _isRestoringProductSearchText = false;
            ProductBox.IsDropDownOpen = false;
            await QueueProductSelectionAsync(product.Id);
        }

        private ProductReadDto? ResolveProductChoice()
        {
            if (ProductBox.SelectedItem is ProductReadDto selectedProduct)
                return selectedProduct;

            var searchText = ProductBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(searchText))
                return null;

            var exactMatch = Products.FirstOrDefault(product =>
                string.Equals(product.Name?.Trim(), searchText, StringComparison.OrdinalIgnoreCase) ||
                product.ITEMCODE.ToString() == searchText);

            if (exactMatch != null)
                return exactMatch;

            return ProductBox.ItemsSource is IEnumerable<ProductReadDto> filteredProducts
                ? filteredProducts.FirstOrDefault()
                : null;
        }

        private void RestoreTypedProductSearchTextAfterNavigation()
        {
            var typedText = _productSearchText;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _isRestoringProductSearchText = true;
                ProductBox.Text = typedText;
                _isRestoringProductSearchText = false;
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private async Task QueueProductSelectionAsync(int productId)
        {
            if (productId <= 0)
                return;

            var selectionVersion = System.Threading.Interlocked.Increment(ref _productSelectionVersion);
            await LoadSelectedProductAsync(productId, selectionVersion);
        }

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

                // 🔷 Load units from DB
                var unitsResult = await _productUnitService
                    .GetAllWriteDtoWithFilteringAndIncludeAsync(
                        pu => pu.ProductId == productId,
                        pu => pu.Unit);

                UnitBox.ItemsSource = unitsResult.Data;

                // 🔷 Auto-select default purchase unit if exists
                var defaultUnit = ProductUnitSelector.GetDefaultPurchaseUnit(unitsResult.Data);
                if (defaultUnit != null)
                {
                    UnitBox.SelectedValue = defaultUnit.Id;
                    PurchaseBox.Text = defaultUnit.PurchasePrice.ToString();
                    SaleBox.Text = defaultUnit.SalePrice.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("خطأ أثناء تحميل الوحدات", "Error while loading units")}: {ex.Message}", UiText.T("خطأ", "Error"));
            }
            finally
            {
                _isLoadingUnits = false;
                _productSelectionSemaphore.Release();
            }
        }

        private void UnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UnitBox.SelectedItem is not ProductUnitWriteDto unit)
                return;

            PurchaseBox.Text = unit.PurchasePrice.ToString("0.000");
            SaleBox.Text = unit.SalePrice.ToString("0.000");
        }

        private void ClearProductInputs()
        {
            ProductBox.SelectedIndex = -1;
            UnitBox.ItemsSource = null;
            QtyBox.Text = "";
            PurchaseBox.Text = "";
            SaleBox.Text = "";
            ExpiryBox.SelectedDate = null;
        }

        private async Task<bool> AddProductFromSearchAsync(PurchaseProductSearchWindow.PurchaseProductSearchRow row)
        {
            if (row.ProductUnitId <= 0)
            {
                MessageBox.Show(UiText.T("لا توجد وحدة شراء معرفة لهذا المنتج.", "No purchase unit is defined for this product."), UiText.T("تنبيه", "Notice"));
                return false;
            }

            InvoiceLines.Add(new InvoiceLineWriteDto
            {
                ProductId = row.Product.Id,
                ProductName = row.Product.Name,
                ProductUnitId = row.ProductUnitId,
                QuantityPerUnitSnapshot = row.QuantityPerUnit,
                BaseQuantity = row.Quantity * row.QuantityPerUnit,
                UnitName = row.UnitName,
                Quantity = row.Quantity,
                UnitPrice = row.PurchasePrice,
                UnitCost = row.PurchasePrice,
                SalePrice = row.SalePrice,
                TaxExempt = row.Product.TaxExempt ?? false,
                TaxRate = row.Product.TaxRate ?? 0m,
                ExpiryDate = row.ExpiryDate,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            });

            UpdateTotal();
            return true;
        }        // ===================== ADD PRODUCT LINE =====================
        private void AddProduct_Click(object sender, RoutedEventArgs e)
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

            if (!ExpiryBox.SelectedDate.HasValue)
            {
                MessageBox.Show(UiText.T("تاريخ الانتهاء مطلوب.", "The expiry date is required."), UiText.T("تنبيه", "Notice"));
                return;
            }

            if (!TryParseDecimalInput(SaleBox.Text, out var salePrice) || salePrice < 0m)
            {
                MessageBox.Show(UiText.T("سعر البيع غير صحيح.", "The sale price is invalid."), UiText.T("تنبيه", "Notice"));
                return;
            }

            // In purchase invoices, UnitPrice is the purchase price and SalePrice is the price for the received stock.
            var line = new InvoiceLineWriteDto
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductUnitId = unit.Id,
                QuantityPerUnitSnapshot = unit.QuantityPerUnit > 0 ? unit.QuantityPerUnit : 1m,
                BaseQuantity = qty * (unit.QuantityPerUnit > 0 ? unit.QuantityPerUnit : 1m),
                UnitName = unit.Unit?.Name,
                Quantity = qty,
                UnitPrice = TryParseDecimalInput(PurchaseBox.Text, out var p) ? p : 0,
                UnitCost = TryParseDecimalInput(PurchaseBox.Text, out var cost) ? cost : 0,
                SalePrice = salePrice,
                TaxExempt = product.TaxExempt ?? false,
                TaxRate = product.TaxRate ?? 0m,
                ExpiryDate = ExpiryBox.SelectedDate.Value,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            InvoiceLines.Add(line);
            UpdateTotal();
            ClearProductInputs();
        }

        private void ProductsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit || e.Row.Item is not InvoiceLineWriteDto line)
                return;

            var property = (e.Column as DataGridBoundColumn)?.Binding is System.Windows.Data.Binding binding
                ? binding.Path?.Path : null;
            if (e.EditingElement is TextBox textBox)
            {
                if (property == nameof(InvoiceLineWriteDto.Quantity))
                {
                    if (!TryParseDecimalInput(textBox.Text, out var quantity) || quantity <= 0)
                    {
                        MessageBox.Show(UiText.T("الكمية يجب أن تكون أكبر من صفر.", "Quantity must be greater than zero."), UiText.T("تنبيه", "Notice"));
                        line.Quantity = 1m;
                    }
                    else
                    {
                        line.Quantity = quantity;
                        line.BaseQuantity = quantity * (line.QuantityPerUnitSnapshot > 0 ? line.QuantityPerUnitSnapshot : 1m);
                    }
                }
                else if (property == nameof(InvoiceLineWriteDto.UnitPrice))
                {
                    if (!TryParseDecimalInput(textBox.Text, out var price) || price < 0)
                    {
                        MessageBox.Show(UiText.T("السعر يجب أن يكون رقماً صحيحاً غير سالب.", "Price must be a valid non-negative number."), UiText.T("تنبيه", "Notice"));
                        line.UnitPrice = line.UnitCost;
                    }
                    else
                    {
                        line.UnitPrice = price;
                        line.UnitCost = price;
                    }
                }
                else if (property == nameof(InvoiceLineWriteDto.SalePrice))
                {
                    if (!TryParseDecimalInput(textBox.Text, out var salePrice) || salePrice < 0m)
                    {
                        MessageBox.Show(UiText.T("سعر البيع يجب أن يكون رقماً غير سالب.", "Sale price must be a valid non-negative number."), UiText.T("تنبيه", "Notice"));
                        line.SalePrice = GetProductUnitSalePrice(line.ProductId, line.ProductUnitId);
                    }
                    else
                    {
                        line.SalePrice = salePrice;
                    }
                }
                else if (property == nameof(InvoiceLineWriteDto.LineDiscountInput))
                {
                    var grossLineTotal = Math.Max(0m, line.Quantity * line.UnitPrice);
                    var rawDiscount = textBox.Text.Trim();
                    if (!TryParseDiscountInput(rawDiscount, grossLineTotal, out var discount, out var isPercentage, out var percentage))
                    {
                        MessageBox.Show(UiText.T("خصم السطر يجب أن يكون رقماً غير سالب.", "Line discount must be a non-negative number."), UiText.T("تنبيه", "Notice"));
                        line.LineDiscountIsPercentage = false;
                        line.LineDiscountAmount = 0m;
                        line.LineDiscountInput = "0";
                    }
                    else
                    {
                        line.LineDiscountIsPercentage = isPercentage;
                        if (isPercentage)
                            line.LineDiscountPercent = percentage;
                        else
                            line.LineDiscountAmount = discount;
                        line.LineDiscountInput = isPercentage
                            ? $"{percentage:0.###}%"
                            : $"{discount:0.000}";
                    }
                }
                else if (property == nameof(InvoiceLineWriteDto.FreeQuantity))
                {
                    if (!TryParseDecimalInput(textBox.Text, out var freeQuantity) || freeQuantity < 0)
                    {
                        MessageBox.Show(UiText.T("الكمية المجانية يجب أن تكون رقماً غير سالب.", "Free quantity must be a non-negative number."), UiText.T("تنبيه", "Notice"));
                        line.FreeQuantity = 0m;
                    }
                    else
                    {
                        // An explicit manual edit replaces any active bonus rule.
                        line.BonusRatio = null;
                        line.BonusPercentage = null;
                        line.BonusPercentageInput = string.Empty;
                        line.FreeQuantity = freeQuantity;
                    }
                }
            }

            // CellEditEnding runs before WPF has finished committing the binding.
            // Recalculate on the dispatcher after the current row values are live.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (IsLoaded)
                    RecalculatePurchaseTotalsFromCurrentRows();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void ProductsGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers != ModifierKeys.None
                || !IsCellTypingKey(e.Key)
                || ProductsGrid.CurrentCell.Column?.IsReadOnly != false)
                return;

            // Once a cell editor has focus, let the textbox handle subsequent
            // keystrokes normally. Re-selecting all text on every key would
            // replace the previous digit (for example, typing 10 becomes 1).
            if (e.OriginalSource is TextBoxBase editor && editor.IsKeyboardFocusWithin)
                return;

            // Start editing as soon as the user begins typing in the selected cell.
            // Leave the key unhandled so the first character reaches the editor.
            ProductsGrid.BeginEdit();

            if (e.OriginalSource is DependencyObject source)
            {
                var cell = FindVisualParent<DataGridCell>(source);
                if (cell != null)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var editor = FindVisualChild<TextBox>(cell);
                        if (editor != null)
                        {
                            editor.Focus();
                            editor.SelectAll();
                        }
                    }), System.Windows.Threading.DispatcherPriority.Input);
                }
            }
        }

        private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T match)
                    return match;
                child = VisualTreeHelper.GetParent(child);
            }

            return null;
        }

        private static T? FindVisualChild<T>(DependencyObject? parent) where T : DependencyObject
        {
            if (parent == null)
                return null;

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T match)
                    return match;

                var nestedMatch = FindVisualChild<T>(child);
                if (nestedMatch != null)
                    return nestedMatch;
            }

            return null;
        }

        private static bool IsCellTypingKey(Key key)
        {
            return key is Key.Back or Key.Delete or Key.Space
                or Key.Decimal or Key.Add or Key.Subtract or Key.Multiply or Key.Divide
                or Key.OemPlus or Key.OemMinus or Key.OemPeriod or Key.OemComma
                || key is >= Key.D0 and <= Key.D9
                || key is >= Key.NumPad0 and <= Key.NumPad9
                || key is >= Key.A and <= Key.Z;
        }

        private void ApplyBonusPercentage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: InvoiceLineWriteDto line }) return;
            ApplyBonusPercentage(line);
        }

        private void BonusPercentageInput_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox
                && textBox.DataContext is InvoiceLineWriteDto line
                && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                var input = textBox.Text;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ApplyBonusPercentage(line, input, showMessage: false);
                }), System.Windows.Threading.DispatcherPriority.DataBind);
            }
        }

        private void BonusPercentageInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || sender is not TextBox textBox || textBox.DataContext is not InvoiceLineWriteDto line)
                return;

            e.Handled = true;
            ApplyBonusPercentage(line, textBox.Text);
        }

        private void ApplyBonusPercentage(InvoiceLineWriteDto line, string? input = null, bool showMessage = true)
        {
            // The bonus editor is hosted in the cell template, so the row model
            // can still contain the previous value while focus is changing.
            // Always apply the text currently displayed by the editor first.
            if (input != null)
                line.BonusPercentageInput = input;

            if (!line.ApplyBonusPercentage())
            {
                if (!showMessage)
                    return;

                MessageBox.Show(
                    UiText.T("نسبة البونص غير صالحة. استخدم قيمة مثل 10% أو 110%.", "Invalid bonus percentage. Use a value such as 10% or 110%."),
                    UiText.T("تنبيه", "Notice"));
                return;
            }
            UpdateTotal();
        }

        private void ApplyBonus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: InvoiceLineWriteDto line }) return;
            ProductsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            ProductsGrid.CommitEdit(DataGridEditingUnit.Row, true);
            if (!line.ApplyBonus())
            {
                MessageBox.Show(UiText.T("عرض البونص غير صالح؛ مثال: 3+1. استخدم أعداداً صحيحة موجبة.",
                    "Invalid bonus ratio; use positive whole numbers, e.g. 3+1."), UiText.T("تنبيه", "Notice"));
                return;
            }
            UpdateTotal();
        }

        private void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is InvoiceLineWriteDto line)
            {
                InvoiceLines.Remove(line);
                UpdateTotal();
            }
        }

        private (decimal Subtotal, decimal Tax, decimal Discount, decimal Total) CalculatePurchaseTotals()
        {
            decimal subtotal = 0m;
            foreach (var line in InvoiceLines)
            {
                var grossLineTotal = Math.Max(0m, line.Quantity * line.UnitPrice);
                if (line.LineDiscountIsPercentage)
                {
                    var discountPercent = Math.Clamp(line.LineDiscountPercent, 0m, 100m);
                    var bonusEquivalentLineTotal = Math.Round(
                        grossLineTotal / (1m + discountPercent / 100m),
                        3,
                        MidpointRounding.AwayFromZero);
                    line.LineDiscountAmount = Math.Round(
                        grossLineTotal - bonusEquivalentLineTotal,
                        3,
                        MidpointRounding.AwayFromZero);
                }
                else
                {
                    line.LineDiscountAmount = Math.Clamp(line.LineDiscountAmount, 0m, grossLineTotal);
                }
                line.FreeQuantity = Math.Max(0m, line.FreeQuantity);
                var lineTotal = Math.Round(grossLineTotal - line.LineDiscountAmount, 3);
                lineTotal = ApplyPercentageBonusToPurchaseCost(line, lineTotal);
                var divisor = CostPriceIncludesTax && !line.TaxExempt && line.TaxRate > 0m
                    ? 1m + line.TaxRate / 100m
                    : 1m;
                line.LineSubTotal = Math.Round(lineTotal / divisor, 3);
                subtotal += line.LineSubTotal;
            }

            subtotal = Math.Round(subtotal, 3);
            if (!TryParseDiscountInput(
                    DiscountTextBox.Text,
                    subtotal,
                    out var discount,
                    out _,
                    out _))
            {
                discount = 0m;
            }

            var taxableRatio = subtotal > 0m ? (subtotal - discount) / subtotal : 0m;
            decimal tax = 0m;
            foreach (var line in InvoiceLines)
            {
                var extractedTax = CostPriceIncludesTax && !line.TaxExempt && line.TaxRate > 0m
                    ? Math.Round((line.LineSubTotal * line.TaxRate / 100m), 3)
                    : 0m;
                line.TaxAmount = line.TaxExempt || line.TaxRate <= 0m
                    ? 0m
                    : Math.Round((CostPriceIncludesTax ? extractedTax : line.LineSubTotal * line.TaxRate / 100m) * taxableRatio, 3);
                line.RefreshCalculatedProperties();
                tax += line.TaxAmount;
            }

            if (InvoiceLines.All(line => !line.BonusNeedsApply))
                PurchaseBonusCosting.Allocate(InvoiceLines, discount, CostPriceIncludesTax);
            tax = Math.Round(tax, 3);
            return (subtotal, tax, discount, Math.Round(subtotal - discount + tax, 3));
        }

        private void CostPriceIncludesTaxCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                UpdateTotal();
                ProductsGrid.Items.Refresh();
            }
        }

        private void UpdateTotal()
        {
            RecalculatePurchaseTotalsFromCurrentRows();
        }

        private void RecalculatePurchaseTotalsFromCurrentRows()
        {
            if (_isRefreshingPurchaseTotals)
                return;

            _isRefreshingPurchaseTotals = true;
            try
            {
                // CalculatePurchaseTotals intentionally reads only InvoiceLines,
                // which is the live grid collection, never _originalLines or the
                // values loaded from the database.
                var totals = CalculatePurchaseTotals();
                SubtotalAmountTextBox.Text = totals.Subtotal.ToString("0.000");
                TaxAmountTextBox.Text = totals.Tax.ToString("0.000");
                TotalAmountTextBox.Text = totals.Total.ToString("0.000");
            }
            finally
            {
                _isRefreshingPurchaseTotals = false;
            }
        }

        private void DiscountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded)
                UpdateTotal();
        }

        private void DiscountTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            if (TryParseDiscountInput(
                    DiscountTextBox.Text,
                    CalculatePurchaseSubtotalBeforeInvoiceDiscount(),
                    out _,
                    out _,
                    out _))
            {
                return;
            }

            MessageBox.Show(
                UiText.T("الخصم يجب أن يكون مبلغاً غير سالب أو نسبة مئوية من 0% إلى 100%.",
                    "Discount must be a non-negative amount or a percentage from 0% to 100%.") ,
                UiText.T("تنبيه", "Notice"));
            DiscountTextBox.Text = "0";
            UpdateTotal();
        }

        private decimal CalculatePurchaseSubtotalBeforeInvoiceDiscount()
        {
            decimal subtotal = 0m;
            foreach (var line in InvoiceLines)
            {
                var grossLineTotal = Math.Max(0m, line.Quantity * line.UnitPrice);
                var lineDiscount = line.LineDiscountIsPercentage
                    ? grossLineTotal - Math.Round(
                        grossLineTotal / (1m + Math.Clamp(line.LineDiscountPercent, 0m, 100m) / 100m),
                        3,
                        MidpointRounding.AwayFromZero)
                    : Math.Clamp(line.LineDiscountAmount, 0m, grossLineTotal);
                var lineTotal = Math.Round(grossLineTotal - lineDiscount, 3);
                lineTotal = ApplyPercentageBonusToPurchaseCost(line, lineTotal);
                var divisor = CostPriceIncludesTax && !line.TaxExempt && line.TaxRate > 0m
                    ? 1m + line.TaxRate / 100m
                    : 1m;
                subtotal += Math.Round(lineTotal / divisor, 3);
            }

            return Math.Round(subtotal, 3);
        }

        private static decimal ApplyPercentageBonusToPurchaseCost(InvoiceLineWriteDto line, decimal lineTotal)
        {
            if (!line.BonusPercentage.HasValue || line.Quantity <= 0m)
                return lineTotal;

            var costQuantity = PurchaseBonusPercentage.CalculateCostQuantity(line.Quantity, line.BonusPercentage.Value);
            return costQuantity > 0m
                ? Math.Round(lineTotal * line.Quantity / costQuantity, 3, MidpointRounding.AwayFromZero)
                : lineTotal;
        }

        private bool ValidateInvoiceDiscountInput()
        {
            if (TryParseDiscountInput(
                    DiscountTextBox.Text,
                    CalculatePurchaseSubtotalBeforeInvoiceDiscount(),
                    out _,
                    out _,
                    out _))
            {
                return true;
            }

            MessageBox.Show(
                UiText.T("الخصم يجب أن يكون مبلغاً غير سالب أو نسبة مئوية من 0% إلى 100%.",
                    "Discount must be a non-negative amount or a percentage from 0% to 100%.") ,
                UiText.T("تنبيه", "Notice"));
            DiscountTextBox.Focus();
            DiscountTextBox.SelectAll();
            return false;
        }

        private static bool TryParseDiscountInput(
            string? rawInput,
            decimal baseAmount,
            out decimal amount,
            out bool isPercentage,
            out decimal percentage)
        {
            amount = 0m;
            isPercentage = false;
            percentage = 0m;

            var raw = rawInput?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw))
                return true;

            isPercentage = raw.EndsWith("%", StringComparison.Ordinal);
            var numericInput = isPercentage ? raw[..^1].Trim() : raw;
            if (!TryParseDecimalInput(numericInput, out var value) || value < 0m)
                return false;

            if (isPercentage)
            {
                if (value > 100m)
                    return false;

                percentage = value;
                amount = Math.Round(
                    Math.Max(0m, baseAmount) * value / 100m,
                    3,
                    MidpointRounding.AwayFromZero);
            }
            else
            {
                amount = value;
            }

            amount = Math.Clamp(amount, 0m, Math.Max(0m, baseAmount));
            return true;
        }

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

        private void UpdateChecksButtonVisibility()
        {
            ChecksBtn.Visibility = GetSelectedPaymentType() == PaymentType.Check || _currentChecks.Any()
                ? Visibility.Visible
                : Visibility.Collapsed;
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
            var invoiceAmount = CalculatePurchaseTotals().Total;
            if (_currentChecks.Count == 0 && !EditCurrentInvoiceChecks(invoiceAmount))
            {
                MessageBox.Show(
                    UiText.T("لا توجد شيكات لعرضها أو تعديلها.", "There are no checks to view or edit."),
                    UiText.T("تنبيه", "Notice"));
            }
        }

        private void PaymentMethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GetSelectedPaymentType() != PaymentType.Check && _currentChecks.Any())
            {
                _currentChecks.Clear();
            }

            UpdateChecksButtonVisibility();
        }

        // ===================== SAVE / UPDATE / PRINT =====================
        /*private async void SaveReceiptBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!InvoiceLines.Any())
                {
                    MessageBox.Show("يرجى إضافة منتج واحد على الأقل.", "تنبيه");
                    return;
                }

                if (SupplierComboBox.SelectedItem == null)
                {
                    MessageBox.Show("❌ يرجى اختيار المورّد.", "تنبيه");
                    return;
                }

                var supplier = SupplierComboBox.SelectedItem as UserReadDto;
                var purchaseTotals = CalculatePurchaseTotals();
                decimal totalAmount = purchaseTotals.Total;
                if (!TryGetActiveCashierSession(out var session))
                    return;

                bool isUpdate = _currentInvoiceId != null;

                var invoiceDto = new InvoiceWriteDto
                {
                    Id = _currentInvoiceId ?? 0,
                    InvoiceNumber = InvoiceNumberTextBox.Text,
                    FalconInvoiceNumber = FalconInvoiceNumberTextBox.Text.Trim(),
                    SupplierId = supplier?.Id,
                    InvoiceType = InvoiceType.Purchase,   // 👈 مشتريات
                    TotalAmount = totalAmount,
                                        SubTotal = purchaseTotals.Subtotal,
                    TotalTax = purchaseTotals.Tax,
                    DiscountAmount = purchaseTotals.Discount,
                    CostPriceIncludesTax = CostPriceIncludesTax,
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

                        // ✅ المشتريات: زيادة الكميات في المخزون
                        foreach (var line in InvoiceLines)
                            await UpdateStockQuantity(line.ProductId, line.ProductUnitId, line.Quantity);

                        MessageBox.Show("تم إنشاء فاتورة المشتريات بنجاح!");
                    }
                }
                else
                {
                    // ============ UPDATE ============
                    // 1️⃣ إعادة كميات الفاتورة القديمة (طرح من المخزون)
                    foreach (var old in _originalLines)
                        await UpdateStockQuantity(old.ProductId, old.ProductUnitId, -old.Quantity);

                    // 2️⃣ إضافة كميات الفاتورة الجديدة
                    var movementResult = await _stockService.PostMovementsAsync(
                        BuildInvoiceStockMovements(
                            InvoiceLines,
                            TransactionType.Purchase,
                            savedInvoiceId,
                            session.CashierId,
                            session.Id,
                            $"Purchase invoice #{invoiceDto.InvoiceNumber}",
                            1m,
                            CostPriceIncludesTax));
                    if (!movementResult.Success)
                    {
                        MessageBox.Show(movementResult.Message ?? "فشل تحديث المخزون.", "خطأ");
                        return;
                    }
                    await PostInvoiceStockTransactionsAsync(
                        InvoiceLines,
                        TransactionType.Purchase,
                        savedInvoiceId,
                        session.CashierId,
                        session.Id,
                        $"Purchase invoice #{invoiceDto.InvoiceNumber}",
                        1m);

                    var result = await _invoicesService.UpdateAsync(invoiceDto);

                    if (result.Success)
                        MessageBox.Show("تم تحديث فاتورة المشتريات بنجاح!");
                }

                PrintBtn.Visibility = Visibility.Visible;
                NewInvoiceBtn.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء حفظ الفاتورة:\n{ex.Message}");
            }
        }*/
        private async void SaveReceiptBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isSavingPurchase) return;
            _isSavingPurchase = true;
            var savedBeforeAttempt = _currentInvoiceId;
            var originalLineIds = InvoiceLines.Select(line => (Line: line, line.Id)).ToList();
            var transactionStarted = false;
            var committed = false;
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
                ProductsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                ProductsGrid.CommitEdit(DataGridEditingUnit.Row, true);
                PurchaseBonusCosting.Validate(InvoiceLines);
                if (!InvoiceLines.Any())
                {
                    MessageBox.Show("يرجى إضافة منتج واحد على الأقل.", "تنبيه");
                    return;
                }

                if (!ValidateInvoiceDiscountInput())
                    return;

                var selectedPaymentType = GetSelectedPaymentType();

                if (selectedPaymentType == PaymentType.Credit && SupplierComboBox.SelectedItem == null)
                {
                    MessageBox.Show(
                        UiText.T("يرجى اختيار المورد للفواتير الآجلة.", "Please select a supplier for credit purchases."),
                        UiText.T("تنبيه", "Notice"));
                    return;
                }

                if (MessageBox.Show(
                        _isPurchaseOrderMode
                            ? UiText.T("هل تريد حفظ أمر الشراء؟", "Do you want to save the purchase order?")
                            : UiText.T("هل تريد حفظ فاتورة المشتريات؟", "Do you want to save the purchase invoice?"),
                        UiText.T("تأكيد الحفظ", "Confirm save"),
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

                var supplier = SupplierComboBox.SelectedItem as UserReadDto; // أو UserReadDto للمورد
                var purchaseTotals = CalculatePurchaseTotals();
                decimal totalAmount = purchaseTotals.Total;
                PreparePurchasePricesForSave();
                purchaseTotals = CalculatePurchaseTotals();
                totalAmount = purchaseTotals.Total;

                if (_isPurchaseOrderMode)
                {
                    await SavePurchaseOrderAsync(supplier, purchaseTotals);
                    return;
                }

                if (!TryGetActiveCashierSession(out var session))
                    return;

                bool isUpdate = _currentInvoiceId != null;

                if (selectedPaymentType == PaymentType.Check)
                {
                    var currentCheckTotal = Math.Round(_currentChecks.Sum(check => check.Amount), 3);
                    var expectedCheckTotal = Math.Round(totalAmount, 3);

                    if (_currentChecks.Count == 0 || currentCheckTotal != expectedCheckTotal)
                    {
                        _loadingService.Hide();
                        loadingShown = false;
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
                        MessageBox.Show(
                            UiText.T(
                                $"مجموع الشيكات ({checkTotal:0.000}) يجب أن يساوي إجمالي الفاتورة ({totalAmount:0.000}).",
                                $"The total check amount ({checkTotal:0.000}) must equal the invoice total ({totalAmount:0.000})."),
                            UiText.T("تنبيه", "Notice"));
                        return;
                    }
                }

                _loadingService.Show();
                loadingShown = true;

                var invoiceDto = new InvoiceWriteDto
                {
                    Id = _currentInvoiceId ?? 0,
                    InvoiceNumber = InvoiceNumberTextBox.Text,
                    FalconInvoiceNumber = FalconInvoiceNumberTextBox.Text.Trim(),
                    SupplierId = supplier?.Id,
                    InvoiceType = InvoiceType.Purchase, // مهم
                    PaymentType = selectedPaymentType,
                    Checks = selectedPaymentType == PaymentType.Check ? _currentChecks : null,
                    TotalAmount = totalAmount,
                    DiscountAmount = purchaseTotals.Discount,
                    SubTotal = purchaseTotals.Subtotal,
                    TotalTax = purchaseTotals.Tax,
                    CostPriceIncludesTax = CostPriceIncludesTax,
CreatedDate = InvoiceDatePicker.SelectedDate.Value,
                    UpdatedDate = DateTime.Now,
                    InvoiceLines = InvoiceLines.ToList(),
                    CasherId = session.CashierId
                };

                await using var purchaseTransaction = await _purchaseUow.BeginTransactionAsync();
                transactionStarted = true;
                int savedInvoiceId;

                if (!isUpdate)
                {
                    // ============ CREATE ============
                    var result = await _invoicesService.CreateAsync(invoiceDto);

                    if (!result.Success)
                    {
                        HideLoadingIfShown();
                        MessageBox.Show(result.Message ?? "فشل إنشاء فاتورة المشتريات", "خطأ");
                        return;
                    }

                    _currentInvoiceId = result.Data.Id;
                    savedInvoiceId = result.Data.Id;

                    var movementResult = await _stockService.PostMovementsAsync(
                        BuildInvoiceStockMovements(
                            InvoiceLines,
                            TransactionType.Purchase,
                            savedInvoiceId,
                            session.CashierId,
                            session.Id,
                            $"Purchase invoice #{invoiceDto.InvoiceNumber}",
                            1m,
                            CostPriceIncludesTax));
                    if (!movementResult.Success)
                    {
                        HideLoadingIfShown();
                        MessageBox.Show(movementResult.Message ?? "فشل تحديث المخزون.", "خطأ");
                        return;
                    }

                    // ✅ POST financial (Purchase Invoice = OUT)
                    if (selectedPaymentType != PaymentType.Credit)
                    {
                        var postDto = new FinancialPostDto
                        {
                            Direction = TransactionDirection.Out,
                            Method = MapPaymentMethod(selectedPaymentType),
                            Amount = totalAmount,
                            TransactionDate = DateTime.Now,

                            SourceType = FinancialSourceType.PurchaseInvoice,
                            SourceId = savedInvoiceId,

                            CashierSessionId = session.Id,
                            CashierId = session.CashierId,

                            Notes = $"Purchase Invoice #{invoiceDto.InvoiceNumber}"
                        };

                        var postResult = await _financialService.PostAsync(postDto);
                        if (!postResult.Success)
                        {
                            HideLoadingIfShown();
                            MessageBox.Show(postResult.Message ?? "تم حفظ الفاتورة لكن فشل تسجيل الحركة المالية", "تحذير");
                            return;
                        }
                    }


                }
                else
                {
                    savedInvoiceId = _currentInvoiceId.Value;

                    // Save new line identities before creating their independent lots.
                    var result = await _invoicesService.UpdateAsync(invoiceDto);
                    if (!result.Success)
                    {
                        HideLoadingIfShown();
                        MessageBox.Show(result.Message ?? "فشل تحديث فاتورة المشتريات", "خطأ");
                        return;
                    }

                    // Validate original allocated lots before any reversal; stock service also enforces exact source quantities.
                    // 0) Void old financial transactions
                    var voidResult = await _financialService.VoidBySourceAsync(
                        FinancialSourceType.PurchaseInvoice,
                        savedInvoiceId,
                        "Purchase invoice updated"
                    );

                    if (!voidResult.Success)
                    {
                        HideLoadingIfShown();
                        MessageBox.Show(voidResult.Message ?? "فشل إلغاء الحركة المالية السابقة", "خطأ");
                        return;
                    }

                    // 1) رجّع أثر الفاتورة القديمة من المخزون (يعني اعكسها)
                    var reverseResult = await _stockService.PostMovementsAsync(
                        BuildInvoiceStockMovements(
                            _originalLines.Select(line => new InvoiceLineWriteDto
                            {
                                Id = line.Id,
                                ProductId = line.ProductId,
                                ProductUnitId = line.ProductUnitId,
                                Quantity = line.Quantity,
                                FreeQuantity = line.BonusPercentage.HasValue ? 0m : line.FreeQuantity,
                                BonusPercentage = line.BonusPercentage,
                                PurchaseAcquisitionCost = line.PurchaseAcquisitionCost,
                                QuantityPerUnitSnapshot = line.QuantityPerUnitSnapshot,
                                BaseQuantity = line.BaseQuantity,
                                UnitPrice = line.UnitPrice,
                                UnitCost = line.UnitCost,
                                TaxExempt = line.TaxExempt,
                                TaxRate = line.TaxRate
                            }),
                            TransactionType.Purchase,
                            savedInvoiceId,
                            session.CashierId,
                            session.Id,
                            $"Reverse purchase invoice #{invoiceDto.InvoiceNumber}",
                            -1m,
                            _originalCostPriceIncludesTax));
                    if (!reverseResult.Success)
                    {
                        HideLoadingIfShown();
                        MessageBox.Show(reverseResult.Message ?? "فشل عكس حركة المخزون.", "خطأ");
                        return;
                    }
                    // (إذا القديم كان يضيف للمخزون، فالعكس خصم: مرّر كمية موجبة)

                    // 2) طبّق الفاتورة الجديدة على المخزون (إضافة)
                    var applyResult = await _stockService.PostMovementsAsync(
                        BuildInvoiceStockMovements(
                            InvoiceLines,
                            TransactionType.Purchase,
                            savedInvoiceId,
                            session.CashierId,
                            session.Id,
                            $"Update purchase invoice #{invoiceDto.InvoiceNumber}",
                            1m,
                            CostPriceIncludesTax));
                    if (!applyResult.Success)
                    {
                        HideLoadingIfShown();
                        MessageBox.Show(applyResult.Message ?? "فشل تحديث حركة المخزون.", "خطأ");
                        return;
                    }

                    // 4) Post new OUT transaction
                    if (selectedPaymentType != PaymentType.Credit)
                    {
                        var postDto = new FinancialPostDto
                        {
                            Direction = TransactionDirection.Out,
                            Method = MapPaymentMethod(selectedPaymentType),
                            Amount = totalAmount,
                            TransactionDate = DateTime.Now,

                            SourceType = FinancialSourceType.PurchaseInvoice,
                            SourceId = savedInvoiceId,

                            CashierSessionId = session.Id,
                            CashierId = session.CashierId,

                            Notes = $"Purchase Invoice UPDATED #{invoiceDto.InvoiceNumber}"
                        };

                        var postResult = await _financialService.PostAsync(postDto);
                        if (!postResult.Success)
                        {
                            HideLoadingIfShown();
                            MessageBox.Show(postResult.Message ?? "تم تحديث الفاتورة لكن فشل تسجيل الحركة المالية الجديدة", "تحذير");
                            return;
                        }
                    }


                }

                if (supplier != null)
                {
                    var supplierResult = await _productService.AssignSupplierToProductsAsync(
                        InvoiceLines.Select(line => line.ProductId),
                        supplier.Id);
                    if (!supplierResult.Success)
                    {
                        HideLoadingIfShown();
                        MessageBox.Show(
                            supplierResult.Message ?? UiText.T("فشل تحديث موردي الأصناف.", "Could not update product suppliers."),
                            UiText.T("خطأ", "Error"));
                        return;
                    }
                }

                await purchaseTransaction.CommitAsync();
                committed = true;
                HideLoadingIfShown();
                MessageBox.Show(UiText.T("تم حفظ فاتورة المشتريات بنجاح.", "Purchase invoice saved successfully."), UiText.T("تنبيه", "Notice"));
                await AskForSupplierJoFotaraReferenceAsync(savedInvoiceId, supplier);
                NewInvoiceBtn_Click(this, new RoutedEventArgs());
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

                HideLoadingIfShown();
                MessageBox.Show($"خطأ أثناء حفظ الفاتورة:\n{details}");
            }
            finally
            {
                if (transactionStarted && !committed)
                {
                    _purchaseDb.ChangeTracker.Clear();
                    _currentInvoiceId = savedBeforeAttempt;
                    foreach (var entry in originalLineIds) entry.Line.Id = entry.Id;
                }
                _isSavingPurchase = false;
                HideLoadingIfShown();
            }
        }

        private async Task AskForSupplierJoFotaraReferenceAsync(int invoiceId, UserReadDto? supplier)
        {
            var choice = MessageBox.Show(
                UiText.T("هل قدم المورد فاتورة رسمية من جوافوترة؟", "Did the supplier provide an official JoFotara invoice?"),
                UiText.T("مرجع فاتورة المورد", "Supplier invoice reference"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (choice != MessageBoxResult.Yes)
                return;

            try
            {
                using var scope = ((RaccoonWarehouse.App)System.Windows.Application.Current).ServiceProvider.CreateScope();
                var window = scope.ServiceProvider.GetRequiredService<SupplierJoFotaraReferenceWindow>();
                window.Initialize(supplier?.Name);
                window.Owner = this;

                if (window.ShowDialog() != true)
                    return;

                var result = await _invoicesService.SaveSupplierJoFotaraReferenceAsync(
                    invoiceId,
                    window.InvoiceNumber,
                    window.InvoiceUuid,
                    window.QrPayload);

                if (!result.Success)
                {
                    MessageBox.Show(
                        result.Message,
                        UiText.T("تعذر حفظ المرجع", "Could not save reference"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    UiText.T($"تم حفظ فاتورة الشراء لكن تعذر حفظ مرجع جوافوترة:\n{ex.Message}", $"The purchase invoice was saved, but the JoFotara reference could not be saved:\n{ex.Message}"),
                    UiText.T("تعذر حفظ المرجع", "Could not save reference"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private async Task SavePurchaseOrderAsync(UserReadDto? supplier, (decimal Subtotal, decimal Tax, decimal Discount, decimal Total) totals)
        {
            if (supplier == null)
            {
                MessageBox.Show(UiText.T("يرجى اختيار المورد.", "Please select a supplier."), UiText.T("تنبيه", "Notice"));
                return;
            }

            _loadingService.Show();
            try
            {
                var dto = new PurchaseOrderWriteDto
                {
                    OrderNumber = InvoiceNumberTextBox.Text.Trim(),
                    SupplierId = supplier.Id,
                    PaymentType = GetSelectedPaymentType(),
                    DocumentDate = InvoiceDatePicker.SelectedDate ?? DateTime.Now,
                    SubTotal = totals.Subtotal,
                    TotalTax = totals.Tax,
                    DiscountAmount = totals.Discount,
                    TotalAmount = totals.Total,
                    CostPriceIncludesTax = CostPriceIncludesTax,
                    Lines = InvoiceLines.Select(line => new PurchaseOrderLineWriteDto
                    {
                        ProductId = line.ProductId,
                        ProductUnitId = line.ProductUnitId,
                        Quantity = line.Quantity,
                        QuantityPerUnitSnapshot = line.QuantityPerUnitSnapshot,
                        BaseQuantity = line.BaseQuantity,
                        UnitPrice = line.UnitPrice,
                        UnitCost = line.UnitCost,
                        LineDiscountAmount = line.LineDiscountAmount,
                        FreeQuantity = line.FreeQuantity,
                        ExpiryDate = line.ExpiryDate == DateTime.MinValue ? null : line.ExpiryDate,
                        TaxExempt = line.TaxExempt,
                        TaxRate = line.TaxRate,
                        TaxAmount = line.TaxAmount,
                        LineSubTotal = line.LineSubTotal
                    }).ToList()
                };

                var result = await _purchaseOrderService.CreateAsync(dto);
                if (!result.Success)
                {
                    _loadingService.Hide();
                    MessageBox.Show(result.Message ?? UiText.T("فشل حفظ أمر الشراء.", "Purchase order could not be saved."));
                    return;
                }

                _loadingService.Hide();
                MessageBox.Show(
                    UiText.T("تم حفظ أمر الشراء وإرساله للاعتماد.", "Purchase order submitted for approval."),
                    UiText.T("نجاح", "Success"));
                Close();
            }
            catch (Exception ex)
            {
                _loadingService.Hide();
                MessageBox.Show(ex.Message, UiText.T("خطأ", "Error"));
            }
            finally
            {
                _loadingService.Hide();
            }
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

        private PaymentMethod MapPaymentMethod(PaymentType paymentType)
        {
            return paymentType switch
            {
                PaymentType.Cash => PaymentMethod.Cash,
                PaymentType.Debit => PaymentMethod.BankTransfer,
                PaymentType.Check => PaymentMethod.Check,
                PaymentType.MobilePayment => PaymentMethod.MobilePayment,
                PaymentType.Credit => PaymentMethod.Credit,
                PaymentType.Master => PaymentMethod.Master,
                PaymentType.Visa => PaymentMethod.Visa,
                _ => PaymentMethod.Cash
            };
        }

        private async void PrintBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentInvoiceId == null)
            {
                MessageBox.Show(UiText.T("لا توجد فاتورة للطباعة.", "There is no invoice to print."));
                return;
            }

            try
            {
            var invoice = await _invoicesService.GetFullInvoiceByIdAsync(_currentInvoiceId.Value);

            if (invoice == null)
            {
                MessageBox.Show(UiText.T("الفاتورة غير موجودة.", "The invoice was not found."), UiText.T("خطأ", "Error"));
                return;
            }

            SavePurchaseInvoicePdf(invoice);
            }
            catch(Exception ex){MessageBox.Show(UiText.T("تعذرت طباعة الفاتورة: ","Invoice printing failed: ")+ex.Message);}
        }

        private void SavePurchaseInvoicePdf(InvoiceReadDto invoice)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF File (*.pdf)|*.pdf",
                FileName = $"PurchaseInvoice_{invoice.InvoiceNumber}.pdf"
            };

            if (dialog.ShowDialog() == true)
            {
                var path = dialog.FileName;

                // نفس SalesInvoice لكن عنوان "فاتورة مشتريات" داخل PdfGenerator
                PdfGenerator.PurchaseInvoice(invoice, path);

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
            // تقدر تعمل نفس SearchSalesInvoiceWindow لكن للمشتريات
            var searchWindow = new SearchSalesInvoiceWindow(
                _invoicesService,
                _allSuppliers ?? Enumerable.Empty<UserReadDto>(),
                false,
                null,
                _productService,
                _stockService,
                _userService)
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
                var sourcePurchaseLineIds = invoice.InvoiceLines
                    .Select(line => line.Id)
                    .Where(id => id > 0)
                    .ToList();

                var salePricesResult = await _stockService.GetPurchaseLineSalePricesAsync(sourcePurchaseLineIds);
                var lotSalePrices = salePricesResult.Success
                    ? salePricesResult.Data ?? new Dictionary<int, decimal>()
                    : new Dictionary<int, decimal>();

                LoadSelectedInvoice(invoice, lotSalePrices);
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private void LoadSelectedInvoice(
            InvoiceReadDto invoice,
            IReadOnlyDictionary<int, decimal>? lotSalePrices = null)
        {
            if (invoice == null) return;

            _currentInvoiceId = invoice.Id;
            _originalCostPriceIncludesTax = invoice.CostPriceIncludesTax;

            _originalLines = invoice.InvoiceLines.ToList();   // 🔥 مهم جداً

            InvoiceNumberTextBox.Text = invoice.InvoiceNumber;
            FalconInvoiceNumberTextBox.Text = invoice.FalconInvoiceNumber ?? string.Empty;
            InvoiceDatePicker.SelectedDate = invoice.CreatedDate;

            SupplierComboBox.SelectedItem =
                _allSuppliers.FirstOrDefault(c => c.Id == invoice.SupplierId);
            DiscountTextBox.Text = (invoice.DiscountAmount ?? 0m).ToString("0.000");
            CostPriceIncludesTaxCheckBox.IsChecked = invoice.CostPriceIncludesTax;
            if (invoice.PaymentType.HasValue)
                SetSelectedPaymentType(invoice.PaymentType.Value);
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
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    UnitCost = line.UnitCost,
                    SalePrice = lotSalePrices != null
                        && lotSalePrices.TryGetValue(line.Id, out var lotSalePrice)
                        ? lotSalePrice
                        : GetProductUnitSalePrice(line.ProductId, line.ProductUnitId),
                    LineDiscountAmount = line.LineDiscountAmount,
                    FreeQuantity = line.BonusPercentage.HasValue ? 0m : line.FreeQuantity,
                    BonusRatio = line.BonusRatio,
                    BonusPercentage = line.BonusPercentage,
                    PurchaseAcquisitionCost = line.PurchaseAcquisitionCost,
                    TaxExempt = line.TaxExempt,
                    TaxRate = line.TaxRate,
                    ExpiryDate = line.ExpiryDate,
                    CreatedDate = line.CreatedDate,
                    UpdatedDate = line.UpdatedDate
                });
            }

            UpdateTotal();
            UiText.ApplyTranslations(ProductsGrid);
            UpdateChecksButtonVisibility();

            PrintBtn.Visibility = Visibility.Visible;
            NewInvoiceBtn.Visibility = Visibility.Visible;
        }

        private void NewInvoiceBtn_Click(object sender, RoutedEventArgs e)
        {
            _currentInvoiceId = null;
            _originalLines.Clear();
            _originalCostPriceIncludesTax = false;

            InvoiceLines.Clear();
            ProductsGrid.Items.Refresh();

            InvoiceNumberTextBox.Text = _isPurchaseOrderMode
                ? GenerateInvoiceNumber()
                : string.Empty;
            FalconInvoiceNumberTextBox.Clear();
            SupplierComboBox.SelectedIndex = -1;
            InvoiceDatePicker.SelectedDate = DateTime.Now;
            _currentChecks.Clear();
            UpdateChecksButtonVisibility();

            SetSelectedPaymentType(PaymentType.Cash);
            SubtotalAmountTextBox.Text = "0";
            TaxAmountTextBox.Text = "0";
            TotalAmountTextBox.Text = "0";
            DiscountTextBox.Text = "0";
            CostPriceIncludesTaxCheckBox.IsChecked = false;
            ProductBox.Text = "";
            ClearProductInputs();
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
            PurchaseBox.Text = "";
            SaleBox.Text = "";
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

        private async void SearchProductBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var searchWindow = new PurchaseProductSearchWindow(
                    _productService,
                    _stockService,
                    async row => await AddProductFromSearchAsync(row),
                    async search => await CreateProductFromSearchAsync(search),
                    supplierId: SupplierComboBox.SelectedItem is UserReadDto supplier ? supplier.Id : null)
                {
                    Owner = this
                };

                searchWindow.ShowDialog();
                ProductBox.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("تعذر فتح بحث المشتريات", "Could not open purchase product search")}: {ex.Message}", UiText.T("خطأ", "Error"));
            }
        }

        private async Task CreateProductFromSearchAsync(string searchText)
        {
            CreateProduct? createWindow = null;
            using (CatalogRefreshNotifier.SuppressNotifications())
            {
                WindowManager.ShowDialog<CreateProduct>(WindowSizeType.LargeRectangle, window =>
                {
                    createWindow = window;
                    if (long.TryParse(searchText, out var barcode))
                        window.InitialItemCode = barcode.ToString();
                });
            }

            await LoadProductsAsync();

            if (createWindow?.CreatedProductId is int createdProductId)
            {
                ProductBox.SelectedValue = createdProductId;
                await QueueProductSelectionAsync(createdProductId);
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

            var exactMatch = long.TryParse(search, out var barcode)
                ? filtered.FirstOrDefault(product => product.ITEMCODE == barcode)
                : null;

            if (exactMatch != null)
            {
                ProductBox.SelectedItem = exactMatch;
                _ = QueueProductSelectionAsync(exactMatch.Id);
            }
        }

        private static bool TryParseDecimalInput(string? text, out decimal value)
        {
            value = 0m;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var normalized = NormalizeDecimalInput(text);
            return decimal.TryParse(
                normalized,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out value);
        }

        private static string NormalizeDecimalInput(string text)
        {
            var mapped = new System.Text.StringBuilder(text.Length);
            foreach (var character in text.Trim())
            {
                if (character is >= '\u0660' and <= '\u0669')
                    mapped.Append((char)('0' + character - '\u0660'));
                else if (character is >= '\u06F0' and <= '\u06F9')
                    mapped.Append((char)('0' + character - '\u06F0'));
                else if (character is '\u066B' or '\u060C' or ',')
                    mapped.Append(',');
                else if (character == '\u066C')
                    mapped.Append(',');
                else if (character == '.')
                    mapped.Append('.');
                else if (!char.IsWhiteSpace(character))
                    mapped.Append(character);
            }

            var normalizedText = mapped.ToString();
            var lastComma = normalizedText.LastIndexOf(',');
            var lastDot = normalizedText.LastIndexOf('.');
            var decimalSeparator = lastComma > lastDot ? ',' : '.';
            var result = new System.Text.StringBuilder(normalizedText.Length);

            foreach (var character in normalizedText)
            {
                if (character is >= '0' and <= '9' || character == '-' || character == '+')
                    result.Append(character);
                else if (character == decimalSeparator && !result.ToString().Contains('.'))
                    result.Append('.');
            }

            return result.ToString();
        }
    }
}

#region Usings
using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.ProductUnits;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.StockTransactions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Common;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Cashiers.DTOs;
using RaccoonWarehouse.Domain.Checks.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.FinancialTransactions.DTOs;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Products;
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
        private bool _isLoadingUnits = false;
        private int _productSelectionVersion;
        private string _productSearchText = string.Empty;
        private bool _isRestoringProductSearchText;
        private readonly System.Threading.SemaphoreSlim _productSelectionSemaphore = new(1, 1);
        private int? _currentInvoiceId = null;   // لتحديث الفاتورة بعد الحفظ الأول
        private bool _originalCostPriceIncludesTax;

        public PayInvoice(
            IStockService stockService,
            IInvoiceService invoiceService,
            IUserService userService,
            IProductService productService,
            IProductUnitService productUnitService,
            IUserSession userSession,
            IStockTransactionService stockTransactionService,
            IFinancialTransactionService financialService,
            ILoadingService loadingService)
        {
            _stockService = stockService;
            _productService = productService;
            _productUnitService = productUnitService;
            _stockTransactionService = stockTransactionService;
            _invoicesService = invoiceService;
            _userService = userService;
            _userSession = userSession;
            _financialService = financialService;
            _loadingService = loadingService;
            _isLoadingUnits = false;

            InitializeComponent();
            UiText.ApplyWindow(this);
            DataContext = this;

            // رقم الفاتورة
            InvoiceNumberTextBox.Text = GenerateInvoiceNumber();

            // ربط الـ Grid
            ProductsGrid.ItemsSource = InvoiceLines;

            Loaded += PayInvoice_Loaded;
            Closed += PayInvoice_Closed;
            CatalogRefreshNotifier.CatalogChanged += CatalogRefreshNotifier_CatalogChanged;
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
                // المورّدين (نفس users لكن أنت تختار اللي يمثل الموردين)
                var result = await _userService.GetAllAsync();
                _allSuppliers = new ObservableCollection<UserReadDto>((result?.Data ?? new List<UserReadDto>()).GroupBy(user => user.Id).Select(group => group.First()));
                SupplierComboBox.ItemsSource = _allSuppliers;
                SupplierComboBox.SelectedIndex = -1;

                InvoiceDatePicker.SelectedDate = DateTime.Now;

                PaymentMethodComboBox.SelectedIndex = 0;
                UiText.ApplyTranslations(PaymentMethodComboBox);

                HideLoadingIfShown();
                await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Background);
                await LoadProductsAsync();
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
            try
            {

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
        }

        private async void CreateProductBtn_Click(object sender, RoutedEventArgs e)
        {
            var existingProductIds = Products.Select(product => product.Id).ToHashSet();

            CreateProduct? createWindow = null;
            WindowManager.ShowDialog<CreateProduct>(WindowSizeType.LargeRectangle, window => createWindow = window);

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
            if (e.Key == Key.F2)
            {
                SearchProductBtn_Click(this, new RoutedEventArgs());
                e.Handled = true;
                return;
            }

            if (e.Key != Key.F1)
                return;

            SaveReceiptBtn_Click(SaveReceiptBtn, new RoutedEventArgs());
            e.Handled = true;
        }
        private void PayInvoice_Closed(object? sender, EventArgs e)
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
                .Where(line => line.ProductId > 0 && line.ProductUnitId > 0 && (line.Quantity != 0 || line.FreeQuantity != 0))
                .SelectMany(line =>
                {
                    var quantityPerUnit = line.QuantityPerUnitSnapshot > 0 ? line.QuantityPerUnitSnapshot : 1m;
                    var paidBaseQuantity = line.BaseQuantity != 0 ? line.BaseQuantity : line.Quantity * quantityPerUnit;
                    var profileSalePrice = GetProductUnitSalePrice(line.ProductId, line.ProductUnitId);
                    var movements = new List<StockMovementPostDto>();

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
                            SalePrice = profileSalePrice,
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

                    if (line.FreeQuantity > 0)
                    {
                        movements.Add(new StockMovementPostDto
                        {
                            ProductId = line.ProductId,
                            ProductUnitId = line.ProductUnitId,
                            Quantity = line.FreeQuantity * multiplier,
                            QuantityPerUnitSnapshot = quantityPerUnit,
                            BaseQuantity = line.FreeQuantity * quantityPerUnit * multiplier,
                            UnitPrice = 0m,
                            PurchasePrice = 0m,
                            SalePrice = profileSalePrice,
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
                WindowManager.ShowDialog<CreateUser>(WindowSizeType.SmallSquare, window => window.InitializeForCustomerQuickCreate(SupplierComboBox.Text));
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

            PurchaseBox.Text = unit.PurchasePrice.ToString("0.00000");
            SaleBox.Text = unit.SalePrice.ToString("0.00000");
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

            // في فاتورة المشتريات نستخدم سعر الشراء
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

            var header = e.Column.Header?.ToString() ?? string.Empty;
            if (e.EditingElement is TextBox textBox)
            {
                if (header.Contains("الكمية", StringComparison.OrdinalIgnoreCase) || header.Contains("Quantity", StringComparison.OrdinalIgnoreCase))
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
                else if (header.Contains("سعر", StringComparison.OrdinalIgnoreCase) || header.Contains("Price", StringComparison.OrdinalIgnoreCase))
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
                else if (header.Contains("Discount", StringComparison.OrdinalIgnoreCase))
                {
                    if (!TryParseDecimalInput(textBox.Text, out var discount) || discount < 0)
                    {
                        MessageBox.Show(UiText.T("خصم السطر يجب أن يكون رقماً غير سالب.", "Line discount must be a non-negative number."), UiText.T("تنبيه", "Notice"));
                        line.LineDiscountAmount = 0m;
                    }
                    else
                    {
                        line.LineDiscountAmount = discount;
                    }
                }
                else if (header.Contains("Free", StringComparison.OrdinalIgnoreCase))
                {
                    if (!TryParseDecimalInput(textBox.Text, out var freeQuantity) || freeQuantity < 0)
                    {
                        MessageBox.Show(UiText.T("الكمية المجانية يجب أن تكون رقماً غير سالب.", "Free quantity must be a non-negative number."), UiText.T("تنبيه", "Notice"));
                        line.FreeQuantity = 0m;
                    }
                    else
                    {
                        line.FreeQuantity = freeQuantity;
                    }
                }
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
                line.LineDiscountAmount = Math.Clamp(line.LineDiscountAmount, 0m, grossLineTotal);
                line.FreeQuantity = Math.Max(0m, line.FreeQuantity);
                var lineTotal = Math.Round(grossLineTotal - line.LineDiscountAmount, 3);
                var divisor = CostPriceIncludesTax && !line.TaxExempt && line.TaxRate > 0m
                    ? 1m + line.TaxRate / 100m
                    : 1m;
                line.LineSubTotal = Math.Round(lineTotal / divisor, 3);
                subtotal += line.LineSubTotal;
            }

            subtotal = Math.Round(subtotal, 3);
            decimal.TryParse(DiscountTextBox.Text, out var discount);
            discount = Math.Clamp(discount, 0m, subtotal);
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
            var totals = CalculatePurchaseTotals();
            SubtotalAmountTextBox.Text = totals.Subtotal.ToString("0.00000");
            TaxAmountTextBox.Text = totals.Tax.ToString("0.00000");
            TotalAmountTextBox.Text = totals.Total.ToString("0.00000");
        }

        private void DiscountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded)
                UpdateTotal();
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
                if (!InvoiceLines.Any())
                {
                    MessageBox.Show("يرجى إضافة منتج واحد على الأقل.", "تنبيه");
                    return;
                }

                var selectedPaymentType = GetSelectedPaymentType();

                if (selectedPaymentType == PaymentType.Credit && SupplierComboBox.SelectedItem == null)
                {
                    MessageBox.Show(
                        UiText.T("يرجى اختيار المورد للفواتير الآجلة.", "Please select a supplier for credit purchases."),
                        UiText.T("تنبيه", "Notice"));
                    return;
                }

                if (MessageBox.Show(
                        UiText.T("هل تريد حفظ فاتورة المشتريات؟", "Do you want to save the purchase invoice?"),
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
                                $"مجموع الشيكات ({checkTotal:0.00000}) يجب أن يساوي إجمالي الفاتورة ({totalAmount:0.00000}).",
                                $"The total check amount ({checkTotal:0.00000}) must equal the invoice total ({totalAmount:0.00000})."),
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

                    HideLoadingIfShown();
                    MessageBox.Show(
                        selectedPaymentType == PaymentType.Credit
                            ? "تم إنشاء فاتورة مشتريات آجلة بنجاح ✅"
                            : "تم إنشاء فاتورة المشتريات وتسجيل الحركة المالية ✅");
                    UiText.ApplyTranslations(this);
                }
                else
                {
                    savedInvoiceId = _currentInvoiceId.Value;

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
                                ProductId = line.ProductId,
                                ProductUnitId = line.ProductUnitId,
                                Quantity = line.Quantity,
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

                    // 3) Update invoice
                    var result = await _invoicesService.UpdateAsync(invoiceDto);
                    if (!result.Success)
                    {
                        HideLoadingIfShown();
                        MessageBox.Show(result.Message ?? "فشل تحديث فاتورة المشتريات", "خطأ");
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

                    HideLoadingIfShown();
                    MessageBox.Show(
                        selectedPaymentType == PaymentType.Credit
                            ? "تم تحديث فاتورة المشتريات الآجلة بنجاح ✅"
                            : "تم تحديث فاتورة المشتريات وتسجيل الحركة المالية ✅");
                    UiText.ApplyTranslations(this);
                }
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
                HideLoadingIfShown();
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

            var invoice = await _invoicesService.GetFullInvoiceByIdAsync(_currentInvoiceId.Value);

            if (invoice == null)
            {
                MessageBox.Show(UiText.T("الفاتورة غير موجودة.", "The invoice was not found."), UiText.T("خطأ", "Error"));
                return;
            }

            SavePurchaseInvoicePdf(invoice);
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
            var searchWindow = new SearchSalesInvoiceWindow(_invoicesService, _allSuppliers ?? Enumerable.Empty<UserReadDto>(), false)
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
            _originalCostPriceIncludesTax = invoice.CostPriceIncludesTax;

            _originalLines = invoice.InvoiceLines.ToList();   // 🔥 مهم جداً

            InvoiceNumberTextBox.Text = invoice.InvoiceNumber;
            FalconInvoiceNumberTextBox.Text = invoice.FalconInvoiceNumber ?? string.Empty;
            InvoiceDatePicker.SelectedDate = invoice.CreatedDate;

            SupplierComboBox.SelectedItem =
                _allSuppliers.FirstOrDefault(c => c.Id == invoice.SupplierId);
            DiscountTextBox.Text = (invoice.DiscountAmount ?? 0m).ToString("0.00000");
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
                    LineDiscountAmount = line.LineDiscountAmount,
                    FreeQuantity = line.FreeQuantity,
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

            InvoiceNumberTextBox.Text = GenerateInvoiceNumber();
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
                MessageBox.Show($"{UiText.T("تعذر فتح بحث المشتريات", "Could not open purchase product search")}: {ex.Message}", UiText.T("خطأ", "Error"));
            }
        }

        private async Task CreateProductFromSearchAsync(string searchText)
        {
            CreateProduct? createWindow = null;
            WindowManager.ShowDialog<CreateProduct>(WindowSizeType.LargeRectangle, window =>
            {
                createWindow = window;
                if (long.TryParse(searchText, out var barcode))
                    window.InitialItemCode = barcode.ToString();
            });

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
            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
                || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }
    }
}

using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using RaccoonWarehouse.Domain.Stock;
using RaccoonWarehouse.Domain.Stock.DTOs;
using RaccoonWarehouse.Domain.Units;
using RaccoonWarehouse.Domain.Units.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Products;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Helpers;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Domain.Settings;

namespace RaccoonWarehouse.Invoices
{
    public sealed class FlexibleDecimalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is decimal decimalValue ? decimalValue.ToString("0.000", culture) : value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value?.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(text))
                return DependencyProperty.UnsetValue;

            text = NormalizeDecimalText(text);
            return decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var result)
                ? result
                : DependencyProperty.UnsetValue;
        }

        private static string NormalizeDecimalText(string text)
        {
            var builder = new System.Text.StringBuilder(text.Length);
            foreach (var character in text)
            {
                builder.Append(character switch
                {
                    '٠' => '0', '١' => '1', '٢' => '2', '٣' => '3', '٤' => '4',
                    '٥' => '5', '٦' => '6', '٧' => '7', '٨' => '8', '٩' => '9',
                    '۰' => '0', '۱' => '1', '۲' => '2', '۳' => '3', '۴' => '4',
                    '۵' => '5', '۶' => '6', '۷' => '7', '۸' => '8', '۹' => '9',
                    '٫' or '،' or ',' => '.',
                    _ => character
                });
            }

            return builder.ToString();
        }
    }
    /// <summary>
    /// Interaction logic for ProductSearchWindow.xaml
    /// </summary>
    public partial class ProductSearchWindow : Window
    {
        static ProductSearchWindow()
        {
            PosProductCatalogRefreshQueue.CatalogRefreshed += InvalidateCatalogCache;
            RaccoonWarehouse.Common.CatalogRefreshNotifier.CatalogChanged
                += (_, _) => InvalidateCatalogCache();
        }

        private const decimal MinimumSellableQuantity = 0m;
        private const int MaxSearchResults = 100;
        private static readonly object CatalogCacheSync = new();
        private static Task<List<PosProductSearchResultDto>>? _catalogLoadTask;
        private static DateTimeOffset _catalogLoadedAt;
        private static readonly TimeSpan CatalogCacheLifetime = TimeSpan.FromMinutes(10);
        private readonly IProductService _productService;
        private readonly IStockService _stockService;
        private readonly IUserService _userService;
        private readonly IPosRowColorRuleService? _rowColorRuleService;
        private readonly Func<ProductSearchRow, Task<bool>>? _onAddProduct;
        private readonly HashSet<string> _disabledProductKeys;
        private readonly string _ruleScope;
        private readonly DispatcherTimer _searchDebounceTimer = new() { Interval = TimeSpan.FromMilliseconds(150) };
        private readonly SemaphoreSlim _serviceCallLock = new(1, 1);
        private CancellationTokenSource? _searchCancellation;
        private int _searchVersion;
        private bool _isAddingProduct;
        private bool _isInitializing;
        private bool _isRefreshingColorRules;
        private int? _selectedSupplierId;
        private List<RaccoonWarehouse.Domain.Users.DTOs.UserReadDto> _supplierLookupItems = new();
        private bool _isFilteringSuppliers;
        private bool _isNavigatingSuppliers;
        private string _supplierSearchText = string.Empty;
        private IReadOnlyList<PosRowColorRule> _loadedColorRules = Array.Empty<PosRowColorRule>();
        private string? _selectedColorRuleId;
        private readonly ObservableCollection<ColorGroupOption> _colorGroups = new();

        public ProductReadDto SelectedProduct { get; private set; }

        private ObservableCollection<ProductSearchRow> _products
            = new ObservableCollection<ProductSearchRow>();

        public ProductSearchWindow(
            IProductService productService,
            IStockService stockService,
            IUserService userService,
            Func<ProductSearchRow, Task<bool>>? onAddProduct = null,
            IEnumerable<string>? disabledProductKeys = null,
            IPosRowColorRuleService? rowColorRuleService = null,
            string ruleScope = "ProductSearch")
        {
            InitializeComponent();
            UiText.ApplyWindow(this);
            _productService = productService;
            _stockService = stockService;
            _userService = userService;
            _rowColorRuleService = rowColorRuleService;
            _ruleScope = string.IsNullOrWhiteSpace(ruleScope) ? "ProductSearch" : ruleScope;
            _onAddProduct = onAddProduct;
            _disabledProductKeys = disabledProductKeys != null
                ? new HashSet<string>(disabledProductKeys)
                : new HashSet<string>();
            ProductsGrid.ItemsSource = _products;
            ColorFilterItemsControl.ItemsSource = _colorGroups;
            SetLoadingState(true);
            _searchDebounceTimer.Tick += SearchDebounceTimer_Tick;
            Loaded += ProductSearchWindow_Loaded;
            Closed += ProductSearchWindow_Closed;
            RaccoonWarehouse.Common.CatalogRefreshNotifier.CatalogChanged
                += ProductCatalogChanged;
            PosProductCatalogRefreshQueue.CatalogRefreshed += RefreshStockResults;
            PosRowColorRuleService.RulesChanged += PosRowColorRuleService_RulesChanged;
        }

        private async void ProductSearchWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _isInitializing = true;
            SetLoadingState(true);
            var initializationLockAcquired = false;
            try
            {
                // Serialize initialization with searches so the same scoped
                // DbContext is not queried concurrently.
                await _serviceCallLock.WaitAsync();
                initializationLockAcquired = true;

                try
                {
                    await WarmCatalogCacheAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"POS product catalog could not be loaded. Please verify the catalog database update.\n\n{ex.Message}",
                        UiText.T("Ø®Ø·Ø£", "Catalog loading error"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }

                if (_rowColorRuleService != null)
                {
                    _loadedColorRules = await _rowColorRuleService.GetRulesAsync();
                    RefreshColorFilterOptions();
                }
                await LoadSupplierFilterAsync();
                await Dispatcher.BeginInvoke(() =>
                {
                    SearchTextBox.Focus();
                    Keyboard.Focus(SearchTextBox);
                    SearchTextBox.SelectAll();
                }, DispatcherPriority.Input);
            }
            finally
            {
                if (initializationLockAcquired)
                    _serviceCallLock.Release();

                _isInitializing = false;
                SetLoadingState(false);
            }
        }

        private void SetLoadingState(bool isLoading)
        {
            _isInitializing = isLoading;
            SearchTextBox.IsEnabled = !isLoading;
            BarcodeSearchTextBox.IsEnabled = !isLoading;
            SupplierFilterComboBox.IsEnabled = !isLoading;
            ProductsGrid.IsEnabled = !isLoading;
            ColorFilterItemsControl.IsEnabled = !isLoading;
        }

        private void ProductSearchWindow_Closed(object? sender, EventArgs e)
        {
            PosRowColorRuleService.RulesChanged -= PosRowColorRuleService_RulesChanged;
            PosProductCatalogRefreshQueue.CatalogRefreshed -= RefreshStockResults;
            RaccoonWarehouse.Common.CatalogRefreshNotifier.CatalogChanged
                -= ProductCatalogChanged;
            _searchDebounceTimer.Stop();
            _searchCancellation?.Cancel();
        }

        private void ProductCatalogChanged(object? sender, EventArgs e)
            => RefreshStockResults();

        private void RefreshStockResults()
        {
            if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished)
                return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!IsLoaded)
                    return;

                _searchCancellation?.Cancel();
                _searchDebounceTimer.Stop();
                _searchDebounceTimer.Start();
            }));
        }

        private void PosRowColorRuleService_RulesChanged()
        {
            if (!IsLoaded || _rowColorRuleService == null)
                return;

            Dispatcher.BeginInvoke(new Action(() => _ = RefreshColorRulesAsync()), DispatcherPriority.Background);
        }

        private async Task RefreshColorRulesAsync()
        {
            if (_rowColorRuleService == null || _isInitializing || _isRefreshingColorRules)
                return;

            _isRefreshingColorRules = true;
            var lockAcquired = false;
            try
            {
                await _serviceCallLock.WaitAsync();
                lockAcquired = true;
                _rowColorRuleService.InvalidateCache();
                _loadedColorRules = await _rowColorRuleService.GetRulesAsync();

                if (!string.IsNullOrWhiteSpace(_selectedColorRuleId) &&
                    !_loadedColorRules.Any(rule => rule.IsActive &&
                        string.Equals(rule.Id, _selectedColorRuleId, StringComparison.OrdinalIgnoreCase)))
                    _selectedColorRuleId = null;

                RefreshColorFilterOptions();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                if (lockAcquired)
                    _serviceCallLock.Release();
                _isRefreshingColorRules = false;
            }

            if (HasSearchCriteria())
                await RunSearchSafelyAsync();
        }

        private bool HasSearchCriteria()
            => SearchTextBox.Text.Trim().Length >= 2
                || BarcodeSearchTextBox.Text.Trim().Length > 0
                || _selectedSupplierId.HasValue
                || !string.IsNullOrWhiteSpace(_selectedColorRuleId);

        private async Task WarmCatalogCacheAsync()
            => await WarmCatalogCacheAsync(_stockService);

        public static async Task WarmCatalogCacheAsync(IStockService stockService)
        {
            Task<List<PosProductSearchResultDto>> loadTask;
            lock (CatalogCacheSync)
            {
                if (_catalogLoadTask != null &&
                    (!_catalogLoadTask.IsCompleted ||
                     DateTimeOffset.UtcNow - _catalogLoadedAt < CatalogCacheLifetime))
                {
                    loadTask = _catalogLoadTask;
                }
                else
                {
                    loadTask = _catalogLoadTask = LoadCatalogCacheAsync(stockService);
                }
            }

            try
            {
                await loadTask;
            }
            catch
            {
                lock (CatalogCacheSync)
                {
                    if (ReferenceEquals(_catalogLoadTask, loadTask))
                        _catalogLoadTask = null;
                }

                throw;
            }
        }

        // Unit metadata is loaded with the catalog and indexed once, so partial
        // quantity entry never scans the catalog or queries the database.
        private static Dictionary<int, IReadOnlyList<ProductUnitReadDto>> _partialSaleUnitIndex = new();
        private static readonly Dictionary<int, (long Version, IReadOnlyList<ProductUnitReadDto>? Units)> PartialSaleUnitOverrides = new();
        private static long _partialSaleUnitVersion;
        private static long _partialSaleCatalogLoadGeneration;

        public static bool TryGetCachedProductUnits(int productId, out IReadOnlyList<ProductUnitReadDto> units)
        {
            lock (CatalogCacheSync)
            {
                if (_partialSaleUnitIndex.TryGetValue(productId, out var cached))
                {
                    units = cached;
                    return true;
                }
            }
            units = Array.Empty<ProductUnitReadDto>();
            return false;
        }

        public static void InvalidateCachedProductUnits(int productId)
        {
            lock (CatalogCacheSync)
            {
                _partialSaleUnitIndex.Remove(productId);
                PartialSaleUnitOverrides[productId] = (++_partialSaleUnitVersion, null);
            }
        }

        public static void UpdateCachedProductUnits(int productId, IReadOnlyList<ProductUnitReadDto> units)
        {
            lock (CatalogCacheSync)
            {
                var snapshot = units.ToArray();
                _partialSaleUnitIndex[productId] = snapshot;
                PartialSaleUnitOverrides[productId] = (++_partialSaleUnitVersion, snapshot);
                if (_catalogLoadTask?.IsCompletedSuccessfully == true)
                    _catalogLoadTask = Task.FromResult(PatchCatalogUnits(_catalogLoadTask.Result, productId, snapshot));
            }
        }

        private static Dictionary<int, IReadOnlyList<ProductUnitReadDto>> IndexCatalogUnits(
            IReadOnlyList<PosProductSearchResultDto> catalog)
            => catalog.GroupBy(row => row.ProductId).ToDictionary(group => group.Key,
                group => (IReadOnlyList<ProductUnitReadDto>)group.GroupBy(row => row.ProductUnitId)
                    .Select(rows => rows.First()).Select(row => new ProductUnitReadDto
                    {
                        Id = row.ProductUnitId,
                        ProductId = row.ProductId,
                        AlternateBarcode = row.AlternateBarcode,
                        SalePrice = row.UnitSalePrice,
                        PurchasePrice = row.UnitPurchasePrice,
                        QuantityPerUnit = row.QuantityPerUnit,
                        AllowFractionalSale = row.AllowFractionalSale,
                        FractionalPartsPerUnit = row.FractionalPartsPerUnit,
                        FractionalPartName = row.FractionalPartName,
                        FractionalSalePrice = row.FractionalSalePrice,
                        FractionalSaleCost = row.FractionalSaleCost,
                        IsBaseUnit = row.IsBaseUnit,
                        IsDefaultSaleUnit = row.IsDefaultSaleUnit,
                        IsDefaultPurchaseUnit = row.IsDefaultPurchaseUnit,
                        Unit = new UnitReadDto { Name = row.UnitName ?? "" }
                    }).ToArray());

        private static List<PosProductSearchResultDto> PatchCatalogUnits(
            IReadOnlyList<PosProductSearchResultDto> catalog, int productId, IReadOnlyList<ProductUnitReadDto> units)
        {
            var productRows = catalog.Where(row => row.ProductId == productId).ToList();
            var first = productRows.FirstOrDefault();
            if (first == null)
                return catalog.ToList();
            var baseStock = first.StockQuantity * first.QuantityPerUnit;
            var updated = catalog.Where(row => row.ProductId != productId).ToList();
            foreach (var unit in units)
            {
                var old = productRows.FirstOrDefault(row => row.ProductUnitId == unit.Id);
                updated.Add(new PosProductSearchResultDto
                {
                    ProductId = productId, ProductUnitId = unit.Id,
                    SubCategoryId = first.SubCategoryId, ProductName = first.ProductName,
                    ItemCode = first.ItemCode, TaxExempt = first.TaxExempt, TaxRate = first.TaxRate,
                    SupplierId = first.SupplierId, SupplierName = first.SupplierName,
                    AlternateBarcode = unit.AlternateBarcode, UnitName = unit.Unit?.Name ?? old?.UnitName,
                    UnitSalePrice = unit.SalePrice, UnitPurchasePrice = unit.PurchasePrice,
                    QuantityPerUnit = unit.QuantityPerUnit,
                    AllowFractionalSale = unit.AllowFractionalSale,
                    FractionalPartsPerUnit = unit.FractionalPartsPerUnit,
                    FractionalPartName = unit.FractionalPartName,
                    FractionalSalePrice = unit.FractionalSalePrice,
                    FractionalSaleCost = unit.FractionalSaleCost,
                    IsBaseUnit = unit.IsBaseUnit, IsDefaultSaleUnit = unit.IsDefaultSaleUnit,
                    IsDefaultPurchaseUnit = unit.IsDefaultPurchaseUnit,
                    StockQuantity = unit.QuantityPerUnit > 0m ? baseStock / unit.QuantityPerUnit : 0m,
                    StockPurchasePrice = old?.StockPurchasePrice ?? unit.PurchasePrice,
                    StockSalePrice = old?.StockSalePrice ?? unit.SalePrice,
                    ExpiryDate = old?.ExpiryDate ?? first.ExpiryDate,
                    AvailableToSale = first.AvailableToSale, StockStatus = old?.StockStatus ?? first.StockStatus
                });
            }
            return updated;
        }

        public static void InvalidateCatalogCache()
        {
            lock (CatalogCacheSync)
            {
                _catalogLoadedAt = DateTimeOffset.MinValue;
                _catalogLoadTask = null;
                _partialSaleCatalogLoadGeneration++;
            }
        }

        private static async Task<List<PosProductSearchResultDto>> LoadCatalogCacheAsync(IStockService stockService)
        {
            long startedUnitVersion;
            long generation;
            lock (CatalogCacheSync)
            {
                startedUnitVersion = _partialSaleUnitVersion;
                generation = ++_partialSaleCatalogLoadGeneration;
            }
            var result = await stockService.GetPosProductCatalogAsync();
            if (!result.Success)
                throw new InvalidOperationException(result.Message ?? "Unable to load POS product catalog.");

            var rows = result.Data ?? new List<PosProductSearchResultDto>();
            lock (CatalogCacheSync)
            {
                if (generation != _partialSaleCatalogLoadGeneration)
                    return rows;

                // A setup/edit that completed during a catalog load wins over
                // the older snapshot returned by that in-flight query.
                var pending = PartialSaleUnitOverrides.Where(entry => entry.Value.Version > startedUnitVersion).ToList();
                foreach (var entry in pending.Where(entry => entry.Value.Units != null))
                    rows = PatchCatalogUnits(rows, entry.Key, entry.Value.Units!);
                _partialSaleUnitIndex = IndexCatalogUnits(rows);
                foreach (var entry in pending.Where(entry => entry.Value.Units == null))
                    _partialSaleUnitIndex.Remove(entry.Key);
                foreach (var id in PartialSaleUnitOverrides.Where(entry => entry.Value.Version <= startedUnitVersion)
                             .Select(entry => entry.Key).ToList())
                    PartialSaleUnitOverrides.Remove(id);
                _catalogLoadedAt = DateTimeOffset.UtcNow;
            }
            return rows;
        }

        private static bool TryGetLoadedCatalog(out List<PosProductSearchResultDto> catalog)
        {
            lock (CatalogCacheSync)
            {
                if (_catalogLoadTask?.IsCompletedSuccessfully == true &&
                    DateTimeOffset.UtcNow - _catalogLoadedAt < CatalogCacheLifetime)
                {
                    catalog = _catalogLoadTask.Result;
                    return true;
                }
            }

            catalog = new List<PosProductSearchResultDto>();
            return false;
        }

        private async Task LoadSupplierFilterAsync()
        {
            var users = await _userService.GetAllAsync();
            _supplierLookupItems = users.Data?
                .Where(user => user.Role == UserRole.Supplier)
                .OrderBy(user => user.Name)
                .ToList() ?? new();

            var suppliers = _supplierLookupItems.ToList();
            suppliers.Insert(0, new RaccoonWarehouse.Domain.Users.DTOs.UserReadDto
            {
                Id = 0,
                Name = "All suppliers"
            });
            SupplierFilterComboBox.ItemsSource = suppliers;
            SupplierFilterComboBox.SelectedIndex = 0;
        }

        private void SupplierFilterComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (SupplierFilterComboBox.Template.FindName("PART_EditableTextBox", SupplierFilterComboBox) is TextBox textBox)
                textBox.TextChanged += SupplierFilterComboBox_TextChanged;
        }

        private void SupplierFilterComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is not (Key.Down or Key.Up or Key.Enter) || SupplierFilterComboBox.Items.Count == 0)
                return;

            _isNavigatingSuppliers = true;
            try
            {
                if (e.Key == Key.Down)
                    SupplierFilterComboBox.SelectedIndex = Math.Min(SupplierFilterComboBox.SelectedIndex + 1, SupplierFilterComboBox.Items.Count - 1);
                else if (e.Key == Key.Up)
                    SupplierFilterComboBox.SelectedIndex = Math.Max(SupplierFilterComboBox.SelectedIndex - 1, 0);
                else
                {
                    SupplierFilterComboBox.IsDropDownOpen = false;
                    e.Handled = true;
                    return;
                }

                RestoreSupplierFilterSearchText();
                e.Handled = true;
            }
            finally
            {
                _isNavigatingSuppliers = false;
            }
        }

        private void RestoreSupplierFilterSearchText()
        {
            if (SupplierFilterComboBox.Template.FindName("PART_EditableTextBox", SupplierFilterComboBox) is not TextBox textBox)
                return;

            textBox.Text = _supplierSearchText;
            textBox.CaretIndex = textBox.Text.Length;
            SupplierFilterComboBox.IsDropDownOpen = true;
        }

        private void SupplierFilterComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFilteringSuppliers || _isNavigatingSuppliers || !SupplierFilterComboBox.IsKeyboardFocusWithin)
                return;

            if (SupplierFilterComboBox.Template.FindName("PART_EditableTextBox", SupplierFilterComboBox) is not TextBox textBox)
                return;

            _supplierSearchText = textBox.Text.Trim();
            _isFilteringSuppliers = true;
            try
            {
                SupplierFilterComboBox.SelectedValue = null;
                SupplierFilterComboBox.ItemsSource = string.IsNullOrWhiteSpace(_supplierSearchText)
                    ? _supplierLookupItems.ToList()
                    : _supplierLookupItems.Where(user =>
                            (user.Name ?? string.Empty).Contains(_supplierSearchText, StringComparison.CurrentCultureIgnoreCase)
                            || (user.PhoneNumber ?? string.Empty).Contains(_supplierSearchText, StringComparison.CurrentCultureIgnoreCase))
                        .ToList();
                SupplierFilterComboBox.IsDropDownOpen = true;
                textBox.CaretIndex = textBox.Text.Length;
            }
            finally
            {
                _isFilteringSuppliers = false;
            }
        }

        private async void SupplierFilterComboBox_DropDownClosed(object sender, EventArgs e)
        {
            _selectedSupplierId = SupplierFilterComboBox.SelectedValue is int supplierId && supplierId > 0
                ? supplierId
                : null;
            await RunSearchSafelyAsync();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }

        private void BarcodeSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }

        private async void SearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down && _products.Count > 0)
            {
                ProductsGrid.Focus();
                ProductsGrid.SelectedIndex = 0;
                ProductsGrid.CurrentCell = new DataGridCellInfo(_products[0], ProductsGrid.Columns[1]);
                ProductsGrid.BeginEdit();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                if (ProductsGrid.SelectedItem is ProductSearchRow row)
                    await AddProductAsync(row);
                e.Handled = true;
            }
        }

        private async void BarcodeSearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            _searchDebounceTimer.Stop();
            await RunSearchSafelyAsync();
            e.Handled = true;
        }

        private async void ProductsGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            await HandleProductsGridKeyAsync(e);
        }

        private void ProductSearchWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape)
                return;

            Close();
            e.Handled = true;
        }

        private async Task HandleProductsGridKeyAsync(KeyEventArgs e)
        {
            if (ProductsGrid.CurrentItem is not ProductSearchRow row)
                return;

            if (e.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (row.Product != null)
                {
                    WindowManager.ShowDialog<UpdateProduct>(WindowSizeType.MediumRectangle, window =>
                    {
                        window.Initialize(row.Product.Id);
                    });

                    await RunSearchSafelyAsync();
                }

                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter)
            {
                ProductsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                ProductsGrid.CommitEdit(DataGridEditingUnit.Row, true);
                var currentColumn = ProductsGrid.CurrentCell.Column;
                var nextColumn = ProductsGrid.Columns
                    .Where(column => column.Visibility == Visibility.Visible)
                    .OrderBy(column => column.DisplayIndex)
                    .FirstOrDefault(column => currentColumn != null && column.DisplayIndex > currentColumn.DisplayIndex);

                if (nextColumn == null)
                {
                    await AddProductAsync(row);
                }
                else
                {
                    ProductsGrid.CurrentCell = new DataGridCellInfo(row, nextColumn);
                    ProductsGrid.BeginEdit();
                }
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Up && ProductsGrid.SelectedIndex == 0)
            {
                SearchTextBox.Focus();
                SearchTextBox.SelectAll();
                e.Handled = true;
            }
        }
        private async Task AddProductAsync(ProductSearchRow row)
        {
            if (row.Product == null || !row.CanAdd || _isAddingProduct)
                return;

            _searchDebounceTimer.Stop();
            try
            {
                _isAddingProduct = true;
                await _serviceCallLock.WaitAsync();
                if (row.Quantity <= 0)
                {
                    MessageBox.Show(UiText.T("الكمية يجب أن تكون أكبر من صفر.", "Quantity must be greater than zero."), UiText.T("تنبيه", "Notice"));
                    return;
                }
                if (_onAddProduct != null && !await _onAddProduct(row))
                    return;

                row.CanAdd = false;
                _disabledProductKeys.Add(BuildProductKey(row.Product, row.SelectedUnit?.Id));
                SearchTextBox.Clear();
                _products.Clear();
                SearchTextBox.Focus();
                Keyboard.Focus(SearchTextBox);
                SearchTextBox.SelectAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("تعذر إضافة الصنف إلى الفاتورة", "Could not add the product to the invoice")}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                if (_serviceCallLock.CurrentCount == 0)
                    _serviceCallLock.Release();
                _isAddingProduct = false;
            }
        }
        private async void SearchDebounceTimer_Tick(object? sender, EventArgs e)
        {
            _searchDebounceTimer.Stop();
            await RunSearchSafelyAsync();
        }

        private async Task RunSearchSafelyAsync()
        {
            try
            {
                await PerformSearchAsync();
            }
            catch (OperationCanceledException)
            {
                // Cancellation is expected when a newer search supersedes this one.
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"POS product catalog could not be loaded.\n\n{ex.Message}",
                    UiText.T("Ø®Ø·Ø£", "Catalog loading error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task PerformSearchAsync()
        {
            if (_isAddingProduct || _isInitializing)
                return;

            var text = SearchTextBox.Text.Trim();
            var barcode = BarcodeSearchTextBox.Text.Trim();
            var searchVersion = ++_searchVersion;
            _searchCancellation?.Cancel();
            _searchCancellation?.Dispose();
            _searchCancellation = new CancellationTokenSource();
            var cancellationToken = _searchCancellation.Token;

            if (text.Length < 2 && barcode.Length == 0 && !_selectedSupplierId.HasValue &&
                string.IsNullOrWhiteSpace(_selectedColorRuleId))
            {
                _products.Clear();
                ProductsGrid.Items.Refresh();
                await RestoreSearchFocusAsync();
                return;
            }

            var serviceCallLockAcquired = false;
            try
            {
                // Do not cancel the semaphore wait itself. A newer keystroke
                // should make this search stale, not surface an exception.
                await _serviceCallLock.WaitAsync();
                serviceCallLockAcquired = true;

                if (cancellationToken.IsCancellationRequested)
                    return;

                List<PosProductSearchResultDto> searchRows;
                if (!TryGetLoadedCatalog(out var catalog))
                {
                    await WarmCatalogCacheAsync();
                    if (cancellationToken.IsCancellationRequested || !TryGetLoadedCatalog(out catalog))
                        return;
                }

                var matchedCatalogRows = catalog
                    .Where(row => MatchesCatalogFilter(row, text, barcode, _selectedSupplierId));
                var candidates = matchedCatalogRows
                    .GroupBy(row => row.ProductId)
                    .Where(group => string.IsNullOrWhiteSpace(_selectedColorRuleId) || MatchesSelectedColorRule(group))
                    .SelectMany(group => group)
                    .Take(MaxSearchResults * 20)
                    .ToList();

                searchRows = candidates
                    .GroupBy(row => row.ProductId)
                    .OrderByDescending(group => group.Any(row => row.AvailableToSale || row.StockQuantity > MinimumSellableQuantity))
                    .ThenBy(group => group.First().ProductName)
                    .SelectMany(group => group)
                    .Take(MaxSearchResults)
                    .ToList();

                if (searchVersion != _searchVersion)
                    return;

                _products.Clear();
                foreach (var productRows in searchRows
                    .GroupBy(row => row.ProductId)
                    .OrderByDescending(group => group.Any(row => row.StockQuantity > MinimumSellableQuantity))
                    .ThenBy(group => group.First().ProductName))
                {
                    var firstRow = productRows.First();
                    var productUnits = productRows
                        .GroupBy(row => row.ProductUnitId)
                        .Select(group => group.First())
                        .Select(row => new ProductUnitReadDto
                        {
                            Id = row.ProductUnitId,
                            ProductId = row.ProductId,
                            AlternateBarcode = row.AlternateBarcode,
                            SalePrice = row.UnitSalePrice,
                            PurchasePrice = row.UnitPurchasePrice,
                            QuantityPerUnit = row.QuantityPerUnit,
                            AllowFractionalSale = row.AllowFractionalSale,
                            FractionalPartsPerUnit = row.FractionalPartsPerUnit,
                            FractionalPartName = row.FractionalPartName,
                            FractionalSalePrice = row.FractionalSalePrice,
                            FractionalSaleCost = row.FractionalSaleCost,
                            IsBaseUnit = row.IsBaseUnit,
                            IsDefaultSaleUnit = row.IsDefaultSaleUnit,
                            IsDefaultPurchaseUnit = row.IsDefaultPurchaseUnit,
                            Unit = new UnitReadDto { Id = 0, Name = row.UnitName }
                        })
                        .ToList();
                    var product = new ProductReadDto
                    {
                        Id = firstRow.ProductId,
                        Name = firstRow.ProductName,
                        ITEMCODE = firstRow.ItemCode,
                        TaxExempt = firstRow.TaxExempt,
                        TaxRate = firstRow.TaxRate,
                        SubCategoryId = firstRow.SubCategoryId,
                        ProductUnits = productUnits
                    };
                    var stocksByProductId = productRows
                        .GroupBy(row => row.ProductUnitId)
                        .Select(group => group.OrderByDescending(row => row.StockQuantity).First())
                        .Select(row => new StockReadDto
                        {
                            ProductId = row.ProductId,
                            Product = product,
                            ProductUnitId = row.ProductUnitId,
                            ProductUnit = new ProductUnit
                            {
                                Id = row.ProductUnitId,
                                ProductId = row.ProductId,
                                SalePrice = row.UnitSalePrice,
                                PurchasePrice = row.UnitPurchasePrice,
                                QuantityPerUnit = row.QuantityPerUnit,
                                AllowFractionalSale = row.AllowFractionalSale,
                                FractionalPartsPerUnit = row.FractionalPartsPerUnit,
                                FractionalPartName = row.FractionalPartName,
                                FractionalSalePrice = row.FractionalSalePrice,
                                FractionalSaleCost = row.FractionalSaleCost,
                                IsBaseUnit = row.IsBaseUnit,
                                IsDefaultSaleUnit = row.IsDefaultSaleUnit,
                                IsDefaultPurchaseUnit = row.IsDefaultPurchaseUnit,
                                Unit = new Unit { Name = row.UnitName }
                            },
                            Quantity = row.StockQuantity,
                            PurchasePrice = row.StockPurchasePrice,
                            SalePrice = row.StockSalePrice,
                            ExpiryDate = row.ExpiryDate
                        })
                        .ToList();
                    var hasStock = productRows.Any(row => row.StockQuantity > MinimumSellableQuantity);
                    var currentStock = ResolvePreferredStock(stocksByProductId);
                    product.CurrentSalePrice = product.DefaultSalePrice;
                    product.CurrentPurchasePrice = currentStock?.PurchasePrice ?? 0m;
                    product.CurrentExpiryDate = currentStock?.ExpiryDate;
                    var defaultSaleUnitId = ProductUnitSelector.GetDefaultSaleUnit(productUnits)?.Id;
                    product.CurrentStockQuantity = defaultSaleUnitId.HasValue
                        ? Math.Max(0m, productRows.FirstOrDefault(row => row.ProductUnitId == defaultSaleUnitId.Value)?.StockQuantity ?? 0m)
                        : productRows.Sum(row => Math.Max(0m, row.StockQuantity));
                    product.Supplier = string.IsNullOrWhiteSpace(firstRow.SupplierName)
                        ? null
                        : new RaccoonWarehouse.Domain.Users.DTOs.UserReadDto { Name = firstRow.SupplierName };
                    var rowColor = _rowColorRuleService?.Classify(
                        product.CurrentExpiryDate,
                        product.CurrentStockQuantity,
                        0m,
                        product.SubCategoryId,
                        scope: _ruleScope,
                        productId: product.Id);
                    var searchRow = new ProductSearchRow
                    {
                        Product = product,
                        RowColor = rowColor,
                        ColorRuleId = rowColor?.RuleId ?? string.Empty,
                        ColorGroupName = rowColor?.Label ?? rowColor?.RuleName ?? string.Empty,
                        Units = new ObservableCollection<ProductUnitReadDto>(productUnits),
                        SalePricesByUnitId = stocksByProductId
                            .GroupBy(stock => stock.ProductUnitId)
                            .ToDictionary(group => group.Key, group => group
                                .OrderByDescending(stock => stock.Quantity)
                                .Select(stock => stock.SalePrice)
                                .FirstOrDefault()),
                        StockQuantitiesByUnitId = stocksByProductId
                            .GroupBy(stock => stock.ProductUnitId)
                            .ToDictionary(group => group.Key, group => group.Sum(stock => Math.Max(0m, stock.Quantity))),
                        PurchasePricesByUnitId = stocksByProductId
                            .GroupBy(stock => stock.ProductUnitId)
                            .ToDictionary(group => group.Key, group => group
                                .OrderByDescending(stock => stock.Quantity)
                                .Select(stock => stock.PurchasePrice)
                                .FirstOrDefault()),
                        HasStock = hasStock,
                        // The search dialog is also used by insurance/setup workflows,
                        // where a product may be configured before stock exists.
                        // POS itself still validates stock in its add callback.
                        CanAdd = productUnits.Any(unit =>
                            !_disabledProductKeys.Contains(BuildProductKey(product, unit.Id)))
                    };
                    searchRow.SelectedUnit = productUnits.FirstOrDefault(unit =>
                        unit.Id == defaultSaleUnitId)
                        ?? productUnits.FirstOrDefault();
                    if (searchRow.SelectedUnit == null)
                        searchRow.SalePrice = product.CurrentSalePrice;
                    if (string.IsNullOrWhiteSpace(_selectedColorRuleId) ||
                        string.Equals(searchRow.ColorRuleId, _selectedColorRuleId, StringComparison.OrdinalIgnoreCase))
                        _products.Add(searchRow);
                }

                if (_products.Count > 0)
                {
                    ProductsGrid.SelectedIndex = 0;
                    ProductsGrid.CurrentCell = new DataGridCellInfo(_products[0], ProductsGrid.Columns[0]);
                    ProductsGrid.ScrollIntoView(_products[0]);
                }

                ProductsGrid.Items.Refresh();
                UiText.ApplyTranslations(ProductsGrid);
            }
            catch (Exception ex) when (IsExpectedSearchCancellation(ex, cancellationToken))
            {
                // EF Core/SqlClient can wrap a canceled database command in an
                // InvalidOperationException. A newer keystroke superseding this
                // search is expected and must not show an error.
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, UiText.T("خطأ", "Error"));
            }
            finally
            {
                await RestoreSearchFocusAsync();
                if (serviceCallLockAcquired)
                    _serviceCallLock.Release();
            }
        }

        private static bool IsExpectedSearchCancellation(Exception exception, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
                return false;

            for (var current = exception; current != null; current = current.InnerException)
            {
                if (current is OperationCanceledException)
                    return true;

                // SQL Server reports a canceled command as SqlException, and
                // EF may wrap it in InvalidOperationException when an execution
                // strategy is active. Only suppress the known cancellation
                // message; genuine database failures remain visible.
                if (current.GetType().FullName == "Microsoft.Data.SqlClient.SqlException" &&
                    current.Message.Contains("Operation cancelled by user", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private async Task RestoreSearchFocusAsync()
        {
            await Dispatcher.BeginInvoke(() =>
            {
                if (!IsVisible)
                    return;

                SearchTextBox.Focus();
                Keyboard.Focus(SearchTextBox);
                SearchTextBox.CaretIndex = SearchTextBox.Text.Length;
            }, DispatcherPriority.Input);
        }

        private static bool MatchesCatalogFilter(
            PosProductSearchResultDto row,
            string productName,
            string barcode,
            int? supplierId)
        {
            if (supplierId.HasValue && row.SupplierId != supplierId.Value)
                return false;

            if (!string.IsNullOrWhiteSpace(barcode))
            {
                var itemCode = row.ItemCode?.ToString(CultureInfo.InvariantCulture);
                return string.Equals(itemCode, barcode, StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(row.AlternateBarcode?.Trim(), barcode, StringComparison.OrdinalIgnoreCase);
            }

            var fragments = productName
                .Split(new[] { ' ', '\t', '\r', '\n', '%' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (fragments.Length == 0)
                return supplierId.HasValue;

            var candidate = row.ProductName ?? string.Empty;
            var searchFrom = 0;
            foreach (var fragment in fragments)
            {
                var matchAt = candidate.IndexOf(fragment, searchFrom, StringComparison.CurrentCultureIgnoreCase);
                if (matchAt < 0)
                    return false;

                searchFrom = matchAt + fragment.Length;
            }

            return true;
        }

        private void ProductsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ProductsGrid.SelectedItem is ProductSearchRow row)
            {
                SelectedProduct = row.Product;
                DialogResult = true;
            }
        }

        private async void AddProductBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: ProductSearchRow row })
                await AddProductAsync(row);
        }
private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Clear();
            BarcodeSearchTextBox.Clear();
            _selectedSupplierId = null;
            SupplierFilterComboBox.SelectedValue = 0;
            _products.Clear();
            _selectedColorRuleId = null;
            RefreshColorFilterOptions();
            SearchTextBox.Focus();
            Keyboard.Focus(SearchTextBox);
            SearchTextBox.SelectAll();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ColorFilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            _selectedColorRuleId = button.Tag as string;
            _ = RunSearchSafelyAsync();
        }

        private void RefreshColorFilterOptions()
        {
            _colorGroups.Clear();
            _colorGroups.Add(new ColorGroupOption
            {
                RuleId = string.Empty,
                Name = "All colors",
                BackgroundColor = "#E7EEF5",
                ForegroundColor = "#1F4568",
                AccentColor = "#7B9AB4"
            });

            foreach (var rule in _loadedColorRules
                .Where(rule => rule.IsActive &&
                    (string.IsNullOrWhiteSpace(rule.Scope) ||
                     string.Equals(rule.Scope, "Both", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(rule.Scope, _ruleScope, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(rule => rule.Priority))
            {
                _colorGroups.Add(new ColorGroupOption
                {
                    RuleId = rule.Id,
                    Name = rule.Name,
                    BackgroundColor = rule.BackgroundColor,
                    ForegroundColor = rule.ForegroundColor,
                    AccentColor = rule.AccentColor
                });
            }
        }

        private bool MatchesSelectedColorRule(IEnumerable<PosProductSearchResultDto> productRows)
        {
            if (string.IsNullOrWhiteSpace(_selectedColorRuleId) || _rowColorRuleService == null)
                return true;

            var rows = productRows.ToList();
            var stockRows = rows.Where(row => row.StockQuantity > MinimumSellableQuantity).ToList();
            var expiryDate = stockRows
                .Where(row => row.ExpiryDate.HasValue)
                .OrderBy(row => row.ExpiryDate)
                .Select(row => row.ExpiryDate)
                .FirstOrDefault();
            var defaultSaleUnit = rows.FirstOrDefault(row => row.IsDefaultSaleUnit)
                ?? rows.FirstOrDefault();
            var stockQuantity = defaultSaleUnit?.StockQuantity
                ?? rows.Sum(row => Math.Max(0m, row.StockQuantity));
            var first = rows.FirstOrDefault();
            var decision = _rowColorRuleService.Classify(
                expiryDate,
                stockQuantity,
                0m,
                first?.SubCategoryId,
                scope: _ruleScope,
                productId: first?.ProductId);
            return string.Equals(decision.RuleId, _selectedColorRuleId, StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildProductKey(ProductReadDto product, int? unitId = null)
        {
            var selectedUnitId = unitId ?? ProductUnitSelector.GetDefaultSaleUnit(product.ProductUnits)?.Id ?? 0;
            return $"{product.Id}:{selectedUnitId}";
        }
        private static StockReadDto? ResolvePreferredStock(IEnumerable<StockReadDto> stocks)
        {
            var stockList = stocks?
                .Where(stock => stock != null && stock.Quantity > 0)
                .ToList() ?? new List<StockReadDto>();

            if (stockList.Count == 0)
                return null;

            var product = stockList.First().Product;
            var defaultUnitId = ProductUnitSelector.GetDefaultSaleUnit(product?.ProductUnits)?.Id;
            if (defaultUnitId.HasValue)
            {
                var defaultStock = stockList.FirstOrDefault(stock => stock.ProductUnitId == defaultUnitId.Value);
                if (defaultStock != null)
                    return defaultStock;
            }

            return stockList
                .OrderByDescending(stock => stock.Quantity)
                .ThenBy(stock => stock.ProductUnit?.Unit?.Name)
                .FirstOrDefault();
        }

        private static bool MatchesSearch(ProductReadDto product, string[] searchTerms)
        {
            var name = product.Name ?? string.Empty;
            var itemCode = product.ITEMCODE.ToString();
            var alternateBarcodes = product.ProductUnits?
                .Where(unit => !string.IsNullOrWhiteSpace(unit.AlternateBarcode))
                .Select(unit => unit.AlternateBarcode!)
                .ToArray() ?? Array.Empty<string>();

            return searchTerms.All(term =>
                name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                itemCode.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                alternateBarcodes.Any(barcode => barcode.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        public class ProductSearchRow : INotifyPropertyChanged
        {
            private bool _canAdd;
            private decimal _quantity = 1m;
            private decimal _salePrice;
            private ProductUnitReadDto? _selectedUnit;
            private PosRowColorDecision? _rowColor;
            private string _colorRuleId = string.Empty;
            private string _colorGroupName = string.Empty;
            public ProductReadDto Product { get; set; } = new();

            public PosRowColorDecision? RowColor
            {
                get => _rowColor;
                set
                {
                    if (ReferenceEquals(_rowColor, value))
                        return;

                    _rowColor = value;
                    Notify(nameof(RowColor));
                    Notify(nameof(RowBackground));
                    Notify(nameof(RowForeground));
                    Notify(nameof(BlocksSale));
                    Notify(nameof(RequiresSaleConfirmation));
                    Notify(nameof(SaleAction));
                }
            }

            public string ColorRuleId
            {
                get => _colorRuleId;
                set
                {
                    if (string.Equals(_colorRuleId, value, StringComparison.Ordinal))
                        return;

                    _colorRuleId = value;
                    Notify(nameof(ColorRuleId));
                }
            }

            public string ColorGroupName
            {
                get => _colorGroupName;
                set
                {
                    if (string.Equals(_colorGroupName, value, StringComparison.Ordinal))
                        return;

                    _colorGroupName = value;
                    Notify(nameof(ColorGroupName));
                }
            }

            public string RowBackground => RowColor?.BackgroundColor ?? "#FFFFFF";
            public string RowForeground => RowColor?.ForegroundColor ?? "#263238";
            public ObservableCollection<ProductUnitReadDto> Units { get; set; } = new();
            public Dictionary<int, decimal> SalePricesByUnitId { get; set; } = new();
            public Dictionary<int, decimal> StockQuantitiesByUnitId { get; set; } = new();
            public Dictionary<int, decimal> PurchasePricesByUnitId { get; set; } = new();
            public ProductUnitReadDto? SelectedUnit
            {
                get => _selectedUnit;
                set
                {
                    _selectedUnit = value;
                    _salePrice = value?.SalePrice ?? 0m;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedUnit)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SalePrice)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UnitName)));
                }
            }
            public string ItemCode => Product.ITEMCODE?.ToString() ?? string.Empty;
            public string ProductName => Product.Name ?? string.Empty;
            public DateTime? ExpiryDate => Product.CurrentExpiryDate;
            public string UnitName => SelectedUnit?.Unit?.Name ?? string.Empty;
            public decimal Quantity { get => _quantity; set { _quantity = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Quantity))); } }
            public decimal SalePrice { get => _salePrice; set { _salePrice = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SalePrice))); } }
            public bool HasStock { get; set; }
            public string StockStatus => HasStock ? UiText.T("متوفر", "In stock") : UiText.T("غير متوفر", "Out of stock");
            public bool CanAdd { get => _canAdd; set { if (_canAdd == value) return; _canAdd = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanAdd))); } }
            public bool BlocksSale => RowColor?.BlocksSale == true;
            public bool RequiresSaleConfirmation => RowColor?.RequiresConfirmation == true;
            public string SaleAction => RowColor?.SaleAction ?? PosRowColorActions.None;
            private void Notify(string propertyName)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            public event PropertyChangedEventHandler? PropertyChanged;
        }

        public sealed class ColorGroupOption
        {
            public string RuleId { get; init; } = string.Empty;
            public string Name { get; init; } = string.Empty;
            public string BackgroundColor { get; init; } = "#E7EEF5";
            public string ForegroundColor { get; init; } = "#1F4568";
            public string AccentColor { get; init; } = "#7B9AB4";
        }

        private void ProductsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}

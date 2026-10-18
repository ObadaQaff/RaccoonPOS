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
        private const decimal MinimumSellableQuantity = 0m;
        private const int MaxSearchResults = 100;
        private static readonly object CatalogCacheSync = new();
        private static Task<List<PosProductSearchResultDto>>? _catalogLoadTask;
        private static DateTimeOffset _catalogLoadedAt;
        private static readonly TimeSpan CatalogCacheLifetime = TimeSpan.FromMinutes(10);
        private readonly IProductService _productService;
        private readonly IStockService _stockService;
        private readonly IUserService _userService;
        private readonly Func<ProductSearchRow, Task<bool>>? _onAddProduct;
        private readonly HashSet<string> _disabledProductKeys;
        private readonly DispatcherTimer _searchDebounceTimer = new() { Interval = TimeSpan.FromMilliseconds(150) };
        private readonly SemaphoreSlim _serviceCallLock = new(1, 1);
        private CancellationTokenSource? _searchCancellation;
        private int _searchVersion;
        private bool _isAddingProduct;
        private int? _selectedSupplierId;
        private List<RaccoonWarehouse.Domain.Users.DTOs.UserReadDto> _supplierLookupItems = new();
        private bool _isFilteringSuppliers;
        private bool _isNavigatingSuppliers;
        private string _supplierSearchText = string.Empty;

        public ProductReadDto SelectedProduct { get; private set; }

        private ObservableCollection<ProductSearchRow> _products
            = new ObservableCollection<ProductSearchRow>();

        public ProductSearchWindow(
            IProductService productService,
            IStockService stockService,
            IUserService userService,
            Func<ProductSearchRow, Task<bool>>? onAddProduct = null,
            IEnumerable<string>? disabledProductKeys = null)
        {
            InitializeComponent();
            UiText.ApplyWindow(this);
            _productService = productService;
            _stockService = stockService;
            _userService = userService;
            _onAddProduct = onAddProduct;
            _disabledProductKeys = disabledProductKeys != null
                ? new HashSet<string>(disabledProductKeys)
                : new HashSet<string>();
            ProductsGrid.ItemsSource = _products;
            _searchDebounceTimer.Tick += SearchDebounceTimer_Tick;
            Loaded += ProductSearchWindow_Loaded;
        }

        private async void ProductSearchWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await WarmCatalogCacheAsync();
            await LoadSupplierFilterAsync();
            await Dispatcher.BeginInvoke(() =>
            {
                SearchTextBox.Focus();
                Keyboard.Focus(SearchTextBox);
                SearchTextBox.SelectAll();
            }, DispatcherPriority.Input);
        }

        private async Task WarmCatalogCacheAsync()
            => await WarmCatalogCacheAsync(_stockService);

        public static async Task WarmCatalogCacheAsync(IStockService stockService)
        {
            Task<List<PosProductSearchResultDto>> loadTask;
            lock (CatalogCacheSync)
            {
                if (_catalogLoadTask != null &&
                    DateTimeOffset.UtcNow - _catalogLoadedAt < CatalogCacheLifetime)
                    return;

                loadTask = _catalogLoadTask = LoadCatalogCacheAsync(stockService);
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
            }
        }

        private static async Task<List<PosProductSearchResultDto>> LoadCatalogCacheAsync(IStockService stockService)
        {
            var result = await stockService.GetPosProductCatalogAsync();
            if (!result.Success)
                throw new InvalidOperationException(result.Message ?? "Unable to load POS product catalog.");

            lock (CatalogCacheSync)
                _catalogLoadedAt = DateTimeOffset.UtcNow;

            return result.Data ?? new List<PosProductSearchResultDto>();
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
        }

        private async Task PerformSearchAsync()
        {
            if (_isAddingProduct)
                return;

            var text = SearchTextBox.Text.Trim();
            var barcode = BarcodeSearchTextBox.Text.Trim();
            var searchVersion = ++_searchVersion;
            _searchCancellation?.Cancel();
            _searchCancellation?.Dispose();
            _searchCancellation = new CancellationTokenSource();
            var cancellationToken = _searchCancellation.Token;

            if (text.Length < 2 && barcode.Length == 0 && !_selectedSupplierId.HasValue)
            {
                _products.Clear();
                ProductsGrid.Items.Refresh();
                await RestoreSearchFocusAsync();
                return;
            }

            try
            {
                await _serviceCallLock.WaitAsync(cancellationToken);

                List<PosProductSearchResultDto> searchRows;
                if (TryGetLoadedCatalog(out var catalog))
                {
                    var candidates = catalog
                        .Where(row => MatchesCatalogFilter(row, text, barcode, _selectedSupplierId))
                        .GroupBy(row => row.ProductId)
                        .SelectMany(group => group)
                        .Take(MaxSearchResults * 20)
                        .ToList();

                    var stockResult = await _stockService.GetPosProductStockAsync(
                        candidates.Select(row => row.ProductId), cancellationToken);
                    var stockByUnit = (stockResult.Data ?? new List<PosProductSearchResultDto>())
                        .ToDictionary(row => (row.ProductId, row.ProductUnitId));

                    searchRows = candidates
                        .Select(row => ApplyCachedStock(row, stockByUnit))
                        .GroupBy(row => row.ProductId)
                        .OrderBy(group => group.First().ProductName)
                        .SelectMany(group => group)
                        .Take(MaxSearchResults)
                        .ToList();
                }
                else
                {
                    var searchResult = await _stockService.SearchPosProductsAsync(
                        text.Length >= 2 ? text : null,
                        barcode.Length > 0 ? barcode : null,
                        MaxSearchResults,
                        _selectedSupplierId,
                        cancellationToken);
                    searchRows = searchResult?.Data ?? new List<PosProductSearchResultDto>();
                }

                if (searchVersion != _searchVersion)
                    return;

                _products.Clear();
                foreach (var productRows in searchRows
                    .GroupBy(row => row.ProductId)
                    .OrderBy(group => group.First().ProductName))
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
                    var searchRow = new ProductSearchRow
                    {
                        Product = product,
                        Units = new ObservableCollection<ProductUnitReadDto>(productUnits),
                        SalePricesByUnitId = stocksByProductId
                            .GroupBy(stock => stock.ProductUnitId)
                            .ToDictionary(group => group.Key, group => group
                                .OrderByDescending(stock => stock.Quantity)
                                .Select(stock => stock.SalePrice)
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
            catch (TaskCanceledException)
            {
                // EF Core reports a canceled database command as TaskCanceledException.
            }
            catch (OperationCanceledException)
            {
                // A newer keystroke superseded this search.
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, UiText.T("خطأ", "Error"));
            }
            finally
            {
                await RestoreSearchFocusAsync();
                if (_serviceCallLock.CurrentCount == 0)
                    _serviceCallLock.Release();
            }
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

            if (productName.Length < 2)
                return supplierId.HasValue;

            return (row.ProductName ?? string.Empty).Contains(
                productName, StringComparison.CurrentCultureIgnoreCase);
        }

        private static PosProductSearchResultDto ApplyCachedStock(
            PosProductSearchResultDto row,
            IReadOnlyDictionary<(int ProductId, int ProductUnitId), PosProductSearchResultDto> stockByUnit)
        {
            if (!stockByUnit.TryGetValue((row.ProductId, row.ProductUnitId), out var stock))
                return row;

            return new PosProductSearchResultDto
            {
                ProductId = row.ProductId,
                ProductName = row.ProductName,
                ItemCode = row.ItemCode,
                TaxExempt = row.TaxExempt,
                TaxRate = row.TaxRate,
                ProductUnitId = row.ProductUnitId,
                AlternateBarcode = row.AlternateBarcode,
                UnitName = row.UnitName,
                UnitSalePrice = row.UnitSalePrice,
                UnitPurchasePrice = row.UnitPurchasePrice,
                QuantityPerUnit = row.QuantityPerUnit,
                IsBaseUnit = row.IsBaseUnit,
                IsDefaultSaleUnit = row.IsDefaultSaleUnit,
                IsDefaultPurchaseUnit = row.IsDefaultPurchaseUnit,
                StockQuantity = stock.StockQuantity,
                StockPurchasePrice = stock.StockPurchasePrice,
                StockSalePrice = stock.StockSalePrice,
                ExpiryDate = stock.ExpiryDate,
                SupplierId = row.SupplierId,
                SupplierName = row.SupplierName
            };
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
            SearchTextBox.Focus();
            Keyboard.Focus(SearchTextBox);
            SearchTextBox.SelectAll();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
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
            public ProductReadDto Product { get; set; } = new();
            public ObservableCollection<ProductUnitReadDto> Units { get; set; } = new();
            public Dictionary<int, decimal> SalePricesByUnitId { get; set; } = new();
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
            public event PropertyChangedEventHandler? PropertyChanged;
        }    }
}

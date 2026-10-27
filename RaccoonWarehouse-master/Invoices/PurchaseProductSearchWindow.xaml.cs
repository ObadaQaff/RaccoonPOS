using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Common;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Products;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace RaccoonWarehouse.Invoices
{
    public partial class PurchaseProductSearchWindow : Window
    {
        public sealed class PurchaseProductSearchRow : INotifyPropertyChanged
        {
            private decimal _quantity = 1m;
            private decimal _purchasePrice;
            private decimal _salePrice;
            private DateTime _expiryDate = DateTime.Today.AddMonths(6);
            private ProductUnitReadDto? _selectedUnit;
            public ProductReadDto Product { get; }
            public ObservableCollection<ProductUnitReadDto> Units { get; }
            public ProductUnitReadDto? SelectedUnit
            {
                get => _selectedUnit;
                set
                {
                    _selectedUnit = value;
                    _purchasePrice = value?.PurchasePrice ?? 0m;
                    _salePrice = value?.SalePrice ?? 0m;
                    PropertyChanged?.Invoke(this, new(nameof(SelectedUnit)));
                    PropertyChanged?.Invoke(this, new(nameof(ProductUnitId)));
                    PropertyChanged?.Invoke(this, new(nameof(UnitName)));
                    PropertyChanged?.Invoke(this, new(nameof(QuantityPerUnit)));
                    PropertyChanged?.Invoke(this, new(nameof(PurchasePrice)));
                    PropertyChanged?.Invoke(this, new(nameof(SalePrice)));
                }
            }
            public int ProductUnitId => SelectedUnit?.Id ?? 0;
            public decimal Quantity { get => _quantity; set { _quantity = value; PropertyChanged?.Invoke(this, new(nameof(Quantity))); } }
            public decimal PurchasePrice { get => _purchasePrice; set { _purchasePrice = value; PropertyChanged?.Invoke(this, new(nameof(PurchasePrice))); } }
            public decimal SalePrice { get => _salePrice; set { _salePrice = value; PropertyChanged?.Invoke(this, new(nameof(SalePrice))); } }
            public DateTime ExpiryDate { get => _expiryDate; set { _expiryDate = value; PropertyChanged?.Invoke(this, new(nameof(ExpiryDate))); } }
            public string ItemCode => Product.ITEMCODE?.ToString() ?? string.Empty;
            public string ProductName => Product.Name ?? string.Empty;
            public string UnitName => SelectedUnit?.Unit?.Name ?? string.Empty;
            public decimal QuantityPerUnit => SelectedUnit?.QuantityPerUnit > 0 ? SelectedUnit.QuantityPerUnit : 1m;
            public event PropertyChangedEventHandler? PropertyChanged;
            public PurchaseProductSearchRow(ProductReadDto product)
            {
                Product = product;
                Units = new ObservableCollection<ProductUnitReadDto>((product.ProductUnits ?? Array.Empty<ProductUnitReadDto>()).ToList());
                SelectedUnit = ProductUnitSelector.GetDefaultPurchaseUnit(Units) ?? Units.FirstOrDefault();
            }
        }

        private readonly IProductService _productService;
        private readonly IStockService? _stockService;
        private readonly Func<PurchaseProductSearchRow, Task<bool>>? _onSelectProduct;
        private readonly Func<string, Task>? _onCreateProduct;
        private readonly int? _supplierId;
        private readonly DispatcherTimer _searchDebounceTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };
        private readonly SemaphoreSlim _searchLock = new(1, 1);
        private int _searchVersion;
        private bool _isSelecting;
        private bool _isInitializing;
        private List<PosProductSearchResultDto> _catalogRows = new();
        private int? _selectedSupplierId;
        private ObservableCollection<PurchaseProductSearchRow> Products { get; } = new();
        private ObservableCollection<SupplierOption> SupplierOptions { get; } = new();

        private sealed class SupplierOption
        {
            public int Id { get; init; }
            public string Name { get; init; } = string.Empty;
        }

        public PurchaseProductSearchWindow(
            IProductService productService,
            IStockService stockService,
            Func<PurchaseProductSearchRow, Task<bool>>? onSelectProduct = null,
            Func<string, Task>? onCreateProduct = null,
            int? supplierId = null)
        {
            _productService = productService; _stockService = stockService; _onSelectProduct = onSelectProduct; _onCreateProduct = onCreateProduct; _supplierId = supplierId;
            RaccoonWarehouse.Security.PermissionWindowGuard.Demand(GetType());
            InitializeComponent(); UiText.ApplyWindow(this); ProductsGrid.ItemsSource = Products;
            SupplierFilter.ItemsSource = SupplierOptions;
            _searchDebounceTimer.Tick += SearchDebounceTimer_Tick;
            Loaded += (_, _) => FocusSearchBox();
            CatalogRefreshNotifier.CatalogChanged += CatalogRefreshNotifier_CatalogChanged;
            Closed += (_, _) => CatalogRefreshNotifier.CatalogChanged -= CatalogRefreshNotifier_CatalogChanged;
        }

        public PurchaseProductSearchWindow(
            IProductService productService,
            Func<PurchaseProductSearchRow, Task<bool>>? onSelectProduct = null,
            Func<string, Task>? onCreateProduct = null,
            int? supplierId = null)
            : this(productService, null!, onSelectProduct, onCreateProduct, supplierId)
        {
        }
        private async void PurchaseProductSearchWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F3)
            {
                await UpdateSelectedProductAsync();
                e.Handled = true;
                return;
            }

            if (e.Key != Key.Escape)
                return;

            Close();
            e.Handled = true;
        }

        private async void CatalogRefreshNotifier_CatalogChanged(object? sender, EventArgs e)
        {
            if (!IsLoaded || _isSelecting || _isInitializing)
                return;

            await RefreshCatalogAndSearchAsync();
        }

        private async void UpdateProductBtn_Click(object sender, RoutedEventArgs e)
        {
            await UpdateSelectedProductAsync(sender is Button button
                ? button.DataContext as PurchaseProductSearchRow
                : ProductsGrid.SelectedItem as PurchaseProductSearchRow);
        }

        private async Task UpdateSelectedProductAsync(PurchaseProductSearchRow? selectedRow = null)
        {
            var row = selectedRow ?? ProductsGrid.CurrentItem as PurchaseProductSearchRow ?? ProductsGrid.SelectedItem as PurchaseProductSearchRow;
            if (row == null)
            {
                MessageBox.Show(UiText.T("يجب تحديد الصنف قبل التحديث.", "Select a product before updating."),
                    UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            CommitProductRowEdits(row);
            await WindowManager.ShowDialogAsync<UpdateProduct>(
                WindowSizeType.MediumRectangle,
                window => window.Initialize(row.Product.Id));

            await RefreshCatalogAndSearchAsync();
        }

        private async Task RefreshCatalogAndSearchAsync()
        {
            if (_stockService == null)
                return;

            try
            {
                _catalogRows = (await ProductSearchWindow.GetCatalogSnapshotAsync(_stockService)).ToList();
                await SearchAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("تعذر تحديث كتالوج المنتجات", "Could not refresh the product catalog")}: {ex.Message}",
                    UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void FocusSearchBox() { SearchTextBox.Focus(); Keyboard.Focus(SearchTextBox); SearchTextBox.SelectAll(); }

        private void SalePriceTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
                QueueTextBoxSelection(textBox, DispatcherPriority.ContextIdle);
        }

        private void SalePriceTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
                QueueTextBoxSelection(textBox, DispatcherPriority.Input);
        }

        private void QueueTextBoxSelection(TextBox textBox, DispatcherPriority priority)
        {
            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    if (!textBox.IsKeyboardFocusWithin)
                        textBox.Focus();

                    textBox.SelectAll();
                }),
                priority);
        }

        private void ExpiryDatePicker_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is DatePicker datePicker)
                QueueDatePickerTextSelection(datePicker, DispatcherPriority.ContextIdle);
        }

        private void ExpiryDatePicker_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is DatePicker datePicker)
                QueueDatePickerTextSelection(datePicker, DispatcherPriority.Input);
        }

        private void QueueDatePickerTextSelection(DatePicker datePicker, DispatcherPriority priority)
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

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _isInitializing = true;
            SetLoadingState(true);
            try
            {
                if (_stockService == null)
                    return;

                _catalogRows = (await ProductSearchWindow.GetCatalogSnapshotAsync(_stockService)).ToList();
                SupplierOptions.Clear();
                foreach (var supplier in _catalogRows
                    .Where(row => row.SupplierId.HasValue && !string.IsNullOrWhiteSpace(row.SupplierName))
                    .GroupBy(row => new { Id = row.SupplierId!.Value, Name = row.SupplierName!.Trim() })
                    .OrderBy(group => group.Key.Name))
                    SupplierOptions.Add(new SupplierOption { Id = supplier.Key.Id, Name = supplier.Key.Name });

                if (_supplierId.HasValue)
                {
                    _selectedSupplierId = _supplierId;
                    SupplierFilter.SelectedValue = _supplierId.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("تعذر تحميل كتالوج المنتجات", "Could not load the product catalog")}: {ex.Message}",
                    UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isInitializing = false;
                SetLoadingState(false);
                FocusSearchBox();
                if (SearchTextBox.Text.Trim().Length > 0 || BarcodeSearchTextBox.Text.Trim().Length > 0 || _selectedSupplierId.HasValue)
                    await SearchAsync();
            }
        }

        private void SetLoadingState(bool isLoading)
        {
            // Keep search input available while the shared catalog cache warms.
            // The initializing guard defers the actual query until loading completes.
            SearchTextBox.IsEnabled = true;
            BarcodeSearchTextBox.IsEnabled = true;
            SupplierFilter.IsEnabled = !isLoading;
            ProductsGrid.IsEnabled = !isLoading;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) { if (!_isInitializing) { _searchDebounceTimer.Stop(); _searchDebounceTimer.Start(); } }
        private void BarcodeSearchTextBox_TextChanged(object sender, TextChangedEventArgs e) { if (!_isInitializing) { _searchDebounceTimer.Stop(); _searchDebounceTimer.Start(); } }
        private async void BarcodeSearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) { _searchDebounceTimer.Stop(); await SearchAsync(); e.Handled = true; } }
        private async void SupplierFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            _selectedSupplierId = SupplierFilter.SelectedValue is int id && id > 0 ? id : null;
            await SearchAsync();
        }
        private async void SearchDebounceTimer_Tick(object? sender, EventArgs e) { _searchDebounceTimer.Stop(); await SearchAsync(); }
        private async Task SearchAsync()
        {
            if (_isSelecting || _isInitializing) return;
            var search = SearchTextBox.Text.Trim();
            var barcode = BarcodeLookup.Normalize(BarcodeSearchTextBox.Text);
            if (string.IsNullOrWhiteSpace(BarcodeSearchTextBox.Text)
                && BarcodeLookup.ExtractGs1ProductBarcode(search) != null)
            {
                barcode = BarcodeLookup.Normalize(search);
                search = string.Empty;
            }
            var version = ++_searchVersion;
            var searchTerms = search.Split(new[] { ' ', '\t', '\r', '\n', '%' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (searchTerms.Length == 0 && string.IsNullOrWhiteSpace(barcode) && !_selectedSupplierId.HasValue) { Products.Clear(); return; }
            await _searchLock.WaitAsync();
            try
            {
                if (version != _searchVersion) return;
                Products.Clear();
                IEnumerable<ProductReadDto> products;
                if (_stockService == null)
                {
                    var databaseSearchTerm = searchTerms.FirstOrDefault() ?? barcode;
                    var result = await _productService.GetAllWithAdvancedIncludeAsync(
                        query => query.Include(p => p.ProductUnits!).ThenInclude(productUnit => productUnit.Unit),
                        p => ((p.Name ?? string.Empty).Contains(databaseSearchTerm) ||
                              p.ITEMCODE.ToString().Contains(databaseSearchTerm) ||
                              p.ProductUnits!.Any(unit => unit.AlternateBarcode == databaseSearchTerm)) &&
                             (!_supplierId.HasValue || p.SupplierId == _supplierId.Value));
                    products = result?.Data ?? new List<ProductReadDto>();
                }
                else
                {
                    var catalogProducts = BuildProducts(_catalogRows
                        .Where(row => MatchesCatalogFilter(row, searchTerms, barcode, _selectedSupplierId)));

                    // The shared catalog is optimized for active POS rows. A
                    // purchase product can exist in the products table without
                    // appearing in that catalog, so preserve the original
                    // purchase-search behavior as a fallback when no catalog
                    // row matches.
                    var catalogProductList = catalogProducts.ToList();
                    if (catalogProductList.Count > 0)
                    {
                        products = catalogProductList;
                    }
                    else
                    {
                        var databaseSearchTerm = searchTerms.FirstOrDefault() ?? barcode;
                        var result = await _productService.GetAllWithAdvancedIncludeAsync(
                            query => query.Include(p => p.ProductUnits!).ThenInclude(productUnit => productUnit.Unit),
                            p => (string.IsNullOrWhiteSpace(databaseSearchTerm) ||
                                  (p.Name ?? string.Empty).Contains(databaseSearchTerm) ||
                                  p.ITEMCODE.ToString().Contains(databaseSearchTerm) ||
                                  p.ProductUnits!.Any(unit => unit.AlternateBarcode == databaseSearchTerm)) &&
                                 (!_selectedSupplierId.HasValue || p.SupplierId == _selectedSupplierId.Value));
                        products = result?.Data ?? new List<ProductReadDto>();
                    }
                }

                foreach (var product in products
                    .Where(p => p.DefaultPurchaseUnit != null)
                    .OrderBy(p => p.Name)) Products.Add(new(product));
                CreateProductBtn.Visibility = Products.Count == 0 && _onCreateProduct != null ? Visibility.Visible : Visibility.Collapsed;
                ProductsGrid.SelectedIndex = -1;
                ProductsGrid.UnselectAllCells();
                if (Products.Count > 0)
                {
                    ProductsGrid.SelectedIndex = 0;
                    ProductsGrid.CurrentCell = new DataGridCellInfo(Products[0], ProductsGrid.Columns[0]);
                    ProductsGrid.ScrollIntoView(Products[0]);
                }

                ProductsGrid.Items.Refresh();
                UiText.ApplyTranslations(ProductsGrid);
            }
            catch (Exception ex) { MessageBox.Show($"{UiText.T("تعذر البحث عن الصنف", "Could not search for the product")}: {ex.Message}", UiText.T("خطأ", "Error")); }
            finally { _searchLock.Release(); }
        }
        private static bool MatchesCatalogFilter(PosProductSearchResultDto row, string[] searchTerms, string barcode, int? supplierId)
        {
            if (supplierId.HasValue && row.SupplierId != supplierId.Value) return false;
            if (!string.IsNullOrWhiteSpace(barcode))
            {
                barcode = BarcodeLookup.Normalize(barcode);
                var itemCode = row.ItemCode?.ToString(CultureInfo.InvariantCulture);
                return BarcodeLookup.MatchesItemCode(row.ItemCode, barcode) ||
                       string.Equals(row.AlternateBarcode?.Trim(), barcode, StringComparison.OrdinalIgnoreCase);
            }

            var candidate = row.ProductName ?? string.Empty;
            var searchFrom = 0;
            foreach (var term in searchTerms)
            {
                var matchAt = candidate.IndexOf(term, searchFrom, StringComparison.CurrentCultureIgnoreCase);
                if (matchAt < 0)
                    return false;

                searchFrom = matchAt + term.Length;
            }

            return true;
        }

        private static IEnumerable<ProductReadDto> BuildProducts(IEnumerable<PosProductSearchResultDto> rows)
            => rows.GroupBy(row => row.ProductId).Select(group =>
            {
                var first = group.First();
                return new ProductReadDto
                {
                    Id = first.ProductId,
                    Name = first.ProductName,
                    ITEMCODE = first.ItemCode,
                    TaxExempt = first.TaxExempt,
                    TaxRate = first.TaxRate,
                    SupplierId = first.SupplierId,
                    ProductUnits = group.GroupBy(row => row.ProductUnitId).Select(unitGroup =>
                    {
                        var row = unitGroup.First();
                        return new ProductUnitReadDto
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
                            Unit = new RaccoonWarehouse.Domain.Units.DTOs.UnitReadDto { Name = row.UnitName ?? string.Empty }
                        };
                    }).ToList()
                };
            });
        private async void SearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down && Products.Count > 0)
            {
                ProductsGrid.Focus();
                ProductsGrid.SelectedIndex = 0;
                ProductsGrid.CurrentCell = new DataGridCellInfo(Products[0], ProductsGrid.Columns[1]);
                ProductsGrid.BeginEdit();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter && ProductsGrid.SelectedItem is PurchaseProductSearchRow row)
            {
                CommitProductRowEdits(row);
                await SelectProductAsync(row);
                e.Handled = true;
            }
        }

        private void CommitProductRowEdits(PurchaseProductSearchRow row)
        {
            ProductsGrid.SelectedItem = row;
            ProductsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            ProductsGrid.CommitEdit(DataGridEditingUnit.Row, true);
        }

        private async void ProductsGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (ProductsGrid.CurrentItem is not PurchaseProductSearchRow row) return;

            if (e.KeyboardDevice.Modifiers == ModifierKeys.None
                && IsCellTypingKey(e.Key)
                && ProductsGrid.CurrentCell.Column?.IsReadOnly == false
                && e.OriginalSource is not TextBoxBase)
            {
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
                        }), DispatcherPriority.Input);
                    }
                }
            }

            if (e.Key == Key.Enter)
            {
                CommitProductRowEdits(row);
                var currentColumn = ProductsGrid.CurrentCell.Column;
                var nextColumn = ProductsGrid.Columns
                    .Where(column => column.Visibility == Visibility.Visible)
                    .OrderBy(column => column.DisplayIndex)
                    .FirstOrDefault(column => column.DisplayIndex > currentColumn.DisplayIndex);

                if (nextColumn == null)
                {
                    await SelectProductAsync(row);
                }
                else
                {
                    ProductsGrid.CurrentCell = new DataGridCellInfo(row, nextColumn);
                    ProductsGrid.BeginEdit();
                }

                e.Handled = true;
                return;
            }
            if (e.Key == Key.Up && ProductsGrid.SelectedIndex == 0) { FocusSearchBox(); e.Handled = true; }
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

            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            {
                var child = VisualTreeHelper.GetChild(parent, index);
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
            return key is Key.Back or Key.Delete or Key.Decimal or Key.Add or Key.Subtract
                or Key.Multiply or Key.Divide or Key.OemPlus or Key.OemMinus
                or Key.OemPeriod or Key.OemComma
                || key is >= Key.D0 and <= Key.D9
                || key is >= Key.NumPad0 and <= Key.NumPad9;
        }

        private async void SelectProductBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: PurchaseProductSearchRow row })
            {
                CommitProductRowEdits(row);
                await SelectProductAsync(row);
            }
        }

        private async Task SelectProductAsync(PurchaseProductSearchRow row)
        {
            if (_isSelecting) return;
            CommitProductRowEdits(row);
            if (row.Quantity <= 0 || row.PurchasePrice < 0 || row.SalePrice < 0 || row.ExpiryDate.Date < DateTime.Today) { MessageBox.Show(UiText.T("تحقق من الكمية وأسعار الشراء والبيع وتاريخ الانتهاء.", "Check quantity, purchase/sale prices, and expiry date."), UiText.T("تنبيه", "Notice")); return; }
            _isSelecting = true;
            try { if (_onSelectProduct == null || await _onSelectProduct(row)) { SearchTextBox.Clear(); Products.Clear(); FocusSearchBox(); } }
            finally { _isSelecting = false; }
        }
        private async void CreateProductBtn_Click(object sender, RoutedEventArgs e)
        {
            var search = SearchTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(search) || _onCreateProduct == null)
                return;

            var lockTaken = false;
            try
            {
                // Finish any active search before the parent window reloads products.
                await _searchLock.WaitAsync();
                lockTaken = true;
                _searchVersion++;
                _searchDebounceTimer.Stop();

                await _onCreateProduct(search);
            }
            finally
            {
                if (lockTaken)
                    _searchLock.Release();
            }

            await SearchAsync();
        }
        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Clear();
            BarcodeSearchTextBox.Clear();
            SupplierFilter.SelectedIndex = -1;
            _selectedSupplierId = null;
            Products.Clear();
            CreateProductBtn.Visibility = Visibility.Collapsed;
            FocusSearchBox();
        }
        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();
    }
}

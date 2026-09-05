using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.ProductUnits;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using RaccoonWarehouse.Domain.Stock;
using RaccoonWarehouse.Domain.Stock.DTOs;
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

namespace RaccoonWarehouse.Invoices
{
    public sealed class FlexibleDecimalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is decimal decimalValue ? decimalValue.ToString("0.00000", culture) : value;

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
        private readonly IStockService _stockService;
        private readonly IProductUnitService _productUnitService;
        private readonly Func<ProductSearchRow, Task<bool>>? _onAddProduct;
        private readonly HashSet<string> _disabledProductKeys;
        private readonly DispatcherTimer _searchDebounceTimer = new() { Interval = TimeSpan.FromMilliseconds(150) };
        private readonly SemaphoreSlim _serviceCallLock = new(1, 1);
        private bool _isAddingProduct;

        public ProductReadDto SelectedProduct { get; private set; }

        private ObservableCollection<ProductSearchRow> _products
            = new ObservableCollection<ProductSearchRow>();

        public ProductSearchWindow(
            IStockService stockService,
            IProductUnitService productUnitService,
            Func<ProductSearchRow, Task<bool>>? onAddProduct = null,
            IEnumerable<string>? disabledProductKeys = null)
        {
            InitializeComponent();
            UiText.ApplyWindow(this);
            _stockService = stockService;
            _productUnitService = productUnitService;
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
            await Dispatcher.BeginInvoke(() =>
            {
                SearchTextBox.Focus();
                Keyboard.Focus(SearchTextBox);
                SearchTextBox.SelectAll();
            }, DispatcherPriority.Input);
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
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
            await PerformSearchAsync();
        }

        private async Task PerformSearchAsync()
        {
            if (_isAddingProduct)
                return;

            var text = SearchTextBox.Text.Trim();
            if (text.Length < 2)
            {
                _products.Clear();
                ProductsGrid.Items.Refresh();
                await RestoreSearchFocusAsync();
                return;
            }

            try
            {
                await _serviceCallLock.WaitAsync();

                var result = await _stockService.GetAllWithFilteringAndIncludeAsync(
                    s =>
                        s.Quantity > 0 &&
                        s.Product != null &&
                        (s.Product.Name != null && s.Product.Name.Contains(text) ||
                         s.Product.ITEMCODE.ToString().Contains(text) ||
                         s.Product.ProductUnits != null &&
                         s.Product.ProductUnits.Any(unit => unit.AlternateBarcode != null && unit.AlternateBarcode.Contains(text))),
                    new Expression<Func<Stock, object>>[]
                    {
                        s => s.Product,
                        s => s.Product.ProductUnits,
                        s => s.ProductUnit,
                        s => s.ProductUnit.Unit
                    });

                var rows = (result?.Data ?? new List<StockReadDto>())
                    .Where(s => s.Product != null && s.ProductUnit != null && s.Quantity > 0)
                    .GroupBy(s => s.ProductId)
                    .Where(g => g.Sum(stock => stock.Quantity) > MinimumSellableQuantity)
                    .Select(g => new
                    {
                        Product = g.First().Product!,
                        CurrentStock = ResolvePreferredStock(g),
                        StockSalePrices = g
                            .GroupBy(stock => stock.ProductUnitId)
                            .ToDictionary(group => group.Key, group => group
                                .OrderByDescending(stock => stock.Quantity)
                                .Select(stock => stock.SalePrice)
                                .FirstOrDefault())
                    })
                    .OrderBy(x => x.Product.Name)
                    .ToList();

                var productIds = rows
                    .Select(row => row.Product.Id)
                    .Distinct()
                    .ToList();
                var unitResult = await _productUnitService.GetAllWithFilteringAndIncludeAsync(
                    unit => productIds.Contains(unit.ProductId),
                    unit => unit.Unit);
                var unitsByProductId = (unitResult?.Data ?? new List<ProductUnitReadDto>())
                    .GroupBy(unit => unit.ProductId)
                    .ToDictionary(group => group.Key, group => group.ToList());

                _products.Clear();
                foreach (var item in rows)
                {
                    item.Product.CurrentSalePrice = item.Product.DefaultSalePrice;
                    item.Product.CurrentPurchasePrice = item.CurrentStock?.PurchasePrice ?? 0m;
                    item.Product.CurrentExpiryDate = item.CurrentStock?.ExpiryDate;
                    var productUnits = unitsByProductId.TryGetValue(item.Product.Id, out var hydratedUnits)
                        ? hydratedUnits
                        : (item.Product.ProductUnits ?? Array.Empty<ProductUnitReadDto>()).ToList();
                    var searchRow = new ProductSearchRow
                    {
                        Product = item.Product,
                        Units = new ObservableCollection<ProductUnitReadDto>(productUnits),
                        SalePricesByUnitId = item.StockSalePrices,
                        CanAdd = productUnits.Any(unit =>
                            !_disabledProductKeys.Contains(BuildProductKey(item.Product, unit.Id)))
                    };
                    var defaultSaleUnitId = ProductUnitSelector.GetDefaultSaleUnit(
                        item.Product.ProductUnits ?? Array.Empty<ProductUnitReadDto>())?.Id;
                    searchRow.SelectedUnit = productUnits.FirstOrDefault(unit =>
                        unit.Id == defaultSaleUnitId)
                        ?? productUnits.FirstOrDefault();
                    if (searchRow.SelectedUnit == null)
                        searchRow.SalePrice = item.Product.CurrentSalePrice;
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
            public string UnitName => SelectedUnit?.Unit?.Name ?? string.Empty;
            public decimal Quantity { get => _quantity; set { _quantity = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Quantity))); } }
            public decimal SalePrice { get => _salePrice; set { _salePrice = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SalePrice))); } }
            public bool CanAdd { get => _canAdd; set { if (_canAdd == value) return; _canAdd = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanAdd))); } }
            public event PropertyChangedEventHandler? PropertyChanged;
        }    }
}

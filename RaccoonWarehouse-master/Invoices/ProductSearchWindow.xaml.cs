using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.Stock;
using RaccoonWarehouse.Domain.Stock.DTOs;
using System;
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
    /// <summary>
    /// Interaction logic for ProductSearchWindow.xaml
    /// </summary>
    public partial class ProductSearchWindow : Window
    {
        private const decimal MinimumSellableQuantity = 10m;
        private readonly IStockService _stockService;
        private readonly Func<ProductReadDto, Task<bool>>? _onAddProduct;
        private readonly HashSet<string> _disabledProductKeys;
        private readonly DispatcherTimer _searchDebounceTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };
        private readonly SemaphoreSlim _serviceCallLock = new(1, 1);
        private bool _isAddingProduct;

        public ProductReadDto SelectedProduct { get; private set; }

        private ObservableCollection<ProductSearchRow> _products
            = new ObservableCollection<ProductSearchRow>();

        public ProductSearchWindow(
            IStockService stockService,
            Func<ProductReadDto, Task<bool>>? onAddProduct = null,
            IEnumerable<string>? disabledProductKeys = null)
        {
            InitializeComponent();
            UiText.ApplyWindow(this);
            _stockService = stockService;
            _onAddProduct = onAddProduct;
            _disabledProductKeys = disabledProductKeys != null
                ? new HashSet<string>(disabledProductKeys)
                : new HashSet<string>();
            ProductsGrid.ItemsSource = _products;
            _searchDebounceTimer.Tick += SearchDebounceTimer_Tick;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
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
                return;
            }

            try
            {
                await _serviceCallLock.WaitAsync();

                var result = await _stockService.GetAllWithFilteringAndIncludeAsync(
                    s =>
                        s.Quantity > 0 &&
                        s.Product != null &&
                        (s.Product.Name.Contains(text) ||
                         s.Product.ITEMCODE.ToString().Contains(text)),
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
                        CurrentStock = ResolvePreferredStock(g)
                    })
                    .OrderBy(x => x.Product.Name)
                    .ToList();

                _products.Clear();
                foreach (var item in rows)
                {
                    item.Product.CurrentSalePrice = item.CurrentStock?.SalePrice ?? item.Product.DefaultSalePrice;
                    item.Product.CurrentPurchasePrice = item.CurrentStock?.PurchasePrice ?? 0m;
                    item.Product.CurrentExpiryDate = item.CurrentStock?.ExpiryDate;

                    _products.Add(new ProductSearchRow
                    {
                        Product = item.Product,
                        CurrentSalePrice = item.Product.CurrentSalePrice,
                        CurrentExpiryDate = item.Product.CurrentExpiryDate,
                        CanAdd = !_disabledProductKeys.Contains(BuildProductKey(item.Product))
                    });
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
                if (_serviceCallLock.CurrentCount == 0)
                    _serviceCallLock.Release();
            }
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
            if (sender is not Button { DataContext: ProductSearchRow row } || row.Product == null || !row.CanAdd)
                return;

            _searchDebounceTimer.Stop();

            try
            {
                _isAddingProduct = true;
                await _serviceCallLock.WaitAsync();

                if (_onAddProduct != null && !await _onAddProduct(row.Product))
                    return;

                row.CanAdd = false;
                _disabledProductKeys.Add(BuildProductKey(row.Product));
            }
            finally
            {
                if (_serviceCallLock.CurrentCount == 0)
                    _serviceCallLock.Release();

                _isAddingProduct = false;
            }
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Clear();
            _products.Clear();
            SearchTextBox.Focus();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static string BuildProductKey(ProductReadDto product)
        {
            var unitId = ProductUnitSelector.GetDefaultSaleUnit(product.ProductUnits)?.Id ?? 0;
            return $"{product.Id}:{unitId}";
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

            public ProductReadDto Product { get; set; }
            public decimal CurrentSalePrice { get; set; }
            public DateTime? CurrentExpiryDate { get; set; }

            public bool CanAdd
            {
                get => _canAdd;
                set
                {
                    if (_canAdd == value)
                        return;

                    _canAdd = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanAdd)));
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
        }
    }

}

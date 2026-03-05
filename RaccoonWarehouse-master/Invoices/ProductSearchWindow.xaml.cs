using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.Stock;
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

namespace RaccoonWarehouse.Invoices
{
    /// <summary>
    /// Interaction logic for ProductSearchWindow.xaml
    /// </summary>
    public partial class ProductSearchWindow : Window
    {
        private readonly IStockService _stockService;
        private readonly Func<ProductReadDto, bool>? _onAddProduct;
        private readonly HashSet<string> _disabledProductKeys;
        private CancellationTokenSource _searchCts;

        public ProductReadDto SelectedProduct { get; private set; }

        private ObservableCollection<ProductSearchRow> _products
            = new ObservableCollection<ProductSearchRow>();

        public ProductSearchWindow(
            IStockService stockService,
            Func<ProductReadDto, bool>? onAddProduct = null,
            IEnumerable<string>? disabledProductKeys = null)
        {
            InitializeComponent();
            _stockService = stockService;
            _onAddProduct = onAddProduct;
            _disabledProductKeys = disabledProductKeys != null
                ? new HashSet<string>(disabledProductKeys)
                : new HashSet<string>();
            ProductsGrid.ItemsSource = _products;
        }

        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = SearchTextBox.Text.Trim();

            if (text.Length < 2)
            {
                _products.Clear();
                return;
            }

            // Cancel previous search
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                // ⏳ Debounce (wait until user stops typing)
                await Task.Delay(300, token);

                var result = await _stockService.GetAllWithFilteringAndIncludeAsync(
                    s =>
                        s.Product.Name.Contains(text) ||
                        s.Product.ITEMCODE.ToString().Contains(text),
                    new Expression<Func<Stock, object>>[]
                    {
                s => s.Product,
                s => s.Product.ProductUnits
                    });

                if (token.IsCancellationRequested)
                    return;

                var products = result.Data
                    .Select(s => s.Product)
                    .Where(p => p != null)
                    .GroupBy(p => p.Id)
                    .Select(g => g.First())
                    .ToList();

                _products.Clear();
                foreach (var product in products)
                {
                    _products.Add(new ProductSearchRow
                    {
                        Product = product,
                        CanAdd = !_disabledProductKeys.Contains(BuildProductKey(product))
                    });
                }
            }
            catch (TaskCanceledException)
            {
                // Normal – ignore
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ");
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

        private void AddProductBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: ProductSearchRow row } || row.Product == null || !row.CanAdd)
                return;

            if (_onAddProduct != null && !_onAddProduct(row.Product))
                return;

            row.CanAdd = false;
            _disabledProductKeys.Add(BuildProductKey(row.Product));
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

        public class ProductSearchRow : INotifyPropertyChanged
        {
            private bool _canAdd;

            public ProductReadDto Product { get; set; }

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Products.DTOs;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using RaccoonWarehouse.Helpers.Localization;

namespace RaccoonWarehouse.Invoices
{
    /// <summary>
    /// Interaction logic for SalesReturn.xaml
    /// </summary>
    public partial class SalesReturn : Window
    {
        private const int ProductSearchDelayMs = 300;
        private readonly IProductService _productService;
        private int _productSearchVersion;

        public ObservableCollection<ProductReadDto> Products { get; } = new();

        public SalesReturn(IProductService productService)
        {
            InitializeComponent();
            UiText.ApplyWindow(this);
            _productService = productService;
            ProductBox.AddHandler(TextBox.TextChangedEvent, new TextChangedEventHandler(ProductBox_TextChanged));
            Loaded += SalesReturn_Loaded;
        }

        private async void SalesReturn_Loaded(object sender, RoutedEventArgs e)
        {
            var result = await _productService.GetReadDtoPagedListAsync(
                pageNumber: 1,
                pageSize: 3000,
                orderBy: query => query.OrderBy(product => product.Name),
                includes: new Expression<Func<Product, object>>[]
                {
                    product => product.ProductUnits,
                    product => product.Brand,
                    product => product.SubCategory
                });

            Products.Clear();
            foreach (var product in result.Items.Where(product => product != null))
                Products.Add(product);

            ProductBox.ItemsSource = Products;
        }

        private void ProductBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchVersion = Interlocked.Increment(ref _productSearchVersion);
            _ = DebouncedFilterProductsAsync(searchVersion);
        }

        private async Task DebouncedFilterProductsAsync(int searchVersion)
        {
            await Task.Delay(ProductSearchDelayMs);
            if (searchVersion == _productSearchVersion)
                FilterProducts();
        }

        private void FilterProducts()
        {
            var search = ProductBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(search))
            {
                ProductBox.ItemsSource = null;
                ProductBox.IsDropDownOpen = false;
                return;
            }

            var filtered = Products.Where(product =>
                (!string.IsNullOrEmpty(product.Name) && product.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                product.ITEMCODE.ToString().Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();

            ProductBox.ItemsSource = filtered;
            ProductBox.IsDropDownOpen = filtered.Count > 0;

            var exactMatch = long.TryParse(search, out var barcode)
                ? filtered.FirstOrDefault(product => product.ITEMCODE == barcode)
                : null;

            if (exactMatch != null)
                ProductBox.SelectedItem = exactMatch;
        }
        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SaveReturnBtn_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}

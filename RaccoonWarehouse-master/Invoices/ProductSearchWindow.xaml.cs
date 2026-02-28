using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.Stock;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private CancellationTokenSource _searchCts;

        public ProductReadDto SelectedProduct { get; private set; }

        private ObservableCollection<ProductReadDto> _products
            = new ObservableCollection<ProductReadDto>();

        public ProductSearchWindow(IStockService stockService)
        {
            InitializeComponent();
            _stockService = stockService;
            ProductsGrid.ItemsSource = _products;
        }

        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = SearchTextBox.Text.Trim();

            if (text.Length < 2)
            {
                ProductsGrid.ItemsSource = null;
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

                ProductsGrid.ItemsSource = result.Data
                    .Select(s => s.Product)
                    .Distinct()
                    .ToList();
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
            if (ProductsGrid.SelectedItem is ProductReadDto product)
            {
                SelectedProduct = product;
                DialogResult = true;
            }
        }
        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Clear();
            SearchTextBox.Focus();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

    }

}

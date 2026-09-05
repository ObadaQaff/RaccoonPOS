using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace RaccoonWarehouse.Invoices
{
    public partial class PurchaseProductSearchWindow : Window
    {
        public sealed class PurchaseProductSearchRow : INotifyPropertyChanged
        {
            private decimal _quantity = 1m;
            private decimal _purchasePrice;
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
                    PropertyChanged?.Invoke(this, new(nameof(SelectedUnit)));
                    PropertyChanged?.Invoke(this, new(nameof(ProductUnitId)));
                    PropertyChanged?.Invoke(this, new(nameof(UnitName)));
                    PropertyChanged?.Invoke(this, new(nameof(QuantityPerUnit)));
                    PropertyChanged?.Invoke(this, new(nameof(PurchasePrice)));
                }
            }
            public int ProductUnitId => SelectedUnit?.Id ?? 0;
            public decimal Quantity { get => _quantity; set { _quantity = value; PropertyChanged?.Invoke(this, new(nameof(Quantity))); } }
            public decimal PurchasePrice { get => _purchasePrice; set { _purchasePrice = value; PropertyChanged?.Invoke(this, new(nameof(PurchasePrice))); } }
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
        private readonly Func<PurchaseProductSearchRow, Task<bool>>? _onSelectProduct;
        private readonly Func<string, Task>? _onCreateProduct;
        private readonly DispatcherTimer _searchDebounceTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };
        private readonly SemaphoreSlim _searchLock = new(1, 1);
        private int _searchVersion;
        private bool _isSelecting;
        private ObservableCollection<PurchaseProductSearchRow> Products { get; } = new();

        public PurchaseProductSearchWindow(
            IProductService productService,
            Func<PurchaseProductSearchRow, Task<bool>>? onSelectProduct = null,
            Func<string, Task>? onCreateProduct = null)
        {
            _productService = productService; _onSelectProduct = onSelectProduct; _onCreateProduct = onCreateProduct;
            InitializeComponent(); UiText.ApplyWindow(this); ProductsGrid.ItemsSource = Products;
            _searchDebounceTimer.Tick += SearchDebounceTimer_Tick; Loaded += (_, _) => FocusSearchBox();
        }
        private void PurchaseProductSearchWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape)
                return;

            Close();
            e.Handled = true;
        }
        private void FocusSearchBox() { SearchTextBox.Focus(); Keyboard.Focus(SearchTextBox); SearchTextBox.SelectAll(); }
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) { _searchDebounceTimer.Stop(); _searchDebounceTimer.Start(); }
        private async void SearchDebounceTimer_Tick(object? sender, EventArgs e) { _searchDebounceTimer.Stop(); await SearchAsync(); }
        private async Task SearchAsync()
        {
            if (_isSelecting) return;
            var search = SearchTextBox.Text.Trim(); var version = ++_searchVersion;
            if (search.Length < 1) { Products.Clear(); return; }
            var searchTerms = search.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var databaseSearchTerm = searchTerms[0];
            await _searchLock.WaitAsync();
            try
            {
                var result = await _productService.GetAllWithAdvancedIncludeAsync(
                    query => query
                        .Include(p => p.ProductUnits!)
                        .ThenInclude(productUnit => productUnit.Unit),
                    p => (p.Name ?? string.Empty).Contains(databaseSearchTerm) ||
                         p.ITEMCODE.ToString().Contains(databaseSearchTerm) ||
                         p.ProductUnits!.Any(unit => unit.AlternateBarcode == databaseSearchTerm));
                if (version != _searchVersion) return;
                Products.Clear();
                foreach (var product in (result?.Data ?? new System.Collections.Generic.List<ProductReadDto>())
                    .Where(p => p.DefaultPurchaseUnit != null && MatchesSearch(p, searchTerms))
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
        private static bool MatchesSearch(ProductReadDto product, string[] searchTerms)
        {
            var name = product.Name ?? string.Empty;
            var itemCode = product.ITEMCODE.ToString();
            var alternateBarcodes = product.ProductUnits?.Where(unit => !string.IsNullOrWhiteSpace(unit.AlternateBarcode))
                .Select(unit => unit.AlternateBarcode!)
                .ToArray() ?? Array.Empty<string>();

            return searchTerms.All(term =>
                name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                itemCode.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                alternateBarcodes.Any(barcode => barcode.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }
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
            else if (e.Key == Key.Enter && ProductsGrid.SelectedItem is PurchaseProductSearchRow row) { await SelectProductAsync(row); e.Handled = true; }
        }
        private async void ProductsGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (ProductsGrid.CurrentItem is not PurchaseProductSearchRow row) return;
            if (e.Key == Key.Enter)
            {
                ProductsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                ProductsGrid.CommitEdit(DataGridEditingUnit.Row, true);
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
        private async void SelectProductBtn_Click(object sender, RoutedEventArgs e) { if (sender is Button { DataContext: PurchaseProductSearchRow row }) await SelectProductAsync(row); }
        private async Task SelectProductAsync(PurchaseProductSearchRow row)
        {
            if (_isSelecting) return;
            if (row.Quantity <= 0 || row.PurchasePrice < 0 || row.ExpiryDate.Date < DateTime.Today) { MessageBox.Show(UiText.T("تحقق من الكمية والسعر وتاريخ الانتهاء.", "Check quantity, price, and expiry date."), UiText.T("تنبيه", "Notice")); return; }
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
        private void ClearBtn_Click(object sender, RoutedEventArgs e) { SearchTextBox.Clear(); Products.Clear(); CreateProductBtn.Visibility = Visibility.Collapsed; FocusSearchBox(); }
        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();
    }
}

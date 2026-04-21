using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.Stock.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Helpers.Pdf;
using RaccoonWarehouse.Helpers.Pdf.Reports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.Stocks.Reports
{
    public partial class StockMovementsReport : Window
    {
        private const int ProductSearchPageSize = 25;
        private const int ProductSearchDelayMs = 220;

        private readonly IStockReportService _stockReportService;
        private readonly IProductService _productService;
        private readonly List<ProductReadDto> _defaultProductItems = new();
        private readonly List<ProductReadDto> _productSearchResults = new();
        private readonly SemaphoreSlim _productSearchLock = new(1, 1);

        private List<StockMovementDto> _currentRows = new();
        private TextBox? _productSearchTextBox;
        private int _productSearchVersion;
        private bool _isUpdatingProductCombo;

        public StockMovementsReport(IStockReportService stockReportService, IProductService productService)
        {
            InitializeComponent();
            _stockReportService = stockReportService;
            _productService = productService;
            UiText.ApplyWindow(this);
            Loaded += StockMovementsReport_Loaded;
        }

        private void StockMovementsReport_Loaded(object sender, RoutedEventArgs e)
        {
            FromDatePicker.SelectedDate = DateTime.Today;
            ToDatePicker.SelectedDate = DateTime.Today;
            InitializeProductFilter();
            AttachProductSearchTextBox();
        }

        private void InitializeProductFilter()
        {
            _defaultProductItems.Clear();
            _defaultProductItems.Add(new ProductReadDto { Id = 0, Name = UiText.T("الكل", "All") });
            ResetProductSelection();
        }

        private async void GenerateReportBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime? from = FromDatePicker.SelectedDate?.Date;
                DateTime? to = ToDatePicker.SelectedDate?.Date.AddDays(1).AddTicks(-1);
                var selection = await ResolveSelectedProductIdAsync();
                if (!selection.IsValid)
                    return;

                var data = await _stockReportService.GetStockMovementsAsync(from, to, selection.ProductId);
                _currentRows = data ?? new List<StockMovementDto>();
                MovementsGrid.ItemsSource = _currentRows;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("خطأ", "Error")}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ExportPdfBtn_Click(object sender, RoutedEventArgs e)
        {
            var document = BuildPdfDocument();
            if (document == null)
                return;

            try
            {
                ReportPrintService.ExportPdf(document, this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("خطأ في تصدير التقرير", "Error exporting the report")}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void PrintBtn_Click(object sender, RoutedEventArgs e)
        {
            var document = BuildPdfDocument();
            if (document == null)
                return;

            try
            {
                ReportPrintService.Print(document, this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("خطأ في طباعة التقرير", "Error printing the report")}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private StockMovementsSummaryReportDocument? BuildPdfDocument()
        {
            if (_currentRows.Count == 0)
            {
                MessageBox.Show(
                    UiText.T("اعرض التقرير أولاً قبل التصدير أو الطباعة.", "Show the report first before exporting or printing."),
                    UiText.T("تنبيه", "Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return null;
            }

            return new StockMovementsSummaryReportDocument(_currentRows, FromDatePicker.SelectedDate, ToDatePicker.SelectedDate);
        }

        private void AttachProductSearchTextBox()
        {
            if (ProductComboBox.Template.FindName("PART_EditableTextBox", ProductComboBox) is not TextBox textBox)
                return;

            if (ReferenceEquals(_productSearchTextBox, textBox))
                return;

            if (_productSearchTextBox != null)
                _productSearchTextBox.TextChanged -= ProductSearchTextBox_TextChanged;

            _productSearchTextBox = textBox;
            _productSearchTextBox.TextChanged += ProductSearchTextBox_TextChanged;
        }

        private void ProductSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingProductCombo || _productSearchTextBox == null)
                return;

            var searchText = _productSearchTextBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(searchText))
            {
                Interlocked.Increment(ref _productSearchVersion);
                ResetProductSelection();
                return;
            }

            ClearCurrentProductSelection();
            var searchVersion = Interlocked.Increment(ref _productSearchVersion);
            _ = SearchProductsAsync(searchText, searchVersion);
        }

        private async Task SearchProductsAsync(string searchText, int searchVersion)
        {
            try
            {
                await Task.Delay(ProductSearchDelayMs);
                if (searchVersion != _productSearchVersion)
                    return;

                await _productSearchLock.WaitAsync();
                try
                {
                    if (searchVersion != _productSearchVersion)
                        return;

                    var matches = await QueryProductsAsync(searchText, ProductSearchPageSize);
                    if (searchVersion != _productSearchVersion)
                        return;

                    ApplyProductSearchResults(matches, searchText);
                }
                finally
                {
                    _productSearchLock.Release();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("تعذر البحث عن الصنف", "Could not search for the item")}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ApplyProductSearchResults(List<ProductReadDto> matches, string searchText)
        {
            _productSearchResults.Clear();
            _productSearchResults.AddRange(matches);

            _isUpdatingProductCombo = true;
            try
            {
                ProductComboBox.ItemsSource = _productSearchResults;
                ProductComboBox.SelectedIndex = -1;
                ProductComboBox.IsDropDownOpen = _productSearchResults.Count > 0;
                ProductComboBox.Text = searchText;
            }
            finally
            {
                _isUpdatingProductCombo = false;
            }
        }

        private void ClearCurrentProductSelection()
        {
            _isUpdatingProductCombo = true;
            try
            {
                ProductComboBox.SelectedItem = null;
                ProductComboBox.SelectedValue = null;
            }
            finally
            {
                _isUpdatingProductCombo = false;
            }
        }

        private async Task<(bool IsValid, int? ProductId)> ResolveSelectedProductIdAsync()
        {
            if (ProductComboBox.SelectedValue is int selectedProductId)
                return (true, selectedProductId == 0 ? null : selectedProductId);

            var searchText = _productSearchTextBox?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(searchText))
                return (true, null);

            var localMatches = _productSearchResults
                .Where(product => ProductMatchesSearch(product, searchText))
                .Take(2)
                .ToList();

            var localExact = localMatches.FirstOrDefault(product => IsExactProductMatch(product, searchText));
            if (localExact != null)
            {
                SelectProduct(localExact);
                return (true, localExact.Id);
            }

            if (localMatches.Count == 1)
            {
                SelectProduct(localMatches[0]);
                return (true, localMatches[0].Id);
            }

            var databaseMatches = await QueryProductsAsync(searchText, 2);
            var exactDatabaseMatch = databaseMatches.FirstOrDefault(product => IsExactProductMatch(product, searchText));
            if (exactDatabaseMatch != null)
            {
                SelectProduct(exactDatabaseMatch);
                return (true, exactDatabaseMatch.Id);
            }

            if (databaseMatches.Count == 1)
            {
                SelectProduct(databaseMatches[0]);
                return (true, databaseMatches[0].Id);
            }

            MessageBox.Show(
                UiText.T("يرجى اختيار صنف صحيح من القائمة.", "Please choose a valid item from the list."),
                UiText.T("تنبيه", "Notice"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return (false, null);
        }

        private async Task<List<ProductReadDto>> QueryProductsAsync(string searchText, int pageSize)
        {
            var normalized = searchText.Trim();
            var hasBarcode = long.TryParse(normalized, out var barcodeValue);

            Expression<Func<Product, bool>> filter = product =>
                !product.IsDeleted &&
                ((product.Name != null && product.Name.Contains(normalized)) ||
                 (hasBarcode && product.ITEMCODE == barcodeValue));

            var page = await _productService.GetReadDtoPagedListAsync(
                pageNumber: 1,
                pageSize: pageSize,
                filter: filter,
                orderBy: q => q.OrderBy(p => p.Name).ThenBy(p => p.ITEMCODE));

            return page?.Items?
                .Where(product => !product.IsDeleted)
                .GroupBy(product => product.Id)
                .Select(group => group.First())
                .ToList() ?? new List<ProductReadDto>();
        }

        private void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingProductCombo || ProductComboBox.SelectedItem is not ProductReadDto selectedProduct)
                return;

            if (selectedProduct.Id == 0)
            {
                ResetProductSelection();
                return;
            }

            SelectProduct(selectedProduct);
        }

        private void ResetProductSelection()
        {
            _productSearchResults.Clear();

            _isUpdatingProductCombo = true;
            try
            {
                ProductComboBox.ItemsSource = _defaultProductItems;
                ProductComboBox.SelectedValue = 0;
                ProductComboBox.IsDropDownOpen = false;
            }
            finally
            {
                _isUpdatingProductCombo = false;
            }
        }

        private void SelectProduct(ProductReadDto product)
        {
            _isUpdatingProductCombo = true;
            try
            {
                ProductComboBox.ItemsSource = new[] { product };
                ProductComboBox.SelectedValue = product.Id;
                ProductComboBox.Text = product.Name ?? string.Empty;
                ProductComboBox.IsDropDownOpen = false;

                if (_productSearchTextBox != null)
                    _productSearchTextBox.CaretIndex = _productSearchTextBox.Text.Length;
            }
            finally
            {
                _isUpdatingProductCombo = false;
            }
        }

        private static bool ProductMatchesSearch(ProductReadDto product, string searchText)
        {
            return product.Name?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true
                || string.Equals(product.ITEMCODE?.ToString(), searchText, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsExactProductMatch(ProductReadDto product, string searchText)
        {
            return string.Equals(product.Name, searchText, StringComparison.OrdinalIgnoreCase)
                || string.Equals(product.ITEMCODE?.ToString(), searchText, StringComparison.OrdinalIgnoreCase);
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

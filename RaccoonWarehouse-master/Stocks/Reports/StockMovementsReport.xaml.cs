using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Stock.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Helpers;
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
        private const int MovementSearchDelayMs = 2000;

        private readonly IStockReportService _stockReportService;
        private readonly IProductService _productService;
        private readonly IUserService _userService;
        private readonly List<ProductReadDto> _defaultProductItems = new();
        private readonly List<ProductReadDto> _productSearchResults = new();
        private readonly SemaphoreSlim _productSearchLock = new(1, 1);

        private List<StockMovementDto> _currentRows = new();
        private TextBox? _productSearchTextBox;
        private int _productSearchVersion;
        private int _movementSearchVersion;
        private readonly SemaphoreSlim _movementLoadLock = new(1, 1);
        private bool _isUpdatingProductCombo;
        private List<RaccoonWarehouse.Domain.Users.DTOs.UserReadDto> _supplierItems = new();

        public StockMovementsReport(IStockReportService stockReportService, IProductService productService, IUserService userService)
        {
            RaccoonWarehouse.Security.PermissionWindowGuard.Demand(GetType());
            InitializeComponent();
            SupplierComboBoxSearch.Attach(SupplierComboBox);
            _stockReportService = stockReportService;
            _productService = productService;
            _userService = userService;
            UiText.ApplyWindow(this);
            Loaded += StockMovementsReport_Loaded;
        }

        private async void StockMovementsReport_Loaded(object sender, RoutedEventArgs e)
        {
            FromDatePicker.SelectedDate = DateTime.Today;
            ToDatePicker.SelectedDate = DateTime.Today;
            InitializeProductFilter();
            InitializeMovementSourceFilter();
            SearchText.TextChanged += SearchText_TextChanged;
            AttachProductSearchTextBox();
            await LoadSupplierFilterAsync();
        }

        private void InitializeMovementSourceFilter()
        {
            MovementSourceComboBox.Items.Clear();
            MovementSourceComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("الكل", "All sources"), Tag = StockMovementSource.All });
            MovementSourceComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("بيع", "Sale"), Tag = StockMovementSource.Sale });
            MovementSourceComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("مرتجع بيع", "Sales return"), Tag = StockMovementSource.Return });
            MovementSourceComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("شراء", "Purchase"), Tag = StockMovementSource.Purchase });
            MovementSourceComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("مرتجع شراء", "Purchase return"), Tag = StockMovementSource.PurchaseReturn });
            MovementSourceComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("إدخال مخزون", "Stock in"), Tag = StockMovementSource.StockIn });
            MovementSourceComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("إخراج مخزون", "Stock out"), Tag = StockMovementSource.StockOut });
            MovementSourceComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("تسوية", "Adjustment"), Tag = StockMovementSource.Adjustment });
            MovementSourceComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("تالف", "Damage"), Tag = StockMovementSource.Damage });
            MovementSourceComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("سند", "Voucher"), Tag = StockMovementSource.Voucher });
            MovementSourceComboBox.SelectedIndex = 0;
        }

        private async Task LoadSupplierFilterAsync()
        {
            _supplierItems = (await _userService.GetAllAsync()).Data?
                .Where(user => user.Role == RaccoonWarehouse.Domain.Enums.UserRole.Supplier)
                .OrderBy(user => user.Name)
                .ToList() ?? new();
            _supplierItems.Insert(0, new RaccoonWarehouse.Domain.Users.DTOs.UserReadDto { Id = 0, Name = UiText.T("الكل", "All suppliers") });
            SupplierComboBoxSearch.SetItems(SupplierComboBox, _supplierItems);
            SupplierComboBox.SelectedIndex = 0;
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
                var supplierId = SupplierComboBox.SelectedValue is int selectedSupplierId && selectedSupplierId > 0
                    ? selectedSupplierId
                    : (int?)null;
                var source = (MovementSourceComboBox.SelectedItem as ComboBoxItem)?.Tag is StockMovementSource selectedSource
                    ? selectedSource
                    : StockMovementSource.All;
                var data = await _stockReportService.GetStockMovementsAsync(
                    from,
                    to,
                    productId: null,
                    supplierId,
                    source,
                    GetSearchText());
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

        private string? GetSearchText() => string.IsNullOrWhiteSpace(SearchText.Text) ? null : SearchText.Text.Trim();

        private async void SearchText_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchVersion = Interlocked.Increment(ref _movementSearchVersion);
            await DebouncedLoadMovementAsync(searchVersion);
        }

        private async Task DebouncedLoadMovementAsync(int searchVersion)
        {
            try
            {
                await Task.Delay(MovementSearchDelayMs);
                if (searchVersion != _movementSearchVersion)
                    return;

                await LoadMovementReportAsync(searchVersion);
            }
            catch (Exception ex)
            {
                if (searchVersion != _movementSearchVersion)
                    return;

                MessageBox.Show(
                    $"{UiText.T("خطأ أثناء تحميل الحركات", "Error loading movements")}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task LoadMovementReportAsync(int searchVersion)
        {
            await _movementLoadLock.WaitAsync();
            try
            {
                if (searchVersion != _movementSearchVersion)
                    return;

                DateTime? from = FromDatePicker.SelectedDate?.Date;
                DateTime? to = ToDatePicker.SelectedDate?.Date.AddDays(1).AddTicks(-1);
                var supplierId = SupplierComboBox.SelectedValue is int selectedSupplierId && selectedSupplierId > 0
                    ? selectedSupplierId
                    : (int?)null;
                var source = (MovementSourceComboBox.SelectedItem as ComboBoxItem)?.Tag is StockMovementSource selectedSource
                    ? selectedSource
                    : StockMovementSource.All;
                var data = await _stockReportService.GetStockMovementsAsync(from, to, null, supplierId, source, GetSearchText());
                if (searchVersion != _movementSearchVersion)
                    return;

                _currentRows = data ?? new List<StockMovementDto>();
                MovementsGrid.ItemsSource = _currentRows;
            }
            finally
            {
                _movementLoadLock.Release();
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

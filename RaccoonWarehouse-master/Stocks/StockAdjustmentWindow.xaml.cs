using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.Stock.DTOs;
using RaccoonWarehouse.Domain.StockAdjustments.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace RaccoonWarehouse.Stocks
{
    public partial class StockAdjustmentWindow : Window
    {
        private readonly IStockService _stockService;
        private readonly IProductService _productService;
        private readonly IUserSession _userSession;
        private readonly ILoadingService _loadingService;
        private readonly List<ProductReadDto> _loadedProducts = new();
        private TextBox? _productSearchTextBox;
        private TextBox? _batchSearchTextBox;
        private ScrollViewer? _productDropdownScrollViewer;
        private bool _isUpdatingProductSearch;
        private bool _isUpdatingBatchSearch;
        private bool _isLoadingProducts;
        private bool _isLoadingBatches;
        private bool _hasMoreProductPages = true;
        private bool _hasMoreBatchPages;
        private int _nextProductPage = 1;
        private int _nextBatchPage = 1;
        private string _currentProductSearchText = string.Empty;
        private string _currentBatchSearchText = string.Empty;
        private int? _currentBatchProductId;
        private int _batchSearchVersion;
        private const int ProductPageSize = 80;
        private const int BatchPageSize = 80;
        private readonly SemaphoreSlim _productLoadLock = new(1, 1);
        private readonly SemaphoreSlim _batchLoadLock = new(1, 1);
        private readonly SemaphoreSlim _dataQueryLock = new(1, 1);
        private int _productSearchVersion;
        private Task? _initialLoadTask;
        private bool _initialDataLoaded;

        public StockAdjustmentWindow(IStockService stockService, IProductService productService, IUserSession userSession, ILoadingService loadingService)
        {
            _stockService = stockService;
            _productService = productService;
            _userSession = userSession;
            _loadingService = loadingService;


            InitializeComponent();
            UiText.ApplyWindow(this);
            Loaded += StockAdjustmentWindow_Loaded;
        }

        public int? InitialProductId { get; set; }
        public bool SavedSuccessfully { get; private set; }

        private async void StockAdjustmentWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _initialLoadTask ??= LoadDataAsync();
            await _initialLoadTask;
        }

        private async Task LoadDataAsync()
        {
            _loadingService.Show();

            ProductComboBox.ItemsSource = _loadedProducts;
            AdjustmentTypeComboBox.ItemsSource = Enum.GetValues(typeof(StockAdjustmentType))
                .Cast<StockAdjustmentType>()
                .ToList();

            AdjustmentTypeComboBox.SelectedItem = StockAdjustmentType.Increase;
            if (string.IsNullOrWhiteSpace(BatchSummaryTextBlock.Text))
                BatchSummaryTextBlock.Text = UiText.T("اختر صنفا ودفعة لبدء التسوية.", "Select a product and batch to start the adjustment.");

            await ResetAndLoadProductsAsync(string.Empty, false);

            if (InitialProductId.HasValue)
            {
                var initialProduct = _loadedProducts.FirstOrDefault(x => x.Id == InitialProductId.Value);
                if (initialProduct == null)
                {
                    await ResetAndLoadProductsAsync(InitialProductId.Value.ToString(CultureInfo.InvariantCulture), false);
                    initialProduct = _loadedProducts.FirstOrDefault(x => x.Id == InitialProductId.Value);
                }

                if (initialProduct != null)
                    ProductComboBox.SelectedItem = initialProduct;
            }
            _initialDataLoaded = true;
            _loadingService.Hide();
        }

        private async void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductComboBox.SelectedItem is ProductReadDto selectedProduct)
                SetComboBoxText(ProductComboBox, _productSearchTextBox, selectedProduct.Name);

            if (ProductComboBox.SelectedItem is not ProductReadDto product)
            {
                _currentBatchProductId = null;
                ++_batchSearchVersion;
                BatchComboBox.ItemsSource = null;
                BatchComboBox.SelectedItem = null;
                BatchSummaryTextBlock.Text = UiText.T("اختر صنفا لعرض الدفعات المتاحة.", "Select a product to view available batches.");
                return;
            }

            await ResetAndLoadBatchesAsync(product.Id, string.Empty);
        }

        private void BatchComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BatchComboBox.SelectedItem is not StockBatchLookupDto batch)
                return;

            SetComboBoxText(BatchComboBox, _batchSearchTextBox, batch.DisplayName);
            PurchasePriceTextBox.Text = batch.PurchasePrice.ToString("0.000", CultureInfo.InvariantCulture);
            SalePriceTextBox.Text = batch.SalePrice.ToString("0.000", CultureInfo.InvariantCulture);
            ExpiryDatePicker.SelectedDate = batch.ExpiryDate;
            BatchSummaryTextBlock.Text = UiText.IsEnglish
                ? $"Status: {GetBatchStatusLabel(batch.Status)} | Used: {(batch.IsUsed ? "Yes" : "No")} | Original Qty: {batch.OriginalQuantity:0.000} | Remaining: {batch.RemainingQuantity:0.000} | Purchase Price: {batch.PurchasePrice:0.000} | Sale Price: {batch.SalePrice:0.000}"
                : $"الحالة: {GetBatchStatusLabel(batch.Status)} | مستخدمة: {(batch.IsUsed ? "نعم" : "لا")} | الكمية الأصلية: {batch.OriginalQuantity:0.000} | المتبقي: {batch.RemainingQuantity:0.000} | سعر الشراء: {batch.PurchasePrice:0.000} | سعر البيع: {batch.SalePrice:0.000}";
        }

        private void AdjustmentTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AdjustmentTypeComboBox.SelectedItem is not StockAdjustmentType type)
                return;

            var requiresReplacementValues = type is StockAdjustmentType.Replace or StockAdjustmentType.CloseAndRecreate;
            PurchasePriceTextBox.IsEnabled = requiresReplacementValues;
            SalePriceTextBox.IsEnabled = requiresReplacementValues;
            ExpiryDatePicker.IsEnabled = requiresReplacementValues || type == StockAdjustmentType.Increase;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (BatchComboBox.SelectedItem is not StockBatchLookupDto batch)
            {
                MessageBox.Show(UiText.T("يرجى اختيار دفعة.", "Please select a batch."), UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (AdjustmentTypeComboBox.SelectedItem is not StockAdjustmentType type)
            {
                MessageBox.Show(UiText.T("يرجى اختيار نوع الإجراء.", "Please select an adjustment type."), UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(QuantityTextBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var quantity))
                quantity = 0m;

            var dto = new StockAdjustmentWriteDto
            {
                ProductId = batch.ProductId,
                ProductUnitId = batch.ProductUnitId,
                StockLotId = batch.StockLotId,
                AdjustmentType = type,
                QuantityDelta = quantity,
                PurchasePrice = TryParseOptionalDecimal(PurchasePriceTextBox.Text),
                SalePrice = TryParseOptionalDecimal(SalePriceTextBox.Text),
                ExpiryDate = ExpiryDatePicker.SelectedDate,
                Reason = ReasonTextBox.Text?.Trim() ?? string.Empty,
                Reference = ReferenceTextBox.Text?.Trim(),
                AdjustmentDate = DateTime.Now,
                UserId = _userSession.CurrentUser?.Id
            };

            _loadingService.Show();
            var result = await _stockService.CreateAdjustmentAsync(dto);
            if (!result.Success)
            {
                _loadingService.Hide();
                MessageBox.Show(result.Message ?? UiText.T("فشل حفظ التسوية.", "Failed to save the adjustment."), UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _loadingService.Hide();
            MessageBox.Show(UiText.T("تم حفظ التسوية بنجاح.", "Adjustment saved successfully."), UiText.T("نجاح", "Success"), MessageBoxButton.OK, MessageBoxImage.Information);
            SavedSuccessfully = true;
            Close();
        }

        private void ProductComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            _productSearchTextBox = ProductComboBox.Template.FindName("PART_EditableTextBox", ProductComboBox) as TextBox;
            if (_productSearchTextBox != null)
                _productSearchTextBox.TextChanged += ProductSearchTextBox_TextChanged;
        }

        private async void ProductComboBox_DropDownOpened(object sender, EventArgs e)
        {
            await EnsureInitialDataLoadedAsync();

            if (_loadedProducts.Count == 0 && !_isLoadingProducts)
                await ResetAndLoadProductsAsync(_productSearchTextBox?.Text?.Trim() ?? string.Empty);

            Dispatcher.BeginInvoke(() =>
            {
                if (_productDropdownScrollViewer != null)
                    _productDropdownScrollViewer.ScrollChanged -= ProductDropdownScrollViewer_ScrollChanged;

                _productDropdownScrollViewer = FindVisualChild<ScrollViewer>(ProductComboBox);
                if (_productDropdownScrollViewer != null)
                    _productDropdownScrollViewer.ScrollChanged += ProductDropdownScrollViewer_ScrollChanged;
            });
        }

        private void BatchComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            _batchSearchTextBox = BatchComboBox.Template.FindName("PART_EditableTextBox", BatchComboBox) as TextBox;
            if (_batchSearchTextBox != null)
                _batchSearchTextBox.TextChanged += BatchSearchTextBox_TextChanged;
        }

        private async void BatchComboBox_DropDownOpened(object sender, EventArgs e)
        {
            if (ProductComboBox.SelectedItem is ProductReadDto product && _currentBatchProductId != product.Id)
                await ResetAndLoadBatchesAsync(product.Id, _batchSearchTextBox?.Text?.Trim() ?? string.Empty);

            Dispatcher.BeginInvoke(() =>
            {
                var scrollViewer = FindVisualChild<ScrollViewer>(BatchComboBox);
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollChanged -= BatchDropdownScrollViewer_ScrollChanged;
                    scrollViewer.ScrollChanged += BatchDropdownScrollViewer_ScrollChanged;
                }
            });
        }

        private async void ProductSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingProductSearch)
                return;

            await EnsureInitialDataLoadedAsync();
            var searchText = _productSearchTextBox?.Text?.Trim() ?? string.Empty;
            if (ProductComboBox.SelectedItem != null)
                ProductComboBox.SelectedItem = null;
            await ResetAndLoadProductsAsync(searchText);
            ProductComboBox.IsDropDownOpen = true;
        }

        private async void BatchSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingBatchSearch || ProductComboBox.SelectedItem is not ProductReadDto product)
                return;

            var searchText = _batchSearchTextBox?.Text?.Trim() ?? string.Empty;
            await ResetAndLoadBatchesAsync(product.Id, searchText);
            BatchComboBox.IsDropDownOpen = true;
        }

        private void ProductComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Escape)
                return;

            if (e.Key != Key.Enter || ProductComboBox.Items.Count == 0)
            {
                ProductComboBox.IsDropDownOpen = true;
                return;
            }

            if (ProductComboBox.SelectedItem == null)
                e.Handled = true;
        }

        private void ProductComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (ProductComboBox.SelectedItem is ProductReadDto selectedProduct)
            {
                SetComboBoxText(ProductComboBox, _productSearchTextBox, selectedProduct.Name);
                return;
            }

        }

        private async void ProductDropdownScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            await EnsureInitialDataLoadedAsync();

            if (e.VerticalChange <= 0)
                return;

            if (e.VerticalOffset + e.ViewportHeight < e.ExtentHeight - 40)
                return;

            await LoadNextProductPageAsync();
        }

        private async void BatchDropdownScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange > 0 && e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 40)
                await LoadNextBatchPageAsync();
        }

        private void SetComboBoxText(ComboBox comboBox, TextBox? textBox, string? text)
        {
            if (comboBox == ProductComboBox)
                _isUpdatingProductSearch = true;
            else if (comboBox == BatchComboBox)
                _isUpdatingBatchSearch = true;

            comboBox.Text = text ?? string.Empty;
            if (textBox != null)
                textBox.Text = text ?? string.Empty;

            if (comboBox == ProductComboBox)
                _isUpdatingProductSearch = false;
            else if (comboBox == BatchComboBox)
                _isUpdatingBatchSearch = false;
        }

        private static decimal? TryParseOptionalDecimal(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
                ? value
                : null;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static string GetBatchStatusLabel(BatchStatus status)
        {
            return status switch
            {
                BatchStatus.Active => UiText.T("نشطة", "Active"),
                BatchStatus.Closed => UiText.T("مغلقة", "Closed"),
                BatchStatus.Replaced => UiText.T("مستبدلة", "Replaced"),
                _ => status.ToString()
            };
        }

        private async Task ResetAndLoadProductsAsync(string searchText, bool showLoading = true)
        {
            var searchVersion = ++_productSearchVersion;
            _currentProductSearchText = searchText;
            _nextProductPage = 1;
            _hasMoreProductPages = true;

            _loadedProducts.Clear();
            ProductComboBox.ItemsSource = _loadedProducts;
            ProductComboBox.Items.Refresh();

            await LoadNextProductPageAsync(searchVersion, showLoading);
        }

        private async Task ResetAndLoadBatchesAsync(int productId, string searchText)
        {
            var searchVersion = ++_batchSearchVersion;
            _currentBatchProductId = productId;
            _currentBatchSearchText = searchText;
            _nextBatchPage = 1;
            _hasMoreBatchPages = true;
            BatchComboBox.ItemsSource = new List<StockBatchLookupDto>();
            if (string.IsNullOrWhiteSpace(searchText))
                SetComboBoxText(BatchComboBox, _batchSearchTextBox, string.Empty);
            BatchSummaryTextBlock.Text = UiText.T("جاري تحميل الدفعات...", "Loading batches...");
            await LoadNextBatchPageAsync(searchVersion);
        }

        private async Task LoadNextBatchPageAsync(int? expectedSearchVersion = null)
        {
            if (_isLoadingBatches || !_hasMoreBatchPages || !_currentBatchProductId.HasValue)
                return;

            await _batchLoadLock.WaitAsync();
            try
            {
                if (_isLoadingBatches || !_hasMoreBatchPages || !_currentBatchProductId.HasValue)
                    return;

                if (expectedSearchVersion.HasValue && expectedSearchVersion.Value != _batchSearchVersion)
                    return;

                _isLoadingBatches = true;
                var pageNumber = _nextBatchPage;
                await _dataQueryLock.WaitAsync();
                Result<PagedResult<StockBatchLookupDto>> result;
                try
                {
                    result = await _stockService.GetBatchLookupPageAsync(
                        _currentBatchProductId.Value,
                        pageNumber,
                        BatchPageSize,
                        _currentBatchSearchText);
                }
                finally
                {
                    _dataQueryLock.Release();
                }

                if (expectedSearchVersion.HasValue && expectedSearchVersion.Value != _batchSearchVersion)
                    return;

                if (!result.Success || result.Data == null)
                {
                    BatchSummaryTextBlock.Text = result.Message ?? UiText.T("فشل تحميل الدفعات.", "Failed to load batches.");
                    _hasMoreBatchPages = false;
                    return;
                }

                var items = result.Data.Items.ToList();
                var existing = (BatchComboBox.ItemsSource as IEnumerable<StockBatchLookupDto>)?.ToList()
                    ?? new List<StockBatchLookupDto>();
                existing.AddRange(items.Where(x => existing.All(e => e.StockLotId != x.StockLotId)));
                BatchComboBox.ItemsSource = existing;
                BatchComboBox.Items.Refresh();

                _nextBatchPage++;
                _hasMoreBatchPages = pageNumber < result.Data.TotalPages;

                if (pageNumber == 1)
                {
                    if (existing.Count == 0)
                        BatchSummaryTextBlock.Text = UiText.T("لا توجد دفعات مخزنية لهذا الصنف.", "There are no stock batches for this product.");
                    else
                        BatchSummaryTextBlock.Text = UiText.T("اختر دفعة لعرض تفاصيلها.", "Select a batch to view its details.");
                }
            }
            finally
            {
                _isLoadingBatches = false;
                _batchLoadLock.Release();
            }
        }

        private async Task LoadNextProductPageAsync(int? expectedSearchVersion = null, bool showLoading = false)
        {
            if (_isLoadingProducts || !_hasMoreProductPages)
                return;

            await _productLoadLock.WaitAsync();
            try
            {
                if (_isLoadingProducts || !_hasMoreProductPages)
                    return;

                if (expectedSearchVersion.HasValue && expectedSearchVersion.Value != _productSearchVersion)
                    return;

                _isLoadingProducts = true;
                var currentPage = _nextProductPage;
                var searchText = _currentProductSearchText;

                Expression<Func<Product, bool>>? filter = null;
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    filter = p => !p.IsDeleted &&
                                  ((p.Name != null && p.Name.Contains(searchText)) ||
                                   p.ITEMCODE.ToString().Contains(searchText));
                }

                if (showLoading)
                    _loadingService.Show();

                await _dataQueryLock.WaitAsync();
                List<ProductReadDto> items;
                try
                {
                    var page = await _productService.GetReadDtoPagedListAsync(
                        pageNumber: currentPage,
                        pageSize: ProductPageSize,
                        filter: filter,
                        orderBy: q => q.OrderBy(p => p.Name),
                        includes: new Expression<Func<Product, object>>[]
                        {
                            p => p.ProductUnits,
                            p => p.Brand,
                            p => p.SubCategory
                        });

                    items = page?.Items?
                        .Where(x => !x.IsDeleted)
                        .GroupBy(x => x.Id)
                        .Select(x => x.First())
                        .ToList() ?? new List<ProductReadDto>();
                }
                finally
                {
                    _dataQueryLock.Release();
                    if (showLoading)
                        _loadingService.Hide();
                }

                if (expectedSearchVersion.HasValue && expectedSearchVersion.Value != _productSearchVersion)
                    return;

                foreach (var item in items.Where(x => _loadedProducts.All(p => p.Id != x.Id)))
                    _loadedProducts.Add(item);

                ProductComboBox.ItemsSource = null;
                ProductComboBox.ItemsSource = _loadedProducts;
                ProductComboBox.Items.Refresh();

                _nextProductPage++;
                _hasMoreProductPages = items.Count >= ProductPageSize;

            }
            finally
            {
                _isLoadingProducts = false;
                _productLoadLock.Release();
            }
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                return null;

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;

                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }

            return null;
        }

        private async Task EnsureInitialDataLoadedAsync()
        {
            if (_initialDataLoaded)
                return;

            if (_initialLoadTask == null)
                _initialLoadTask = LoadDataAsync();

            await _initialLoadTask;
        }
    }
}

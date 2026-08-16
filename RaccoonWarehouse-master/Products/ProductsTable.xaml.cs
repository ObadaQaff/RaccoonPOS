using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse;
using RaccoonWarehouse.Application.Service.Brands;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.ProductUnits;
using RaccoonWarehouse.Application.Service.SubCategories;
using RaccoonWarehouse.Application.Service.Units;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.Products
{
    public partial class ProductsTable : Window
    {
        private const int SearchDelayMs = 300;
        private readonly ISubCategoryService _subCategoryService;
        private readonly IBrandService _brandService;
        private readonly IUnitService _unitService;
        private readonly IProductUnitService _productUnitService;

        private int _currentPage = 1;
        private int _totalPages = 1;
        private const int PageSize = 20;

        private string _currentNameSearch = string.Empty;
        private string _currentBarcodeSearch = string.Empty;
        private int? _selectedSubCategoryId;
        private string? _selectedSubCategoryName;

        private int _searchVersion;
        private readonly SemaphoreSlim _loadSemaphore = new(1, 1);

        public ProductsTable(
            ISubCategoryService subCategoryService,
            IBrandService brandService,
            IUnitService unitService,
            IProductUnitService productUnitService)
        {
            _subCategoryService = subCategoryService;
            _brandService = brandService;
            _unitService = unitService;
            _productUnitService = productUnitService;

            InitializeComponent();
            UiText.ApplyWindow(this);
            UpdateFilterInfoText();
            Loaded += async (_, _) => await LoadPageAsync(1);
        }

        public void ApplySubCategoryFilter(int subCategoryId, string? subCategoryName = null)
        {
            _selectedSubCategoryId = subCategoryId;
            _selectedSubCategoryName = subCategoryName;
            UpdateFilterInfoText();

            if (IsLoaded)
                _ = LoadPageAsync(1);
        }

        private async void PrevPageBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                await LoadPageAsync(_currentPage - 1);
            }
        }

        private async void NextPageBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                await LoadPageAsync(_currentPage + 1);
            }
        }

        private void SearchByNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentNameSearch = SearchByNameTextBox.Text.Trim();
            QueueSearch();
        }

        private void SearchByBarcodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentBarcodeSearch = SearchByBarcodeTextBox.Text.Trim();
            QueueSearch();
        }

        private void QueueSearch()
        {
            var searchVersion = Interlocked.Increment(ref _searchVersion);
            _ = DebouncedLoadPageAsync(searchVersion);
        }

        private async Task DebouncedLoadPageAsync(int searchVersion)
        {
            try
            {
                await Task.Delay(SearchDelayMs);
                if (searchVersion != _searchVersion)
                    return;

                await LoadPageAsync(1, searchVersion);
            }
            catch (Exception ex)
            {
                if (searchVersion != _searchVersion)
                    return;

                MessageBox.Show(
                    $"{UiText.T("تعذر البحث في الأصناف.", "Failed to search products.")} {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task LoadPageAsync(int pageNumber, int? searchVersion = null)
        {
            await _loadSemaphore.WaitAsync();
            try
            {
                if (searchVersion.HasValue && searchVersion.Value != _searchVersion)
                    return;

                using var scope = ((App)System.Windows.Application.Current).ServiceProvider.CreateScope();
                var productService = scope.ServiceProvider.GetRequiredService<IProductService>();

                var subCategoryId = _selectedSubCategoryId;
                var nameSearch = _currentNameSearch;
                long? barcode = null;
                if (!string.IsNullOrWhiteSpace(_currentBarcodeSearch) && long.TryParse(_currentBarcodeSearch, out var parsedBarcode))
                    barcode = parsedBarcode;

                Expression<Func<Product, bool>> filter = product =>
                    (!subCategoryId.HasValue || product.SubCategoryId == subCategoryId.Value) &&
                    (string.IsNullOrEmpty(nameSearch) || (product.Name != null && product.Name.Contains(nameSearch))) &&
                    (!barcode.HasValue || product.ITEMCODE == barcode.Value);

                var result = await productService.GetReadDtoPagedListAsync(
                    pageNumber: pageNumber,
                    pageSize: PageSize,
                    filter: filter,
                    orderBy: query => query.OrderBy(product => product.Name),
                    includes: new System.Linq.Expressions.Expression<Func<Product, object>>[]
                    {
                        product => product.SubCategory,
                        product => product.ProductUnits,
                        product => product.Brand
                    });

                if (searchVersion.HasValue && searchVersion.Value != _searchVersion)
                    return;

                ProductsTable1.ItemsSource = result.Items;

                _currentPage = pageNumber;
                _totalPages = Math.Max(1, (int)Math.Ceiling((double)result.TotalCount / PageSize));

                PageInfoTextBlock.Text = UiText.IsEnglish
                    ? $"Page {_currentPage} of {_totalPages}"
                    : $"الصفحة {_currentPage} من {_totalPages}";

                PrevPageBtn.IsEnabled = _currentPage > 1;
                NextPageBtn.IsEnabled = _currentPage < _totalPages;
                UpdateFilterInfoText();
                UiText.ApplyTranslations(this);
            }
            finally
            {
                _loadSemaphore.Release();
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void CreateProductBtn_Click(object sender, RoutedEventArgs e)
        {
            WindowManager.ShowDialog<CreateProduct>(WindowSizeType.MediumRectangle, window =>
            {
                if (_selectedSubCategoryId.HasValue)
                    window.InitializeForSubCategory(_selectedSubCategoryId.Value, _selectedSubCategoryName);
            });

            await LoadPageAsync(_currentPage);
        }

        private async void Delete_Product(object sender, RoutedEventArgs e)
        {
            if (ProductsTable1.SelectedItem is not ProductReadDto selectedProduct)
            {
                MessageBox.Show(UiText.T("يجب تحديد الصنف قبل التحديث أو الحذف.", "No product selected."));
                return;
            }

            var message = UiText.IsEnglish
                ? $"Are you sure you want to delete the product '{selectedProduct.Name}'?"
                : $"هل أنت متأكد من أنك تريد حذف الصنف '{selectedProduct.Name}'؟";

            var messageResult = MessageBox.Show(
                message,
                UiText.T("تأكيد الحذف", "Confirm Delete"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (messageResult != MessageBoxResult.Yes)
            {
                return;
            }

            using var scope = ((App)System.Windows.Application.Current).ServiceProvider.CreateScope();
            var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
            await productService.SoftDeleteAsync(selectedProduct.Id);

            MessageBox.Show(UiText.T("تم الحذف بنجاح.", "Delete was successful."));
            await LoadPageAsync(_currentPage);
        }

        private void Update_Product(object sender, RoutedEventArgs e)
        {
            if (ProductsTable1.SelectedItem is not ProductReadDto selectedProduct)
            {
                MessageBox.Show(UiText.T("يجب تحديد الصنف قبل التحديث أو الحذف.", "No product selected."));
                return;
            }

            WindowManager.ShowDialog<UpdateProduct>(WindowSizeType.MediumRectangle, window =>
            {
                window.Initialize(selectedProduct.Id);
            });
        }
        private void UpdateFilterInfoText()
        {
            if (FilterInfoTextBlock == null)
                return;

            FilterInfoTextBlock.Text = _selectedSubCategoryId.HasValue && !string.IsNullOrWhiteSpace(_selectedSubCategoryName)
                ? UiText.T(
                    $"عرض الأصناف التابعة للفئة الفرعية: {_selectedSubCategoryName}",
                    $"Showing products for subcategory: {_selectedSubCategoryName}")
                : UiText.T("جميع الأصناف", "Showing all products");
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse;
using RaccoonWarehouse.Application.Service.Brands;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.ProductUnits;
using RaccoonWarehouse.Application.Service.SubCategories;
using RaccoonWarehouse.Application.Service.Units;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Common;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RaccoonWarehouse.Products
{
    public partial class ProductsTable : Window
    {
        private const int SearchDelayMs = 250;
        private readonly ISubCategoryService _subCategoryService;
        private readonly IBrandService _brandService;
        private readonly IUnitService _unitService;
        private readonly IProductUnitService _productUnitService;
        private readonly ILoadingService _loadingService;
        private readonly IUserService _userService;

        private int _currentPage = 1;
        private int _totalPages = 1;
        private const int PageSize = 100;

        private string _currentNameSearch = string.Empty;
        private string _currentBarcodeSearch = string.Empty;
        private int? _currentSupplierId;
        private int _searchVersion;
        private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
        private List<RaccoonWarehouse.Domain.Users.DTOs.UserReadDto> _supplierLookupItems = new();
        private bool _isFilteringSuppliers;
        private bool _isNavigatingSuppliers;
        private string _supplierSearchText = string.Empty;

        public ProductsTable(
            ISubCategoryService subCategoryService,
            IBrandService brandService,
            IUnitService unitService,
            IProductUnitService productUnitService,
            ILoadingService loadingService,
            IUserService userService)
        {
            _subCategoryService = subCategoryService;
            _brandService = brandService;
            _unitService = unitService;
            _productUnitService = productUnitService;
            _loadingService = loadingService;
            _userService = userService;

            InitializeComponent();
            UiText.ApplyWindow(this);
            Loaded += async (_, _) =>
            {
                await LoadSupplierFilterAsync();
                await LoadPageAsync(1);
            };
            CatalogRefreshNotifier.CatalogChanged += CatalogRefreshNotifier_CatalogChanged;
            Closed += (_, _) => CatalogRefreshNotifier.CatalogChanged -= CatalogRefreshNotifier_CatalogChanged;
        }

        private async void CatalogRefreshNotifier_CatalogChanged(object? sender, EventArgs e)
        {
            if (!IsLoaded || _isFilteringSuppliers || _isNavigatingSuppliers)
                return;

            await LoadPageAsync(1);
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

        private void SupplierFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            _currentSupplierId = SupplierFilterComboBox.SelectedValue is int supplierId && supplierId > 0
                ? supplierId
                : null;
            QueueSearch();
        }

        private async Task LoadSupplierFilterAsync()
        {
            var users = await _userService.GetAllAsync();
            _supplierLookupItems = users.Data?
                .Where(user => user.Role == UserRole.Supplier)
                .OrderBy(user => user.Name)
                .ToList() ?? new();
            var suppliers = _supplierLookupItems.ToList();
            suppliers.Insert(0, new RaccoonWarehouse.Domain.Users.DTOs.UserReadDto { Id = 0, Name = "All suppliers" });
            SupplierFilterComboBox.ItemsSource = suppliers;
            SupplierFilterComboBox.SelectedIndex = 0;
        }

        private void SupplierFilterComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (SupplierFilterComboBox.Template.FindName("PART_EditableTextBox", SupplierFilterComboBox) is TextBox textBox)
                textBox.TextChanged += SupplierFilterComboBox_TextChanged;
        }

        private void SupplierFilterComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is not (Key.Down or Key.Up or Key.Enter) || SupplierFilterComboBox.Items.Count == 0)
                return;

            _isNavigatingSuppliers = true;
            try
            {
                if (e.Key == Key.Down)
                    SupplierFilterComboBox.SelectedIndex = Math.Min(SupplierFilterComboBox.SelectedIndex + 1, SupplierFilterComboBox.Items.Count - 1);
                else if (e.Key == Key.Up)
                    SupplierFilterComboBox.SelectedIndex = Math.Max(SupplierFilterComboBox.SelectedIndex - 1, 0);
                else if (SupplierFilterComboBox.SelectedItem is RaccoonWarehouse.Domain.Users.DTOs.UserReadDto)
                    SupplierFilterComboBox.IsDropDownOpen = false;
                else
                    return;

                RestoreSupplierFilterSearchText();
                e.Handled = true;
            }
            finally
            {
                _isNavigatingSuppliers = false;
            }
        }

        private void RestoreSupplierFilterSearchText()
        {
            if (SupplierFilterComboBox.Template.FindName("PART_EditableTextBox", SupplierFilterComboBox) is not TextBox textBox)
                return;

            textBox.Text = _supplierSearchText;
            textBox.CaretIndex = textBox.Text.Length;
            SupplierFilterComboBox.IsDropDownOpen = true;
        }

        private void SupplierFilterComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFilteringSuppliers || _isNavigatingSuppliers || !SupplierFilterComboBox.IsKeyboardFocusWithin)
                return;

            if (SupplierFilterComboBox.Template.FindName("PART_EditableTextBox", SupplierFilterComboBox) is not TextBox textBox)
                return;

            _supplierSearchText = textBox.Text.Trim();
            _isFilteringSuppliers = true;
            try
            {
                SupplierFilterComboBox.SelectedValue = null;

                var filteredSuppliers = string.IsNullOrWhiteSpace(_supplierSearchText)
                    ? _supplierLookupItems.ToList()
                    : _supplierLookupItems.Where(user =>
                            (user.Name ?? string.Empty).Contains(_supplierSearchText, StringComparison.CurrentCultureIgnoreCase)
                            || (user.PhoneNumber ?? string.Empty).Contains(_supplierSearchText, StringComparison.CurrentCultureIgnoreCase))
                        .ToList();

                if (string.IsNullOrWhiteSpace(_supplierSearchText))
                    filteredSuppliers.Insert(0, new RaccoonWarehouse.Domain.Users.DTOs.UserReadDto { Id = 0, Name = "All suppliers" });

                SupplierFilterComboBox.ItemsSource = filteredSuppliers;
                textBox.Text = _supplierSearchText;
                textBox.CaretIndex = textBox.Text.Length;
                SupplierFilterComboBox.IsDropDownOpen = true;
            }
            finally
            {
                _isFilteringSuppliers = false;
            }
        }

        private void ProductSearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                FocusFirstFilteredProduct();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter)
            {
                if (FocusFirstFilteredProduct())
                    Update_Product(this, new RoutedEventArgs());

                e.Handled = true;
            }
        }

        private void ProductsTable1_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            Update_Product(this, new RoutedEventArgs());
            e.Handled = true;
        }

        private bool FocusFirstFilteredProduct()
        {
            if (ProductsTable1.Items.Count == 0)
                return false;

            ProductsTable1.SelectedIndex = 0;
            ProductsTable1.ScrollIntoView(ProductsTable1.SelectedItem);
            ProductsTable1.Focus();
            Keyboard.Focus(ProductsTable1);
            return true;
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
            var loadingShown = false;
            try
            {
                if (searchVersion.HasValue && searchVersion.Value != _searchVersion)
                    return;

                _loadingService.Show();
                loadingShown = true;

                using var scope = ((App)System.Windows.Application.Current).ServiceProvider.CreateScope();
                var productService = scope.ServiceProvider.GetRequiredService<IProductService>();

                var nameSearchTerms = _currentNameSearch
                    .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var barcodeSearchTerms = _currentBarcodeSearch
                    .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                // Match the purchase product search behavior: use a lightweight database
                // predicate first, then apply the culture-safe multi-term matcher across
                // names, item codes, and alternate barcodes.
                if (nameSearchTerms.Length > 0 || barcodeSearchTerms.Length > 0)
                {
                    var databaseSearchTerm = nameSearchTerms.FirstOrDefault()
                        ?? barcodeSearchTerms.First();

                    var searchResult = await productService.GetAllWithAdvancedIncludeAsync(
                        query => query
                            .Include(product => product.Brand)
                            .Include(product => product.Supplier)
                            .Include(product => product.ProductUnits!)
                            .ThenInclude(productUnit => productUnit.Unit),
                        product => (!_currentSupplierId.HasValue || product.SupplierId == _currentSupplierId.Value) &&
                                   ((product.Name ?? string.Empty).Contains(databaseSearchTerm) ||
                                   product.ITEMCODE.ToString().Contains(databaseSearchTerm) ||
                                   product.ProductUnits!.Any(unit => unit.AlternateBarcode == databaseSearchTerm)));

                    if (searchVersion.HasValue && searchVersion.Value != _searchVersion)
                        return;

                    var filteredItems = (searchResult.Data ?? new List<ProductReadDto>())
                        .Where(product => MatchesSearch(product, nameSearchTerms))
                        .Where(product => MatchesSearch(product, barcodeSearchTerms))
                        .OrderByDescending(product => product.CreatedDate)
                        .ThenBy(product => product.Name)
                        .ToList();

                    var totalCount = filteredItems.Count;
                    ProductsTable1.ItemsSource = filteredItems
                        .Skip((pageNumber - 1) * PageSize)
                        .Take(PageSize)
                        .ToList();

                    _currentPage = pageNumber;
                    _totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / PageSize));
                    PageInfoTextBlock.Text = UiText.IsEnglish
                        ? $"Page {_currentPage} of {_totalPages}"
                        : $"الصفحة {_currentPage} من {_totalPages}";
                    PrevPageBtn.IsEnabled = _currentPage > 1;
                    NextPageBtn.IsEnabled = _currentPage < _totalPages;
                    UiText.ApplyTranslations(this);
                    return;
                }

                long? barcode = null;
                if (!string.IsNullOrWhiteSpace(_currentBarcodeSearch) && long.TryParse(_currentBarcodeSearch, out var parsedBarcode))
                    barcode = parsedBarcode;

                var parameter = System.Linq.Expressions.Expression.Parameter(typeof(Product), "product");
                System.Linq.Expressions.Expression filterBody = System.Linq.Expressions.Expression.Constant(true);

                if (_currentSupplierId.HasValue)
                {
                    filterBody = System.Linq.Expressions.Expression.AndAlso(
                        filterBody,
                        System.Linq.Expressions.Expression.Equal(
                            System.Linq.Expressions.Expression.Property(parameter, nameof(Product.SupplierId)),
                            System.Linq.Expressions.Expression.Constant(_currentSupplierId, typeof(int?))));
                }

                var nameProperty = System.Linq.Expressions.Expression.Property(parameter, nameof(Product.Name));
                foreach (var term in nameSearchTerms)
                {
                    var containsTerm = System.Linq.Expressions.Expression.Call(
                        nameProperty,
                        nameof(string.Contains),
                        Type.EmptyTypes,
                        System.Linq.Expressions.Expression.Constant(term));
                    filterBody = System.Linq.Expressions.Expression.AndAlso(
                        filterBody,
                        System.Linq.Expressions.Expression.AndAlso(
                            System.Linq.Expressions.Expression.NotEqual(nameProperty, System.Linq.Expressions.Expression.Constant(null, typeof(string))),
                            containsTerm));
                }

                if (barcode.HasValue)
                {
                    filterBody = System.Linq.Expressions.Expression.AndAlso(
                        filterBody,
                        System.Linq.Expressions.Expression.Equal(
                            System.Linq.Expressions.Expression.Property(parameter, nameof(Product.ITEMCODE)),
                            System.Linq.Expressions.Expression.Constant(barcode.Value)));
                }

                Expression<Func<Product, bool>> filter = System.Linq.Expressions.Expression.Lambda<Func<Product, bool>>(filterBody, parameter);

                var result = await productService.GetReadDtoPagedListAsync(
                    pageNumber: pageNumber,
                    pageSize: PageSize,
                    filter: filter,
                    orderBy: query => query
                        .OrderByDescending(product => product.CreatedDate)
                        .ThenBy(product => product.Name),
                    includes: new System.Linq.Expressions.Expression<Func<Product, object>>[]
                    {
                        product => product.ProductUnits,
                        product => product.Brand,
                        product => product.Supplier
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
                UiText.ApplyTranslations(this);
            }
            finally
            {
                if (loadingShown)
                    _loadingService.Hide();
                _loadSemaphore.Release();
            }
        }

        private static bool MatchesSearch(ProductReadDto product, string[] searchTerms)
        {
            var name = product.Name ?? string.Empty;
            var itemCode = product.ITEMCODE.ToString();
            var alternateBarcodes = product.ProductUnits?
                .Where(unit => !string.IsNullOrWhiteSpace(unit.AlternateBarcode))
                .Select(unit => unit.AlternateBarcode!)
                .ToArray() ?? Array.Empty<string>();

            return searchTerms.All(term =>
                name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                itemCode.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                alternateBarcodes.Any(barcode => barcode.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void CreateProductBtn_Click(object sender, RoutedEventArgs e)
        {
            WindowManager.ShowDialog<CreateProduct>(WindowSizeType.MediumRectangle, window =>
            {
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
    }
}

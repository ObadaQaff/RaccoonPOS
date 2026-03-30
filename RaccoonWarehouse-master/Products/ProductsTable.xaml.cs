using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse;
using RaccoonWarehouse.Application.Service.Brands;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.ProductUnits;
using RaccoonWarehouse.Application.Service.SubCategories;
using RaccoonWarehouse.Application.Service.Units;
using RaccoonWarehouse.Domain.Products;
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
        private readonly ISubCategoryService _subCategoryService;
        private readonly IBrandService _brandService;
        private readonly IUnitService _unitService;
        private readonly IProductUnitService _productUnitService;

        private int _currentPage = 1;
        private int _totalPages = 1;
        private const int PageSize = 20;

        private string _currentNameSearch = string.Empty;
        private string _currentBarcodeSearch = string.Empty;

        private CancellationTokenSource _searchCts;
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
            Loaded += async (_, _) => await LoadPageAsync(1);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
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
            DebounceSearch();
        }

        private void SearchByBarcodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentBarcodeSearch = SearchByBarcodeTextBox.Text.Trim();
            DebounceSearch();
        }

        private void DebounceSearch()
        {
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(300, token);
                    if (!token.IsCancellationRequested)
                    {
                        await Dispatcher.InvokeAsync(async () => await LoadPageAsync(1));
                    }
                }
                catch (TaskCanceledException)
                {
                }
            });
        }

        private async Task LoadPageAsync(int pageNumber)
        {
            await _loadSemaphore.WaitAsync();
            try
            {
                using var scope = ((App)System.Windows.Application.Current).ServiceProvider.CreateScope();
                var productService = scope.ServiceProvider.GetRequiredService<IProductService>();

                System.Linq.Expressions.Expression<Func<Product, bool>> filter = null;

                if (!string.IsNullOrEmpty(_currentNameSearch))
                {
                    filter = product => product.Name.Contains(_currentNameSearch);
                }

                if (!string.IsNullOrEmpty(_currentBarcodeSearch) && long.TryParse(_currentBarcodeSearch, out var barcode))
                {
                    System.Linq.Expressions.Expression<Func<Product, bool>> barcodeFilter = product => product.ITEMCODE == barcode;
                    filter = filter == null ? barcodeFilter : CombineExpressions(filter, barcodeFilter);
                }

                var result = await productService.GetPagedListAsync(
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

                ProductsTable1.ItemsSource = result.Items;

                _currentPage = pageNumber;
                _totalPages = (int)Math.Ceiling((double)result.TotalCount / PageSize);

                PageInfoTextBlock.Text = UiText.IsEnglish
                    ? $"Page {_currentPage} of {_totalPages}"
                    : $"الصفحة {_currentPage} من {_totalPages}";

                PrevPageBtn.IsEnabled = _currentPage > 1;
                NextPageBtn.IsEnabled = _currentPage < _totalPages;
                UiText.ApplyTranslations(this);
            }
            finally
            {
                _loadSemaphore.Release();
            }
        }

        private static System.Linq.Expressions.Expression<Func<Product, bool>> CombineExpressions(
            System.Linq.Expressions.Expression<Func<Product, bool>> expr1,
            System.Linq.Expressions.Expression<Func<Product, bool>> expr2)
        {
            var param = System.Linq.Expressions.Expression.Parameter(typeof(Product));
            var body = System.Linq.Expressions.Expression.AndAlso(
                System.Linq.Expressions.Expression.Invoke(expr1, param),
                System.Linq.Expressions.Expression.Invoke(expr2, param));

            return System.Linq.Expressions.Expression.Lambda<Func<Product, bool>>(body, param);
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CreateProductBtn_Click(object sender, RoutedEventArgs e)
        {
            var dashboard = new Dashboard();
            dashboard.StocksBtn_Click(null, null);
            dashboard.Show();
            Close();
        }

        private async void Delete_Product(object sender, RoutedEventArgs e)
        {
            if (ProductsTable1.SelectedItem is not Product selectedProduct)
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
            if (ProductsTable1.SelectedItem is not Product selectedProduct)
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

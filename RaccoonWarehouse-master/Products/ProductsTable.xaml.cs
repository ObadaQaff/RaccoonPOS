using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse;
using RaccoonWarehouse.Application.Service.Brands;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.ProductUnits;
using RaccoonWarehouse.Application.Service.SubCategories;
using RaccoonWarehouse.Application.Service.Units;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.Users.DTOs;
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
        private const int _pageSize = 20;

        private string _currentNameSearch = "";
        private string _currentBarcodeSearch = "";

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

            Loaded += async (_, _) => await LoadPageAsync(1);


        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadPageAsync(1);
        }

        #region Pagination Buttons
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
        #endregion

        #region Search
        private async void SearchByNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentNameSearch = SearchByNameTextBox.Text.Trim();
            DebounceSearch();
        }

        private async void SearchByBarcodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
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
                    await Task.Delay(300, token); // 300ms debounce
                    if (!token.IsCancellationRequested)
                    {
                        await Dispatcher.InvokeAsync(async () => await LoadPageAsync(1));
                    }
                }
                catch (TaskCanceledException) { }
            });
        }
        #endregion

        private async Task LoadPageAsync(int pageNumber)
        {
            await _loadSemaphore.WaitAsync();
            try
            {
                using var scope = ((App)System.Windows.Application.Current).ServiceProvider.CreateScope();
                var productService = scope.ServiceProvider.GetRequiredService<IProductService>();

                // Build filter expression
                Expression<Func<Product, bool>> filter = null;

                if (!string.IsNullOrEmpty(_currentNameSearch))
                    filter = u => u.Name.Contains(_currentNameSearch);

                if (!string.IsNullOrEmpty(_currentBarcodeSearch) && long.TryParse(_currentBarcodeSearch, out long barcode))
                {
                    var barcodeFilter = (Expression<Func<Product, bool>>)(u => u.ITEMCODE == barcode);
                    filter = filter == null ? barcodeFilter : CombineExpressions(filter, barcodeFilter);
                }

                var result = await productService.GetPagedListAsync(
                    pageNumber: pageNumber,
                    pageSize: _pageSize,
                    filter: filter,
                    orderBy: q => q.OrderBy(u => u.Name),
                    includes: new Expression<Func<Product, object>>[]
                    {
                        p => p.SubCategory,
                        p => p.ProductUnits,
                        p => p.Brand
                    });

                ProductsTable1.ItemsSource = result.Items;

                _currentPage = pageNumber;
                _totalPages = (int)Math.Ceiling((double)result.TotalCount / _pageSize);

                PageInfoTextBlock.Text = $"الصفحة {_currentPage} من {_totalPages}";
                PrevPageBtn.IsEnabled = _currentPage > 1;
                NextPageBtn.IsEnabled = _currentPage < _totalPages;
            }
            finally
            {
                _loadSemaphore.Release();
            }
        }

        // Combine two expressions with AND
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


        #region CRUD
        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CreateProductBtn_Click(object sender, RoutedEventArgs e)
        {
            var dashboard = new Dashboard();
            dashboard.StocksBtn_Click(null, null);
            dashboard.Show();
            this.Close();
        }

        private async void Delete_Product(object sender, RoutedEventArgs e)
        {
            var selectedProduct = ProductsTable1.SelectedItem as Product;
            if (selectedProduct != null)
            {
                var messageResult = MessageBox.Show(
                    $"هل انت متأكد من انك تريد حذف الصنف '{selectedProduct.Name}'؟",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (messageResult == MessageBoxResult.Yes)
                {
                    using var scope = ((App)System.Windows.Application.Current).ServiceProvider.CreateScope();
                    var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
                    await productService.SoftDeleteAsync(selectedProduct.Id);

                    MessageBox.Show("تم الحذف بنجاح !!");
                    await LoadPageAsync(_currentPage); 
                }
            }
        }

        private void Update_Product(object sender, RoutedEventArgs e)
        {

            if (ProductsTable1.SelectedItem is not Product selectedProduct)
            {
                MessageBox.Show("No product selected.");
                return;
            }

            WindowManager.ShowDialog<UpdateProduct>(WindowSizeType.MediumRectangle,w =>
            {
                w.Initialize(selectedProduct.Id);
            });
          /*  var selectedProduct = ProductsTable1.SelectedItem as Product;
            if (selectedProduct != null)
            {
                using var scope = ((App)System.Windows.Application.Current).ServiceProvider.CreateScope();

                var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
                var subCategoryService = scope.ServiceProvider.GetRequiredService<ISubCategoryService>();
                var brandService = scope.ServiceProvider.GetRequiredService<IBrandService>();
                var unitService = scope.ServiceProvider.GetRequiredService<IUnitService>();
                var productUnitService = scope.ServiceProvider.GetRequiredService<IProductUnitService>();

                var updateWindow = new UpdateProduct(selectedProduct.Id, productService, subCategoryService, brandService,productUnitService, unitService );
                updateWindow.ShowDialog(); // safe, because all services have their own scope
                
            }
            else
            {
                MessageBox.Show("يجب تحديد الصنف قبل التحديث أو الحذف");
            }*/
        }

        #endregion
    }
}






















/*using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Brands;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.ProductUnits;
using RaccoonWarehouse.Application.Service.SubCategories;
using RaccoonWarehouse.Application.Service.Units;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.Products
{
    public partial class ProductsTable : Window
    {
        private readonly IProductService _productService;
        private readonly ISubCategoryService _subCategoryService;
        private readonly IBrandService _brandService;
        private readonly IProductUnitService _productUnitService;
        private readonly IUnitService _unitService;

        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalPages = 1;
        private CancellationTokenSource _searchCts;

        private string _currentNameSearch = string.Empty;
        private string _currentBarcodeSearch = string.Empty;

        public ProductsTable(ISubCategoryService subCategoryService,
                             IProductService productService,
                             IBrandService brandService,
                             IUnitService unitService,
                             IProductUnitService productUnitService)
        {
            _productUnitService = productUnitService;
            _unitService = unitService;
            _productService = productService;
            _subCategoryService = subCategoryService;
            _brandService = brandService;

            InitializeComponent();
            LoadPageAsync(1);
        }

       

        private async Task LoadPageAsync(int pageNumber)
        {
            Expression<Func<Product, bool>> filter = null;

            if (!string.IsNullOrEmpty(_currentNameSearch))
                filter = u => u.Name.Contains(_currentNameSearch);

            if (!string.IsNullOrEmpty(_currentBarcodeSearch))
            {
                if (long.TryParse(_currentBarcodeSearch, out long barcode))
                {
                    filter = u => u.ITEMCODE == barcode;
                }
                else
                {
                    filter = null; // ignore invalid barcode input
                }
            }

            var result = await _productService.GetPagedListAsync(
                pageNumber: pageNumber,
                pageSize: _pageSize,
                filter: filter,
                orderBy: q => q.OrderBy(u => u.Name),
                includes: new Expression<Func<Product, object>>[]
                {
                    p => p.SubCategory,
                    p => p.ProductUnits,
                    p => p.Brand
                });

            ProductsTable1.ItemsSource = result.Items;

            _currentPage = pageNumber;
            _totalPages = (int)Math.Ceiling((double)result.TotalCount / _pageSize);

            PageInfoTextBlock.Text = $"Page {_currentPage} of {_totalPages}";

            PrevPageBtn.IsEnabled = _currentPage > 1;
            NextPageBtn.IsEnabled = _currentPage < _totalPages;
        }

        private async void PrevPageBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
                await LoadPageAsync(_currentPage - 1);
        }

        private async void NextPageBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
                await LoadPageAsync(_currentPage + 1);
        }

        *//*private async void SearchByNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            _currentNameSearch = textBox?.Text.Trim() ?? string.Empty;

            // reset page to 1 whenever search changes
            await LoadPageAsync(1);
        }

        private async void SearchByBarcodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            _currentBarcodeSearch = textBox?.Text.Trim() ?? string.Empty;

            // reset page to 1 whenever search changes
            await LoadPageAsync(1);
        }
*//*


        private async void SearchByNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentNameSearch = SearchByNameTextBox.Text.Trim();
            _currentBarcodeSearch = SearchByBarcodeTextBox.Text.Trim(); // keep barcode if used

            // Cancel previous pending search
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                await Task.Delay(300, token); // debounce 300ms
                if (!token.IsCancellationRequested)
                {
                    await LoadPageAsync(1);
                }
            }
            catch (TaskCanceledException)
            {
                // ignored
            }
        }

        private async void SearchByBarcodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentBarcodeSearch = SearchByBarcodeTextBox.Text.Trim();
            _currentNameSearch = SearchByNameTextBox.Text.Trim();

            // Cancel previous pending search
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                await Task.Delay(300, token); // debounce 300ms
                if (!token.IsCancellationRequested)
                {
                    await LoadPageAsync(1);
                }
            }
            catch (TaskCanceledException)
            {
                // ignored
            }
        }





        // ---------------- Existing Buttons and Actions ----------------
        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CreateProductBtn_Click(object sender, RoutedEventArgs e)
        {
            Dashboard dashboard = new Dashboard();
            dashboard.StocksBtn_Click(null, null);
            dashboard.Show();
            this.Close();
        }

        private void Delete_Product(object sender, RoutedEventArgs e)
        {
            var selectedProduct = ProductsTable1.SelectedItem as Product;
            if (selectedProduct != null)
            {
                var messageResult = MessageBox.Show(
                    $" ؟'{selectedProduct.Name}' : هل انت متاكد من انك تريد حذف الصنف",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (messageResult == MessageBoxResult.Yes)
                {
                    _productService.DeleteAsync(selectedProduct.Id);
                    MessageBox.Show("تم الحذف بنجاح !!");
                    LoadPageAsync(_currentPage);
                }
            }
        }

        private void Update_Product(object sender, RoutedEventArgs e)
        {
            var selectedProduct = ProductsTable1.SelectedItem as Product;
            if (selectedProduct != null)
            {
                var productService = ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<IProductService>();
                var subCategoryService = ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<ISubCategoryService>();
                var brandService = ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<IBrandService>();
                var unitService = ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<IUnitService>();
                var productUnitService = ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<IProductUnitService>();

                var updateWindow = new UpdateProduct(selectedProduct.Id, productService, subCategoryService, brandService, unitService, productUnitService);
                updateWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("يجب تحديد الصنف قبل التحديث أو الحذف");
            }
        }

        private void ProductsTable1_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Id": e.Column.Header = "الرقم التعريفي"; break;
                case "Name": e.Column.Header = "اسم المنتج"; break;
                case "SubCategory.Name": e.Column.Header = "الفئة الفرعية"; break;
                case "Brand.Name": e.Column.Header = "البراند"; break;
                case "ITEMCODE": e.Column.Header = "الباركود"; break;
                case "Status": e.Column.Header = "الحالة"; break;
                case "TaxExempt": e.Column.Header = "معفاة من الضريبة"; break;
                case "ProductUnits.FirstOrDefault().SalePrice": e.Column.Header = "السعر الإجمالي"; break;
                case "ProductUnits.FirstOrDefault().PurchasePrice": e.Column.Header = "التكلفة الحالية"; break;
                case "ProductUnits.FirstOrDefault().QuantityPerUnit": e.Column.Header = "الكمية الإجمالية"; break;
                case "ProductUnits.FirstOrDefault().Name": e.Column.Header = "الوحدات"; break;
                case "CreatedDate": e.Column.Header = "تاريخ الإنشاء"; break;
                case "UpdatedDate": e.Column.Header = "تاريخ التحديث"; break;
                default: e.Cancel = true; break;
            }
        }
    }
}
*/

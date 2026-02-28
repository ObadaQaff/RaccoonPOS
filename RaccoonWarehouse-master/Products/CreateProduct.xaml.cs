using RaccoonWarehouse.Application.Service.Brands;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.SubCategories;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using System.IO;
using System.Windows.Media.Imaging;
using RaccoonWarehouse.Application.Service.Units;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using System.Collections.Generic;
using RaccoonWarehouse.Application.Service.ProductUnits;


namespace RaccoonWarehouse.Products
{
    public partial class CreateProduct : Window
    {
        private readonly IProductService _productService;
        private readonly ISubCategoryService _subCategoryService;
        private readonly IBrandService _brandService;
        private readonly IProductUnitService _productUnitService;
        private readonly List<ProductUnitWriteDto> _productUnits = new();
        private readonly IUnitService _unitService;
        private bool _isLoaded = false;


        public CreateProduct(IProductService productService,
                             ISubCategoryService subCategoryService,
                             IBrandService brandService,
                             IProductUnitService productUnitService,
                             IUnitService unitService)
        {
            _productService = productService;
            _subCategoryService = subCategoryService;
            _brandService = brandService;
            _productUnitService = productUnitService;
            _unitService = unitService;

            InitializeComponent();
            Loaded += async (_, _) =>
            {
                if (!_isLoaded)
                {
                    _isLoaded = true;
                    await LoadDataAsync();
                }
            };

        }


        private async Task LoadDataAsync()
        {
            var categories = await _subCategoryService.GetAllAsync();
            SubCategoryComboBox.ItemsSource = categories.Data;
            SubCategoryComboBox.DisplayMemberPath = "Name";
            SubCategoryComboBox.SelectedValuePath = "Id";

            var brands = await _brandService.GetAllAsync();
            BrandComboBox.ItemsSource = brands.Data;
            BrandComboBox.DisplayMemberPath = "Name";
            BrandComboBox.SelectedValuePath = "Id";

            var units = await _unitService.GetAllAsync();
            UnitComboBox.ItemsSource = units.Data;
            UnitComboBox.DisplayMemberPath = "Name";
            UnitComboBox.SelectedValuePath = "Id";

            StatusComboBox.ItemsSource = Enum.GetValues(typeof(ProductStatus)).Cast<ProductStatus>();
        }
        private void AddUnit_Click(object sender, RoutedEventArgs e)
        {
            if (UnitComboBox.SelectedValue == null)
            {
                MessageBox.Show("يرجى اختيار وحدة أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(SalePriceTextBox.Text, out var salePrice) ||
                !decimal.TryParse(PurchasePriceTextBox.Text, out var purchasePrice) ||
                !decimal.TryParse(QuantityPerUnitTextBox.Text, out var qty))
            {
                MessageBox.Show("يرجى إدخال أسعار وأرقام صحيحة.", "خطأ في الإدخال", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var unitDto = new ProductUnitWriteDto
            {
                UnitId = (int)UnitComboBox.SelectedValue,
                SalePrice = salePrice,
                PurchasePrice = purchasePrice,
                QuantityPerUnit = qty,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            _productUnits.Add(unitDto);
            MessageBox.Show("✅ تم إضافة الوحدة بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

            // Clear inputs for next unit
            UnitComboBox.SelectedIndex = -1;
            SalePriceTextBox.Clear();
            PurchasePriceTextBox.Clear();
            QuantityPerUnitTextBox.Clear();
        }



        // 🟢 Back button
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Dashboard dashboard = new Dashboard();
            dashboard.Show();
            this.Hide();
        }

        // 🟢 Save Product
        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                var dto = new ProductWriteDto
                {
                    Name = NameTextBox.Text,
                    ITEMCODE = long.TryParse(ITEMCODE.Text, out var itemCode) ? itemCode : (long?)null,
                    Description = DescriptionTextBox.Text,
                    Status = (ProductStatus)(StatusComboBox.SelectedValue ?? ProductStatus.InStock),
                    TaxExempt = TaxExemptCheckBox.IsChecked ?? false,
                    TaxRate = Convert.ToDecimal(TaxRate.Text),
                    MiniQuantity = decimal.TryParse(MinimumQuantityTextBox.Text, out var minQty) ? minQty : (decimal?)null,
                    BrandId = BrandComboBox.SelectedValue != null ? (int)BrandComboBox.SelectedValue : (int?)null,
                    SubCategoryId = SubCategoryComboBox.SelectedValue != null ? (int)SubCategoryComboBox.SelectedValue : 0,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                };
                if (dto.TaxExempt != true)
                {
                    dto.TaxRate = decimal.TryParse(TaxRate.Text, out var rate) ? rate : 0;

                    //var unit = _productUnits.FirstOrDefault();
                    foreach (var unit in _productUnits)
                    {
                        if (unit != null)
                        {
                            var taxRate = dto.TaxRate ?? 0m;
                            unit.UnTaxedPrice = unit.SalePrice;

                            var newSalePrice = unit.SalePrice + (unit.SalePrice * taxRate / 100m);
                            unit.SalePrice = newSalePrice;
                        }
                        else
                        {
                            dto.TaxRate = 0;
                        }

                    }


                    // Save product first
                    var result = await _productService.CreateAsync(dto);

                    if (result.Success)
                    {
                        int productId = result.Data.Id;

                        // Save product units
                        foreach (var unit in _productUnits)
                        {
                            unit.ProductId = productId;
                            await _productUnitService.CreateAsync(unit);
                        }
                        // await _productService.ApplyTaxToProductUnitsAsync(productId);

                        MessageBox.Show("✅ تم إنشاء المنتج والوحدات بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                        ClearForm();
                    }
                    else
                    {
                        MessageBox.Show($"❌ فشل في إنشاء المنتج: {result.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }


            catch (Exception ex)
            {
                MessageBox.Show($"خطأ: {ex.Message}", "استثناء", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🧹 Clear the form
        private void ClearForm()
        {
            NameTextBox.Text = string.Empty;
            ITEMCODE.Text = string.Empty;
            DescriptionTextBox.Text = string.Empty;
            MinimumQuantityTextBox.Text = string.Empty;

            TaxExemptCheckBox.IsChecked = false;

            BrandComboBox.SelectedIndex = -1;
            SubCategoryComboBox.SelectedIndex = -1;
            StatusComboBox.SelectedIndex = -1;
        }

        // 🟢 Go back to dashboard
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            /*Dashboard dashboard = new Dashboard();
            dashboard.StocksBtn_Click(null, null);
            dashboard.Show();*/
            this.Close();
        }

        // 🟢 Upload image button
        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "اختر صورة المنتج",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var filePath = openFileDialog.FileName;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

              /*  UploadedImage.Source = bitmap;
                UploadedImage.Visibility = Visibility.Visible;*/
            }
        }
    }
}

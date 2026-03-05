using Microsoft.Win32;
using RaccoonWarehouse;
using RaccoonWarehouse.Application.Service.Brands;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.ProductUnits;
using RaccoonWarehouse.Application.Service.SubCategories;
using RaccoonWarehouse.Application.Service.Units;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
        private bool _isLoaded;

        public CreateProduct(
            IProductService productService,
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
                if (_isLoaded)
                    return;

                _isLoaded = true;
                await LoadDataAsync();
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

            var selectedUnitId = (int)UnitComboBox.SelectedValue;
            if (_productUnits.Any(u => u.UnitId == selectedUnitId))
            {
                MessageBox.Show("لا يمكن تكرار نفس الوحدة أكثر من مرة للصنف نفسه.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var unitDto = new ProductUnitWriteDto
            {
                UnitId = selectedUnitId,
                SalePrice = salePrice,
                PurchasePrice = purchasePrice,
                QuantityPerUnit = qty,
                IsBaseUnit = IsBaseUnitCheckBox.IsChecked == true,
                IsDefaultSaleUnit = IsDefaultSaleUnitCheckBox.IsChecked == true,
                IsDefaultPurchaseUnit = IsDefaultPurchaseUnitCheckBox.IsChecked == true,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            _productUnits.Add(unitDto);
            NormalizeUnitFlags(_productUnits);
            RebuildUnitsPanel();
            ResetUnitEntryFields();
        }

        private void RebuildUnitsPanel()
        {
            UnitsStackPanel.Children.Clear();
            foreach (var unit in _productUnits)
                AddUnitRow(unit, UnitComboBoxItemText(unit.UnitId));
        }

        private string UnitComboBoxItemText(int unitId)
        {
            var selected = UnitComboBox.ItemsSource?.Cast<object>()
                .FirstOrDefault(x => (int)((dynamic)x).Id == unitId);
            return selected == null ? unitId.ToString() : ((dynamic)selected).Name;
        }

        private void AddUnitRow(ProductUnitWriteDto unit, string unitName)
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10),
                FlowDirection = FlowDirection.RightToLeft,
                Tag = unit
            };

            row.Children.Add(CreateUnitInfoBlock("الوحدة", unitName));
            row.Children.Add(CreateUnitInfoBlock("سعر البيع", unit.SalePrice.ToString()));
            row.Children.Add(CreateUnitInfoBlock("سعر الشراء", unit.PurchasePrice.ToString()));
            row.Children.Add(CreateUnitInfoBlock("الكمية", unit.QuantityPerUnit.ToString()));
            row.Children.Add(CreateUnitInfoBlock("أساسية", unit.IsBaseUnit ? "نعم" : "لا"));
            row.Children.Add(CreateUnitInfoBlock("بيع", unit.IsDefaultSaleUnit ? "افتراضي" : "-"));
            row.Children.Add(CreateUnitInfoBlock("شراء", unit.IsDefaultPurchaseUnit ? "افتراضي" : "-"));

            var removeButton = new Button
            {
                Content = "حذف",
                Width = 100,
                Height = 42,
                Margin = new Thickness(10, 22, 0, 0),
                Background = Brushes.Firebrick,
                Foreground = Brushes.White,
                BorderBrush = Brushes.Transparent
            };

            removeButton.Click += (_, _) =>
            {
                _productUnits.Remove(unit);
                NormalizeUnitFlags(_productUnits);
                RebuildUnitsPanel();
            };

            row.Children.Add(removeButton);
            UnitsStackPanel.Children.Add(row);
        }

        private static StackPanel CreateUnitInfoBlock(string label, string value)
        {
            var panel = new StackPanel
            {
                Width = 120,
                Margin = new Thickness(0, 0, 10, 0)
            };

            panel.Children.Add(new TextBlock
            {
                Text = label,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            });

            panel.Children.Add(new TextBox
            {
                Text = value,
                IsReadOnly = true,
                Height = 42,
                Padding = new Thickness(10, 0, 10, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray
            });

            return panel;
        }

        private static void NormalizeUnitFlags(List<ProductUnitWriteDto> units)
        {
            if (units.Count == 0)
                return;

            if (units.Count == 1)
            {
                units[0].IsBaseUnit = true;
                units[0].IsDefaultSaleUnit = true;
                units[0].IsDefaultPurchaseUnit = true;
                return;
            }

            var baseUnit = units.FirstOrDefault(u => u.IsBaseUnit) ?? units[0];
            var saleUnit = units.FirstOrDefault(u => u.IsDefaultSaleUnit) ?? baseUnit;
            var purchaseUnit = units.FirstOrDefault(u => u.IsDefaultPurchaseUnit) ?? baseUnit;

            foreach (var unit in units)
            {
                unit.IsBaseUnit = ReferenceEquals(unit, baseUnit);
                unit.IsDefaultSaleUnit = ReferenceEquals(unit, saleUnit);
                unit.IsDefaultPurchaseUnit = ReferenceEquals(unit, purchaseUnit);
            }
        }

        private static string? ValidateUnits(List<ProductUnitWriteDto> units)
        {
            if (units.Count == 0)
                return "يجب إضافة وحدة واحدة على الأقل.";

            if (units.Any(u => u.QuantityPerUnit <= 0))
                return "الكمية لكل وحدة يجب أن تكون أكبر من صفر.";

            if (units.GroupBy(u => u.UnitId).Any(g => g.Count() > 1))
                return "لا يمكن تكرار نفس الوحدة أكثر من مرة.";

            return null;
        }

        private void ResetUnitEntryFields()
        {
            UnitComboBox.SelectedIndex = -1;
            SalePriceTextBox.Clear();
            PurchasePriceTextBox.Clear();
            QuantityPerUnitTextBox.Clear();
            IsBaseUnitCheckBox.IsChecked = false;
            IsDefaultSaleUnitCheckBox.IsChecked = false;
            IsDefaultPurchaseUnitCheckBox.IsChecked = false;
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                NormalizeUnitFlags(_productUnits);
                var unitsValidation = ValidateUnits(_productUnits);
                if (unitsValidation != null)
                {
                    MessageBox.Show(unitsValidation, "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dto = new ProductWriteDto
                {
                    Name = NameTextBox.Text,
                    ITEMCODE = long.TryParse(ITEMCODE.Text, out var itemCode) ? itemCode : (long?)null,
                    Description = DescriptionTextBox.Text,
                    Status = (ProductStatus)(StatusComboBox.SelectedValue ?? ProductStatus.InStock),
                    TaxExempt = TaxExemptCheckBox.IsChecked ?? false,
                    TaxRate = decimal.TryParse(TaxRate.Text, out var rate) ? rate : 0m,
                    MiniQuantity = decimal.TryParse(MinimumQuantityTextBox.Text, out var minQty) ? minQty : (decimal?)null,
                    BrandId = BrandComboBox.SelectedValue != null ? (int)BrandComboBox.SelectedValue : (int?)null,
                    SubCategoryId = SubCategoryComboBox.SelectedValue != null ? (int)SubCategoryComboBox.SelectedValue : 0,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                };

                if (dto.TaxExempt != true)
                {
                    foreach (var unit in _productUnits)
                    {
                        unit.UnTaxedPrice = unit.SalePrice;
                        unit.SalePrice = unit.SalePrice + (unit.SalePrice * (dto.TaxRate ?? 0m) / 100m);
                    }
                }

                var result = await _productService.CreateAsync(dto);
                if (!result.Success)
                {
                    MessageBox.Show($"❌ فشل في إنشاء المنتج: {result.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var productId = result.Data.Id;
                foreach (var unit in _productUnits)
                {
                    unit.ProductId = productId;
                    unit.CreatedDate = DateTime.Now;
                    unit.UpdatedDate = DateTime.Now;
                    await _productUnitService.CreateAsync(unit);
                }

                MessageBox.Show("✅ تم إنشاء المنتج والوحدات بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ: {ex.Message}", "استثناء", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearForm()
        {
            NameTextBox.Text = string.Empty;
            ITEMCODE.Text = string.Empty;
            DescriptionTextBox.Text = string.Empty;
            MinimumQuantityTextBox.Text = string.Empty;
            TaxRate.Text = string.Empty;
            TaxExemptCheckBox.IsChecked = false;
            BrandComboBox.SelectedIndex = -1;
            SubCategoryComboBox.SelectedIndex = -1;
            StatusComboBox.SelectedIndex = -1;
            _productUnits.Clear();
            UnitsStackPanel.Children.Clear();
            ResetUnitEntryFields();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "اختر صورة المنتج",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif"
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            var filePath = openFileDialog.FileName;
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(filePath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
        }
    }
}

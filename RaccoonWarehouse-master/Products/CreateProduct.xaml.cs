using Microsoft.Win32;
using RaccoonWarehouse;
using RaccoonWarehouse.Application.Service.Brands;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.ProductUnits;
using RaccoonWarehouse.Application.Service.SubCategories;
using RaccoonWarehouse.Application.Service.Units;
using RaccoonWarehouse.Helpers.Localization;
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
        private int? _preferredSubCategoryId;
        private List<UnitLookupItem> _unitLookupItems = new();

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

        public void InitializeForSubCategory(int subCategoryId, string? subCategoryName = null)
        {
            _preferredSubCategoryId = subCategoryId;
            TryApplyPreferredSubCategory();
        }

        private async Task LoadDataAsync()
        {
            var categories = await _subCategoryService.GetAllAsync();
            SubCategoryComboBox.ItemsSource = categories.Data;
            SubCategoryComboBox.DisplayMemberPath = "Name";
            SubCategoryComboBox.SelectedValuePath = "Id";
            TryApplyPreferredSubCategory();

            var brands = await _brandService.GetAllAsync();
            BrandComboBox.ItemsSource = brands.Data;
            BrandComboBox.DisplayMemberPath = "Name";
            BrandComboBox.SelectedValuePath = "Id";

            var units = await _unitService.GetAllAsync();
            _unitLookupItems = units.Data?
                .Select(unit => new UnitLookupItem(unit.Id, unit.Name, UiText.Translate(unit.Name)))
                .ToList() ?? new List<UnitLookupItem>();
            UnitComboBox.ItemsSource = _unitLookupItems;
            UnitComboBox.DisplayMemberPath = nameof(UnitLookupItem.DisplayName);
            UnitComboBox.SelectedValuePath = "Id";

            StatusComboBox.ItemsSource = Enum.GetValues(typeof(ProductStatus)).Cast<ProductStatus>();
            UiText.ApplyWindow(this);
        }

        private void TryApplyPreferredSubCategory()
        {
            if (!_preferredSubCategoryId.HasValue || SubCategoryComboBox == null)
                return;

            SubCategoryComboBox.SelectedValue = _preferredSubCategoryId.Value;
        }

        private void AddUnit_Click(object sender, RoutedEventArgs e)
        {
            if (UnitComboBox.SelectedValue == null)
            {
                MessageBox.Show(UiText.T("يرجى اختيار وحدة أولاً.", "Please choose a unit first."), UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(SalePriceTextBox.Text, out var salePrice) ||
                !decimal.TryParse(PurchasePriceTextBox.Text, out var purchasePrice) ||
                !decimal.TryParse(QuantityPerUnitTextBox.Text, out var qty))
            {
                MessageBox.Show(UiText.T("يرجى إدخال أسعار وأرقام صحيحة.", "Please enter valid prices and numbers."), UiText.T("خطأ في الإدخال", "Input Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var selectedUnitId = (int)UnitComboBox.SelectedValue;
            if (_productUnits.Any(u => u.UnitId == selectedUnitId))
            {
                MessageBox.Show(UiText.T("لا يمكن تكرار نفس الوحدة أكثر من مرة للصنف نفسه.", "The same unit cannot be added more than once for the same product."), UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Warning);
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
            UiText.ApplyTranslations(UnitsStackPanel);
        }

        private string UnitComboBoxItemText(int unitId)
        {
            var selected = _unitLookupItems.FirstOrDefault(x => x.Id == unitId);
            return selected?.DisplayName ?? unitId.ToString();
        }

        private void AddUnitRow(ProductUnitWriteDto unit, string unitName)
        {
            var row = new WrapPanel
            {
                Margin = new Thickness(0, 0, 0, 10),
                FlowDirection = UiText.CurrentFlowDirection,
                Tag = unit
            };

            row.Children.Add(CreateUnitInfoBlock(UiText.T("الوحدة", "Unit"), unitName));
            row.Children.Add(CreateUnitInfoBlock(UiText.T("سعر البيع", "Sale Price"), unit.SalePrice.ToString()));
            row.Children.Add(CreateUnitInfoBlock(UiText.T("سعر الشراء", "Purchase Price"), unit.PurchasePrice.ToString()));
            row.Children.Add(CreateUnitInfoBlock(UiText.T("الكمية", "Quantity"), unit.QuantityPerUnit.ToString()));
            row.Children.Add(CreateUnitInfoBlock(UiText.T("أساسية", "Primary"), unit.IsBaseUnit ? UiText.T("نعم", "Yes") : UiText.T("لا", "No")));
            row.Children.Add(CreateUnitInfoBlock(UiText.T("بيع", "Sale"), unit.IsDefaultSaleUnit ? UiText.T("افتراضي", "Default") : "-"));
            row.Children.Add(CreateUnitInfoBlock(UiText.T("شراء", "Purchase"), unit.IsDefaultPurchaseUnit ? UiText.T("افتراضي", "Default") : "-"));

            var removeButton = new Button
            {
                Content = UiText.T("حذف", "Delete"),
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
                Width = 150,
                Margin = new Thickness(0, 0, 14, 10),
                FlowDirection = UiText.CurrentFlowDirection
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
                BorderBrush = Brushes.LightGray,
                FlowDirection = UiText.CurrentFlowDirection,
                TextAlignment = UiText.IsEnglish ? TextAlignment.Left : TextAlignment.Right
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
                return UiText.T("يجب إضافة وحدة واحدة على الأقل.", "At least one unit must be added.");

            if (units.Any(u => u.QuantityPerUnit <= 0))
                return UiText.T("الكمية لكل وحدة يجب أن تكون أكبر من صفر.", "Quantity per unit must be greater than zero.");

            if (units.GroupBy(u => u.UnitId).Any(g => g.Count() > 1))
                return UiText.T("لا يمكن تكرار نفس الوحدة أكثر من مرة.", "The same unit cannot be repeated more than once.");

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
                    MessageBox.Show(unitsValidation, UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    MessageBox.Show($"{UiText.T("فشل في إنشاء المنتج", "Failed to create the product")}: {result.Message}", UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
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

                MessageBox.Show(UiText.T("تم إنشاء المنتج والوحدات بنجاح!", "The product and units were created successfully!"), UiText.T("نجاح", "Success"), MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("خطأ", "Error")}: {ex.Message}", UiText.T("استثناء", "Exception"), MessageBoxButton.OK, MessageBoxImage.Error);
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
                Title = UiText.T("اختر صورة المنتج", "Choose Product Image"),
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

        private sealed record UnitLookupItem(int Id, string Name, string DisplayName);
    }
}

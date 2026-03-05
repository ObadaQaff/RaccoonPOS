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
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.Products
{
    public partial class UpdateProduct : Window
    {
        private readonly IProductService _productService;
        private readonly ISubCategoryService _subCategoryService;
        private readonly IBrandService _brandService;
        private readonly IProductUnitService _productUnitService;
        private readonly IUnitService _unitService;

        private int _productId;
        private List<ProductUnitWriteDto> _productUnits = new();

        public UpdateProduct(
            IProductService productService,
            ISubCategoryService subCategoryService,
            IBrandService brandService,
            IProductUnitService productUnitService,
            IUnitService unitService)
        {
            InitializeComponent();

            _productService = productService;
            _subCategoryService = subCategoryService;
            _brandService = brandService;
            _productUnitService = productUnitService;
            _unitService = unitService;
        }

        public async Task Initialize(int id)
        {
            _productId = id;
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            SubCategoryComboBox.ItemsSource = (await _subCategoryService.GetAllAsync()).Data;
            SubCategoryComboBox.DisplayMemberPath = "Name";
            SubCategoryComboBox.SelectedValuePath = "Id";

            BrandComboBox.ItemsSource = (await _brandService.GetAllAsync()).Data;
            BrandComboBox.DisplayMemberPath = "Name";
            BrandComboBox.SelectedValuePath = "Id";

            UnitComboBox.ItemsSource = (await _unitService.GetAllAsync()).Data;
            UnitComboBox.DisplayMemberPath = "Name";
            UnitComboBox.SelectedValuePath = "Id";

            StatusComboBox.ItemsSource = Enum.GetValues(typeof(ProductStatus)).Cast<ProductStatus>();

            var product = await _productService.GetByIdAsync(_productId);
            if (product.Data == null)
                return;

            NameTextBox.Text = product.Data.Name;
            ITEMCODE.Text = product.Data.ITEMCODE?.ToString();
            DescriptionTextBox.Text = product.Data.Description;
            StatusComboBox.SelectedValue = product.Data.Status;
            TaxExemptCheckBox.IsChecked = product.Data.TaxExempt;
            MinimumQuantityTextBox.Text = product.Data.MiniQuantity?.ToString();
            BrandComboBox.SelectedValue = product.Data.BrandId;
            SubCategoryComboBox.SelectedValue = product.Data.SubCategoryId;
            TaxRate.Text = product.Data.TaxRate.ToString();

            var units = await _productUnitService.GetAllWriteDtoWithFilteringAndIncludeAsync(pu => pu.ProductId == _productId, pu => pu.Unit);
            _productUnits = units.Data?.ToList() ?? new List<ProductUnitWriteDto>();
            NormalizeUnitFlags(_productUnits);
            RebuildUnitsPanel();
        }

        private void RebuildUnitsPanel()
        {
            UnitsStackPanel.Children.Clear();
            foreach (var unit in _productUnits)
                AddUnitRow(unit);
        }

        private void AddUnitRow(ProductUnitWriteDto unit)
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10),
                Tag = unit
            };

            row.Children.Add(CreateReadOnlyUnitPanel("الوحدة", unit.Unit?.Name ?? unit.UnitId.ToString(), 140));
            row.Children.Add(CreateEditableDecimalPanel("سعر البيع", unit.SalePrice.ToString(), 100));
            row.Children.Add(CreateEditableDecimalPanel("سعر الشراء", unit.PurchasePrice.ToString(), 100));
            row.Children.Add(CreateEditableDecimalPanel("الكمية لكل وحدة", unit.QuantityPerUnit.ToString(), 110));
            row.Children.Add(CreateCheckPanel("أساسية", unit.IsBaseUnit));
            row.Children.Add(CreateCheckPanel("بيع", unit.IsDefaultSaleUnit));
            row.Children.Add(CreateCheckPanel("شراء", unit.IsDefaultPurchaseUnit));

            var deleteBtn = new Button
            {
                Content = "حذف",
                Margin = new Thickness(0, 22, 10, 0),
                Width = 90,
                Style = (Style)FindResource("PrimaryButtonStyle"),
                VerticalAlignment = VerticalAlignment.Bottom,
                Background = System.Windows.Media.Brushes.Red
            };

            deleteBtn.Click += async (_, _) =>
            {
                var result = MessageBox.Show("هل أنت متأكد من حذف هذه الوحدة؟", "تأكيد الحذف", MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes)
                    return;

                if (unit.Id > 0)
                {
                    var deleteResult = await _productUnitService.DeleteAsync(unit.Id);
                    if (!deleteResult.Success)
                    {
                        MessageBox.Show($"❌ فشل الحذف: {deleteResult.Message}");
                        return;
                    }
                }

                _productUnits.Remove(unit);
                NormalizeUnitFlags(_productUnits);
                RebuildUnitsPanel();
            };

            row.Children.Add(deleteBtn);
            UnitsStackPanel.Children.Add(row);
        }

        private static StackPanel CreateReadOnlyUnitPanel(string label, string value, double width)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
            panel.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold });
            panel.Children.Add(new TextBox
            {
                Width = width,
                Height = 30,
                IsReadOnly = true,
                Text = value
            });
            return panel;
        }

        private static StackPanel CreateEditableDecimalPanel(string label, string value, double width)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
            panel.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold });
            panel.Children.Add(new TextBox
            {
                Width = width,
                Height = 30,
                Text = value
            });
            return panel;
        }

        private static StackPanel CreateCheckPanel(string label, bool value)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 0, 10, 0), VerticalAlignment = VerticalAlignment.Bottom };
            panel.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold });
            panel.Children.Add(new CheckBox
            {
                IsChecked = value,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 6, 0, 0)
            });
            return panel;
        }

        private async void AddUnit_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(SalePriceTextBox.Text, out var salePrice) ||
                !decimal.TryParse(PurchasePriceTextBox.Text, out var purchasePrice) ||
                !decimal.TryParse(QuantityPerUnitTextBox.Text, out var qty) ||
                UnitComboBox.SelectedValue == null)
            {
                MessageBox.Show("❌ يرجى ملء جميع بيانات الوحدة بشكل صحيح.");
                return;
            }

            var selectedUnitId = (int)UnitComboBox.SelectedValue;
            if (_productUnits.Any(u => u.UnitId == selectedUnitId))
            {
                MessageBox.Show("لا يمكن تكرار نفس الوحدة أكثر من مرة للصنف نفسه.");
                return;
            }

            var unit = new ProductUnitWriteDto
            {
                ProductId = _productId,
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

            var addResult = await _productUnitService.CreateAsync(unit);
            if (!addResult.Success)
            {
                MessageBox.Show($"❌ فشل في إضافة الوحدة: {addResult.Message}");
                return;
            }

            if (addResult.Data != null)
            {
                unit.Id = addResult.Data.Id;
                unit.Unit = addResult.Data.Unit;
            }

            _productUnits.Add(unit);
            NormalizeUnitFlags(_productUnits);
            RebuildUnitsPanel();
            ResetUnitEntryFields();
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var productDto = new ProductWriteDto
                {
                    Id = _productId,
                    Name = NameTextBox.Text,
                    ITEMCODE = long.TryParse(ITEMCODE.Text, out var itemCode) ? itemCode : (long?)null,
                    Description = DescriptionTextBox.Text,
                    Status = (ProductStatus)(StatusComboBox.SelectedValue ?? ProductStatus.InStock),
                    TaxExempt = TaxExemptCheckBox.IsChecked ?? false,
                    TaxRate = decimal.TryParse(TaxRate.Text, out var rate) ? rate : 0,
                    MiniQuantity = decimal.TryParse(MinimumQuantityTextBox.Text, out var minQty) ? minQty : (decimal?)null,
                    BrandId = BrandComboBox.SelectedValue != null ? (int)BrandComboBox.SelectedValue : (int?)null,
                    SubCategoryId = SubCategoryComboBox.SelectedValue != null ? (int)SubCategoryComboBox.SelectedValue : 0,
                    UpdatedDate = DateTime.Now
                };

                var unitsDto = CollectUnitsFromUI();
                NormalizeUnitFlags(unitsDto);

                var result = await _productService.UpdateProductWithUnitsAsync(productDto, unitsDto);
                MessageBox.Show(result.Success ? "✅ تم تحديث المنتج والوحدات!" : $"❌ فشل التحديث: {result.Message}");

                if (result.Success)
                {
                    _productUnits = unitsDto;
                    RebuildUnitsPanel();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ خطأ: {ex.Message}");
            }
        }

        private List<ProductUnitWriteDto> CollectUnitsFromUI()
        {
            var list = new List<ProductUnitWriteDto>();

            foreach (var rowObj in UnitsStackPanel.Children)
            {
                if (rowObj is not StackPanel row || row.Tag is not ProductUnitWriteDto unit)
                    continue;

                var saleBox = ((row.Children[1] as StackPanel)?.Children[1] as TextBox);
                var purchaseBox = ((row.Children[2] as StackPanel)?.Children[1] as TextBox);
                var qtyBox = ((row.Children[3] as StackPanel)?.Children[1] as TextBox);
                var baseCheck = ((row.Children[4] as StackPanel)?.Children[1] as CheckBox);
                var saleCheck = ((row.Children[5] as StackPanel)?.Children[1] as CheckBox);
                var purchaseCheck = ((row.Children[6] as StackPanel)?.Children[1] as CheckBox);

                if (saleBox == null || purchaseBox == null || qtyBox == null ||
                    baseCheck == null || saleCheck == null || purchaseCheck == null)
                {
                    continue;
                }

                if (!decimal.TryParse(saleBox.Text, out var sale) ||
                    !decimal.TryParse(purchaseBox.Text, out var purchase) ||
                    !decimal.TryParse(qtyBox.Text, out var qty))
                {
                    throw new Exception("يوجد وحدة فيها قيم غير رقمية.");
                }

                unit.SalePrice = sale;
                unit.PurchasePrice = purchase;
                unit.QuantityPerUnit = qty;
                unit.IsBaseUnit = baseCheck.IsChecked == true;
                unit.IsDefaultSaleUnit = saleCheck.IsChecked == true;
                unit.IsDefaultPurchaseUnit = purchaseCheck.IsChecked == true;
                unit.UpdatedDate = DateTime.Now;
                unit.ProductId = _productId;

                list.Add(unit);
            }

            return list;
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

        private void ResetUnitEntryFields()
        {
            SalePriceTextBox.Clear();
            PurchasePriceTextBox.Clear();
            QuantityPerUnitTextBox.Clear();
            UnitComboBox.SelectedIndex = -1;
            IsBaseUnitCheckBox.IsChecked = false;
            IsDefaultSaleUnitCheckBox.IsChecked = false;
            IsDefaultPurchaseUnitCheckBox.IsChecked = false;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

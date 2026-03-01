using RaccoonWarehouse.Application.Service.Brands;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.SubCategories;
using RaccoonWarehouse.Application.Service.Units;
using RaccoonWarehouse.Application.Service.ProductUnits;
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

        private  int _productId;

        private  List<ProductUnitWriteDto> _productUnits = new();

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
        public async Task Initialize(int Id)
        {
            _productId = Id;
            await LoadDataAsync();
        }
        private async Task LoadDataAsync()
        {
            // Load comboboxes
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

            // Load product
            var product = await _productService.GetByIdAsync(_productId);
            if (product.Data != null)
            {
                NameTextBox.Text = product.Data.Name;
                ITEMCODE.Text = product.Data.ITEMCODE?.ToString();
                DescriptionTextBox.Text = product.Data.Description;
                StatusComboBox.SelectedValue = product.Data.Status;
                TaxExemptCheckBox.IsChecked = product.Data.TaxExempt;
                MinimumQuantityTextBox.Text = product.Data.MiniQuantity?.ToString();
                BrandComboBox.SelectedValue = product.Data.BrandId;
                SubCategoryComboBox.SelectedValue = product.Data.SubCategoryId;
                TaxRate.Text = product.Data.TaxRate.ToString();
                // Load units
                var units = await _productUnitService.GetAllWriteDtoWithFilteringAndIncludeAsync(pu=>pu.ProductId==_productId,pu=>pu.Unit);
               /* foreach (var u in units.Data)
                {
                    AddUnitRow(u);
                    _productUnits.Add(new ProductUnitWriteDto
                    {
                        Id = u.Id,
                        ProductId = _productId,
                        UnitId = u.UnitId,
                        SalePrice = u.SalePrice,
                        PurchasePrice = u.PurchasePrice,
                        QuantityPerUnit = u.QuantityPerUnit,
                        CreatedDate = u.CreatedDate,
                        UpdatedDate = u.UpdatedDate
                    });
                }*/
                foreach (var u in units.Data)
                {
                    // ensure these are set
                    u.ProductId = _productId;

                    _productUnits.Add(u);   // ✅ keep same reference
                    AddUnitRow(u);          // ✅ row edits will edit the SAME object
                }



            }
        }

        private void AddUnitRow(ProductUnitWriteDto unit = null)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            sp.Tag = unit;

            // Unit
            var unitPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 10, 0) };
            unitPanel.Children.Add(new TextBlock { Text = "الوحدة", FontWeight = FontWeights.SemiBold });
            var unitBox = new TextBox
            {
                Width = 150,
                Height = 25,

                Text = unit != null ? unit.Unit?.Name.ToString() : ""
            };
            unitPanel.Children.Add(unitBox);

            // SalePrice
            var salePanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 10, 0) };
            salePanel.Children.Add(new TextBlock { Text = "سعر البيع", FontWeight = FontWeights.SemiBold });
            var saleBox = new TextBox
            {
                Width = 100,
                Height = 25,
                Text = unit != null ? unit.SalePrice.ToString() : ""
            };
            salePanel.Children.Add(saleBox);

            // PurchasePrice
            var purchasePanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 10, 0) };
            purchasePanel.Children.Add(new TextBlock { Text = "سعر الشراء", FontWeight = FontWeights.SemiBold });
            var purchaseBox = new TextBox
            {
                Width = 100,
                Height = 25,
                Text = unit != null ? unit.PurchasePrice.ToString() : ""
            };
            purchasePanel.Children.Add(purchaseBox);

            // QuantityPerUnit
            var qtyPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 10, 0) };
            qtyPanel.Children.Add(new TextBlock { Text = "الكمية لكل وحدة", FontWeight = FontWeights.SemiBold });
            var qtyBox = new TextBox
            {
                Width = 100,
                Height = 25,
                Text = unit != null ? unit.QuantityPerUnit.ToString() : ""
            };
            qtyPanel.Children.Add(qtyBox);

            // Update button
            var updateBtn = new Button
            {
                Content = "تحديث",
                Width = 100,
                Margin = new Thickness(0, 0, 10, 0),
                Style = (Style)FindResource("PrimaryButtonStyle"),
                VerticalAlignment = VerticalAlignment.Bottom
            };
            var deleteBtn = new Button
            {
                Background = System.Windows.Media.Brushes.Red,
                Content = "حذف",
                Margin = new Thickness(0, 0, 10, 0),
                Width = 100,
                Style = (Style)FindResource("PrimaryButtonStyle"),
                VerticalAlignment = VerticalAlignment.Bottom
            };

            /*updateBtn.Click += async (s, e) =>
            {
                if (decimal.TryParse(saleBox.Text, out var sale) &&
                    decimal.TryParse(purchaseBox.Text, out var purchase) &&
                    decimal.TryParse(qtyBox.Text, out var qty))
                {
                    unit.SalePrice = sale;
                    unit.PurchasePrice = purchase;
                    unit.QuantityPerUnit = qty;
                    unit.UpdatedDate = DateTime.Now;

                    var result = await _productUnitService.UpdateAsync(unit);
                    MessageBox.Show(result.Success ? "✅ تم تحديث الوحدة!" : $"❌ فشل التحديث: {result.Message}");
                }
                else
                {
                    MessageBox.Show("❌ يرجى إدخال أرقام صحيحة.");
                }
            };*/
            updateBtn.Click += (s, e) =>
            {
                if (unit == null)
                {
                    MessageBox.Show("❌ لا يمكن تحديث وحدة غير موجودة.");
                    return;
                }

                if (decimal.TryParse(saleBox.Text, out var sale) &&
                    decimal.TryParse(purchaseBox.Text, out var purchase) &&
                    decimal.TryParse(qtyBox.Text, out var q))
                {
                    unit.SalePrice = sale;
                    unit.PurchasePrice = purchase;
                    unit.QuantityPerUnit = q;
                    unit.UpdatedDate = DateTime.Now;

                    MessageBox.Show("✅ تم تحديث البيانات محلياً. اضغط (تحديث المنتج) للحفظ.");
                }
                else
                {
                    MessageBox.Show("❌ يرجى إدخال أرقام صحيحة.");
                }
            };
            deleteBtn.Click += async (s, e) =>
            {

                var result= MessageBox.Show("هل أنت متأكد من حذف هذه الوحدة؟", "تأكيد الحذف", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    var  deleteResult = await _productUnitService.DeleteAsync(unit.Id);
                    if (deleteResult.Success)
                    {
                        UnitsStackPanel.Children.Remove(sp);
                        MessageBox.Show("✅ تم حذف الوحدة!");
                    }
                    else
                    {
                        MessageBox.Show($"❌ فشل الحذف: {deleteResult.Message}");
                    }
                }
             
            };
          
            sp.Children.Add(unitPanel);
            sp.Children.Add(salePanel);
            sp.Children.Add(purchasePanel);
            sp.Children.Add(qtyPanel);
            sp.Children.Add(updateBtn);
            sp.Children.Add(deleteBtn);

            UnitsStackPanel.Children.Add(sp);
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

            var unit = new ProductUnitWriteDto
            {
                ProductId = _productId,
                UnitId = (int)UnitComboBox.SelectedValue,
                SalePrice = salePrice,
                PurchasePrice = purchasePrice,
                QuantityPerUnit = qty,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            var addResult = await _productUnitService.CreateAsync(unit);
            if (!addResult.Success)
            {
                MessageBox.Show($"❌ فشل في إضافة الوحدة: {addResult.Message}");
                return;
            }

            // ✅ IMPORTANT: assign returned Id (depends on your Result shape)
            if (addResult.Data != null)
            {
                unit.Id = addResult.Data.Id;
                unit.Unit = addResult.Data.Unit; // if returned
            }
          

          /*  var addResult = await _productUnitService.CreateAsync(unit);
            if (!addResult.Success)
            {
                MessageBox.Show($"❌ فشل في إضافة الوحدة: {addResult.Message}");
                return;
            }*/

            // ✅ Make sure _productUnits list exists
            if (_productUnits == null)
                _productUnits = new List<ProductUnitWriteDto>();

            _productUnits.Add(unit); // Add BEFORE updating UI
            AddUnitRow(unit);        // Safe now

            MessageBox.Show("✅ تم إضافة الوحدة بنجاح!");

            SalePriceTextBox.Clear();
            PurchasePriceTextBox.Clear();
            QuantityPerUnitTextBox.Clear();
            UnitComboBox.SelectedIndex = -1;
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

                // ✅ get latest values from UI
                var unitsDto = CollectUnitsFromUI();

                var result = await _productService.UpdateProductWithUnitsAsync(productDto, unitsDto);

                MessageBox.Show(result.Success
                    ? "✅ تم تحديث المنتج والوحدات!"
                    : $"❌ فشل التحديث: {result.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ خطأ: {ex.Message}");
            }
        }


        /*  private async void Update_Click(object sender, RoutedEventArgs e)
          {
              try
              {
                  var dto = new ProductWriteDto
                  {
                      Id = _productId,
                      Name = NameTextBox.Text,
                      ITEMCODE = long.TryParse(ITEMCODE.Text, out var itemCode) ? itemCode : (long?)null,
                      Description = DescriptionTextBox.Text,
                      Status = (ProductStatus)(StatusComboBox.SelectedValue ?? ProductStatus.InStock),
                      TaxExempt = TaxExemptCheckBox.IsChecked ?? false,
                      MiniQuantity = decimal.TryParse(MinimumQuantityTextBox.Text, out var minQty) ? minQty : (decimal?)null,
                      BrandId = BrandComboBox.SelectedValue != null ? (int)BrandComboBox.SelectedValue : (int?)null,
                      SubCategoryId = SubCategoryComboBox.SelectedValue != null ? (int)SubCategoryComboBox.SelectedValue : 12,
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

                              var newSalePrice = unit.SalePrice + (unit.SalePrice * taxRate / 100m);
                              unit.SalePrice = newSalePrice;
                              var updateResult = await _productUnitService.UpdateAsync(unit);
                          }
                          else
                          {
                              dto.TaxRate = 0;
                          }
                      }
                  }   


                  var result = await _productService.UpdateAsync(dto);
                  MessageBox.Show(result.Success ? "✅ تم تحديث المنتج!" : $"❌ فشل التحديث: {result.Message}");
              }
              catch (Exception ex)
              {
                  MessageBox.Show($"❌ خطأ: {ex.Message}");
              }
          }*/

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        private List<ProductUnitWriteDto> CollectUnitsFromUI()
        {
            var list = new List<ProductUnitWriteDto>();

            foreach (var rowObj in UnitsStackPanel.Children)
            {
                if (rowObj is not StackPanel row) continue;

                // ✅ unit linked to UI row
                if (row.Tag is not ProductUnitWriteDto unit) continue;

                // row structure: [unitPanel, salePanel, purchasePanel, qtyPanel, updateBtn, deleteBtn]
                // each panel: TextBlock + TextBox
                var saleBox = ((row.Children[1] as StackPanel)?.Children[1] as TextBox);
                var purchaseBox = ((row.Children[2] as StackPanel)?.Children[1] as TextBox);
                var qtyBox = ((row.Children[3] as StackPanel)?.Children[1] as TextBox);

                if (saleBox == null || purchaseBox == null || qtyBox == null)
                    continue;

                if (!decimal.TryParse(saleBox.Text, out var sale) ||
                    !decimal.TryParse(purchaseBox.Text, out var purchase) ||
                    !decimal.TryParse(qtyBox.Text, out var qty))
                {
                    throw new Exception("يوجد وحدة فيها قيم غير رقمية.");
                }

                unit.SalePrice = sale;
                unit.PurchasePrice = purchase;
                unit.QuantityPerUnit = qty;
                unit.UpdatedDate = DateTime.Now;
                unit.ProductId = _productId;

                list.Add(unit);
            }

            return list;
        }
    }
}
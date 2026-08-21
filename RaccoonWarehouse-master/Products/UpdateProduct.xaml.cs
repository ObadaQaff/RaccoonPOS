using RaccoonWarehouse.Application.Service.Brands;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.ProductUnits;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.StockTransactions;
using RaccoonWarehouse.Application.Service.SubCategories;
using RaccoonWarehouse.Application.Service.Units;
using RaccoonWarehouse.Common;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using RaccoonWarehouse.Domain.Stock;
using RaccoonWarehouse.Domain.Stock.DTOs;
using RaccoonWarehouse.Domain.StockTransactions;
using RaccoonWarehouse.Domain.StockTransactions.DTOs;
using System;
using System.Collections.ObjectModel;
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
        private readonly IStockTransactionService _stockTransactionService;
        private readonly SourceDocumentNavigationService _sourceDocumentNavigationService;
        private readonly IStockService _stockService;

        private int _productId;
        private List<ProductUnitWriteDto> _productUnits = new();
        private List<UnitLookupItem> _unitLookupItems = new();
        public ObservableCollection<ProductMovementRow> ProductMovements { get; } = new();
        public ObservableCollection<ProductStockUnitRow> ProductStockByUnit { get; } = new();
        public decimal CurrentStockTotalBaseQuantity { get; set; }
        public decimal CurrentStockTotalQuantity { get; set; }
        public decimal CurrentStockAverageCost { get; set; }
        public decimal CurrentStockAverageSalePrice { get; set; }
        public string CurrentStockNearestExpiry { get; set; } = "-";

        public UpdateProduct(
            IProductService productService,
            ISubCategoryService subCategoryService,
            IBrandService brandService,
            IProductUnitService productUnitService,
            IUnitService unitService,
            IStockTransactionService stockTransactionService,
            SourceDocumentNavigationService sourceDocumentNavigationService,
            IStockService stockService)
        {
            InitializeComponent();

            _productService = productService;
            _subCategoryService = subCategoryService;
            _brandService = brandService;
            _productUnitService = productUnitService;
            _unitService = unitService;
            _stockTransactionService = stockTransactionService;
            _sourceDocumentNavigationService = sourceDocumentNavigationService;
            _stockService = stockService;
            DataContext = this;
        }

        public async Task Initialize(int id)
        {
            _productId = id;
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                SubCategoryComboBox.ItemsSource = (await _subCategoryService.GetAllAsync()).Data;
                SubCategoryComboBox.DisplayMemberPath = "Name";
                SubCategoryComboBox.SelectedValuePath = "Id";

                BrandComboBox.ItemsSource = (await _brandService.GetAllAsync()).Data;
                BrandComboBox.DisplayMemberPath = "Name";
                BrandComboBox.SelectedValuePath = "Id";

                _unitLookupItems = (await _unitService.GetAllAsync()).Data?
                    .Select(unit => new UnitLookupItem(unit.Id, unit.Name, UiText.Translate(unit.Name)))
                    .ToList() ?? new List<UnitLookupItem>();
                UnitComboBox.ItemsSource = _unitLookupItems;
                UnitComboBox.DisplayMemberPath = nameof(UnitLookupItem.DisplayName);
                UnitComboBox.SelectedValuePath = "Id";

                StatusComboBox.ItemsSource = Enum.GetValues(typeof(ProductStatus)).Cast<ProductStatus>();

                var product = await _productService.GetByIdAsync(_productId);
                if (!product.Success || product.Data == null)
                {
                    MessageBox.Show(product.Message ?? UiText.T("تعذر تحميل بيانات الصنف.", "Could not load the product data."));
                    return;
                }

                NameTextBox.Text = product.Data.Name;
                ITEMCODE.Text = product.Data.ITEMCODE?.ToString();
                DescriptionTextBox.Text = product.Data.Description;
                StatusComboBox.SelectedValue = product.Data.Status;
                TaxExemptCheckBox.IsChecked = product.Data.TaxExempt;
                MinimumQuantityTextBox.Text = product.Data.MiniQuantity?.ToString("0.00000");
                BrandComboBox.SelectedValue = product.Data.BrandId;
                SubCategoryComboBox.SelectedValue = product.Data.SubCategoryId;
                TaxRate.Text = product.Data.TaxRate.ToString();

                var units = await _productUnitService.GetAllWriteDtoWithFilteringAndIncludeAsync(pu => pu.ProductId == _productId, pu => pu.Unit);
                _productUnits = units.Data?.ToList() ?? new List<ProductUnitWriteDto>();
                NormalizeUnitFlags(_productUnits);
                RebuildUnitsPanel();
                await LoadCurrentStockSummaryAsync();
                await LoadProductMovementsAsync();
                UiText.ApplyWindow(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء تحميل بيانات الصنف", "An error occurred while loading the product data")}: {ex.Message}");
            }
        }

        private async Task LoadCurrentStockSummaryAsync()
        {
            var result = await _stockService.GetAllWithFilteringAndIncludeAsync(
                stock => stock.ProductId == _productId,
                stock => stock.ProductUnit,
                stock => stock.ProductUnit.Unit);

            var rows = result.Data?.ToList() ?? new List<StockReadDto>();
            ProductStockByUnit.Clear();

            CurrentStockTotalQuantity = rows.Sum(stock => stock.Quantity);
            CurrentStockTotalBaseQuantity = rows.Sum(stock =>
            {
                var qtyPerUnit = stock.ProductUnit?.QuantityPerUnit ?? 1m;
                return stock.Quantity * (qtyPerUnit <= 0 ? 1m : qtyPerUnit);
            });

            if (CurrentStockTotalQuantity > 0)
            {
                CurrentStockAverageCost = rows.Sum(stock => stock.Quantity * stock.PurchasePrice) / CurrentStockTotalQuantity;
                CurrentStockAverageSalePrice = rows.Sum(stock => stock.Quantity * stock.SalePrice) / CurrentStockTotalQuantity;
            }
            else
            {
                CurrentStockAverageCost = 0m;
                CurrentStockAverageSalePrice = 0m;
            }

            var nearestExpiry = rows
                .Where(stock => stock.ExpiryDate.HasValue)
                .Select(stock => stock.ExpiryDate!.Value)
                .OrderBy(date => date)
                .FirstOrDefault();
            CurrentStockNearestExpiry = nearestExpiry == default ? "-" : nearestExpiry.ToString("yyyy-MM-dd");

            foreach (var group in rows.GroupBy(stock => stock.ProductUnitId))
            {
                var first = group.First();
                var unitName = first.ProductUnit?.Unit?.Name ?? UiText.T("غير محدد", "Unspecified");
                var unitQuantity = group.Sum(stock => stock.Quantity);
                var qtyPerUnit = first.ProductUnit?.QuantityPerUnit ?? 1m;
                var baseQty = unitQuantity * (qtyPerUnit <= 0 ? 1m : qtyPerUnit);
                ProductStockByUnit.Add(new ProductStockUnitRow
                {
                    UnitName = unitName,
                    Quantity = unitQuantity,
                    BaseQuantity = baseQty,
                    PurchasePrice = first.PurchasePrice,
                    SalePrice = first.SalePrice
                });
            }

            DataContext = null;
            DataContext = this;
        }

        private async Task LoadProductMovementsAsync()
        {
            var result = await _stockTransactionService.GetAllWithFilteringAndIncludeAsync(
                transaction => transaction.ProductId == _productId,
                transaction => transaction.Invoice,
                transaction => transaction.Voucher,
                transaction => transaction.Casher,
                transaction => transaction.Customer,
                transaction => transaction.ProductUnit);

            ProductMovements.Clear();

            foreach (var movement in (result.Data ?? new List<StockTransactionReadDto>())
                .OrderByDescending(m => m.TransactionDate)
                .Take(200))
            {
                ProductMovements.Add(new ProductMovementRow
                {
                    TransactionDate = movement.TransactionDate,
                    TransactionTypeLabel = GetTransactionTypeLabel(movement.TransactionType),
                    Quantity = movement.Quantity,
                    BaseQuantity = movement.BaseQuantity,
                    UnitPrice = movement.UnitPrice,
                    InvoiceId = movement.InvoiceId,
                    InvoiceRef = movement.Invoice?.InvoiceNumber ?? (movement.InvoiceId?.ToString() ?? "-"),
                    VoucherId = movement.VoucherId,
                    VoucherRef = movement.VoucherId?.ToString() ?? "-",
                    CashierName = movement.Casher?.Name ?? "-",
                    CustomerName = movement.Customer?.Name ?? "-",
                    Notes = movement.Notes ?? "-"
                });
            }
        }

        private async void InvoiceRef_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.DataContext is not ProductMovementRow row || !row.InvoiceId.HasValue || row.InvoiceId <= 0)
                return;

            try
            {
                await _sourceDocumentNavigationService.OpenSourceDocument("Invoice", row.InvoiceId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("تعذر فتح الفاتورة", "Could not open the invoice")}: {ex.Message}", UiText.T("خطأ", "Error"));
            }
        }

        private async void VoucherRef_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.DataContext is not ProductMovementRow row || !row.VoucherId.HasValue || row.VoucherId <= 0)
                return;

            try
            {
                await _sourceDocumentNavigationService.OpenSourceDocument("Voucher", row.VoucherId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("تعذر فتح السند", "Could not open the voucher")}: {ex.Message}", UiText.T("خطأ", "Error"));
            }
        }

        private static string GetTransactionTypeLabel(TransactionType transactionType)
        {
            return transactionType switch
            {
                TransactionType.Sale => UiText.T("بيع", "Sale"),
                TransactionType.Return => UiText.T("مردود", "Return"),
                TransactionType.Purchase => UiText.T("شراء", "Purchase"),
                TransactionType.Adjustment => UiText.T("تسوية", "Adjustment"),
                TransactionType.Damage => UiText.T("تالف", "Damage"),
                _ => transactionType.ToString()
            };
        }

        private void RebuildUnitsPanel()
        {
            UnitsStackPanel.Children.Clear();
            foreach (var unit in _productUnits)
                AddUnitRow(unit);

            UiText.ApplyTranslations(UnitsStackPanel);
        }

        private void AddUnitRow(ProductUnitWriteDto unit)
        {
            var row = new WrapPanel
            {
                Margin = new Thickness(0, 0, 0, 10),
                FlowDirection = UiText.CurrentFlowDirection,
                Tag = unit
            };

            row.Children.Add(CreateReadOnlyUnitPanel(UiText.T("الوحدة", "Unit"), GetLocalizedUnitName(unit), 140));
            row.Children.Add(CreateEditableDecimalPanel(UiText.T("سعر البيع", "Sale Price"), unit.SalePrice.ToString(), 100));
            row.Children.Add(CreateEditableDecimalPanel(UiText.T("سعر الشراء", "Purchase Price"), unit.PurchasePrice.ToString(), 100));
            row.Children.Add(CreateEditableDecimalPanel(UiText.T("الكمية لكل وحدة", "Quantity per Unit"), unit.QuantityPerUnit.ToString(), 110));
            row.Children.Add(CreateEditableTextPanel(UiText.T("الرمز المماثل", "Alternate barcode"), unit.AlternateBarcode ?? string.Empty, 150));
            row.Children.Add(CreateCheckPanel(UiText.T("أساسية", "Primary"), unit.IsBaseUnit));
            row.Children.Add(CreateCheckPanel(UiText.T("بيع", "Sale"), unit.IsDefaultSaleUnit));
            row.Children.Add(CreateCheckPanel(UiText.T("شراء", "Purchase"), unit.IsDefaultPurchaseUnit));

            var deleteBtn = new Button
            {
                Content = UiText.T("حذف", "Delete"),
                Margin = new Thickness(0, 22, 10, 0),
                Width = 90,
                Style = (Style)FindResource("PrimaryButtonStyle"),
                VerticalAlignment = VerticalAlignment.Bottom,
                Background = System.Windows.Media.Brushes.Red
            };

            deleteBtn.Click += async (_, _) =>
            {
                var result = MessageBox.Show(UiText.T("هل أنت متأكد من حذف هذه الوحدة؟", "Are you sure you want to delete this unit?"), UiText.T("تأكيد الحذف", "Delete confirmation"), MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes)
                    return;

                if (unit.Id > 0)
                {
                    var deleteResult = await _productUnitService.DeleteAsync(unit.Id);
                    if (!deleteResult.Success)
                    {
                        MessageBox.Show($"{UiText.T("فشل الحذف", "Delete failed")}: {deleteResult.Message}");
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
            var panel = new StackPanel
            {
                Width = Math.Max(width, 150),
                Margin = new Thickness(0, 0, 14, 10),
                FlowDirection = UiText.CurrentFlowDirection
            };
            panel.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold });
            panel.Children.Add(new TextBox
            {
                Width = Math.Max(width, 150),
                Height = 30,
                IsReadOnly = true,
                Text = value,
                FlowDirection = UiText.CurrentFlowDirection,
                TextAlignment = UiText.IsEnglish ? TextAlignment.Left : TextAlignment.Right
            });
            return panel;
        }

        private static StackPanel CreateEditableDecimalPanel(string label, string value, double width)
        {
            var panel = new StackPanel
            {
                Width = Math.Max(width, 150),
                Margin = new Thickness(0, 0, 14, 10),
                FlowDirection = UiText.CurrentFlowDirection
            };
            panel.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold });
            panel.Children.Add(new TextBox
            {
                Width = Math.Max(width, 150),
                Height = 30,
                Text = value,
                FlowDirection = UiText.CurrentFlowDirection,
                TextAlignment = UiText.IsEnglish ? TextAlignment.Left : TextAlignment.Right
            });
            return panel;
        }

        private static StackPanel CreateEditableTextPanel(string label, string value, double width)
        {
            var panel = new StackPanel
            {
                Width = Math.Max(width, 150),
                Margin = new Thickness(0, 0, 14, 10),
                FlowDirection = UiText.CurrentFlowDirection
            };
            panel.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold });
            panel.Children.Add(new TextBox
            {
                Width = Math.Max(width, 150),
                Height = 30,
                Text = value,
                FlowDirection = UiText.CurrentFlowDirection,
                TextAlignment = UiText.IsEnglish ? TextAlignment.Left : TextAlignment.Right
            });
            return panel;
        }

        private static StackPanel CreateCheckPanel(string label, bool value)
        {
            var panel = new StackPanel
            {
                Width = 150,
                Margin = new Thickness(0, 0, 14, 10),
                VerticalAlignment = VerticalAlignment.Bottom,
                FlowDirection = UiText.CurrentFlowDirection
            };
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
                MessageBox.Show(UiText.T("يرجى ملء جميع بيانات الوحدة بشكل صحيح.", "Please fill in all unit fields correctly."));
                return;
            }

            var selectedUnitId = (int)UnitComboBox.SelectedValue;
            if (_productUnits.Any(u => u.UnitId == selectedUnitId))
            {
                MessageBox.Show(UiText.T("لا يمكن تكرار نفس الوحدة أكثر من مرة للصنف نفسه.", "The same unit cannot be added more than once for the same product."));
                return;
            }

            var unit = new ProductUnitWriteDto
            {
                ProductId = _productId,
                UnitId = selectedUnitId,
                AlternateBarcode = string.IsNullOrWhiteSpace(AlternateBarcodeTextBox.Text) ? null : AlternateBarcodeTextBox.Text.Trim(),
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
                MessageBox.Show($"{UiText.T("فشل في إضافة الوحدة", "Failed to add the unit")}: {addResult.Message}");
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
                MessageBox.Show(result.Success ? UiText.T("تم تحديث المنتج والوحدات بنجاح.", "The product and units were updated successfully.") : $"{UiText.T("فشل التحديث", "Update failed")}: {result.Message}");

                if (result.Success)
                {
                    _productUnits = unitsDto;
                    RebuildUnitsPanel();
                    await LoadCurrentStockSummaryAsync();
                    CatalogRefreshNotifier.NotifyCatalogChanged();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("خطأ", "Error")}: {ex.Message}");
            }
        }

        private List<ProductUnitWriteDto> CollectUnitsFromUI()
        {
            var list = new List<ProductUnitWriteDto>();

            foreach (var rowObj in UnitsStackPanel.Children)
            {
                if (rowObj is not WrapPanel row || row.Tag is not ProductUnitWriteDto unit)
                    continue;

                var saleBox = ((row.Children[1] as StackPanel)?.Children[1] as TextBox);
                var purchaseBox = ((row.Children[2] as StackPanel)?.Children[1] as TextBox);
                var qtyBox = ((row.Children[3] as StackPanel)?.Children[1] as TextBox);
                var alternateBarcodeBox = ((row.Children[4] as StackPanel)?.Children[1] as TextBox);
                var baseCheck = ((row.Children[5] as StackPanel)?.Children[1] as CheckBox);
                var saleCheck = ((row.Children[6] as StackPanel)?.Children[1] as CheckBox);
                var purchaseCheck = ((row.Children[7] as StackPanel)?.Children[1] as CheckBox);

                if (saleBox == null || purchaseBox == null || qtyBox == null ||
                    alternateBarcodeBox == null || baseCheck == null || saleCheck == null || purchaseCheck == null)
                {
                    continue;
                }

                if (!decimal.TryParse(saleBox.Text, out var sale) ||
                    !decimal.TryParse(purchaseBox.Text, out var purchase) ||
                    !decimal.TryParse(qtyBox.Text, out var qty))
                {
                    throw new Exception(UiText.T("يوجد وحدة فيها قيم غير رقمية.", "A unit row contains non-numeric values."));
                }

                unit.SalePrice = sale;
                unit.PurchasePrice = purchase;
                unit.QuantityPerUnit = qty;
                unit.AlternateBarcode = string.IsNullOrWhiteSpace(alternateBarcodeBox.Text) ? null : alternateBarcodeBox.Text.Trim();
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
            AlternateBarcodeTextBox.Clear();
            UnitComboBox.SelectedIndex = -1;
            IsBaseUnitCheckBox.IsChecked = false;
            IsDefaultSaleUnitCheckBox.IsChecked = false;
            IsDefaultPurchaseUnitCheckBox.IsChecked = false;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private string GetLocalizedUnitName(ProductUnitWriteDto unit)
        {
            var rawName = unit.Unit?.Name
                ?? _unitLookupItems.FirstOrDefault(x => x.Id == unit.UnitId)?.Name;

            return string.IsNullOrWhiteSpace(rawName)
                ? unit.UnitId.ToString()
                : UiText.Translate(rawName);
        }

        private sealed record UnitLookupItem(int Id, string Name, string DisplayName);

        public sealed class ProductMovementRow
        {
            public DateTime TransactionDate { get; set; }
            public string TransactionTypeLabel { get; set; } = "-";
            public decimal Quantity { get; set; }
            public decimal BaseQuantity { get; set; }
            public decimal UnitPrice { get; set; }
            public int? InvoiceId { get; set; }
            public string InvoiceRef { get; set; } = "-";
            public int? VoucherId { get; set; }
            public string VoucherRef { get; set; } = "-";
            public string CashierName { get; set; } = "-";
            public string CustomerName { get; set; } = "-";
            public string Notes { get; set; } = "-";
            public bool HasInvoiceRef => InvoiceId.HasValue && InvoiceId.Value > 0;
            public bool HasVoucherRef => VoucherId.HasValue && VoucherId.Value > 0;
        }

        public sealed class ProductStockUnitRow
        {
            public string UnitName { get; set; } = "-";
            public decimal Quantity { get; set; }
            public decimal BaseQuantity { get; set; }
            public decimal PurchasePrice { get; set; }
            public decimal SalePrice { get; set; }
        }
    }
}

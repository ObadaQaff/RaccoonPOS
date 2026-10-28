using RaccoonWarehouse.Application.Service.Brands;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.ProductUnits;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.StockTransactions;
using RaccoonWarehouse.Application.Service.SubCategories;
using RaccoonWarehouse.Application.Service.Units;
using RaccoonWarehouse.Application.Service.Users;
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
using System.ComponentModel;
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
        private readonly IUserService _userService;
        private readonly IStockTransactionService _stockTransactionService;
        private readonly SourceDocumentNavigationService _sourceDocumentNavigationService;
        private readonly IStockService _stockService;

        private int _productId;
        private bool _isInitializing;
        private bool _isSaving;
        private bool _closeWhenIdle;
        private List<ProductUnitWriteDto> _productUnits = new();
        private List<UnitLookupItem> _unitLookupItems = new();
        private List<RaccoonWarehouse.Domain.Users.DTOs.UserReadDto> _supplierLookupItems = new();
        private bool _isFilteringSuppliers;
        private bool _isNavigatingSuppliers;
        private string _supplierSearchText = string.Empty;
        private int? _selectedSupplierId;
        private readonly Dictionary<ProductUnitWriteDto, FractionalSuggestionState> _fractionalSuggestionStates = new();
        private bool _isUpdatingFractionalSuggestions;
        public ObservableCollection<ProductMovementRow> ProductMovements { get; } = new();
        public ObservableCollection<ProductStockUnitRow> ProductStockByUnit { get; } = new();
        public ObservableCollection<ProductStockLotRow> ProductStockLots { get; } = new();
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
            IStockService stockService,
            IUserService userService)
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
            _userService = userService;
            DataContext = this;
            Closing += UpdateProduct_Closing;
        }

        private void UpdateProduct_Closing(object? sender, CancelEventArgs e)
        {
            if (!_isInitializing && !_isSaving)
                return;

            // WindowManager owns this window's DI scope and disposes it when
            // the window closes. Keep the window alive until all EF queries
            // using that scope have completed.
            e.Cancel = true;
            _closeWhenIdle = true;
        }

        private void CloseWhenIdleIfRequested()
        {
            if (!_closeWhenIdle || _isInitializing || _isSaving || !IsLoaded)
                return;

            _closeWhenIdle = false;
            Dispatcher.BeginInvoke(
                new Action(Close),
                System.Windows.Threading.DispatcherPriority.Background);
        }

        public async Task Initialize(int id)
        {
            _productId = id;
            _isInitializing = true;
            SetEditorEnabled(false);
            try
            {
                await LoadDataAsync();
            }
            finally
            {
                _isInitializing = false;
                SetEditorEnabled(true);
                CloseWhenIdleIfRequested();
            }
        }

        private void SetEditorEnabled(bool enabled)
        {
            if (UpdateButton != null)
                UpdateButton.IsEnabled = enabled && !_isSaving;
            if (AddUnitButton != null)
                AddUnitButton.IsEnabled = enabled && !_isSaving;
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

                var users = await _userService.GetAllAsync();
                _supplierLookupItems = users.Data?
                    .Where(user => user.Role == UserRole.Supplier)
                    .OrderBy(user => user.Name)
                    .ToList() ?? new List<RaccoonWarehouse.Domain.Users.DTOs.UserReadDto>();
                SupplierComboBox.ItemsSource = _supplierLookupItems.ToList();
                SupplierComboBox.DisplayMemberPath = "Name";
                SupplierComboBox.SelectedValuePath = "Id";

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
                MinimumQuantityTextBox.Text = product.Data.MiniQuantity?.ToString("0.000");
                BrandComboBox.SelectedValue = product.Data.BrandId;
                _selectedSupplierId = product.Data.SupplierId;
                SupplierComboBox.SelectedValue = product.Data.SupplierId;
                AgentNameTextBox.Text = product.Data.AgentName ?? string.Empty;
                SubCategoryComboBox.SelectedValue = product.Data.SubCategoryId;
                TaxRate.Text = product.Data.TaxRate.ToString();

                var units = await _productUnitService.GetAllWriteDtoWithFilteringAndIncludeAsync(pu => pu.ProductId == _productId, pu => pu.Unit);
                _productUnits = units.Data?.ToList() ?? new List<ProductUnitWriteDto>();
                NormalizeUnitFlags(_productUnits);
                await LoadUnitCostDisplaysAsync(product.Data.TaxExempt, product.Data.TaxRate);
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

        private void SupplierComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (SupplierComboBox.Template.FindName("PART_EditableTextBox", SupplierComboBox) is TextBox textBox)
                textBox.TextChanged += SupplierComboBox_TextChanged;
        }

        private void SupplierComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _isFilteringSuppliers || _isNavigatingSuppliers)
                return;

            if (SupplierComboBox.SelectedItem is RaccoonWarehouse.Domain.Users.DTOs.UserReadDto supplier && supplier.Id > 0)
                _selectedSupplierId = supplier.Id;
        }

        private void SupplierComboBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key is not (System.Windows.Input.Key.Down or System.Windows.Input.Key.Up or System.Windows.Input.Key.Enter) || SupplierComboBox.Items.Count == 0)
                return;

            _isNavigatingSuppliers = true;
            try
            {
                if (e.Key == System.Windows.Input.Key.Down)
                    SupplierComboBox.SelectedIndex = Math.Min(SupplierComboBox.SelectedIndex + 1, SupplierComboBox.Items.Count - 1);
                else if (e.Key == System.Windows.Input.Key.Up)
                    SupplierComboBox.SelectedIndex = Math.Max(SupplierComboBox.SelectedIndex - 1, 0);
                else if (SupplierComboBox.SelectedItem is RaccoonWarehouse.Domain.Users.DTOs.UserReadDto)
                {
                    _selectedSupplierId = ((RaccoonWarehouse.Domain.Users.DTOs.UserReadDto)SupplierComboBox.SelectedItem).Id;
                    SupplierComboBox.IsDropDownOpen = false;
                }
                else
                    return;

                RestoreSupplierSearchText();
                e.Handled = true;
            }
            finally
            {
                _isNavigatingSuppliers = false;
            }
        }

        private void RestoreSupplierSearchText()
        {
            if (SupplierComboBox.Template.FindName("PART_EditableTextBox", SupplierComboBox) is not TextBox textBox)
                return;

            textBox.Text = _supplierSearchText;
            textBox.CaretIndex = textBox.Text.Length;
            SupplierComboBox.IsDropDownOpen = true;
        }

        private void SupplierComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFilteringSuppliers || _isNavigatingSuppliers || !SupplierComboBox.IsKeyboardFocusWithin)
                return;

            if (SupplierComboBox.Template.FindName("PART_EditableTextBox", SupplierComboBox) is not TextBox textBox)
                return;

            _supplierSearchText = textBox.Text.Trim();
            _isFilteringSuppliers = true;
            try
            {
                SupplierComboBox.SelectedValue = null;
                SupplierComboBox.ItemsSource = string.IsNullOrWhiteSpace(_supplierSearchText)
                    ? _supplierLookupItems.ToList()
                    : _supplierLookupItems.Where(user =>
                            (user.Name ?? string.Empty).Contains(_supplierSearchText, StringComparison.CurrentCultureIgnoreCase)
                            || (user.PhoneNumber ?? string.Empty).Contains(_supplierSearchText, StringComparison.CurrentCultureIgnoreCase))
                        .ToList();
                SupplierComboBox.IsDropDownOpen = true;
            }
            finally
            {
                _isFilteringSuppliers = false;
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
            ProductStockLots.Clear();

            var lotResult = await _stockService.GetBatchLookupAsync(_productId);
            var activeLots = (lotResult.Data ?? new List<StockBatchLookupDto>())
                .Where(lot => lot.Status == BatchStatus.Active && lot.RemainingQuantity > 0)
                .OrderBy(lot => lot.ExpiryDate.HasValue ? 0 : 1)
                .ThenBy(lot => lot.ExpiryDate)
                .ThenBy(lot => lot.CreatedDate)
                .ToList();

            foreach (var lot in activeLots)
            {
                ProductStockLots.Add(new ProductStockLotRow
                {
                    StockLotId = lot.StockLotId,
                    UnitName = lot.UnitName,
                    Quantity = lot.RemainingQuantity,
                    SalePrice = lot.SalePrice,
                    ExpiryDate = lot.ExpiryDate
                });
            }

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

            var nearestExpiry = activeLots
                .Where(lot => lot.ExpiryDate.HasValue)
                .Select(lot => lot.ExpiryDate!.Value)
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

            var availableQuantity = CurrentStockTotalBaseQuantity;
            var movementIndex = 0;
            foreach (var movement in (result.Data ?? new List<StockTransactionReadDto>())
                .OrderByDescending(m => m.TransactionDate)
                .ThenByDescending(m => m.Id))
            {
                var availableAfterMovement = availableQuantity;
                availableQuantity -= movement.BaseQuantity != 0
                    ? movement.BaseQuantity
                    : movement.Quantity * (movement.QuantityPerUnitSnapshot > 0 ? movement.QuantityPerUnitSnapshot : 1m);

                if (movementIndex++ >= 200)
                    continue;

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
                    Notes = movement.Notes ?? "-",
                    AvailableQuantityAfterMovement = availableAfterMovement
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
            UnitCostSummaryStackPanel.Children.Clear();
            UnitsStackPanel.Children.Clear();
            AddProductCostSummary();
            foreach (var unit in _productUnits)
                AddUnitRow(unit);

            UiText.ApplyTranslations(UnitCostSummaryStackPanel);
            UiText.ApplyTranslations(UnitsStackPanel);
        }

        private void AddProductCostSummary()
        {
            var costUnit = _productUnits.FirstOrDefault(unit => unit.IsDefaultPurchaseUnit)
                ?? _productUnits.FirstOrDefault(unit => unit.IsBaseUnit)
                ?? _productUnits.FirstOrDefault();
            var saleUnit = _productUnits.FirstOrDefault(unit => unit.IsDefaultSaleUnit)
                ?? costUnit;

            if (costUnit == null)
                return;

            var row = new WrapPanel
            {
                Margin = new Thickness(0, 0, 0, 8),
                FlowDirection = UiText.CurrentFlowDirection
            };

            row.Children.Add(CreateReadOnlyUnitPanel(UiText.T("آخر تكلفة بدون ضريبة", "Last cost without tax"), costUnit.LastCostWithoutTax.ToString("0.000"), 160));
            row.Children.Add(CreateReadOnlyUnitPanel(UiText.T("متوسط التكلفة بدون ضريبة", "Average cost without tax"), costUnit.AverageCostWithoutTax.ToString("0.000"), 170));
            row.Children.Add(CreateReadOnlyUnitPanel(UiText.T("متوسط التكلفة شامل الضريبة", "Average cost incl. tax"), costUnit.AverageCostIncludingTax.ToString("0.000"), 170));
            row.Children.Add(CreateReadOnlyUnitPanel(UiText.T("سعر البيع الحالي", "Current sale price"), (saleUnit?.SalePrice ?? 0m).ToString("0.000"), 150));

            UnitCostSummaryStackPanel.Children.Add(row);
        }

        private async Task LoadUnitCostDisplaysAsync(bool? taxExempt, decimal? taxRate)
        {
            var batchesResult = await _stockService.GetBatchLookupAsync(_productId);
            var latestCostByUnit = (batchesResult.Data ?? new List<StockBatchLookupDto>())
                .Where(batch => batch.PurchasePrice > 0)
                .GroupBy(batch => batch.ProductUnitId)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderByDescending(batch => batch.CreatedDate).First().PurchasePrice);

            foreach (var unit in _productUnits)
            {
                unit.AverageCostWithoutTax = Math.Round(Math.Max(unit.PurchasePrice, 0m), 3);
                unit.CostWithoutTax = unit.AverageCostWithoutTax;
                unit.AverageCostIncludingTax = ToTaxInclusiveCost(unit.PurchasePrice, taxExempt, taxRate);
                unit.LastCostIncludingTax = latestCostByUnit.TryGetValue(unit.Id, out var lastCost)
                    ? ToTaxInclusiveCost(lastCost, taxExempt, taxRate)
                    : 0m;
                unit.LastCostWithoutTax = latestCostByUnit.TryGetValue(unit.Id, out lastCost)
                    ? Math.Round(Math.Max(lastCost, 0m), 3)
                    : 0m;
            }
        }

        private static decimal ToTaxInclusiveCost(decimal cost, bool? taxExempt, decimal? taxRate)
        {
            if (cost <= 0 || taxExempt == true)
                return Math.Round(Math.Max(cost, 0m), 3);

            return Math.Round(Math.Max(cost, 0m) * (1m + taxRate.GetValueOrDefault() / 100m), 3);
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
            var salePanel = CreateEditableDecimalPanel(UiText.T("سعر البيع", "Sale Price"), unit.SalePrice.ToString(), 100);
            var purchasePanel = CreateEditableDecimalPanel(UiText.T("سعر الشراء", "Purchase Price"), unit.PurchasePrice.ToString(), 100);
            var fractionalCheckPanel = CreateCheckPanel(UiText.T("بيع جزئي", "Fractional sale"), unit.AllowFractionalSale);
            var partsPanel = CreateEditableDecimalPanel(UiText.T("الأجزاء لكل عبوة", "Parts per package"), unit.FractionalPartsPerUnit > 0 ? unit.FractionalPartsPerUnit.ToString("0") : string.Empty, 110);
            var fractionalPricePanel = CreateEditableDecimalPanel(UiText.T("سعر البيع الجزئي", "Partial Sale Price"), unit.FractionalSalePrice?.ToString() ?? string.Empty, 125);
            var fractionalCostPanel = CreateEditableDecimalPanel(UiText.T("تكلفة البيع الجزئي", "Partial Sale Cost"), unit.FractionalSaleCost?.ToString() ?? string.Empty, 125);
            row.Children.Add(salePanel);
            row.Children.Add(purchasePanel);
            row.Children.Add(CreateEditableDecimalPanel(UiText.T("الكمية لكل وحدة", "Quantity per Unit"), unit.QuantityPerUnit.ToString(), 110));
            row.Children.Add(fractionalCheckPanel);
            row.Children.Add(partsPanel);
            row.Children.Add(fractionalPricePanel);
            row.Children.Add(fractionalCostPanel);
            row.Children.Add(CreateEditableTextPanel(UiText.T("اسم الجزء", "Fraction name"), unit.FractionalPartName ?? string.Empty, 130));
            row.Children.Add(CreateEditableTextPanel(UiText.T("الرمز المماثل", "Alternate barcode"), unit.AlternateBarcode ?? string.Empty, 150));
            row.Children.Add(CreateCheckPanel(UiText.T("أساسية", "Primary"), unit.IsBaseUnit));
            row.Children.Add(CreateCheckPanel(UiText.T("بيع", "Sale"), unit.IsDefaultSaleUnit));
            row.Children.Add(CreateCheckPanel(UiText.T("شراء", "Purchase"), unit.IsDefaultPurchaseUnit));

            AttachFractionalSuggestionEvents(unit, salePanel, purchasePanel, fractionalCheckPanel, partsPanel, fractionalPricePanel, fractionalCostPanel);

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

                ProductUnitService.NotifyUnitsChanged(_productId);
                _productUnits.Remove(unit);
                NormalizeUnitFlags(_productUnits);
                RebuildUnitsPanel();
            };

            row.Children.Add(deleteBtn);
            UnitsStackPanel.Children.Add(row);
        }

        private sealed class FractionalSuggestionState
        {
            public decimal? AutoPrice { get; set; }
            public decimal? AutoCost { get; set; }
        }

        private void AttachFractionalSuggestionEvents(ProductUnitWriteDto unit, StackPanel salePanel, StackPanel purchasePanel, StackPanel checkPanel, StackPanel partsPanel, StackPanel pricePanel, StackPanel costPanel)
        {
            var saleBox = (TextBox)salePanel.Children[1];
            var purchaseBox = (TextBox)purchasePanel.Children[1];
            var fractionalCheck = (CheckBox)checkPanel.Children[1];
            var partsBox = (TextBox)partsPanel.Children[1];
            var priceBox = (TextBox)pricePanel.Children[1];
            var costBox = (TextBox)costPanel.Children[1];

            saleBox.TextChanged += (_, _) => SuggestFractionalPrices(unit, saleBox, purchaseBox, fractionalCheck, partsBox, priceBox, costBox);
            purchaseBox.TextChanged += (_, _) => SuggestFractionalPrices(unit, saleBox, purchaseBox, fractionalCheck, partsBox, priceBox, costBox);
            partsBox.TextChanged += (_, _) => SuggestFractionalPrices(unit, saleBox, purchaseBox, fractionalCheck, partsBox, priceBox, costBox);
            fractionalCheck.Checked += (_, _) => SuggestFractionalPrices(unit, saleBox, purchaseBox, fractionalCheck, partsBox, priceBox, costBox);
            fractionalCheck.Unchecked += (_, _) => SuggestFractionalPrices(unit, saleBox, purchaseBox, fractionalCheck, partsBox, priceBox, costBox);
            priceBox.TextChanged += (_, _) => MarkManualFractionalValue(unit, priceBox, true);
            costBox.TextChanged += (_, _) => MarkManualFractionalValue(unit, costBox, false);
        }

        private void MarkManualFractionalValue(ProductUnitWriteDto unit, TextBox box, bool price)
        {
            if (_isUpdatingFractionalSuggestions || !_fractionalSuggestionStates.TryGetValue(unit, out var state))
                return;

            var isAutoValue = decimal.TryParse(box.Text, out var value) && (price ? state.AutoPrice == value : state.AutoCost == value);
            if (!isAutoValue)
            {
                if (price) state.AutoPrice = null;
                else state.AutoCost = null;
            }
        }

        private void SuggestFractionalPrices(ProductUnitWriteDto unit, TextBox saleBox, TextBox purchaseBox, CheckBox fractionalCheck, TextBox partsBox, TextBox priceBox, TextBox costBox)
        {
            if (_isUpdatingFractionalSuggestions || fractionalCheck.IsChecked != true ||
                !decimal.TryParse(saleBox.Text, out var salePrice) || salePrice <= 0m ||
                !decimal.TryParse(purchaseBox.Text, out var purchasePrice) || purchasePrice < 0m ||
                !decimal.TryParse(partsBox.Text, out var parts) || parts < 2m || parts != decimal.Truncate(parts))
                return;

            if (!_fractionalSuggestionStates.TryGetValue(unit, out var state))
            {
                state = new FractionalSuggestionState();
                _fractionalSuggestionStates[unit] = state;
            }

            var suggestedPrice = Math.Round(salePrice / parts, 3, MidpointRounding.AwayFromZero);
            var suggestedCost = Math.Round(purchasePrice / parts, 3, MidpointRounding.AwayFromZero);
            _isUpdatingFractionalSuggestions = true;
            try
            {
                if (string.IsNullOrWhiteSpace(priceBox.Text) || (state.AutoPrice.HasValue && decimal.TryParse(priceBox.Text, out var currentPrice) && currentPrice == state.AutoPrice.Value))
                {
                    priceBox.Text = suggestedPrice.ToString("0.###");
                    state.AutoPrice = suggestedPrice;
                }

                if (string.IsNullOrWhiteSpace(costBox.Text) || (state.AutoCost.HasValue && decimal.TryParse(costBox.Text, out var currentCost) && currentCost == state.AutoCost.Value))
                {
                    costBox.Text = suggestedCost.ToString("0.###");
                    state.AutoCost = suggestedCost;
                }
            }
            finally
            {
                _isUpdatingFractionalSuggestions = false;
            }
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
            if (_isInitializing || _isSaving)
                return;

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
                AllowFractionalSale = AllowFractionalSaleCheckBox.IsChecked == true,
                FractionalPartsPerUnit = 0m,
                FractionalPartName = null,
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
            if (_isInitializing || _isSaving)
                return;

            _isSaving = true;
            SetEditorEnabled(false);
            try
            {
                var selectedSupplierId = _selectedSupplierId ?? (SupplierComboBox.SelectedItem
                    is RaccoonWarehouse.Domain.Users.DTOs.UserReadDto selectedSupplier && selectedSupplier.Id > 0
                    ? selectedSupplier.Id
                    : TryGetSelectedSupplierId());

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
                    SupplierId = selectedSupplierId,
                    AgentName = AgentNameTextBox.Text.Trim(),
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
                    // Invalidate POS metadata instead of publishing the read-back
                    // object from this window's tracked DbContext. POS refreshes
                    // the units through an independent scope and receives the
                    // committed AllowFractionalSale/parts/price values.
                    ProductUnitService.NotifyUnitsChanged(_productId);
                    RebuildUnitsPanel();
                    await LoadCurrentStockSummaryAsync();
                    CatalogRefreshNotifier.NotifyCatalogChanged();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("خطأ", "Error")}: {ex.Message}");
            }
            finally
            {
                _isSaving = false;
                SetEditorEnabled(!_isInitializing);
                CloseWhenIdleIfRequested();
            }
        }

        private int? TryGetSelectedSupplierId()
        {
            if (SupplierComboBox.SelectedValue == null)
                return null;

            return int.TryParse(SupplierComboBox.SelectedValue.ToString(), out var supplierId) && supplierId > 0
                ? supplierId
                : null;
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
                var fractionalCheck = ((row.Children[4] as StackPanel)?.Children[1] as CheckBox);
                var fractionalPartsBox = ((row.Children[5] as StackPanel)?.Children[1] as TextBox);
                var fractionalPriceBox = ((row.Children[6] as StackPanel)?.Children[1] as TextBox);
                var fractionalCostBox = ((row.Children[7] as StackPanel)?.Children[1] as TextBox);
                var fractionalNameBox = ((row.Children[8] as StackPanel)?.Children[1] as TextBox);
                var alternateBarcodeBox = ((row.Children[9] as StackPanel)?.Children[1] as TextBox);
                var baseCheck = ((row.Children[10] as StackPanel)?.Children[1] as CheckBox);
                var saleCheck = ((row.Children[11] as StackPanel)?.Children[1] as CheckBox);
                var purchaseCheck = ((row.Children[12] as StackPanel)?.Children[1] as CheckBox);

                if (saleBox == null || purchaseBox == null || qtyBox == null ||
                    fractionalCheck == null || fractionalPartsBox == null || fractionalNameBox == null ||
                    fractionalPriceBox == null || fractionalCostBox == null || alternateBarcodeBox == null || baseCheck == null ||
                    saleCheck == null || purchaseCheck == null)
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
                unit.AverageCostWithoutTax = Math.Round(Math.Max(purchase, 0m), 3);
                unit.CostWithoutTax = Math.Round(Math.Max(purchase, 0m), 3);
                unit.QuantityPerUnit = qty;
                unit.AllowFractionalSale = fractionalCheck.IsChecked == true;
                var parsedParts = 0m;
                if (unit.AllowFractionalSale &&
                    (!decimal.TryParse(fractionalPartsBox.Text, out parsedParts) ||
                     parsedParts < 2m || parsedParts != decimal.Truncate(parsedParts)))
                {
                    fractionalPartsBox.Focus();
                    fractionalPartsBox.SelectAll();
                    throw new Exception(UiText.T(
                        $"عدد الأجزاء يجب أن يكون رقماً صحيحاً أكبر من 1. القيمة الحالية: {fractionalPartsBox.Text.Trim()}. مثال: 2 أو 10.",
                        $"Parts per package must be a whole number greater than 1. Current value: {fractionalPartsBox.Text.Trim()}. Example: 2 or 10."));

                }
                unit.FractionalPartsPerUnit = unit.AllowFractionalSale ? parsedParts : 0m;
                unit.FractionalSalePrice = ParseOptionalFractionalValue(fractionalPriceBox.Text, "Partial Sale Price", greaterThanZero: true);
                unit.FractionalSaleCost = ParseOptionalFractionalValue(fractionalCostBox.Text, "Partial Sale Cost", greaterThanZero: false);
                if (!unit.AllowFractionalSale)
                {
                    unit.FractionalSalePrice = null;
                    unit.FractionalSaleCost = null;
                }
                unit.FractionalPartName = unit.AllowFractionalSale && !string.IsNullOrWhiteSpace(fractionalNameBox.Text)
                    ? fractionalNameBox.Text.Trim()
                    : null;
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

        private static decimal? ParseOptionalFractionalValue(string text, string fieldName, bool greaterThanZero)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            if (!decimal.TryParse(text, out var value) ||
                (greaterThanZero ? value <= 0m : value < 0m))
            {
                var arabicRequirement = greaterThanZero ? "أكبر من صفر" : "أكبر من أو يساوي صفر";
                var englishRequirement = greaterThanZero ? "greater than zero" : "zero or greater";
                throw new Exception(UiText.T(
                    $"{fieldName} يجب أن يكون رقماً و{arabicRequirement}.",
                    $"{fieldName} must be numeric and {englishRequirement}."));
            }

            return Math.Round(value, 3);
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
            AllowFractionalSaleCheckBox.IsChecked = false;
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
            public decimal AvailableQuantityAfterMovement { get; set; }
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

        public sealed class ProductStockLotRow
        {
            public int StockLotId { get; set; }
            public string UnitName { get; set; } = "-";
            public decimal Quantity { get; set; }
            public decimal SalePrice { get; set; }
            public DateTime? ExpiryDate { get; set; }
        }
    }
}

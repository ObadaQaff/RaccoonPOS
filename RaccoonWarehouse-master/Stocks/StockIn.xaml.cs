using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.ProductUnits;
using RaccoonWarehouse.Application.Service.StockDocuments;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.StockTransactions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Application.Service.Warehouses;
using RaccoonWarehouse.Common;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Checks.DTOs;
using RaccoonWarehouse.Invoices;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Products;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using RaccoonWarehouse.Domain.Stock.DTOs;
using RaccoonWarehouse.Domain.StockDocuments.DTOs;
using RaccoonWarehouse.Domain.StockItems.DTOs;
using RaccoonWarehouse.Domain.StockTransactions.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Helpers.Pdf;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;


namespace RaccoonWarehouse.Stocks
{
    public partial class StockIn : Window
    {
        private Dictionary<StockItemWriteDto, int> _itemUnits = new();
        private readonly IProductService _productService;
        private readonly IProductUnitService _productUnitService;
        private readonly IStockDocumentService _stockDocumentService;
        private readonly IUserService _userService;
        private readonly IUserSession _userSession;
        private readonly IWarehouseService _warehouseService;
        private readonly IFalconStockImportService _falconStockImportService;
        private readonly ILoadingService _loadingService;
        private bool _isLoadingUnits = false;
        private int? _currentDocumentId = null;
        private List<StockItemReadDto> _originalItems = new(); // Used for stock adjustment
        private List<CheckWriteDto> _currentChecks = new();


        public ObservableCollection<ProductUnitWriteDto> GetUnitsForProduct(int productId)
        {
            if (_productUnitsMap.ContainsKey(productId))
                return _productUnitsMap[productId];
            return new ObservableCollection<ProductUnitWriteDto>();
        }


        private Dictionary<int, ObservableCollection<ProductUnitWriteDto>> _productUnitsMap = new();

        public ObservableCollection<StockItemWriteDto> Items { get; set; } = new();
        public ObservableCollection<ProductReadDto> Products { get; set; } = new();
        public ObservableCollection<ProductUnitWriteDto> Units { get; set; } = new();
        private readonly IStockService _stockService;
        private readonly IStockTransactionService _stockTransactionService;
        public StockIn(
            IUserService userService,
            IUserSession userSession,
            IProductService productService,
            IProductUnitService productUnitService,
            IStockDocumentService stockDocumentService,
            IStockService stockService,
            IStockTransactionService stockTransactionService,
            IWarehouseService warehouseService,
            IFalconStockImportService falconStockImportService,
            ILoadingService loadingService)
        {
            _userService = userService;
            _userSession = userSession;
            _stockService = stockService;
            _stockTransactionService = stockTransactionService;
            _productService = productService;
            _productUnitService = productUnitService;
            _stockDocumentService = stockDocumentService;
            _warehouseService = warehouseService;
            _falconStockImportService = falconStockImportService;
            _loadingService = loadingService;

            InitializeComponent();
            UiText.ApplyWindow(this);
            DataContext = this;
            ProductsGrid.ItemsSource = Items;

            this.Loaded += StockIn_Loaded;
            Closed += StockIn_Closed;
            CatalogRefreshNotifier.CatalogChanged += CatalogRefreshNotifier_CatalogChanged;
        }
        #region Page Load 
        private async void StockIn_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private void StockIn_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1)
            {
                SaveStockInBtn_Click(SaveStockInBtn, new RoutedEventArgs());
                e.Handled = true;
                return;
            }

            if (e.Key != Key.F2)
                return;

            SearchProductBtn_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }


        private async Task LoadDataAsync()
        {
            var loadingShown = false;

            void HideLoadingIfShown()
            {
                if (!loadingShown)
                    return;

                _loadingService.Hide();
                loadingShown = false;
            }

            try
            {
                _loadingService.Show();
                loadingShown = true;

                VoucherNumberTxt.Text = GenerateDocumentNumber();
                FalconInvoiceNumberTextBox.Clear();
                DatePickerInvoice.SelectedDate = DateTime.Now;
                var warehouses = await _warehouseService.GetAllAsync();
                WarehouseComboBox.ItemsSource = warehouses.Data;
                WarehouseComboBox.DisplayMemberPath = "Name";
                WarehouseComboBox.SelectedValuePath = "Id";

                var users = await _userService.GetAllAsync();
                SupplierComboBox.ItemsSource = users.Data?
                    .OrderBy(user => user.Name)
                    .ToList();
                SupplierComboBox.DisplayMemberPath = "Name";
                SupplierComboBox.SelectedValuePath = "Id";
                PaymentTypeComboBox.SelectedIndex = 0;

                var result = await _productService.GetReadDtoPagedListAsync(
                    pageNumber: 1,
                    pageSize: 3000,
                    orderBy: q => q.OrderBy(p => p.Name),
                    includes: new Expression<Func<Product, object>>[]
                    {
                        p => p.ProductUnits,
                        p => p.Brand,
                        p => p.SubCategory
                    });

                if (result?.Items != null)
                {
                    _productUnitsMap.Clear();
                    Products.Clear();  // 🔥 Must use Clear() + Add()

                    foreach (var p in result.Items)
                        Products.Add(p);
                }
            }
            catch (Exception ex)
            {
                HideLoadingIfShown();
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء تحميل البيانات", "An error occurred while loading data")}: {ex.Message}",
                    UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                HideLoadingIfShown();
            }
        }

        private async void CatalogRefreshNotifier_CatalogChanged(object? sender, EventArgs e)
        {
            if (!IsLoaded)
                return;

            await LoadDataAsync();
        }

        private void StockIn_Closed(object? sender, EventArgs e)
        {
            CatalogRefreshNotifier.CatalogChanged -= CatalogRefreshNotifier_CatalogChanged;
        }

        #endregion

        private void AddProductBtn_Click(object sender, RoutedEventArgs e)
        {
            Items.Add(new StockItemWriteDto
            {

                Quantity = 0,
                PurchasePrice = 0,
                SalePrice = 0,
                LineDiscountAmount = 0,
                FreeQuantity = 0,
                ExpiryDate = DateTime.Now.AddMonths(6),
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            });
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is StockItemWriteDto item)
            {

                Items.Remove(item);
                if (_itemUnits.ContainsKey(item))
                {
                    _itemUnits.Remove(item); // Automatically removes the mapping
                }
                UpdateStockTotals();
            }
           
        }

        private async void SaveStockInBtn_Click(object sender, RoutedEventArgs e)
        {
            var loadingShown = false;

            void HideLoadingIfShown()
            {
                if (!loadingShown)
                    return;

                _loadingService.Hide();
                loadingShown = false;
            }

            try
            {   
                if (Items.Count == 0)
                {
                    MessageBox.Show(UiText.T("يرجى إضافة منتج واحد على الأقل.", "Please add at least one product."), UiText.T("تنبيه", "Notice"),
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (MessageBox.Show(
                        UiText.T("هل تريد حفظ سند إدخال البضاعة؟", "Do you want to save the stock-in document?"),
                        UiText.T("تأكيد الحفظ", "Confirm save"),
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

                _loadingService.Show();
                loadingShown = true;

                // Validate Units
                foreach (var item in Items)
                {
                    if (!_itemUnits.TryGetValue(item, out var unitId) || unitId <= 0)
                    {
                        HideLoadingIfShown();
                        MessageBox.Show(UiText.T($"الوحدة غير صحيحة للمنتج {item.ProductName ?? "غير معروف"}.", $"The unit is invalid for product {item.ProductName ?? "Unknown"}."), UiText.T("تنبيه", "Notice"));
                        return;
                    }

                    item.ProductUnitId = unitId;
                    await NormalizeStockItemAsync(item);

                    if (item.Quantity <= 0)
                    {
                        HideLoadingIfShown();
                        MessageBox.Show(UiText.T($"الكمية غير صحيحة للمنتج {item.ProductName ?? "غير معروف"}.", $"The quantity is invalid for product {item.ProductName ?? "Unknown"}."), UiText.T("تنبيه", "Notice"));
                        return;
                    }

                    if (item.LineDiscountAmount < 0 || item.LineDiscountAmount > item.Quantity * item.PurchasePrice)
                    {
                        HideLoadingIfShown();
                        MessageBox.Show(UiText.T("خصم السطر غير صحيح.", "The row discount is invalid."), UiText.T("تنبيه", "Notice"));
                        return;
                    }

                    if (item.FreeQuantity < 0)
                    {
                        HideLoadingIfShown();
                        MessageBox.Show(UiText.T("الكمية المجانية لا يمكن أن تكون سالبة.", "Free quantity cannot be negative."), UiText.T("تنبيه", "Notice"));
                        return;
                    }

                    if (item.PurchasePrice <= 0)
                    {
                        HideLoadingIfShown();
                        MessageBox.Show(UiText.T($"سعر الشراء يجب أن يكون أكبر من صفر للمنتج {item.ProductName ?? "غير معروف"}.", $"The purchase price must be greater than zero for product {item.ProductName ?? "Unknown"}."), UiText.T("تنبيه", "Notice"));
                        return;
                    }

                    if (item.SalePrice <= 0)
                    {
                        HideLoadingIfShown();
                        MessageBox.Show(UiText.T($"سعر البيع يجب أن يكون أكبر من صفر للمنتج {item.ProductName ?? "غير معروف"}.", $"The sale price must be greater than zero for product {item.ProductName ?? "Unknown"}."), UiText.T("تنبيه", "Notice"));
                        return;
                    }

                    if (!item.ExpiryDate.HasValue)
                    {
                        HideLoadingIfShown();
                        MessageBox.Show(UiText.T($"تاريخ الانتهاء مطلوب للمنتج {item.ProductName ?? "غير معروف"}.", $"The expiry date is required for product {item.ProductName ?? "Unknown"}."), UiText.T("تنبيه", "Notice"));
                        return;
                    }
                }

                bool isUpdate = _currentDocumentId != null;

                var paymentType = GetSelectedPaymentType();
                var stockTotal = Math.Round(Items.Sum(GetPaidLineTotal) - GetDiscountAmount(), 3);
                var selectedUserId = SupplierComboBox.SelectedValue is int userId ? userId : 0;
                if (paymentType == PaymentType.Credit && selectedUserId <= 0)
                {
                    HideLoadingIfShown();
                    MessageBox.Show(UiText.T("يرجى اختيار الحساب للشراء الآجل.", "Please select an account for credit stock-in."), UiText.T("تنبيه", "Notice"));
                    return;
                }

                if (paymentType == PaymentType.Check)
                {
                    if (_currentChecks.Count == 0 || Math.Round(_currentChecks.Sum(check => check.Amount), 3) != stockTotal)
                    {
                        HideLoadingIfShown();
                        MessageBox.Show(UiText.T("يجب إضافة شيكات يساوي مجموعها إجمالي السند.", "Add checks whose total equals the document total."), UiText.T("تنبيه", "Notice"));
                        return;
                    }
                }

                if (isUpdate)
                {
                    HideLoadingIfShown();
                    MessageBox.Show(
                        UiText.T("لا يمكن تعديل سند إدخال مخزون بعد حفظه مباشرة لأن ذلك قد يغيّر التاريخ المحاسبي وحركات الدُفعات المستخدمة. استخدم شاشة تسوية/تصحيح المخزون الجديدة لإجراء أي تعديل آمن.", "A stock-in document cannot be edited after saving because that may change the accounting date and used batch movements. Use the stock adjustment window for safe changes."),
                        UiText.T("تعديل محظور", "Edit blocked"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // ============= CREATE DTO =============
                var documentDto = new StockDocumentWriteDto
                {
                    Id = _currentDocumentId ?? 0,
                    DocumentNumber = VoucherNumberTxt.Text,
                    FalconInvoiceNumber = FalconInvoiceNumberTextBox.Text.Trim(),
                    Type = StockVoucherType.In,
                    SupplierId = SupplierComboBox.SelectedValue is int supplierId && supplierId > 0 ? supplierId : null,
                    PaymentType = paymentType,
                    Checks = _currentChecks.ToList(),
                    WarehouseId = WarehouseComboBox.SelectedValue != null ? (int)WarehouseComboBox.SelectedValue : null,
                    Notes = NotesTxt.Text,
                    DiscountAmount = GetDiscountAmount(),
                    Items = Items.ToList(),
                    CreatedDate = isUpdate ? _originalItems.FirstOrDefault()?.CreatedDate ?? DateTime.Now : DateTime.Now,
                    UpdatedDate = DateTime.Now
                };

                if (!isUpdate)
                {
                    // ============= CREATE =============
                    var result = await _stockDocumentService.CreateAsync(documentDto);

                    if (!result.Success)
                    {
                        HideLoadingIfShown();
                        MessageBox.Show(result.Message ?? UiText.T("تعذر حفظ سند الإدخال.", "The stock-in document could not be saved."), UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var movementResult = await _stockService.PostMovementsAsync(
                        BuildStockMovements(Items, TransactionType.Purchase, $"Stock in document #{documentDto.DocumentNumber}"));

                    if (!movementResult.Success)
                    {
                        HideLoadingIfShown();
                        MessageBox.Show(movementResult.Message ?? UiText.T("فشل تحديث المخزون.", "Failed to update stock."), UiText.T("خطأ", "Error"));
                        return;
                    }

                    _currentDocumentId = result.Data?.Id;

                    HideLoadingIfShown();
                    MessageBox.Show(UiText.T("تم إنشاء السند بنجاح.", "The document was created successfully."), UiText.T("نجاح", "Success"),
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    ClearForm();
                    PrintBtn.Visibility = Visibility.Collapsed;
                    NewStockInBtn.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                HideLoadingIfShown();
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء الحفظ", "An error occurred while saving")}: {ex.Message}",
                    UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                HideLoadingIfShown();
            }
        }

        private void ClearForm()
        {
            _currentDocumentId = null;
            _originalItems.Clear();
            VoucherNumberTxt.Text = GenerateDocumentNumber();
            DatePickerInvoice.SelectedDate = DateTime.Now;
            WarehouseComboBox.SelectedIndex = -1;
            SupplierComboBox.SelectedIndex = -1;
            SetSelectedPaymentType(PaymentType.Cash);
            _currentChecks.Clear();
            UpdateChecksButtonVisibility();

            NotesTxt.Text = "";
            DiscountTextBox.Text = "0";
            Items.Clear();
            _itemUnits.Clear();

            ProductBox.SelectedIndex = -1;
            UnitBox.ItemsSource = null;
            QtyBox.Text = "";
            PurchaseBox.Text = "";
            SaleBox.Text = "";
            ExpiryBox.SelectedDate = null;

            ProductsGrid.Items.Refresh();
            UpdateStockTotals();
        }

        private decimal GetDiscountAmount()
        {
            var subtotal = Items.Sum(GetPaidLineTotal);
            decimal.TryParse(DiscountTextBox.Text, out var discount);
            return Math.Clamp(discount, 0m, subtotal);
        }

        private static decimal GetPaidLineTotal(StockItemWriteDto item)
        {
            return Math.Max(0m, item.Quantity * item.PurchasePrice - item.LineDiscountAmount);
        }

        private void UpdateStockTotals()
        {
            var subtotal = Items.Sum(GetPaidLineTotal);
            var discount = GetDiscountAmount();
            SubtotalAmountTextBox.Text = subtotal.ToString("0.00000");
            DiscountTextBox.Text = discount.ToString("0.00000");
            TotalAmountTextBox.Text = (subtotal - discount).ToString("0.00000");
        }

        private void DiscountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded)
                UpdateStockTotals();
        }

        private void StockLineValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded)
                UpdateStockTotals();
        }

        private void StockLineValue_LostFocus(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
                ProductsGrid.Items.Refresh();
        }
        private string GenerateDocumentNumber()
        {
            return (DateTime.Now.Ticks % 90000 + 10000).ToString();
        }

        private async Task NormalizeStockItemAsync(StockItemWriteDto item)
        {
            if (item.ProductUnitId <= 0)
                return;

            var unitResult = await _productUnitService.GetWriteDtoByIdAsync(item.ProductUnitId);
            var quantityPerUnit = unitResult.Data?.QuantityPerUnit > 0
                ? unitResult.Data.QuantityPerUnit
                : 1m;

            item.QuantityPerUnitSnapshot = quantityPerUnit;
            item.BaseQuantity = item.Quantity * quantityPerUnit;
        }

        private IEnumerable<StockMovementPostDto> BuildStockMovements(
            IEnumerable<StockItemWriteDto> items,
            TransactionType transactionType,
            string notes,
            bool reverseSign = false)
        {
            foreach (var item in items)
            {
                var quantity = reverseSign ? -item.Quantity : item.Quantity;
                var baseQuantity = reverseSign ? -item.BaseQuantity : item.BaseQuantity;

                yield return new StockMovementPostDto
                {
                    ProductId = item.ProductId,
                    ProductUnitId = item.ProductUnitId,
                    Quantity = quantity,
                    QuantityPerUnitSnapshot = item.QuantityPerUnitSnapshot > 0 ? item.QuantityPerUnitSnapshot : 1m,
                    BaseQuantity = baseQuantity,
                    UnitPrice = item.PurchasePrice,
                    PurchasePrice = item.PurchasePrice,
                    SalePrice = item.SalePrice,
                    ExpiryDate = item.ExpiryDate,
                    TransactionType = transactionType,
                    UpdateCatalogAverageCost = true,
                    TransactionDate = DateTime.Now,
                    Notes = notes + (item.LineDiscountAmount > 0 ? $" | Row discount: {item.LineDiscountAmount:0.###}" : string.Empty)
                };

                if (item.FreeQuantity > 0)
                {
                    var freeBaseQuantity = item.FreeQuantity * (item.QuantityPerUnitSnapshot > 0 ? item.QuantityPerUnitSnapshot : 1m);
                    yield return new StockMovementPostDto
                    {
                        ProductId = item.ProductId,
                        ProductUnitId = item.ProductUnitId,
                        Quantity = reverseSign ? -item.FreeQuantity : item.FreeQuantity,
                        QuantityPerUnitSnapshot = item.QuantityPerUnitSnapshot > 0 ? item.QuantityPerUnitSnapshot : 1m,
                        BaseQuantity = reverseSign ? -freeBaseQuantity : freeBaseQuantity,
                        UnitPrice = 0,
                        PurchasePrice = 0,
                        SalePrice = item.SalePrice,
                        ExpiryDate = item.ExpiryDate,
                        TransactionType = transactionType,
                        UpdateCatalogAverageCost = false,
                        TransactionDate = DateTime.Now,
                        Notes = notes + " | Free quantity"
                    };
                }
            }
        }

        private IEnumerable<StockMovementPostDto> BuildStockMovements(
            IEnumerable<StockItemReadDto> items,
            TransactionType transactionType,
            string notes,
            bool reverseSign = false)
        {
            foreach (var item in items)
            {
                var quantityPerUnit = item.QuantityPerUnitSnapshot > 0 ? item.QuantityPerUnitSnapshot : 1m;
                var baseQuantity = item.BaseQuantity != 0 ? item.BaseQuantity : item.Quantity * quantityPerUnit;
                var quantity = reverseSign ? -item.Quantity : item.Quantity;

                yield return new StockMovementPostDto
                {
                    ProductId = item.ProductId,
                    ProductUnitId = item.ProductUnitId,
                    Quantity = quantity,
                    QuantityPerUnitSnapshot = quantityPerUnit,
                    BaseQuantity = reverseSign ? -baseQuantity : baseQuantity,
                    UnitPrice = item.PurchasePrice,
                    PurchasePrice = item.PurchasePrice,
                    SalePrice = item.SalePrice,
                    ExpiryDate = item.ExpiryDate,
                    TransactionType = transactionType,
                    TransactionDate = DateTime.Now,
                    Notes = notes
                };

                if (item.FreeQuantity > 0)
                {
                    var freeBaseQuantity = item.FreeQuantity * quantityPerUnit;
                    yield return new StockMovementPostDto
                    {
                        ProductId = item.ProductId,
                        ProductUnitId = item.ProductUnitId,
                        Quantity = reverseSign ? -item.FreeQuantity : item.FreeQuantity,
                        QuantityPerUnitSnapshot = quantityPerUnit,
                        BaseQuantity = reverseSign ? -freeBaseQuantity : freeBaseQuantity,
                        UnitPrice = 0,
                        PurchasePrice = 0,
                        SalePrice = item.SalePrice,
                        ExpiryDate = item.ExpiryDate,
                        TransactionType = transactionType,
                        TransactionDate = DateTime.Now,
                        Notes = notes + " | Free quantity"
                    };
                }
            }
        }
        private async void Product_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is ComboBox cb && cb.SelectedValue is int selectedProductId && selectedProductId > 0)
                {
                    // Get the bound row item (StockItemWriteDto)
                    if (cb.DataContext is not StockItemWriteDto item)
                        return;

                    // Cancel any previous unit load operation (prevent thread overlap)
                    // Optional: use a CancellationTokenSource if you already have one

                    item.ProductId = selectedProductId;
                    item.ProductUnitId = 0; // Reset the unit selection

                    // 🌀 Load product units filtered by product ID
                    var unitsResult = await _productUnitService
                        .GetAllWriteDtoWithFilteringAndIncludeAsync(
                            pu => pu.ProductId == selectedProductId,
                            pu => pu.Unit);

                    // Update the item's Units collection
                    item.Units.Clear();
                    if (unitsResult?.Data != null)
                    {
                        foreach (var unit in unitsResult.Data)
                            item.Units.Add(unit);
                    }

                    // ✅ Auto-select the first available unit (if any)
                    var defaultUnit = ProductUnitSelector.GetDefaultPurchaseUnit(item.Units);
                    if (defaultUnit != null)
                    {
                        item.ProductUnitId = defaultUnit.Id;
                        item.PurchasePrice = defaultUnit.PurchasePrice;
                        item.SalePrice = defaultUnit.SalePrice;

                        // Map the selected unit for saving
                        _itemUnits[item] = defaultUnit.Id;
                    }

                    // ✅ Set default quantity = 1 if it's 0 or less
                    if (item.Quantity <= 0)
                        item.Quantity = 1;

                    // ✅ Force UI refresh safely on the main thread
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        cb.Items.Refresh();
                    }, System.Windows.Threading.DispatcherPriority.Background);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء تحديث الوحدات", "An error occurred while updating units")}: {ex.Message}",
                                UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
      

        private void ProductComboBox_Loaded(object sender, RoutedEventArgs e)
        {
           
            /*  if (sender is ComboBox comboBox)
              {
                  if (comboBox.Template.FindName("PART_EditableTextBox", comboBox) is TextBox textBox)
                  {
                      // Avoid multiple subscriptions
                      textBox.TextChanged -= ProductCombo_TextChanged;
                      textBox.TextChanged += ProductCombo_TextChanged;
                  }
              }*/
        }

        #region SearchAboutProduct Handle




        private async void SearchProductBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var searchWindow = new PurchaseProductSearchWindow(
                    _productService,
                    async row => await AddProductFromSearchAsync(row),
                    async search => await CreateProductFromSearchAsync(search))
                {
                    Owner = this
                };

                searchWindow.ShowDialog();
                ProductBox.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("Product search could not be opened", "Could not open the product search window")}: {ex.Message}",
                    UiText.T("Error", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task CreateProductFromSearchAsync(string searchText)
        {
            WindowManager.ShowDialog<CreateProduct>(WindowSizeType.LargeRectangle, window =>
            {
                if (long.TryParse(searchText, out var barcode))
                    window.InitialItemCode = barcode.ToString();
            });

            await LoadDataAsync();
        }

        private Task<bool> AddProductFromSearchAsync(PurchaseProductSearchWindow.PurchaseProductSearchRow row)
        {
            if (row.ProductUnitId <= 0)
            {
                MessageBox.Show(UiText.T("لا توجد وحدة شراء معرفة لهذا المنتج.", "No purchase unit is defined for this product."), UiText.T("تنبيه", "Notice"));
                return Task.FromResult(false);
            }

            var unit = row.Product.ProductUnits?.FirstOrDefault(x => x.Id == row.ProductUnitId);
            if (unit == null)
            {
                MessageBox.Show(UiText.T("The product unit could not be loaded.", "The product unit could not be loaded."), UiText.T("Notice", "Notice"));
                return Task.FromResult(false);
            }

            var quantityPerUnit = unit.QuantityPerUnit > 0m ? unit.QuantityPerUnit : 1m;
            var purchasePrice = row.PurchasePrice > 0m ? row.PurchasePrice : unit.PurchasePrice;
            if (row.Quantity <= 0 || purchasePrice <= 0)
            {
                MessageBox.Show(UiText.T("الكمية وسعر الشراء يجب أن يكونا أكبر من صفر.", "Quantity and purchase price must be greater than zero."), UiText.T("تنبيه", "Notice"));
                return Task.FromResult(false);
            }

            var item = new StockItemWriteDto
            {
                ProductId = row.Product.Id,
                ProductUnitId = row.ProductUnitId,
                Quantity = row.Quantity,
                QuantityPerUnitSnapshot = quantityPerUnit,
                BaseQuantity = row.Quantity * quantityPerUnit,
                PurchasePrice = purchasePrice,
                SalePrice = unit.SalePrice,
                ExpiryDate = row.ExpiryDate,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now,
                ProductName = row.ProductName,
                UnitName = row.UnitName
            };

            Items.Add(item);
            _itemUnits[item] = row.ProductUnitId;
            ProductsGrid.Items.Refresh();
            UpdateStockTotals();
            return Task.FromResult(true);
        }

        #endregion

        private static T FindVisualChild<T>(DependencyObject parent, string name = null) where T : FrameworkElement
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T element && (name == null || element.Name == name))
                    return element;

                var result = FindVisualChild<T>(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }





        // Helper method to find parent of type T in Visual Tree
        public static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject? parent = VisualTreeHelper.GetParent(child);
            while (parent != null && parent is not T)
                parent = VisualTreeHelper.GetParent(parent);
            return parent as T;
        }

        private void Unit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb && cb.DataContext is StockItemWriteDto item)
            {
                // Get the selected value directly from ComboBox
                if (cb.SelectedValue is int selectedUnitId && selectedUnitId > 0)
                {
                    // Manually set the ProductUnitId
                    item.ProductUnitId = selectedUnitId;
                    item.ProductUnitId = selectedUnitId;
                    _itemUnits[item] = selectedUnitId;  // Map item to its selected unit


                    var unit = item.Units.FirstOrDefault(pu=>pu.Id == selectedUnitId);
                    if (unit != null)
                    {
                        item.PurchasePrice = unit.PurchasePrice;
                        item.SalePrice = unit.SalePrice;
                        item.ProductUnitId = unit.Id;
                        ProductsGrid.Items.Refresh();
                    }
                }
            }
        }
        private void ProductComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.IsEditable)
            {
                // Keep dropdown open for navigation keys
                if (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Enter || e.Key == Key.Escape)
                    return;

                // For other keys, ensure dropdown stays open
                comboBox.IsDropDownOpen = true;
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


        #region AddProductHandle 
        private async void ProductBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_isLoadingUnits)
                    return;

                if (ProductBox.SelectedValue is not int productId || productId <= 0)
                    return;

                _isLoadingUnits = true;

                // 🔷 Load units from DB
                var unitsResult = await _productUnitService
                    .GetAllWriteDtoWithFilteringAndIncludeAsync(
                        pu => pu.ProductId == productId,
                        pu => pu.Unit);

                UnitBox.ItemsSource = unitsResult.Data;

                // 🔷 Auto-select first unit if exists
                var firstUnit = unitsResult.Data.FirstOrDefault();
                if (firstUnit != null)
                {
                    UnitBox.SelectedValue = firstUnit.Id;
                    PurchaseBox.Text = firstUnit.PurchasePrice.ToString();
                    SaleBox.Text = firstUnit.SalePrice.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("خطأ أثناء تحميل الوحدات", "Error while loading units")}: {ex.Message}", UiText.T("خطأ", "Error"));
            }
            finally
            {
                _isLoadingUnits = false;
            }
        }


        private void ClearProductInputs()
        {
            ProductBox.SelectedIndex = -1;
            UnitBox.ItemsSource = null;
            QtyBox.Text = "";
            PurchaseBox.Text = "";
            SaleBox.Text = "";
            ExpiryBox.SelectedDate = null;
        }

        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            if (ProductBox.SelectedItem is not ProductReadDto product)
            {
                MessageBox.Show(UiText.T("يرجى اختيار منتج.", "Please choose a product."), UiText.T("تنبيه", "Notice"));
                return;
            }

            if (UnitBox.SelectedItem is not ProductUnitWriteDto unit)
            {
                MessageBox.Show(UiText.T("يرجى اختيار وحدة.", "Please choose a unit."), UiText.T("تنبيه", "Notice"));
                return;
            }

            if (!decimal.TryParse(QtyBox.Text, out decimal qty) || qty <= 0)
            {
                MessageBox.Show(UiText.T("الكمية غير صحيحة.", "The quantity is invalid."), UiText.T("تنبيه", "Notice"));
                return;
            }

            if (!TryParseDecimalInput(PurchaseBox.Text, out var purchasePrice) || purchasePrice <= 0)
            {
                MessageBox.Show(UiText.T("سعر الشراء غير صحيح.", "The purchase price is invalid."), UiText.T("تنبيه", "Notice"));
                return;
            }

            if (!TryParseDecimalInput(SaleBox.Text, out var salePrice) || salePrice <= 0)
            {
                MessageBox.Show(UiText.T("سعر البيع غير صحيح.", "The sale price is invalid."), UiText.T("تنبيه", "Notice"));
                return;
            }

            if (!ExpiryBox.SelectedDate.HasValue)
            {
                MessageBox.Show(UiText.T("تاريخ الانتهاء مطلوب.", "The expiry date is required."), UiText.T("تنبيه", "Notice"));
                return;
            }

            var item = new StockItemWriteDto
            {
                ProductId = product.Id,
                ProductUnitId = unit.Id,
                Quantity = qty,
                QuantityPerUnitSnapshot = unit.QuantityPerUnit > 0 ? unit.QuantityPerUnit : 1m,
                BaseQuantity = qty * (unit.QuantityPerUnit > 0 ? unit.QuantityPerUnit : 1m),
                PurchasePrice = purchasePrice,
                SalePrice = salePrice,
                ExpiryDate = ExpiryBox.SelectedDate.Value,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now,

                // 🔥 Extra fields for DataGrid display
                ProductName = product.Name,
                UnitName = unit.Unit.Name
            };

            Items.Add(item);
            _itemUnits[item] = unit.Id;

            ClearProductInputs();
            UpdateStockTotals();
        }

        private static bool TryParseDecimalInput(string? text, out decimal value)
        {
            value = 0m;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
                || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }
        private void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is StockItemWriteDto item)
            {
                Items.Remove(item);

                if (_itemUnits.ContainsKey(item))
                    _itemUnits.Remove(item);
            }
        }


        #endregion

        #region printing

        private void PrintStockInA4(StockDocumentReadDto dto)
        {
            FlowDocument doc = new FlowDocument
            {
                FlowDirection = FlowDirection.RightToLeft,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 16,
                PagePadding = new Thickness(50),
                ColumnWidth = double.PositiveInfinity,
                TextAlignment = TextAlignment.Right
            };

            // HEADER
            var header = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 26,
                FontWeight = FontWeights.Bold
            };
            header.Inlines.Add("Raccoon Warehouse");
            doc.Blocks.Add(header);

            var title = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 24,
                FontWeight = FontWeights.Bold
            };
            title.Inlines.Add(UiText.T("سند إدخال بضاعة", "Stock In Document"));
            doc.Blocks.Add(title);

            doc.Blocks.Add(new Paragraph(new Run("________________________________________________________")));

            // INFO TABLE
            Table infoTable = new Table();
            infoTable.CellSpacing = 10;
            infoTable.Columns.Add(new TableColumn());
            infoTable.Columns.Add(new TableColumn());

            TableRowGroup infoGroup = new TableRowGroup();
            infoTable.RowGroups.Add(infoGroup);

            void AddInfo(string label, string value)
            {
                var row = new TableRow();
                row.Cells.Add(new TableCell(new Paragraph(new Run(label))) { FontWeight = FontWeights.Bold });
                row.Cells.Add(new TableCell(new Paragraph(new Run(value))));
                infoGroup.Rows.Add(row);
            }

            AddInfo(UiText.T("رقم السند:", "Document No:"), dto.DocumentNumber);
            AddInfo(UiText.T("التاريخ:", "Date:"), dto.CreatedDate.ToString("yyyy/MM/dd"));
            AddInfo(UiText.T("المستخدم:", "User:"), dto.Supplier?.Name ?? "-");
            AddInfo(UiText.T("ملاحظات:", "Notes:"), dto.Notes ?? "");

            doc.Blocks.Add(infoTable);
            doc.Blocks.Add(new Paragraph(new Run(" ")));

            // ITEMS TABLE
            Table itemsTable = new Table();
            itemsTable.CellSpacing = 0;

            string[] headers =
            {
                UiText.T("المنتج", "Product"),
                UiText.T("الوحدة", "Unit"),
                UiText.T("الكمية", "Quantity"),
                UiText.T("سعر الشراء", "Purchase Price"),
                UiText.T("سعر البيع", "Sale Price"),
                UiText.T("تاريخ الانتهاء", "Expiry Date")
            };

            foreach (var _ in headers)
                itemsTable.Columns.Add(new TableColumn());

            TableRowGroup itemsGroup = new TableRowGroup();
            itemsTable.RowGroups.Add(itemsGroup);

            // Header row
            var headerRow = new TableRow();
            foreach (var h in headers)
            {
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run(h)))
                {
                    FontWeight = FontWeights.Bold,
                    Padding = new Thickness(5),
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(0, 0, 0, 1)
                });
            }
            itemsGroup.Rows.Add(headerRow);

            // Data rows
            foreach (var item in dto.Items)
            {
                var row = new TableRow();

                row.Cells.Add(new TableCell(new Paragraph(new Run(item.Product?.Name ?? ""))) { Padding = new Thickness(5) });
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.ProductUnit?.Unit?.Name ?? ""))) { Padding = new Thickness(5) });
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.Quantity.ToString()))) { Padding = new Thickness(5) });
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.PurchasePrice.ToString("N5")))) { Padding = new Thickness(5) });
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.SalePrice.ToString("N5")))) { Padding = new Thickness(5) });
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.ExpiryDate?.ToString("yyyy/MM/dd") ?? "-"))) { Padding = new Thickness(5) });

                itemsGroup.Rows.Add(row);
            }

            doc.Blocks.Add(itemsTable);

            // FOOTER
            doc.Blocks.Add(new Paragraph(new Run("\n________________________________________________________")));

            var footer = new Paragraph
            {
                TextAlignment = TextAlignment.Left,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 20, 0, 0)
            };
            footer.Inlines.Add(UiText.T("توقيع الموظف: ________________________", "Employee Signature: ________________________"));
            UiText.ApplyDocument(doc);
            doc.Blocks.Add(footer);

            // PRINT
            PrintDialog dialog = new PrintDialog();
            if (dialog.ShowDialog() == true)
            {
                IDocumentPaginatorSource dps = doc;
                dialog.PrintDocument(dps.DocumentPaginator, "Print Stock In A4");
            }
        }

        private async void PrintBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentDocumentId == null)
                return;

            var doc = await _stockDocumentService.GetFullDocumentByIdAsync(_currentDocumentId.Value);

            if (doc == null)
            {
                MessageBox.Show(UiText.T("السند غير موجود.", "The document was not found."), UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }else
            {

                SaveStockInPdf(doc);
                return;

            }  
            
        }
        private void SaveStockInPdf(StockDocumentReadDto doc)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF File (*.pdf)|*.pdf",
                FileName = $"StockIn_{doc.DocumentNumber}.pdf"
            };

            if (dlg.ShowDialog() == true)
            {
                var path = dlg.FileName;

                // Generate PDF using QuestPDF
                PdfGenerator.StockIn(doc, path);

                MessageBox.Show(UiText.T("تم حفظ ملف PDF بنجاح.", "The PDF file was saved successfully."),
                    UiText.T("تم الحفظ", "Saved"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Open the PDF
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
        }


        private void NewStockInBtn_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            PrintBtn.Visibility = Visibility.Collapsed;  // 🔥 Show Print Button
            NewStockInBtn.Visibility = Visibility.Collapsed;


        }

        private async void ImportFalconStockBtn_Click(object sender, RoutedEventArgs e)
        {
            if (WarehouseComboBox.SelectedValue is not int warehouseId || warehouseId <= 0)
            {
                MessageBox.Show(
                    UiText.T("يرجى اختيار مستودع قبل الاستيراد.", "Please choose a warehouse before importing."),
                    UiText.T("تنبيه", "Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                UiText.T("سيتم استيراد مخزون فالكون ومقارنته مع المخزون الحالي. هل تريد المتابعة؟",
                    "Falcon stock will be imported and compared with current stock. Do you want to continue?"),
                UiText.T("تأكيد", "Confirm"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            var loadingShown = false;

            void HideLoadingIfShown()
            {
                if (!loadingShown)
                    return;

                _loadingService.Hide();
                loadingShown = false;
            }

            try
            {
                ImportFalconStockBtn.IsEnabled = false;
                _loadingService.Show();
                loadingShown = true;

                var result = await _falconStockImportService.ImportAsync(new FalconStockImportRequestDto
                {
                    WarehouseId = warehouseId,
                    UserId = _userSession.CurrentUser?.Id
                });

                HideLoadingIfShown();

                if (!result.Success)
                {
                    MessageBox.Show(
                        result.Message ?? UiText.T("فشل استيراد مخزون فالكون.", "Failed to import Falcon stock."),
                        UiText.T("خطأ", "Error"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                var data = result.Data;
                var summary = data == null
                    ? result.Message
                    : UiText.T(
                        $"تم استيراد مخزون فالكون.\nعدد عناصر API: {data.ApiItemCount}\nالعناصر ذات الكمية: {data.PositiveApiItemCount}\nالمنتجات المطابقة: {data.MatchedProductCount}\nزيادة المخزون: {data.IncreasedProductCount}\nنقص المخزون: {data.DecreasedProductCount}\nبدون تغيير: {data.UnchangedProductCount}\nغير مطابقة: {data.UnmatchedProductCount}\nالمتجاهلة: {data.IgnoredItemCount}\nرقم السند: {data.StockDocumentNumber ?? "-"}",
                        $"Falcon stock imported.\nAPI items: {data.ApiItemCount}\nItems with quantity: {data.PositiveApiItemCount}\nMatched products: {data.MatchedProductCount}\nStock increases: {data.IncreasedProductCount}\nStock decreases: {data.DecreasedProductCount}\nUnchanged: {data.UnchangedProductCount}\nUnmatched: {data.UnmatchedProductCount}\nIgnored: {data.IgnoredItemCount}\nDocument number: {data.StockDocumentNumber ?? "-"}");

                MessageBox.Show(
                    summary,
                    UiText.T("نجاح", "Success"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                HideLoadingIfShown();
                MessageBox.Show(
                    $"{UiText.T("حدث خطأ أثناء استيراد مخزون فالكون", "An error occurred while importing Falcon stock")}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                HideLoadingIfShown();
                ImportFalconStockBtn.IsEnabled = true;
            }
        }
        #endregion

        #region Search Daialog about stock  


         private async void SearchStockBtn_Click(object sender, RoutedEventArgs e)
         {
            var searchWindow = new SearchStockInWindow(_stockDocumentService,true)
            {
                Owner = this
            };

            if (searchWindow.ShowDialog() == true)
            {
                await LoadSelectedStockInWithLoadingAsync(searchWindow.Result);
            }
         }


        private async Task LoadSelectedStockInWithLoadingAsync(StockDocumentReadDto doc)
        {
            _loadingService.Show();
            try
            {
                await Task.Delay(1);
                LoadSelectedStockIn(doc);
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private void LoadSelectedStockIn(StockDocumentReadDto doc)
        {
            ClearForm();

            _currentDocumentId = doc.Id;                 // <-- critical
            _originalItems = doc.Items.ToList();         // <-- for adjusting stock differences
            WarehouseComboBox.SelectedValue = doc.WarehouseId;
            SupplierComboBox.SelectedValue = doc.SupplierId;
            SetSelectedPaymentType(doc.PaymentType ?? PaymentType.Cash);
            _currentChecks = doc.Checks?.Select(check => new CheckWriteDto
            {
                Id = check.Id,
                CheckNumber = check.CheckNumber,
                BankName = check.BankName,
                DueDate = check.DueDate,
                Amount = check.Amount,
                Status = check.Status,
                Notes = check.Notes,
                CreatedDate = check.CreatedDate,
                UpdatedDate = check.UpdatedDate
            }).ToList() ?? new List<CheckWriteDto>();
            UpdateChecksButtonVisibility();

            VoucherNumberTxt.Text = doc.DocumentNumber;
            FalconInvoiceNumberTextBox.Text = doc.FalconInvoiceNumber ?? string.Empty;
            DatePickerInvoice.SelectedDate = doc.CreatedDate;
            NotesTxt.Text = doc.Notes;
            DiscountTextBox.Text = (doc.DiscountAmount ?? 0m).ToString("0.00000");

            Items.Clear();
            _itemUnits.Clear();

            foreach (var item in doc.Items)
            {
                var stockItem = new StockItemWriteDto
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductName = item.Product?.Name,
                    ProductUnitId = item.ProductUnitId,
                    UnitName = item.ProductUnit?.Unit?.Name,
                    Quantity = item.Quantity,
                    PurchasePrice = item.PurchasePrice,
                    SalePrice = item.SalePrice,
                    ExpiryDate = item.ExpiryDate,
                    CreatedDate = item.CreatedDate,
                    UpdatedDate = item.UpdatedDate
                };

                Items.Add(stockItem);
                _itemUnits[stockItem] = item.ProductUnitId;
            }

            ProductsGrid.Items.Refresh();
            UpdateStockTotals();
            PrintBtn.Visibility = Visibility.Visible;
            NewStockInBtn.Visibility = Visibility.Visible;
        }

        private PaymentType GetSelectedPaymentType()
        {
            if (PaymentTypeComboBox.SelectedItem is ComboBoxItem item &&
                int.TryParse(item.Tag?.ToString(), out var value))
            {
                return (PaymentType)value;
            }

            return PaymentType.Cash;
        }

        private void SetSelectedPaymentType(PaymentType paymentType)
        {
            PaymentTypeComboBox.SelectedItem = PaymentTypeComboBox.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(item => int.TryParse(item.Tag?.ToString(), out var value) && value == (int)paymentType);
            PaymentTypeComboBox.SelectedIndex = PaymentTypeComboBox.SelectedItem == null ? 0 : PaymentTypeComboBox.SelectedIndex;
        }

        private void PaymentTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GetSelectedPaymentType() != PaymentType.Check)
                _currentChecks.Clear();

            UpdateChecksButtonVisibility();
        }

        private void UpdateChecksButtonVisibility()
        {
            ChecksBtn.Visibility = GetSelectedPaymentType() == PaymentType.Check || _currentChecks.Any()
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void ChecksBtn_Click(object sender, RoutedEventArgs e)
        {
            var total = Math.Round(Items.Sum(item => item.Quantity * item.PurchasePrice) - GetDiscountAmount(), 3);
            var dialog = new CheckDetailsWindow(total, _currentChecks);
            if (dialog.ShowDialog() == true)
            {
                _currentChecks = dialog.ResultChecks.ToList();
                UpdateChecksButtonVisibility();
            }
        }

        private void ClearProductBtn_Click(object sender, RoutedEventArgs e)
        {
            ProductBox.Text = "";
            ProductBox.SelectedIndex = -1;
            ProductBox.ItemsSource = Products;
            UnitBox.ItemsSource = null;

            PurchaseBox.Text = "";
            SaleBox.Text = "";
            QtyBox.Text = "";
            ExpiryBox.SelectedDate = null;

            ProductBox.IsDropDownOpen = false;
        }




        #endregion

    }
}

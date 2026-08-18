#region Usings
using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.ProductUnits;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.StockTransactions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Common;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Cashiers.DTOs;
using RaccoonWarehouse.Domain.Checks.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.FinancialTransactions.DTOs;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.Stock;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Domain.StockTransactions.DTOs;
using RaccoonWarehouse.Helpers.Pdf;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Products;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
#endregion
namespace RaccoonWarehouse.Invoices
{
    public partial class PayInvoice : Window
    {
        private const int ProductSelectionDelayMs = 150;
        // ====== مجموعات للـ Binding ======
        public ObservableCollection<ProductReadDto> Products { get; set; } = new();
        public ObservableCollection<InvoiceLineWriteDto> InvoiceLines { get; set; } = new();

        private ObservableCollection<UserReadDto> _allSuppliers;
        private List<InvoiceLineReadDto> _originalLines = new(); // to restore stock on update
        private List<CheckWriteDto> _currentChecks = new();

        private readonly IInvoiceService _invoicesService;
        private readonly IUserService _userService;
        private readonly IProductService _productService;
        private readonly IProductUnitService _productUnitService;
        private readonly IStockService _stockService;
        private readonly IStockTransactionService _stockTransactionService;
        private readonly IFinancialTransactionService _financialService;
        private readonly IUserSession _userSession;
        private readonly ILoadingService _loadingService;
        private bool _isLoadingUnits = false;
        private int _productSelectionVersion;
        private string _productSearchText = string.Empty;
        private bool _isRestoringProductSearchText;
        private readonly System.Threading.SemaphoreSlim _productSelectionSemaphore = new(1, 1);
        private int? _currentInvoiceId = null;   // لتحديث الفاتورة بعد الحفظ الأول

        public PayInvoice(
            IStockService stockService,
            IInvoiceService invoiceService,
            IUserService userService,
            IProductService productService,
            IProductUnitService productUnitService,
            IUserSession userSession,
            IStockTransactionService stockTransactionService,
            IFinancialTransactionService financialService,
            ILoadingService loadingService)
        {
            _stockService = stockService;
            _productService = productService;
            _productUnitService = productUnitService;
            _stockTransactionService = stockTransactionService;
            _invoicesService = invoiceService;
            _userService = userService;
            _userSession = userSession;
            _financialService = financialService;
            _loadingService = loadingService;
            _isLoadingUnits = false;

            InitializeComponent();
            UiText.ApplyWindow(this);
            DataContext = this;

            // رقم الفاتورة
            InvoiceNumberTextBox.Text = GenerateInvoiceNumber();

            // ربط الـ Grid
            ProductsGrid.ItemsSource = InvoiceLines;

            Loaded += PayInvoice_Loaded;
            Closed += PayInvoice_Closed;
            CatalogRefreshNotifier.CatalogChanged += CatalogRefreshNotifier_CatalogChanged;
        }

        private string GenerateInvoiceNumber()
        {
            string prefix = "PINV"; // Payment Invoice
            string datePart = DateTime.Now.ToString("yyyyMMddHHmmss");
            return $"{prefix}-{datePart}";
        }

        // ===================== LOAD DATA =====================
        private async void PayInvoice_Loaded(object sender, RoutedEventArgs e)
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

                UiText.ApplyTranslations(this);
                // المورّدين (نفس users لكن أنت تختار اللي يمثل الموردين)
                var result = await _userService.GetAllAsync();
                _allSuppliers = new ObservableCollection<UserReadDto>(result?.Data ?? new List<UserReadDto>());
                SupplierComboBox.ItemsSource = _allSuppliers;
                SupplierComboBox.SelectedIndex = -1;

                InvoiceDatePicker.SelectedDate = DateTime.Now;

                PaymentMethodComboBox.SelectedIndex = 0;
                UiText.ApplyTranslations(PaymentMethodComboBox);

                await LoadProductsAsync();
                UiText.ApplyTranslations(this);
            }
            catch (Exception ex)
            {
                HideLoadingIfShown();
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء تحميل البيانات", "An error occurred while loading data")}: {ex.Message}", UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                HideLoadingIfShown();
            }
        }

        private async Task LoadProductsAsync()
        {
            try
            {

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

                Products.Clear();

                foreach (var stock in result.Items)
                {
                    if (stock != null)
                        Products.Add(stock);
                }

                ProductBox.ItemsSource = Products;
                ProductBox.DisplayMemberPath = "Name";
                ProductBox.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("خطأ عند تحميل المنتجات", "Error loading products")}: {ex.Message}", UiText.T("خطأ", "Error"));
            }
        }

        private async void CreateProductBtn_Click(object sender, RoutedEventArgs e)
        {
            var existingProductIds = Products.Select(product => product.Id).ToHashSet();

            WindowManager.ShowDialog<CreateProduct>();

            await LoadProductsAsync();

            var createdProduct = Products.FirstOrDefault(product => !existingProductIds.Contains(product.Id));
            if (createdProduct != null)
                ProductBox.SelectedItem = createdProduct;
        }

        private async void CatalogRefreshNotifier_CatalogChanged(object? sender, EventArgs e)
        {
            if (!IsLoaded)
                return;

            await LoadProductsAsync();
        }

        private void PayInvoice_Closed(object? sender, EventArgs e)
        {
            CatalogRefreshNotifier.CatalogChanged -= CatalogRefreshNotifier_CatalogChanged;
        }
        private bool TryGetActiveCashierSession(out CashierSessionReadDto? session)
        {
            session = _userSession.CurrentCashierSession;
            if (session != null)
                return true;

            MessageBox.Show(UiText.T("لا توجد جلسة كاشير مفتوحة. الرجاء فتح جلسة أولاً.", "There is no open cashier session. Please open a session first."), UiText.T("خطأ", "Error"));
            return false;
        }

        private IEnumerable<StockMovementPostDto> BuildInvoiceStockMovements(
            IEnumerable<InvoiceLineWriteDto> lines,
            TransactionType transactionType,
            int? invoiceId,
            int? cashierId,
            int? cashierSessionId,
            string notes,
            decimal multiplier)
        {
            return lines
                .Where(line => line.ProductId > 0 && line.ProductUnitId > 0 && line.Quantity != 0)
                .Select(line =>
                {
                    var quantityPerUnit = line.QuantityPerUnitSnapshot > 0 ? line.QuantityPerUnitSnapshot : 1m;
                    var baseQuantity = line.BaseQuantity != 0 ? line.BaseQuantity : line.Quantity * quantityPerUnit;
                    var profileSalePrice = GetProductUnitSalePrice(line.ProductId, line.ProductUnitId);

                    return new StockMovementPostDto
                    {
                        ProductId = line.ProductId,
                        ProductUnitId = line.ProductUnitId,
                        Quantity = line.Quantity * multiplier,
                        QuantityPerUnitSnapshot = quantityPerUnit,
                        BaseQuantity = baseQuantity * multiplier,
                        UnitPrice = line.UnitPrice,
                        PurchasePrice = line.UnitPrice,
                        SalePrice = profileSalePrice,
                        ExpiryDate = line.ExpiryDate,
                        TransactionType = transactionType,
                        UpdateCatalogAverageCost = _userSession.CurrentUser?.Role == UserRole.Admin,
                        InvoiceId = invoiceId,
                        CasherId = cashierId,
                        CashierSessionId = cashierSessionId,
                        TransactionDate = DateTime.Now,
                        Notes = notes
                    };
                });
        }

        private decimal GetProductUnitSalePrice(int productId, int productUnitId)
        {
            var product = Products.FirstOrDefault(item => item.Id == productId);
            var unit = product?.ProductUnits?.FirstOrDefault(item => item.Id == productUnitId);
            return unit?.SalePrice ?? product?.DefaultSalePrice ?? 0m;
        }

        // ===================== SUPPLIER SEARCH =====================
        private void SupplierComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            SupplierComboBox.DisplayMemberPath = "Name";
            SupplierComboBox.SelectedValuePath = "Id";
        }

        private void SupplierComboBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            FilterSupplierList(SupplierComboBox.Text + e.Text);
        }

        private void SupplierComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back || e.Key == Key.Delete)
                FilterSupplierList(SupplierComboBox.Text);
        }

        private void FilterSupplierList(string text)
        {
            if (_allSuppliers == null) return;

            var filtered = _allSuppliers
                .Where(c => c.Name != null &&
                            c.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
                .ToList();

            SupplierComboBox.ItemsSource = filtered;
            SupplierComboBox.IsDropDownOpen = true;
        }

        // ===================== PRODUCT & UNIT =====================
        private void ProductBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Selection changes while the user navigates the result list are not confirmed until Enter is pressed.
        }

        private void ProductBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Up or Key.Down && ProductBox.IsDropDownOpen)
            {
                RestoreTypedProductSearchTextAfterNavigation();
                return;
            }

            if (e.Key != Key.Enter)
                return;

            var product = ResolveProductChoice();
            if (product == null)
                return;

            e.Handled = true;
            _isRestoringProductSearchText = true;
            ProductBox.Text = product.Name ?? string.Empty;
            _productSearchText = ProductBox.Text;
            _isRestoringProductSearchText = false;
            ProductBox.IsDropDownOpen = false;
            QueueProductSelection(product.Id);
        }

        private async void ProductBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isLoadingUnits || !ProductBox.IsDropDownOpen)
                return;

            await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Background);

            var product = ResolveProductChoice();
            if (product == null)
                return;

            _isRestoringProductSearchText = true;
            ProductBox.Text = product.Name ?? string.Empty;
            _productSearchText = ProductBox.Text;
            _isRestoringProductSearchText = false;
            ProductBox.IsDropDownOpen = false;
            QueueProductSelection(product.Id);
        }

        private ProductReadDto? ResolveProductChoice()
        {
            if (ProductBox.SelectedItem is ProductReadDto selectedProduct)
                return selectedProduct;

            var searchText = ProductBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(searchText))
                return null;

            var exactMatch = Products.FirstOrDefault(product =>
                string.Equals(product.Name?.Trim(), searchText, StringComparison.OrdinalIgnoreCase) ||
                product.ITEMCODE.ToString() == searchText);

            if (exactMatch != null)
                return exactMatch;

            return ProductBox.ItemsSource is IEnumerable<ProductReadDto> filteredProducts
                ? filteredProducts.FirstOrDefault()
                : null;
        }

        private void RestoreTypedProductSearchTextAfterNavigation()
        {
            var typedText = _productSearchText;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _isRestoringProductSearchText = true;
                ProductBox.Text = typedText;
                _isRestoringProductSearchText = false;
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void QueueProductSelection(int productId)
        {
            if (productId <= 0)
                return;

            var selectionVersion = System.Threading.Interlocked.Increment(ref _productSelectionVersion);
            _ = LoadSelectedProductAsync(productId, selectionVersion);
        }

        private async Task LoadSelectedProductAsync(int productId, int selectionVersion)
        {
            await Task.Delay(ProductSelectionDelayMs);
            if (selectionVersion != _productSelectionVersion)
                return;

            await _productSelectionSemaphore.WaitAsync();
            try
            {
                if (selectionVersion != _productSelectionVersion)
                    return;

                _isLoadingUnits = true;

                // 🔷 Load units from DB
                var unitsResult = await _productUnitService
                    .GetAllWriteDtoWithFilteringAndIncludeAsync(
                        pu => pu.ProductId == productId,
                        pu => pu.Unit);

                UnitBox.ItemsSource = unitsResult.Data;

                // 🔷 Auto-select default purchase unit if exists
                var defaultUnit = ProductUnitSelector.GetDefaultPurchaseUnit(unitsResult.Data);
                if (defaultUnit != null)
                {
                    UnitBox.SelectedValue = defaultUnit.Id;
                    PurchaseBox.Text = defaultUnit.PurchasePrice.ToString();
                    SaleBox.Text = defaultUnit.SalePrice.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("خطأ أثناء تحميل الوحدات", "Error while loading units")}: {ex.Message}", UiText.T("خطأ", "Error"));
            }
            finally
            {
                _isLoadingUnits = false;
                _productSelectionSemaphore.Release();
            }
        }

        private void UnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UnitBox.SelectedItem is not ProductUnitWriteDto unit)
                return;

            PurchaseBox.Text = unit.PurchasePrice.ToString("0.###");
            SaleBox.Text = unit.SalePrice.ToString("0.###");
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

        // ===================== ADD PRODUCT LINE =====================
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

            if (!TryParseDecimalInput(QtyBox.Text, out decimal qty) || qty <= 0)
            {
                MessageBox.Show(UiText.T("الكمية غير صحيحة.", "The quantity is invalid."), UiText.T("تنبيه", "Notice"));
                return;
            }

            if (!ExpiryBox.SelectedDate.HasValue)
            {
                MessageBox.Show(UiText.T("تاريخ الانتهاء مطلوب.", "The expiry date is required."), UiText.T("تنبيه", "Notice"));
                return;
            }

            // في فاتورة المشتريات نستخدم سعر الشراء
            var line = new InvoiceLineWriteDto
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductUnitId = unit.Id,
                QuantityPerUnitSnapshot = unit.QuantityPerUnit > 0 ? unit.QuantityPerUnit : 1m,
                BaseQuantity = qty * (unit.QuantityPerUnit > 0 ? unit.QuantityPerUnit : 1m),
                UnitName = unit.Unit?.Name,
                Quantity = qty,
                UnitPrice = TryParseDecimalInput(PurchaseBox.Text, out var p) ? p : 0,
                UnitCost = TryParseDecimalInput(PurchaseBox.Text, out var cost) ? cost : 0,
                ExpiryDate = ExpiryBox.SelectedDate.Value,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            InvoiceLines.Add(line);
            UpdateTotal();
            ClearProductInputs();
        }

        private void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is InvoiceLineWriteDto line)
            {
                InvoiceLines.Remove(line);
                UpdateTotal();
            }
        }

        private void UpdateTotal()
        {
            TotalAmountTextBox.Text =
                InvoiceLines.Sum(x => x.LineTotal).ToString("0.###");
        }

        private static List<CheckWriteDto> CloneChecks(IEnumerable<CheckWriteDto>? checks)
        {
            return checks?.Select(check => new CheckWriteDto
            {
                Id = check.Id,
                CheckNumber = check.CheckNumber,
                BankName = check.BankName,
                DueDate = check.DueDate,
                Amount = check.Amount,
                Status = check.Status,
                Notes = check.Notes,
                VoucherId = check.VoucherId,
                InvoiceId = check.InvoiceId,
                CreatedDate = check.CreatedDate,
                UpdatedDate = check.UpdatedDate
            }).ToList() ?? new List<CheckWriteDto>();
        }

        private static List<CheckWriteDto> CloneChecks(IEnumerable<CheckReadDto>? checks)
        {
            return checks?.Select(check => new CheckWriteDto
            {
                Id = check.Id,
                CheckNumber = check.CheckNumber,
                BankName = check.BankName,
                DueDate = check.DueDate,
                Amount = check.Amount,
                Status = check.Status,
                Notes = check.Notes,
                VoucherId = check.VoucherId,
                InvoiceId = check.InvoiceId,
                CreatedDate = check.CreatedDate,
                UpdatedDate = check.UpdatedDate
            }).ToList() ?? new List<CheckWriteDto>();
        }

        private void UpdateChecksButtonVisibility()
        {
            ChecksBtn.Visibility = GetSelectedPaymentType() == PaymentType.Check || _currentChecks.Any()
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private bool EditCurrentInvoiceChecks(decimal invoiceAmount)
        {
            var dialog = new CheckDetailsWindow(invoiceAmount, _currentChecks);
            if (dialog.ShowDialog() != true)
                return false;

            _currentChecks = CloneChecks(dialog.ResultChecks);
            UpdateChecksButtonVisibility();
            return true;
        }

        private void ChecksBtn_Click(object sender, RoutedEventArgs e)
        {
            var invoiceAmount = InvoiceLines.Sum(line => line.LineTotal);
            if (_currentChecks.Count == 0 && !EditCurrentInvoiceChecks(invoiceAmount))
            {
                MessageBox.Show(
                    UiText.T("لا توجد شيكات لعرضها أو تعديلها.", "There are no checks to view or edit."),
                    UiText.T("تنبيه", "Notice"));
            }
        }

        private void PaymentMethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GetSelectedPaymentType() != PaymentType.Check && _currentChecks.Any())
            {
                _currentChecks.Clear();
            }

            UpdateChecksButtonVisibility();
        }

        // ===================== SAVE / UPDATE / PRINT =====================
        /*private async void SaveReceiptBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!InvoiceLines.Any())
                {
                    MessageBox.Show("يرجى إضافة منتج واحد على الأقل.", "تنبيه");
                    return;
                }

                if (SupplierComboBox.SelectedItem == null)
                {
                    MessageBox.Show("❌ يرجى اختيار المورّد.", "تنبيه");
                    return;
                }

                var supplier = SupplierComboBox.SelectedItem as UserReadDto;
                decimal totalAmount = InvoiceLines.Sum(l => l.LineTotal);
                if (!TryGetActiveCashierSession(out var session))
                    return;

                bool isUpdate = _currentInvoiceId != null;

                var invoiceDto = new InvoiceWriteDto
                {
                    Id = _currentInvoiceId ?? 0,
                    InvoiceNumber = InvoiceNumberTextBox.Text,
                    SupplierId = supplier?.Id,
                    InvoiceType = InvoiceType.Purchase,   // 👈 مشتريات
                    TotalAmount = totalAmount,
                    CreatedDate = InvoiceDatePicker.SelectedDate.Value,
                    UpdatedDate = DateTime.Now,
                    InvoiceLines = InvoiceLines.ToList()
                };

                if (!isUpdate)
                {
                    // ============ CREATE ============
                    var result = await _invoicesService.CreateAsync(invoiceDto);

                    if (result.Success)
                    {
                        _currentInvoiceId = result.Data.Id;

                        // ✅ المشتريات: زيادة الكميات في المخزون
                        foreach (var line in InvoiceLines)
                            await UpdateStockQuantity(line.ProductId, line.ProductUnitId, line.Quantity);

                        MessageBox.Show("تم إنشاء فاتورة المشتريات بنجاح!");
                    }
                }
                else
                {
                    // ============ UPDATE ============
                    // 1️⃣ إعادة كميات الفاتورة القديمة (طرح من المخزون)
                    foreach (var old in _originalLines)
                        await UpdateStockQuantity(old.ProductId, old.ProductUnitId, -old.Quantity);

                    // 2️⃣ إضافة كميات الفاتورة الجديدة
                    var movementResult = await _stockService.PostMovementsAsync(
                        BuildInvoiceStockMovements(
                            InvoiceLines,
                            TransactionType.Purchase,
                            savedInvoiceId,
                            session.CashierId,
                            session.Id,
                            $"Purchase invoice #{invoiceDto.InvoiceNumber}",
                            1m));
                    if (!movementResult.Success)
                    {
                        MessageBox.Show(movementResult.Message ?? "فشل تحديث المخزون.", "خطأ");
                        return;
                    }
                    await PostInvoiceStockTransactionsAsync(
                        InvoiceLines,
                        TransactionType.Purchase,
                        savedInvoiceId,
                        session.CashierId,
                        session.Id,
                        $"Purchase invoice #{invoiceDto.InvoiceNumber}",
                        1m);

                    var result = await _invoicesService.UpdateAsync(invoiceDto);

                    if (result.Success)
                        MessageBox.Show("تم تحديث فاتورة المشتريات بنجاح!");
                }

                PrintBtn.Visibility = Visibility.Visible;
                NewInvoiceBtn.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء حفظ الفاتورة:\n{ex.Message}");
            }
        }*/
        private async void SaveReceiptBtn_Click(object sender, RoutedEventArgs e)
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
                if (!InvoiceLines.Any())
                {
                    MessageBox.Show("يرجى إضافة منتج واحد على الأقل.", "تنبيه");
                    return;
                }

                var selectedPaymentType = GetSelectedPaymentType();

                if (selectedPaymentType == PaymentType.Credit && SupplierComboBox.SelectedItem == null)
                {
                    MessageBox.Show(
                        UiText.T("يرجى اختيار المورد للفواتير الآجلة.", "Please select a supplier for credit purchases."),
                        UiText.T("تنبيه", "Notice"));
                    return;
                }

                var supplier = SupplierComboBox.SelectedItem as UserReadDto; // أو UserReadDto للمورد
                decimal totalAmount = InvoiceLines.Sum(l => l.LineTotal);
                if (!TryGetActiveCashierSession(out var session))
                    return;

                bool isUpdate = _currentInvoiceId != null;

                if (selectedPaymentType == PaymentType.Check)
                {
                    var currentCheckTotal = Math.Round(_currentChecks.Sum(check => check.Amount), 3);
                    var expectedCheckTotal = Math.Round(totalAmount, 3);

                    if (_currentChecks.Count == 0 || currentCheckTotal != expectedCheckTotal)
                    {
                        _loadingService.Hide();
                        loadingShown = false;
                        if (!EditCurrentInvoiceChecks(totalAmount))
                            return;
                    }
                }
                else if (_currentChecks.Any())
                {
                    _currentChecks.Clear();
                    UpdateChecksButtonVisibility();
                }

                if (selectedPaymentType == PaymentType.Check && !_currentChecks.Any())
                {
                    MessageBox.Show(
                        UiText.T("يرجى إدخال شيك واحد على الأقل.", "Please enter at least one check."),
                        UiText.T("تنبيه", "Notice"));
                    return;
                }

                if (selectedPaymentType == PaymentType.Check)
                {
                    var checkTotal = Math.Round(_currentChecks.Sum(check => check.Amount), 3);
                    if (checkTotal != Math.Round(totalAmount, 3))
                    {
                        MessageBox.Show(
                            UiText.T(
                                $"مجموع الشيكات ({checkTotal:0.###}) يجب أن يساوي إجمالي الفاتورة ({totalAmount:0.###}).",
                                $"The total check amount ({checkTotal:0.###}) must equal the invoice total ({totalAmount:0.###})."),
                            UiText.T("تنبيه", "Notice"));
                        return;
                    }
                }

                _loadingService.Show();
                loadingShown = true;

                var invoiceDto = new InvoiceWriteDto
                {
                    Id = _currentInvoiceId ?? 0,
                    InvoiceNumber = InvoiceNumberTextBox.Text,
                    SupplierId = supplier?.Id,
                    InvoiceType = InvoiceType.Purchase, // مهم
                    PaymentType = selectedPaymentType,
                    Checks = selectedPaymentType == PaymentType.Check ? _currentChecks : null,
                    TotalAmount = totalAmount,
                    CreatedDate = InvoiceDatePicker.SelectedDate.Value,
                    UpdatedDate = DateTime.Now,
                    InvoiceLines = InvoiceLines.ToList(),
                    CasherId = session.CashierId
                };

                int savedInvoiceId;

                if (!isUpdate)
                {
                    // ============ CREATE ============
                    var result = await _invoicesService.CreateAsync(invoiceDto);

                    if (!result.Success)
                    {
                        HideLoadingIfShown();
                        MessageBox.Show(result.Message ?? "فشل إنشاء فاتورة المشتريات", "خطأ");
                        return;
                    }

                    _currentInvoiceId = result.Data.Id;
                    savedInvoiceId = result.Data.Id;

                    var movementResult = await _stockService.PostMovementsAsync(
                        BuildInvoiceStockMovements(
                            InvoiceLines,
                            TransactionType.Purchase,
                            savedInvoiceId,
                            session.CashierId,
                            session.Id,
                            $"Purchase invoice #{invoiceDto.InvoiceNumber}",
                            1m));
                    if (!movementResult.Success)
                    {
                        HideLoadingIfShown();
                        MessageBox.Show(movementResult.Message ?? "فشل تحديث المخزون.", "خطأ");
                        return;
                    }

                    // ✅ POST financial (Purchase Invoice = OUT)
                    if (selectedPaymentType != PaymentType.Credit)
                    {
                        var postDto = new FinancialPostDto
                        {
                            Direction = TransactionDirection.Out,
                            Method = MapPaymentMethod(selectedPaymentType),
                            Amount = totalAmount,
                            TransactionDate = DateTime.Now,

                            SourceType = FinancialSourceType.PurchaseInvoice,
                            SourceId = savedInvoiceId,

                            CashierSessionId = session.Id,
                            CashierId = session.CashierId,

                            Notes = $"Purchase Invoice #{invoiceDto.InvoiceNumber}"
                        };

                        var postResult = await _financialService.PostAsync(postDto);
                        if (!postResult.Success)
                        {
                            HideLoadingIfShown();
                            MessageBox.Show(postResult.Message ?? "تم حفظ الفاتورة لكن فشل تسجيل الحركة المالية", "تحذير");
                            return;
                        }
                    }

                    HideLoadingIfShown();
                    MessageBox.Show(
                        selectedPaymentType == PaymentType.Credit
                            ? "تم إنشاء فاتورة مشتريات آجلة بنجاح ✅"
                            : "تم إنشاء فاتورة المشتريات وتسجيل الحركة المالية ✅");
                    UiText.ApplyTranslations(this);
                }
                else
                {
                    savedInvoiceId = _currentInvoiceId.Value;

                    // 0) Void old financial transactions
                    var voidResult = await _financialService.VoidBySourceAsync(
                        FinancialSourceType.PurchaseInvoice,
                        savedInvoiceId,
                        "Purchase invoice updated"
                    );

                    if (!voidResult.Success)
                    {
                        HideLoadingIfShown();
                        MessageBox.Show(voidResult.Message ?? "فشل إلغاء الحركة المالية السابقة", "خطأ");
                        return;
                    }

                    // 1) رجّع أثر الفاتورة القديمة من المخزون (يعني اعكسها)
                    var reverseResult = await _stockService.PostMovementsAsync(
                        BuildInvoiceStockMovements(
                            _originalLines.Select(line => new InvoiceLineWriteDto
                            {
                                ProductId = line.ProductId,
                                ProductUnitId = line.ProductUnitId,
                                Quantity = line.Quantity,
                                QuantityPerUnitSnapshot = line.QuantityPerUnitSnapshot,
                                BaseQuantity = line.BaseQuantity,
                                UnitPrice = line.UnitPrice,
                                UnitCost = line.UnitCost
                            }),
                            TransactionType.Purchase,
                            savedInvoiceId,
                            session.CashierId,
                            session.Id,
                            $"Reverse purchase invoice #{invoiceDto.InvoiceNumber}",
                            -1m));
                    if (!reverseResult.Success)
                    {
                        HideLoadingIfShown();
                        MessageBox.Show(reverseResult.Message ?? "فشل عكس حركة المخزون.", "خطأ");
                        return;
                    }
                    // (إذا القديم كان يضيف للمخزون، فالعكس خصم: مرّر كمية موجبة)

                    // 2) طبّق الفاتورة الجديدة على المخزون (إضافة)
                    var applyResult = await _stockService.PostMovementsAsync(
                        BuildInvoiceStockMovements(
                            InvoiceLines,
                            TransactionType.Purchase,
                            savedInvoiceId,
                            session.CashierId,
                            session.Id,
                            $"Update purchase invoice #{invoiceDto.InvoiceNumber}",
                            1m));
                    if (!applyResult.Success)
                    {
                        HideLoadingIfShown();
                        MessageBox.Show(applyResult.Message ?? "فشل تحديث حركة المخزون.", "خطأ");
                        return;
                    }

                    // 3) Update invoice
                    var result = await _invoicesService.UpdateAsync(invoiceDto);
                    if (!result.Success)
                    {
                        HideLoadingIfShown();
                        MessageBox.Show(result.Message ?? "فشل تحديث فاتورة المشتريات", "خطأ");
                        return;
                    }

                    // 4) Post new OUT transaction
                    if (selectedPaymentType != PaymentType.Credit)
                    {
                        var postDto = new FinancialPostDto
                        {
                            Direction = TransactionDirection.Out,
                            Method = MapPaymentMethod(selectedPaymentType),
                            Amount = totalAmount,
                            TransactionDate = DateTime.Now,

                            SourceType = FinancialSourceType.PurchaseInvoice,
                            SourceId = savedInvoiceId,

                            CashierSessionId = session.Id,
                            CashierId = session.CashierId,

                            Notes = $"Purchase Invoice UPDATED #{invoiceDto.InvoiceNumber}"
                        };

                        var postResult = await _financialService.PostAsync(postDto);
                        if (!postResult.Success)
                        {
                            HideLoadingIfShown();
                            MessageBox.Show(postResult.Message ?? "تم تحديث الفاتورة لكن فشل تسجيل الحركة المالية الجديدة", "تحذير");
                            return;
                        }
                    }

                    HideLoadingIfShown();
                    MessageBox.Show(
                        selectedPaymentType == PaymentType.Credit
                            ? "تم تحديث فاتورة المشتريات الآجلة بنجاح ✅"
                            : "تم تحديث فاتورة المشتريات وتسجيل الحركة المالية ✅");
                    UiText.ApplyTranslations(this);
                }
                PrintBtn.Visibility = Visibility.Visible;
                NewInvoiceBtn.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                var details = ex.Message;
                var inner = ex.InnerException;
                while (inner != null)
                {
                    details += Environment.NewLine + inner.Message;
                    inner = inner.InnerException;
                }

                HideLoadingIfShown();
                MessageBox.Show($"خطأ أثناء حفظ الفاتورة:\n{details}");
            }
            finally
            {
                HideLoadingIfShown();
            }
        }

        private PaymentType GetSelectedPaymentType()
        {
            if (PaymentMethodComboBox.SelectedItem is ComboBoxItem item &&
                int.TryParse(item.Tag?.ToString(), out var value))
            {
                return (PaymentType)value;
            }

            return PaymentType.Cash;
        }

        private void SetSelectedPaymentType(PaymentType paymentType)
        {
            foreach (var item in PaymentMethodComboBox.Items.OfType<ComboBoxItem>())
            {
                if (int.TryParse(item.Tag?.ToString(), out var value) && (PaymentType)value == paymentType)
                {
                    PaymentMethodComboBox.SelectedItem = item;
                    return;
                }
            }

            PaymentMethodComboBox.SelectedIndex = 0;
        }

        private PaymentMethod MapPaymentMethod(PaymentType paymentType)
        {
            return paymentType switch
            {
                PaymentType.Cash => PaymentMethod.Cash,
                PaymentType.Debit => PaymentMethod.BankTransfer,
                PaymentType.Check => PaymentMethod.Check,
                PaymentType.MobilePayment => PaymentMethod.MobilePayment,
                PaymentType.Credit => PaymentMethod.Credit,
                PaymentType.Master => PaymentMethod.Master,
                PaymentType.Visa => PaymentMethod.Visa,
                _ => PaymentMethod.Cash
            };
        }

        private async void PrintBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentInvoiceId == null)
            {
                MessageBox.Show(UiText.T("لا توجد فاتورة للطباعة.", "There is no invoice to print."));
                return;
            }

            var invoice = await _invoicesService.GetFullInvoiceByIdAsync(_currentInvoiceId.Value);

            if (invoice == null)
            {
                MessageBox.Show(UiText.T("الفاتورة غير موجودة.", "The invoice was not found."), UiText.T("خطأ", "Error"));
                return;
            }

            SavePurchaseInvoicePdf(invoice);
        }

        private void SavePurchaseInvoicePdf(InvoiceReadDto invoice)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF File (*.pdf)|*.pdf",
                FileName = $"PurchaseInvoice_{invoice.InvoiceNumber}.pdf"
            };

            if (dialog.ShowDialog() == true)
            {
                var path = dialog.FileName;

                // نفس SalesInvoice لكن عنوان "فاتورة مشتريات" داخل PdfGenerator
                PdfGenerator.PurchaseInvoice(invoice, path);

                MessageBox.Show(UiText.T("تم حفظ ملف PDF بنجاح.", "The PDF file was saved successfully."), UiText.T("تم الحفظ", "Saved"), MessageBoxButton.OK, MessageBoxImage.Information);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
        }

        private void SearchInvoiceBtn_Click(object sender, RoutedEventArgs e)
        {
            // تقدر تعمل نفس SearchSalesInvoiceWindow لكن للمشتريات
            var searchWindow = new SearchSalesInvoiceWindow(_invoicesService,false)
            {
                Owner = this
            };

            if (searchWindow.ShowDialog() == true)
            {
                LoadSelectedInvoice(searchWindow.Result);
            }
        }

        private void LoadSelectedInvoice(InvoiceReadDto invoice)
        {
            if (invoice == null) return;

            _currentInvoiceId = invoice.Id;

            _originalLines = invoice.InvoiceLines.ToList();   // 🔥 مهم جداً

            InvoiceNumberTextBox.Text = invoice.InvoiceNumber;
            InvoiceDatePicker.SelectedDate = invoice.CreatedDate;

            SupplierComboBox.SelectedItem =
                _allSuppliers.FirstOrDefault(c => c.Id == invoice.SupplierId);
            if (invoice.PaymentType.HasValue)
                SetSelectedPaymentType(invoice.PaymentType.Value);
            _currentChecks = CloneChecks(invoice.Checks);
            UpdateChecksButtonVisibility();
            UiText.ApplyTranslations(PaymentMethodComboBox);

            InvoiceLines.Clear();

            foreach (var line in invoice.InvoiceLines)
            {
                InvoiceLines.Add(new InvoiceLineWriteDto
                {
                    Id = line.Id,
                    ProductId = line.ProductId,
                    ProductName = line.Product?.Name,
                    ProductUnitId = line.ProductUnitId,
                    QuantityPerUnitSnapshot = line.QuantityPerUnitSnapshot,
                    BaseQuantity = line.BaseQuantity,
                    UnitName = line.ProductUnit?.Unit?.Name,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    UnitCost = line.UnitPrice,
                    ExpiryDate = line.ExpiryDate,
                    CreatedDate = line.CreatedDate,
                    UpdatedDate = line.UpdatedDate
                });
            }

            UpdateTotal();
            UiText.ApplyTranslations(ProductsGrid);
            UpdateChecksButtonVisibility();

            PrintBtn.Visibility = Visibility.Visible;
            NewInvoiceBtn.Visibility = Visibility.Visible;
        }

        private void NewInvoiceBtn_Click(object sender, RoutedEventArgs e)
        {
            _currentInvoiceId = null;
            _originalLines.Clear();

            InvoiceLines.Clear();
            ProductsGrid.Items.Refresh();

            InvoiceNumberTextBox.Text = GenerateInvoiceNumber();
            SupplierComboBox.SelectedIndex = -1;
            InvoiceDatePicker.SelectedDate = DateTime.Now;
            _currentChecks.Clear();
            UpdateChecksButtonVisibility();

            TotalAmountTextBox.Text = "0";
            PrintBtn.Visibility = Visibility.Collapsed;
            NewInvoiceBtn.Visibility = Visibility.Collapsed;
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ClearProductBtn_Click(object sender, RoutedEventArgs e)
        {
            ProductBox.Text = "";
            ProductBox.SelectedIndex = -1;
            ProductBox.ItemsSource = Products;

            UnitBox.ItemsSource = null;

            QtyBox.Text = "";
            PurchaseBox.Text = "";
            SaleBox.Text = "";
            ExpiryBox.SelectedDate = null;

            ProductBox.IsDropDownOpen = false;
        }

        private void ProductBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            _productSearchText = ProductBox.Text + e.Text;
            FilterProducts(_productSearchText);
        }

        private void ProductBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key is not (Key.Back or Key.Delete))
                return;

            _productSearchText = ProductBox.Text ?? string.Empty;
            FilterProducts(_productSearchText);
        }

        private void SearchProductBtn_Click(object sender, RoutedEventArgs e)
        {
            FilterProducts();
        }

        private void FilterProducts(string? searchText = null)
        {
            string search = (searchText ?? ProductBox.Text)?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(search))
            {
                ProductBox.ItemsSource = null;
                ProductBox.IsDropDownOpen = false;
                return;
            }

            var filtered = Products.Where(p =>
                (!string.IsNullOrEmpty(p.Name) &&
                 p.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                ||
                p.ITEMCODE.ToString().Contains(search)
            ).ToList();

            ProductBox.ItemsSource = filtered;
            ProductBox.IsDropDownOpen = true;

            var exactMatch = long.TryParse(search, out var barcode)
                ? filtered.FirstOrDefault(product => product.ITEMCODE == barcode)
                : null;

            if (exactMatch != null)
            {
                ProductBox.SelectedItem = exactMatch;
                QueueProductSelection(exactMatch.Id);
            }
        }

        private static bool TryParseDecimalInput(string? text, out decimal value)
        {
            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
                || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }
    }
}

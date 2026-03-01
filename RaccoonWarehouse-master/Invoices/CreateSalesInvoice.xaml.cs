using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.ProductUnits;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Cashiers.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.FinancialTransactions.DTOs;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using RaccoonWarehouse.Domain.Stock;
using RaccoonWarehouse.Domain.Users.DTOs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace RaccoonWarehouse.Invoices
{
    public partial class CreateSalesInvoice : Window
    {
        // ====== مجموعات للـ Binding ======
        public ObservableCollection<ProductReadDto> Products { get; set; } = new();
        private Dictionary<StockItemWriteDto, int> _itemUnits = new();

        public ObservableCollection<InvoiceLineWriteDto> InvoiceLines { get; set; } = new();

        private ObservableCollection<UserReadDto> _allCustomers;
        private List<InvoiceLineReadDto> _originalLines = new(); // to restore stock on update


        private readonly IInvoiceService _invoicesService;
        private readonly IUserService _userService;
        private readonly IProductService _productService;
        private readonly IProductUnitService _productUnitService;
        private readonly IStockService _stockService;
        private readonly IFinancialTransactionService _financialService; // لو عندك خدمة مالية 
        private readonly IUserSession _userSession; // لو عندك جلسة مستخدم

        private bool _isLoadingUnits = false;
        private int? _currentInvoiceId = null;   // لتحديث الفاتورة بعد الحفظ الأول

        public CreateSalesInvoice(
            IStockService stockService,
            IInvoiceService invoiceService,
            IUserService userService,
            IProductService productService,
            IProductUnitService productUnitService,
            IUserSession userSession,
            IFinancialTransactionService
            financialService)
        {
            _stockService = stockService;
            _productService = productService;
            _productUnitService = productUnitService;
            _invoicesService = invoiceService;
            _userService = userService;
            _userSession = userSession;
            _financialService = financialService;

            InitializeComponent();

            DataContext = this;

            // رقم الفاتورة
            InvoiceNumberTextBox.Text = GenerateInvoiceNumber();

            // ربط الـ Grid
            ProductsGrid.ItemsSource = InvoiceLines;

            Loaded += CreateSalesInvoice_Loaded;
            _userSession = userSession;
        }

        private string GenerateInvoiceNumber()
        {
            string prefix = "INV";
            string datePart = DateTime.Now.ToString("yyyyMMddHHmmss");
            return $"{prefix}-{datePart}";
        }

        // ===================== LOAD DATA =====================
        private async void CreateSalesInvoice_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // العملاء
                var result = await _userService.GetAllAsync();
                _allCustomers = new ObservableCollection<UserReadDto>(result?.Data ?? new List<UserReadDto>());
                CustomerComboBox.ItemsSource = _allCustomers;
                CustomerComboBox.SelectedIndex = -1;

                InvoiceDatePicker.SelectedDate = DateTime.Now;

                PaymentMethodComboBox.ItemsSource = new string[] { "نقدي", "آجل" };
                PaymentMethodComboBox.SelectedIndex = 0;

                await LoadProductsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تحميل البيانات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                var stockedProducts = await _stockService.GetAllWithFilteringAndIncludeAsync(
                            s => s.Quantity > 10,    // 🔥 نفس StockOut
                            new Expression<Func<Stock, object>>[]
                            {
                        s => s.Product,
                        s => s.Product.SubCategory,
                        s => s.Product.Brand,
                        s => s.Product.ProductUnits
                            });

                Products.Clear();

                foreach (var stock in stockedProducts.Data)
                {
                    if (stock.Product != null)
                        Products.Add(stock.Product);
                }

                ProductBox.ItemsSource = Products;
                ProductBox.DisplayMemberPath = "Name";
                ProductBox.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ عند تحميل المنتجات: {ex.Message}", "خطأ");
            }
        }
        private bool TryGetActiveCashierSession(out CashierSessionReadDto? session)
        {
            session = _userSession.CurrentCashierSession;
            if (session != null)
                return true;

            MessageBox.Show("لا توجد جلسة كاشير مفتوحة. الرجاء فتح جلسة أولاً.", "خطأ");
            return false;
        }
        private async Task UpdateStockQuantity(int productId, int productUnitId, decimal quantity)
        {
            try
            {
                var existingStock = await _stockService.GetAllWriteDtoWithFilteringAndIncludeAsync(
                    s => s.ProductId == productId && s.ProductUnitId == productUnitId);

                if (existingStock.Data?.Count > 0)
                {
                    var stock = existingStock.Data.First();

                    if (stock.Quantity >= quantity)
                    {
                        stock.Quantity -= quantity;  // 👈 طرح الكمية
                        stock.UpdatedDate = DateTime.Now;
                        await _stockService.UpdateAsync(stock);
                    }
                    else
                    {
                        throw new InvalidOperationException("الكمية غير متوفرة في المخزون.");
                    }
                }
                else
                {
                    MessageBox.Show("المنتج غير موجود في المخزون.", "خطأ");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ عند تحديث المخزون: {ex.Message}");
                return;
            }
        }



        // ===================== CUSTOMER SEARCH =====================
        private void CustomerComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            CustomerComboBox.DisplayMemberPath = "Name";
            CustomerComboBox.SelectedValuePath = "Id";
        }

        private void CustomerComboBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            FilterCustomerList(CustomerComboBox.Text + e.Text);
        }

        private void CustomerComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back || e.Key == Key.Delete)
                FilterCustomerList(CustomerComboBox.Text);
        }

        private void FilterCustomerList(string text)
        {
            if (_allCustomers == null) return;

            var filtered = _allCustomers
                .Where(c => c.Name != null &&
                            c.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
                .ToList();

            CustomerComboBox.ItemsSource = filtered;
            CustomerComboBox.IsDropDownOpen = true;
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
                    var firstUnit = item.Units.FirstOrDefault();
                    if (firstUnit != null)
                    {
                        item.ProductUnitId = firstUnit.Id;
                        item.PurchasePrice = firstUnit.PurchasePrice;
                        item.SalePrice = firstUnit.SalePrice;

                        // Map the selected unit for saving
                        _itemUnits[item] = firstUnit.Id;
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
                MessageBox.Show($"حدث خطأ أثناء تحديث الوحدات: {ex.Message}",
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearProductInputs()
        {
            ProductBox.SelectedIndex = -1;
            UnitBox.ItemsSource = null;
            QtyBox.Text = "";
         
            ExpiryBox.SelectedDate = null;
        }
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
                MessageBox.Show($"خطأ أثناء تحميل الوحدات: {ex.Message}", "خطأ");
            }
            finally
            {
                _isLoadingUnits = false;
            }
        }

        // ===================== ADD PRODUCT LINE =====================
        /*private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            if (ProductBox.SelectedItem is not ProductReadDto product)
            {
                MessageBox.Show("يرجى اختيار منتج.", "تنبيه");
                return;
            }

            if (UnitBox.SelectedItem is not ProductUnitWriteDto unit)
            {
                MessageBox.Show("يرجى اختيار وحدة.", "تنبيه");
                return;
            }

            if (!decimal.TryParse(QtyBox.Text, out decimal qty) || qty <= 0)
            {
                MessageBox.Show("الكمية غير صحيحة.", "تنبيه");
                return;
            }

            // 🔥 إضافة منتج مع كل البيانات (نفس StockOut)
            var line = new InvoiceLineWriteDto
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductUnitId = unit.Id,
                UnitName = unit.Unit?.Name,
                Quantity = qty,
                UnitPrice = unit.SalePrice,
                ExpiryDate = ExpiryBox.SelectedDate ?? DateTime.Now.AddMonths(6),
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            InvoiceLines.Add(line);
            UpdateTotal();
            ClearProductInputs();
        }*/
        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            if (ProductBox.SelectedItem is not ProductReadDto product)
            {
                MessageBox.Show("يرجى اختيار منتج.", "تنبيه");
                return;
            }

            if (UnitBox.SelectedItem is not ProductUnitWriteDto unit)
            {
                MessageBox.Show("يرجى اختيار وحدة.", "تنبيه");
                return;
            }

            if (!decimal.TryParse(QtyBox.Text, out decimal qty) || qty <= 0)
            {
                MessageBox.Show("الكمية غير صحيحة.", "تنبيه");
                return;
            }

            // ✅ ADD: Snapshot tax info from product at time of invoice
            bool taxExempt = product.TaxExempt ?? false;
            decimal taxRate = taxExempt ? 0m : (product.TaxRate ?? 0m);
            decimal unitPrice = unit.SalePrice;
            decimal lineTotal = qty * unitPrice;
            decimal divisor = 1m + (taxRate / 100m);
            decimal lineSubTotal = taxExempt || divisor <= 0m
                ? lineTotal
                : Math.Round(lineTotal / divisor, 3);
            decimal taxAmount = Math.Round(lineTotal - lineSubTotal, 3);

            var line = new InvoiceLineWriteDto
            {
                ProductId = product.Id,
                ProductName = product.Name,

                ProductUnitId = unit.Id,
                UnitName = unit.Unit?.Name,

                Quantity = qty,

                // ✅ sale price stored on product already includes tax
                UnitPrice = unitPrice,

                // ✅ ADD: store purchase cost used (snapshot)
                UnitCost = unit.PurchasePrice,

                // ✅ ADD: tax snapshot fields
                TaxExempt = taxExempt,
                TaxRate = taxRate,
                TaxAmount = taxAmount,

                // ✅ ADD: store subtotal before tax
                LineSubTotal = lineSubTotal,

                ExpiryDate = ExpiryBox.SelectedDate ?? DateTime.Now.AddMonths(6),
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            InvoiceLines.Add(line);
            UpdateTotals();   // ✅ ADD: new totals method
            ClearProductInputs();
        }


        private void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is InvoiceLineWriteDto line)
            {
                InvoiceLines.Remove(line);
                UpdateTotals();
            }
        }

        /* private void UpdateTotal()
         {
             TotalAmountTextBox.Text =
                 InvoiceLines.Sum(x => x.LineTotal).ToString("0.###");
         }
            */
        // ✅ ADD: Calculates invoice summary fields needed for reports
        private void UpdateTotals()
        {
            decimal subTotal = InvoiceLines.Sum(l => l.LineSubTotal);   // قبل الضريبة
            decimal taxTotal = InvoiceLines.Sum(l => l.TaxAmount);      // الضريبة
            decimal grossSales = InvoiceLines.Sum(l => l.Quantity * l.UnitPrice);
            decimal discount = 0m;

            // ✅ ADD: (optional) if you later add Discount UI textbox
            // decimal.TryParse(DiscountTextBox.Text, out discount);

            decimal netTotal = grossSales - discount;

            // ✅ Existing UI field shows final
            TotalAmountTextBox.Text = netTotal.ToString("0.###");

            // ✅ Optional: if you want show subtotal/tax in UI, bind to labels/textboxes
             SubTotalTextBox.Text = subTotal.ToString("0.###");
             TaxTotalTextBox.Text = taxTotal.ToString("0.###");
        }

        private async void SaveReceiptBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!InvoiceLines.Any())
                {
                    MessageBox.Show("يرجى إضافة منتج واحد على الأقل.", "تنبيه");
                    return;
                }

                if (CustomerComboBox.SelectedItem == null)
                {
                    MessageBox.Show("❌ يرجى اختيار الزبون.", "تنبيه");
                    return;
                }

                var customer = CustomerComboBox.SelectedItem as UserReadDto;
                //decimal totalAmount = InvoiceLines.Sum(l => l.LineTotal);
                // ✅ ADD: invoice totals required for reporting
                decimal subTotal = InvoiceLines.Sum(l => l.LineSubTotal);
                decimal totalTax = InvoiceLines.Sum(l => l.TaxAmount);
                decimal grossSales = InvoiceLines.Sum(l => l.Quantity * l.UnitPrice);
                decimal discount = 0m; // ✅ ADD: later from UI if needed

                decimal totalAmount = grossSales - discount;
                bool isUpdate = _currentInvoiceId != null;
                if (!TryGetActiveCashierSession(out var session))
                    return;

                var invoiceDto = new InvoiceWriteDto
                {
                    Id = _currentInvoiceId ?? 0,
                    InvoiceNumber = InvoiceNumberTextBox.Text,
                    CustomerId = customer?.Id,
                    InvoiceType = InvoiceType.Sale,
                    TotalAmount = totalAmount,
                    CreatedDate = InvoiceDatePicker.SelectedDate.Value,
                    UpdatedDate = DateTime.Now,
                    InvoiceLines = InvoiceLines.ToList(),
                    CasherId = session.CashierId,
                    SubTotal = subTotal,        // ✅ ADD
                    TotalTax = totalTax,        // ✅ ADD
                    DiscountAmount = discount,  // ✅ ADD (or keep your existing)
                    Status = InvoiceStatus.Completed
                };


                int savedInvoiceId;

                if (!isUpdate)
                {
                    // ============ CREATE ============
                    var result = await _invoicesService.CreateAsync(invoiceDto);

                    if (!result.Success)
                    {
                        MessageBox.Show(result.Message ?? "فشل إنشاء الفاتورة", "خطأ");
                        return;
                    }

                    _currentInvoiceId = result.Data.Id;
                    savedInvoiceId = result.Data.Id;

                    // 🔥 طرح الكميات من المخزون
                    foreach (var line in InvoiceLines)
                        await UpdateStockQuantity(line.ProductId, line.ProductUnitId, line.Quantity);

                    // ✅ POST financial (Sale Invoice)
                    var postDto = new FinancialPostDto
                    {
                        Direction = TransactionDirection.In,
                        Method = PaymentMethod.Cash, // أو MapPaymentMethod(selectedPaymentType)
                        Amount = totalAmount,
                        TransactionDate = DateTime.Now,

                        SourceType = FinancialSourceType.SaleInvoice,
                        SourceId = savedInvoiceId,

                        CashierSessionId = session.Id,
                        CashierId = session.CashierId,

                        Notes = $"Sale Invoice #{invoiceDto.InvoiceNumber}"
                    };

                    var postResult = await _financialService.PostAsync(postDto);
                    if (!postResult.Success)
                    {
                        MessageBox.Show(postResult.Message ?? "تم حفظ الفاتورة لكن فشل تسجيل الحركة المالية", "تحذير");
                        return;
                    }

                    MessageBox.Show("تم إنشاء الفاتورة وتسجيل الحركة المالية ✅");
                }
                else
                {
                    savedInvoiceId = _currentInvoiceId.Value;

                    // ============ UPDATE ============

                    // 0) Void old financial transactions for this invoice
                    var voidResult = await _financialService.VoidBySourceAsync(
                        FinancialSourceType.SaleInvoice,
                        savedInvoiceId,
                        "Invoice updated"
                    );

                    if (!voidResult.Success)
                    {
                        MessageBox.Show(voidResult.Message ?? "فشل إلغاء الحركة المالية السابقة", "خطأ");
                        return;
                    }

                    // 1️⃣ إعادة كميات الفاتورة القديمة إلى المخزون
                    foreach (var old in _originalLines)
                        await UpdateStockQuantity(old.ProductId, old.ProductUnitId, -old.Quantity);

                    // 2️⃣ طرح كميات الفاتورة الجديدة
                    foreach (var line in InvoiceLines)
                        await UpdateStockQuantity(line.ProductId, line.ProductUnitId, line.Quantity);

                    // 3️⃣ Update invoice
                    var result = await _invoicesService.UpdateAsync(invoiceDto);

                    if (!result.Success)
                    {
                        MessageBox.Show(result.Message ?? "فشل تحديث الفاتورة", "خطأ");
                        return;
                    }

                    // 4️⃣ Post new financial transaction with new amount
                    var postDto = new FinancialPostDto
                    {
                        Direction = TransactionDirection.In,
                        Method = PaymentMethod.Cash, // أو MapPaymentMethod(selectedPaymentType)
                        Amount = totalAmount,
                        TransactionDate = DateTime.Now,

                        SourceType = FinancialSourceType.SaleInvoice,
                        SourceId = savedInvoiceId,

                        CashierSessionId = session.Id,
                        CashierId = session.CashierId,

                        Notes = $"Sale Invoice UPDATED #{invoiceDto.InvoiceNumber}"
                    };

                    var postResult = await _financialService.PostAsync(postDto);
                    if (!postResult.Success)
                    {
                        MessageBox.Show(postResult.Message ?? "تم تحديث الفاتورة لكن فشل تسجيل الحركة المالية الجديدة", "تحذير");
                        return;
                    }

                    MessageBox.Show("تم تحديث الفاتورة وتسجيل الحركة المالية ✅");
                }

                PrintBtn.Visibility = Visibility.Visible;
                NewInvoiceBtn.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء حفظ الفاتورة:\n{ex.Message}");
            }
        }
        private PaymentMethod MapPaymentMethod(PaymentType paymentType)
        {
            return paymentType switch
            {
                PaymentType.Cash => PaymentMethod.Cash,
                PaymentType.Visa => PaymentMethod.Visa,
                PaymentType.Master => PaymentMethod.Master,
                PaymentType.Credit => PaymentMethod.Credit,
                _ => PaymentMethod.Cash
            };
        }


        /*// ===================== SAVE / UPDATE / PRINT =====================
        private async void SaveReceiptBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!InvoiceLines.Any())
                {
                    MessageBox.Show("يرجى إضافة منتج واحد على الأقل.", "تنبيه");
                    return;
                }

                if (CustomerComboBox.SelectedItem == null)
                {
                    MessageBox.Show("❌ يرجى اختيار الزبون.", "تنبيه");
                    return;
                }

                var customer = CustomerComboBox.SelectedItem as UserReadDto;
                decimal totalAmount = InvoiceLines.Sum(l => l.LineTotal);

                bool isUpdate = _currentInvoiceId != null;

                var invoiceDto = new InvoiceWriteDto
                {
                    Id = _currentInvoiceId ?? 0,
                    InvoiceNumber = InvoiceNumberTextBox.Text,
                    CustomerId = customer?.Id,
                    InvoiceType = InvoiceType.Sale,
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

                        // 🔥 طرح الكميات من المخزون
                        foreach (var line in InvoiceLines)
                            await UpdateStockQuantity(line.ProductId, line.ProductUnitId, line.Quantity);

                        MessageBox.Show("تم إنشاء الفاتورة بنجاح!");
                    }
                }
                else
                {
                    // ============ UPDATE ============

                    // 1️⃣ إعادة كميات الفاتورة القديمة إلى المخزون
                    foreach (var old in _originalLines)
                        await UpdateStockQuantity(old.ProductId, old.ProductUnitId, -old.Quantity);

                    // 2️⃣ طرح كميات الفاتورة الجديدة
                    foreach (var line in InvoiceLines)
                        await UpdateStockQuantity(line.ProductId, line.ProductUnitId, line.Quantity);

                    var result = await _invoicesService.UpdateAsync(invoiceDto);

                    if (result.Success)
                        MessageBox.Show("تم تحديث الفاتورة بنجاح!");
                }

                PrintBtn.Visibility = Visibility.Visible;
                NewInvoiceBtn.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء حفظ الفاتورة:\n{ex.Message}");
            }
        }


     
        }*/
        private async void UpdateInvoiceBtn_Click(object sender, RoutedEventArgs e)
        {

            SaveReceiptBtn_Click(sender, e);

            /* if (_currentInvoiceId == null)
            {
                MessageBox.Show("لا توجد فاتورة محفوظة لتحديثها.", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (!InvoiceLines.Any())
                {
                    MessageBox.Show("يرجى إضافة منتج واحد على الأقل.", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var customer = CustomerComboBox.SelectedItem as UserReadDto;

                decimal totalAmount = InvoiceLines.Sum(l => l.LineTotal);

                var invoiceDto = new InvoiceWriteDto
                {
                    Id = _currentInvoiceId.Value,
                    InvoiceType = InvoiceType.Sale,
                    CustomerId = customer?.Id,
                    CasherId = null,
                    VoucherId = null,
                    TotalAmount = totalAmount,
                    CreatedDate = InvoiceDatePicker.SelectedDate ?? DateTime.Now,
                    InvoiceLines = InvoiceLines.ToList(),
                    UpdatedDate = DateTime.Now
                };

                var result = await _invoicesService.UpdateAsync(invoiceDto);

                if (result.Success)
                {
                    MessageBox.Show("✅ تم تحديث الفاتورة بنجاح!", "نجاح",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"❌ فشل تحديث الفاتورة: {result.Message}", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ حدث خطأ أثناء تحديث الفاتورة:\n{ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }*/
        }
        private async void PrintBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentInvoiceId == null)
            {
                MessageBox.Show("لا توجد فاتورة للطباعة.");
                return;
            }

            var invoice = await _invoicesService.GetFullInvoiceByIdAsync(_currentInvoiceId.Value);

            if (invoice == null)
            {
                MessageBox.Show("الفاتورة غير موجودة.", "خطأ");
                return;
            }

            SaveSalesInvoicePdf(invoice);
        }



        private void SaveSalesInvoicePdf(InvoiceReadDto invoice)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF File (*.pdf)|*.pdf",
                FileName = $"Invoice_{invoice.InvoiceNumber}.pdf"
            };

            if (dialog.ShowDialog() == true)
            {
                var path = dialog.FileName;

                PdfGenerator.SalesInvoice(invoice, path);

                MessageBox.Show("تم حفظ ملف PDF بنجاح.", "تم الحفظ", MessageBoxButton.OK, MessageBoxImage.Information);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
        }


        private void SearchInvoiceBtn_Click(object sender, RoutedEventArgs e)
        {
            var searchWindow = new SearchSalesInvoiceWindow(_invoicesService,true)
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

            CustomerComboBox.SelectedItem =
                _allCustomers.FirstOrDefault(c => c.Id == invoice.CustomerId);

            InvoiceLines.Clear();

            foreach (var line in invoice.InvoiceLines)
            {
                InvoiceLines.Add(new InvoiceLineWriteDto
                {
                    Id = line.Id,
                    ProductId = line.ProductId,
                    ProductName = line.Product?.Name,
                    ProductUnitId = line.ProductUnitId,
                    UnitName = line.ProductUnit?.Unit?.Name,
                    Quantity = line.Quantity,
                    ExpiryDate = line.ExpiryDate,
                    CreatedDate = line.CreatedDate,
                    UpdatedDate = line.UpdatedDate,
                    UnitPrice = line.UnitPrice,
                    UnitCost = line.UnitCost,          // ✅ ADD
                    TaxExempt = line.TaxExempt,        // ✅ ADD
                    TaxRate = line.TaxRate,            // ✅ ADD
                    TaxAmount = line.TaxAmount,        // ✅ ADD
                    LineSubTotal = line.LineSubTotal,  // ✅ ADD

                });
            }

            UpdateTotals();

            PrintBtn.Visibility = Visibility.Visible;
        }

        private void NewInvoiceBtn_Click(object sender, RoutedEventArgs e)
        {
            _currentInvoiceId = null;
            _originalLines.Clear();

            InvoiceLines.Clear();
            ProductsGrid.Items.Refresh();

            InvoiceNumberTextBox.Text = GenerateInvoiceNumber();
            CustomerComboBox.SelectedIndex = -1;
            InvoiceDatePicker.SelectedDate = DateTime.Now;

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
          
            ExpiryBox.SelectedDate = null;

            ProductBox.IsDropDownOpen = false;
        }


        private void SearchProductBtn_Click(object sender, RoutedEventArgs e)
        {
            string search = ProductBox.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(search))
            {
                ProductBox.ItemsSource = Products;
                ProductBox.IsDropDownOpen = true;
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

            if (filtered.Count == 1)
                ProductBox.SelectedItem = filtered.First();
        }



    }
}

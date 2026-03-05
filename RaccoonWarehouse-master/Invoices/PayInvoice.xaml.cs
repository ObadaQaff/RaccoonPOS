#region Usings
using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.ProductUnits;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.StockTransactions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Cashiers.DTOs;
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
#endregion
namespace RaccoonWarehouse.Invoices
{
    public partial class PayInvoice : Window
    {
        // ====== مجموعات للـ Binding ======
        public ObservableCollection<ProductReadDto> Products { get; set; } = new();
        public ObservableCollection<InvoiceLineWriteDto> InvoiceLines { get; set; } = new();

        private ObservableCollection<UserReadDto> _allSuppliers;
        private List<InvoiceLineReadDto> _originalLines = new(); // to restore stock on update

        private readonly IInvoiceService _invoicesService;
        private readonly IUserService _userService;
        private readonly IProductService _productService;
        private readonly IProductUnitService _productUnitService;
        private readonly IStockService _stockService;
        private readonly IStockTransactionService _stockTransactionService;
        private readonly IFinancialTransactionService _financialService;
        private readonly IUserSession _userSession;
        private bool _isLoadingUnits = false;
        private int? _currentInvoiceId = null;   // لتحديث الفاتورة بعد الحفظ الأول

        public PayInvoice(
            IStockService stockService,
            IInvoiceService invoiceService,
            IUserService userService,
            IProductService productService,
            IProductUnitService productUnitService,
            IUserSession userSession,
            IStockTransactionService stockTransactionService,
            IFinancialTransactionService financialService)
        {
            _stockService = stockService;
            _productService = productService;
            _productUnitService = productUnitService;
            _stockTransactionService = stockTransactionService;
            _invoicesService = invoiceService;
            _userService = userService;
            _userSession = userSession;
            _financialService = financialService;
            _isLoadingUnits = false;

            InitializeComponent();

            DataContext = this;

            // رقم الفاتورة
            InvoiceNumberTextBox.Text = GenerateInvoiceNumber();

            // ربط الـ Grid
            ProductsGrid.ItemsSource = InvoiceLines;

            Loaded += PayInvoice_Loaded;
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
            try
            {
                // المورّدين (نفس users لكن أنت تختار اللي يمثل الموردين)
                var result = await _userService.GetAllAsync();
                _allSuppliers = new ObservableCollection<UserReadDto>(result?.Data ?? new List<UserReadDto>());
                SupplierComboBox.ItemsSource = _allSuppliers;
                SupplierComboBox.SelectedIndex = -1;

                InvoiceDatePicker.SelectedDate = DateTime.Now;

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

                    return new StockMovementPostDto
                    {
                        ProductId = line.ProductId,
                        ProductUnitId = line.ProductUnitId,
                        Quantity = line.Quantity * multiplier,
                        QuantityPerUnitSnapshot = quantityPerUnit,
                        BaseQuantity = baseQuantity * multiplier,
                        UnitPrice = line.UnitPrice,
                        TransactionType = transactionType,
                        InvoiceId = invoiceId,
                        CasherId = cashierId,
                        CashierSessionId = cashierSessionId,
                        TransactionDate = DateTime.Now,
                        Notes = notes
                    };
                });
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
                MessageBox.Show($"خطأ أثناء تحميل الوحدات: {ex.Message}", "خطأ");
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

        // ===================== ADD PRODUCT LINE =====================
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
                UnitPrice = decimal.TryParse(PurchaseBox.Text,out var p) ? p : 0,  // 👈 حسب اختيارك (A)
                ExpiryDate = ExpiryBox.SelectedDate ?? DateTime.Now.AddMonths(6),
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
            try
            {
                if (!InvoiceLines.Any())
                {
                    MessageBox.Show("يرجى إضافة منتج واحد على الأقل.", "تنبيه");
                    return;
                }

                if (SupplierComboBox.SelectedItem == null) // أو CustomerComboBox إذا نفس الاسم عندك
                {
                    MessageBox.Show("❌ يرجى اختيار المورد.", "تنبيه");
                    return;
                }

                var supplier = SupplierComboBox.SelectedItem as UserReadDto; // أو UserReadDto للمورد
                var selectedPaymentType = GetSelectedPaymentType();
                decimal totalAmount = InvoiceLines.Sum(l => l.LineTotal);
                if (!TryGetActiveCashierSession(out var session))
                    return;

                bool isUpdate = _currentInvoiceId != null;

                var invoiceDto = new InvoiceWriteDto
                {
                    Id = _currentInvoiceId ?? 0,
                    InvoiceNumber = InvoiceNumberTextBox.Text,
                    CustomerId = supplier?.Id,          // إذا عندك SupplierId الأفضل تستخدمه بدل CustomerId
                    InvoiceType = InvoiceType.Purchase, // مهم
                    PaymentType = selectedPaymentType,
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
                            MessageBox.Show(postResult.Message ?? "تم حفظ الفاتورة لكن فشل تسجيل الحركة المالية", "تحذير");
                            return;
                        }
                    }

                    MessageBox.Show(
                        selectedPaymentType == PaymentType.Credit
                            ? "تم إنشاء فاتورة مشتريات آجلة بنجاح ✅"
                            : "تم إنشاء فاتورة المشتريات وتسجيل الحركة المالية ✅");
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
                                UnitPrice = line.UnitPrice
                            }),
                            TransactionType.Purchase,
                            savedInvoiceId,
                            session.CashierId,
                            session.Id,
                            $"Reverse purchase invoice #{invoiceDto.InvoiceNumber}",
                            -1m));
                    if (!reverseResult.Success)
                    {
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
                        MessageBox.Show(applyResult.Message ?? "فشل تحديث حركة المخزون.", "خطأ");
                        return;
                    }

                    // 3) Update invoice
                    var result = await _invoicesService.UpdateAsync(invoiceDto);
                    if (!result.Success)
                    {
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
                            MessageBox.Show(postResult.Message ?? "تم تحديث الفاتورة لكن فشل تسجيل الحركة المالية الجديدة", "تحذير");
                            return;
                        }
                    }

                    MessageBox.Show(
                        selectedPaymentType == PaymentType.Credit
                            ? "تم تحديث فاتورة المشتريات الآجلة بنجاح ✅"
                            : "تم تحديث فاتورة المشتريات وتسجيل الحركة المالية ✅");
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

                MessageBox.Show($"خطأ أثناء حفظ الفاتورة:\n{details}");
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
                MessageBox.Show("لا توجد فاتورة للطباعة.");
                return;
            }

            var invoice = await _invoicesService.GetFullInvoiceByIdAsync(_currentInvoiceId.Value);

            if (invoice == null)
            {
                MessageBox.Show("الفاتورة غير موجودة.", "خطأ");
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
                    ExpiryDate = line.ExpiryDate,
                    CreatedDate = line.CreatedDate,
                    UpdatedDate = line.UpdatedDate
                });
            }

            UpdateTotal();

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

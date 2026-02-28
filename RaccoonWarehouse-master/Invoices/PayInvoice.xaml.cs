#region Usings
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
            IFinancialTransactionService financialService)
        {
            _stockService = stockService;
            _productService = productService;
            _productUnitService = productUnitService;
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

        // هنا منطق المخزون للمشتريات: الكمية تزيد مع الموجب، تنقص مع السالب
        private async Task UpdateStockQuantity(int productId, int productUnitId, decimal quantityDelta)
        {
            try
            {
                var existingStock = await _stockService.GetAllWriteDtoWithFilteringAndIncludeAsync(
                    s => s.ProductId == productId && s.ProductUnitId == productUnitId);

                if (existingStock.Data.Count > 0)
                {
                    var stock = existingStock.Data.First();

                    var newQuantity = stock.Quantity + quantityDelta;

                    if (newQuantity < 0)
                        throw new InvalidOperationException("لا يمكن أن تصبح الكمية في المخزون سالبة.");

                    stock.Quantity = newQuantity;
                    stock.UpdatedDate = DateTime.Now;
                    await _stockService.UpdateAsync(stock);
                }
                else
                {
                    // إذا ما فيش مخزون للسطر ده، نعمل سجل جديد في المخزون بكميّة موجبة فقط
                    if (quantityDelta > 0)
                    {
                        var newStock = new Domain.Stock.DTOs.StockWriteDto
                        {
                            ProductId = productId,
                            ProductUnitId = productUnitId,
                            Quantity = quantityDelta,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        };

                        await _stockService.CreateAsync(newStock);
                    }
                    else
                    {
                        throw new InvalidOperationException("المنتج غير موجود في المخزون ولا يمكن إنقاص كميته.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ عند تحديث المخزون: {ex.Message}");
            }
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
                    foreach (var line in InvoiceLines)
                        await UpdateStockQuantity(line.ProductId, line.ProductUnitId, line.Quantity);

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

                    // ✅ المشتريات: نضيف الكميات للمخزون
                    foreach (var line in InvoiceLines)
                        await UpdateStockQuantity(line.ProductId, line.ProductUnitId, line.Quantity);
                    // ملاحظة: إذا UpdateStockQuantity عندك "يخصم" بالكمية الموجبة
                    // فإضافة للمخزون = مرّر كمية سالبة (مثل ما كنت تعمل بريترن).
                    // إذا عندك ميثود منفصلة للإضافة، استخدمها بدل هالسطر.

                    // ✅ POST financial (Purchase Invoice = OUT)
                    var postDto = new FinancialPostDto
                    {
                        Direction = TransactionDirection.Out,
                        Method = PaymentMethod.Cash, // أو MapPaymentMethod(selectedPaymentType)
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

                    MessageBox.Show("تم إنشاء فاتورة المشتريات وتسجيل الحركة المالية ✅");
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
                    foreach (var old in _originalLines)
                        await UpdateStockQuantity(old.ProductId, old.ProductUnitId, old.Quantity);
                    // (إذا القديم كان يضيف للمخزون، فالعكس خصم: مرّر كمية موجبة)

                    // 2) طبّق الفاتورة الجديدة على المخزون (إضافة)
                    foreach (var line in InvoiceLines)
                        await UpdateStockQuantity(line.ProductId, line.ProductUnitId, -line.Quantity);

                    // 3) Update invoice
                    var result = await _invoicesService.UpdateAsync(invoiceDto);
                    if (!result.Success)
                    {
                        MessageBox.Show(result.Message ?? "فشل تحديث فاتورة المشتريات", "خطأ");
                        return;
                    }

                    // 4) Post new OUT transaction
                    var postDto = new FinancialPostDto
                    {
                        Direction = TransactionDirection.Out,
                        Method = PaymentMethod.Cash, // أو MapPaymentMethod(selectedPaymentType)
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

                    MessageBox.Show("تم تحديث فاتورة المشتريات وتسجيل الحركة المالية ✅");
                }

                PrintBtn.Visibility = Visibility.Visible;
                NewInvoiceBtn.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء حفظ الفاتورة:\n{ex.Message}");
            }
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

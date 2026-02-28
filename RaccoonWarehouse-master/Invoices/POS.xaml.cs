#region Usings
using RaccoonWarehouse.Application.Service.Cashers;
using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Auth;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Cashiers;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.FinancialTransactions.DTOs;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.Stock;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.FinancialTransactions;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.POS;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
#endregion




namespace RaccoonWarehouse.Invoices
{
    public partial class POS : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IInvoiceService _invoiceService;
        private readonly IProductService _productService;
        private readonly IUserService _userService;
        private readonly IStockService _stockService;
        private readonly ICashierSessionService _cashierSessionService;
        private readonly IUserSession _userSession;
        private readonly IFinancialTransactionService _financialService;

        private CancellationTokenSource? _searchCts;
        private Popup _currentPopup;
        private TextBox _currentEditingTextBox;
        private string _currentCasherName;
        public string CurrentCasherName
        {
            get => _currentCasherName;
            set
            {
                if (_currentCasherName != value)
                {
                    _currentCasherName = value;
                    OnPropertyChanged(nameof(CurrentCasherName));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private InvoiceReadDto? _lastSavedInvoice = new InvoiceReadDto();


        private InvoiceWriteDto _currentInvoice;
        private ObservableCollection<UserReadDto> _allCustomers;
        private List<ProductWriteDto> _invoiceProducts;
        private readonly ILoadingService _loading;
        private ObservableCollection<ProductReadDto> Products { get; set; }
            = new ObservableCollection<ProductReadDto>();
        private ObservableCollection<InvoiceLineWriteDto> _invoiceLines
            = new ObservableCollection<InvoiceLineWriteDto>();

        public POS(
        #region ctor            
                   IServiceProvider serviceProvider, IProductService productService,
                   IStockService stockService, IUserService userService,
                   IInvoiceService invoiceService, ILoadingService loading,
                   ICashierSessionService cashierSessionService,
                   IUserSession userSession,
                   IFinancialTransactionService financialService
        #endregion
            )
        {
            #region initialization
            _serviceProvider = serviceProvider;
            _productService = productService;
            _invoiceService = invoiceService;
            _userService = userService;
            _stockService = stockService;
            _loading = loading;
            _cashierSessionService = cashierSessionService;
            _userSession = userSession;
            _financialService = financialService;
            #endregion

            InitializeComponent();
            this.DataContext = this;
            Loaded += POS_Loaded;
        }


        // ===================== LOAD DATA =====================
        private async void POS_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _loading.Show();
                CreateNewInvoice();

                CurrentCasherName = _userSession.CurrentCashierSession.CashierName;
                // العملاء
                var result = await _userService.GetAllAsync();
                _allCustomers = new ObservableCollection<UserReadDto>(result?.Data ?? new List<UserReadDto>());

                CustomerComboBox.ItemsSource = _allCustomers;
                CustomerComboBox.SelectedIndex = -1;
                InvoiceGrid.ItemsSource = _invoiceLines;
                CashierName.Text = CurrentCasherName.ToString();

                //InvoiceDatePicker.SelectedDate = DateTime.Now;
                await LoadProductsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تحميل البيانات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _loading.Hide();

            }
        }
        private string GenerateInvoiceNumber()
        {
            string prefix = "INV";
            string datePart = DateTime.Now.ToString("yyyyMMddHHmmss");
            return $"{prefix}-{datePart}";
        }
        //Create new invoice
        private void CreateNewInvoice()
        {
            CreateNewInvoice(InvoiceType.Sale, null);
        }

        private void CreateNewInvoice(
             InvoiceType invoiceType,
             string? originalInvoiceNumber = null)
        {
            _invoiceLines.Clear();

            _currentInvoice = new InvoiceWriteDto
            {
                InvoiceNumber = GenerateInvoiceNumber(),
                InvoiceType = invoiceType,
                OriginalInvoiceId = originalInvoiceNumber,
                OpenedAt = DateTime.Now,
                InvoiceLines = _invoiceLines,
                TotalAmount = 0,
                IsPOS = true,
                CasherId = _userSession.CurrentCashierSession.CashierId
            };

            // ✅ UI
            CurrentDateTextBlock.Text = DateTime.Now.ToString("yyyy/MM/dd");

            RecalculateTotals();
        }
        #region useabellty 
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F1:
                    NewInvoiceBtn_Click(this, null);
                    e.Handled = true;
                    break;

                case Key.F2:
                    SearchProductBtn_Click(this, null);
                    e.Handled = true;
                    break;

                case Key.F3:
                    DeleteItemBtn_Click(this, null);
                    e.Handled = true;
                    break;

                case Key.F4:
                    FinishSaleBtn_Click(this, null);
                    e.Handled = true;
                    break;

                case Key.F5:
                    HoldSaleBtn_Click(this, null);
                    e.Handled = true;
                    break;

                case Key.F6:
                    ResumeHoldBtn_Click(this, null);
                    e.Handled = true;
                    break;

                case Key.Escape:
                    CancelInvoiceBtn_Click(this, null);
                    e.Handled = true;
                    break;

                case Key.Delete:
                    DeleteItemBtn_Click(this, null);
                    e.Handled = true;
                    break;
            }
        }

        private void InvoiceGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key != Key.Enter &&
                e.Key != Key.Left &&
                e.Key != Key.Right &&
                e.Key != Key.Up &&
                e.Key != Key.Down)
                return;
            if (_currentPopup != null && _currentPopup.IsOpen)
            {
                if (e.Key == Key.Down ||
                    e.Key == Key.Up ||
                    e.Key == Key.Enter ||
                    e.Key == Key.Escape)
                {
                    e.Handled = true;
                    return;
                }
            }
            var grid = (DataGrid)sender;

            if (grid.CurrentCell.Item == null)
                return;

            int rowIndex = grid.Items.IndexOf(grid.CurrentCell.Item);
            int colIndex = grid.Columns.IndexOf(grid.CurrentCell.Column);

            bool isRtl = grid.FlowDirection == FlowDirection.RightToLeft;

            if (e.Key == Key.Enter)
            {
                // Commit edit - handle errors gracefully
                try
                {
                    // Commit cell edit
                    grid.CommitEdit(DataGridEditingUnit.Cell, true);

                    // Commit row edit
                    grid.CommitEdit(DataGridEditingUnit.Row, true);
                }
                catch (Exception ex)
                {
                    // If commit fails, cancel edit and continue
                    try
                    {
                        grid.CancelEdit(DataGridEditingUnit.Cell);
                        grid.CancelEdit(DataGridEditingUnit.Row);
                    }
                    catch { }
                    System.Diagnostics.Debug.WriteLine($"Commit error: {ex.Message}");
                }

                // Recalculate totals
                RecalculateTotals();

                // Move to next cell in memory (0..N), which matches visual RTL
                colIndex += 1; // always +1, independent of FlowDirection
            }

            else
            {
                switch (e.Key)
                {
                    case Key.Left:
                        colIndex += isRtl ? 1 : -1;
                        break;

                    case Key.Right:
                        colIndex += isRtl ? -1 : 1;
                        break;

                    case Key.Up:
                        rowIndex--;
                        break;

                    case Key.Down:
                        rowIndex++;
                        break;
                }

            }

            // Handle row overflow
            if (colIndex < 0 || colIndex >= grid.Columns.Count)
            {
                colIndex = isRtl ? grid.Columns.Count - 1 : 0;
                rowIndex++;
            }

            if (rowIndex < 0 || rowIndex >= grid.Items.Count)
                return;

            // 🔁 SKIP READ-ONLY COLUMNS
            while (colIndex >= 0 &&
                   colIndex < grid.Columns.Count &&
                   grid.Columns[colIndex].IsReadOnly)
            {
                colIndex += isRtl ? -1 : 1;

                if (colIndex < 0 || colIndex >= grid.Columns.Count)
                    return;
            }

            var nextCell = new DataGridCellInfo(
                grid.Items[rowIndex],
                grid.Columns[colIndex]);

            grid.CurrentCell = nextCell;
            grid.ScrollIntoView(nextCell.Item, nextCell.Column);
            grid.BeginEdit();

            e.Handled = true;
        }

        #endregion
        private async Task LoadProductsAsync()
        {
            try
            {
                var stockedProducts = await _stockService.GetAllWithFilteringAndIncludeAsync(
                            s => s.Quantity > 10,
                            new Expression<Func<Stock, object>>[]
                            {
                                s => s.Product,
                                s => s.Product.SubCategory,
                                s => s.Product.Brand,
                                s => s.Product.ProductUnits
                            });

                Products.Clear();
                ProductSuggestions.Clear();

                foreach (var stock in stockedProducts.Data)
                {
                    if (stock.Product != null)
                    {
                        Products.Add(stock.Product);
                        ProductSuggestions.Add(stock.Product);
                    }
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ عند تحميل المنتجات: {ex.Message}", "خطأ");
            }
        }
        private void RecalculateTotals()
        {
            _currentInvoice.TotalAmount = _invoiceLines.Sum(l => l.Quantity * l.UnitPrice);

            TotalTextBlock.Text = _currentInvoice.TotalAmount.ToString("0.000");
        }


        private async void BarcodeTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            var barcode = BarcodeTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(barcode)) return;

            try
            {
                //var result = await _productService.(barcode);
                var result = await _stockService.GetAllWithFilteringAndIncludeAsync(
                            s => s.Quantity > 10 & s.Product.ITEMCODE.ToString() == barcode,
                            new Expression<Func<Stock, object>>[]
                            {
                                s => s.Product,
                                s => s.Product.SubCategory,
                                s => s.Product.Brand,
                                s => s.Product.ProductUnits
                            });
                if (result == null || result.Data == null)
                {
                    MessageBox.Show("الصنف غير موجود", "تنبيه");
                    return;
                }

                AddProductToInvoice(result.Data?.FirstOrDefault().Product);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ");
            }
            finally
            {
                BarcodeTextBox.Clear();
                BarcodeTextBox.Focus();
            }
        }

        private void AddProductToInvoice(ProductReadDto product)
        {
            var existingLine = _invoiceLines
                .FirstOrDefault(l => l.ProductId == product.Id);

            if (existingLine != null)
            {
                existingLine.Quantity += 1;
            }
            else
            {
                _invoiceLines.Add(new InvoiceLineWriteDto
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductUnitId = product.ProductUnits.FirstOrDefault().Id,
                    Quantity = 1,
                    UnitPrice = product.ProductUnits.FirstOrDefault().SalePrice,
                });
            }

            RecalculateTotals();
            InvoiceGrid.Items.Refresh();

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (InvoiceGrid.Items.Count == 0)
                    return;

                int rowIndex = InvoiceGrid.Items.Count - 1; // newly added row

                // Find first editable column
                int colIndex = 0;
                while (colIndex < InvoiceGrid.Columns.Count &&
                       InvoiceGrid.Columns[colIndex].IsReadOnly)
                {
                    colIndex++;
                }

                if (colIndex >= InvoiceGrid.Columns.Count)
                    return;

                var cell = new DataGridCellInfo(
                    InvoiceGrid.Items[rowIndex],
                    InvoiceGrid.Columns[colIndex]);

                InvoiceGrid.Focus();
                InvoiceGrid.CurrentCell = cell;
                InvoiceGrid.ScrollIntoView(cell.Item, cell.Column);
                InvoiceGrid.BeginEdit();
            }), System.Windows.Threading.DispatcherPriority.Input);

        }
        private void InvoiceGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private async void FinishSaleBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!CanSaveInvoice())
                    return;

                PrepareInvoiceForSave();


                var selectecCustomer = CustomerComboBox.SelectedItem as UserReadDto;
                _currentInvoice.CustomerId = selectecCustomer?.Id;
                var result = await _invoiceService.CreateAsync(_currentInvoice);

                if (!result.Success)
                {
                    MessageBox.Show(result.Message ?? "فشل حفظ الفاتورة", "خطأ");
                    return;
                }

                MessageBox.Show("تم حفظ الفاتورة بنجاح ✅", "نجاح");
                _lastSavedInvoice = await _invoiceService.GetFullInvoiceByIdAsync(result.Data.Id);



                await UpdateStockAfterSaleAsync();


                //Financial handle
                if (_currentInvoice.InvoiceType != InvoiceType.Return
                    && _currentInvoice.InvoiceType != InvoiceType.Return)
                {
                    var postDto = new FinancialPostDto
                    {
                        Direction = TransactionDirection.In,
                        Method = PaymentMethod.Cash,
                        Amount = _currentInvoice.TotalAmount,
                        TransactionDate = DateTime.Now,
                        SourceType = FinancialSourceType.PosSaleInvoice,
                        SourceId = _lastSavedInvoice.Id,
                        CashierSessionId = _userSession.CurrentCashierSession.Id,
                        CashierId = _userSession.CurrentCashierSession.CashierId,
                        Notes = $"POS Invoice #{_currentInvoice.InvoiceNumber}"
                    };

                    await _financialService.PostAsync(postDto);

                    ResetPOS();
                }
                else if (_currentInvoice.InvoiceType == InvoiceType.Return) // Finish Retunrs Invoice 
                {

                    var postDto = new FinancialPostDto
                    {
                        Direction = TransactionDirection.Out,
                        Method = PaymentMethod.Cash,
                        Amount = -(_currentInvoice.TotalAmount),
                        TransactionDate = DateTime.Now,
                        SourceType = FinancialSourceType.SaleReturn,
                        SourceId = _lastSavedInvoice.Id,
                        CashierSessionId = _userSession.CurrentCashierSession.Id,
                        CashierId = _userSession.CurrentCashierSession.CashierId,
                        Notes = $"POS Invoice #{_currentInvoice.InvoiceNumber}"
                    };

                    await _financialService.PostAsync(postDto);

                    ResetPOS();
                }






            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ");
            }
        }
        private async Task UpdateStockAfterSaleAsync()
        {
            foreach (var line in _invoiceLines)
            {
                await UpdateStockQuantity(
                    line.ProductId,
                    line.ProductUnitId,
                    line.Quantity
                );
            }
        }
        private async Task UpdateStockQuantity(int productId, int productUnitId, decimal quantity)
        {
            var existingStock = await _stockService.GetAllWriteDtoWithFilteringAndIncludeAsync(
                s => s.ProductId == productId && s.ProductUnitId == productUnitId);

            if (existingStock.Data.Count == 0)
                throw new InvalidOperationException("المنتج غير موجود في المخزون.");

            var stock = existingStock.Data.First();

            if (stock.Quantity < quantity)
                throw new InvalidOperationException("الكمية غير متوفرة في المخزون.");

            stock.Quantity -= quantity;
            stock.UpdatedDate = DateTime.Now;

            await _stockService.UpdateAsync(stock);
        }


        private bool CanSaveInvoice()
        {
            if (_invoiceLines.Count == 0)
            {
                MessageBox.Show("لا يوجد أصناف في الفاتورة", "تنبيه");
                return false;
            }

            /* if (CustomerComboBox.SelectedItem == null)
             {
                 MessageBox.Show("يرجى اختيار العميل", "تنبيه");
                 return false;
             }*/

            return true;
        }
        private void PrepareInvoiceForSave()
        {
            var customer = CustomerComboBox.SelectedItem as UserReadDto;
            _currentInvoice.CustomerId = customer?.Id;
            _currentInvoice.IsPOS = true;
            _currentInvoice.Status = InvoiceStatus.Completed;
            _currentInvoice.ClosedAt = DateTime.Now;

            _currentInvoice.TotalAmount = _invoiceLines.Sum(l => l.Quantity * l.UnitPrice);
        }
        private void ResetPOS()
        {
            _invoiceLines.Clear();
            InvoiceGrid.Items.Refresh();

            TotalTextBlock.Text = "0.000";

            CustomerComboBox.SelectedIndex = -1;

            CreateNewInvoice();

            BarcodeTextBox.Focus();
        }

        private void SearchProductBtn_Click(object sender, RoutedEventArgs e)
        {
            var searchWindow = new ProductSearchWindow(_stockService)
            {
                Owner = this
            };

            if (searchWindow.ShowDialog() == true)
            {
                AddProductToInvoice(searchWindow.SelectedProduct);
            }
        }

        private void DailyReportBtn_Click(object sender, RoutedEventArgs e)
        {
            var reportWindow = new DailySalesReport(_invoiceService);
            reportWindow.Owner = this;
            reportWindow.ShowDialog();
        }


        #region OnHold
        private async void HoldSaleBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_invoiceLines.Count == 0)
            {
                MessageBox.Show("لا توجد مواد لحفظها", "تنبيه");
                return;
            }

            try
            {
                _currentInvoice.Status = InvoiceStatus.OnHold;
                _currentInvoice.IsPOS = true;
                _currentInvoice.ClosedAt = null;
                _currentInvoice.TotalAmount = _invoiceLines.Sum(l => l.Quantity * l.UnitPrice);

                var result = await _invoiceService.CreateAsync(_currentInvoice);

                if (!result.Success)
                {
                    MessageBox.Show(result.Message, "خطأ");
                    return;
                }

                MessageBox.Show("تم حفظ الفاتورة في وضع الانتظار", "تم");

                ResetPOS(); //clear
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ");
            }
        }
        //resume held invoice
        private void ResumeHoldBtn_Click(object sender, RoutedEventArgs e)
        {
            var win = new ResumeHeldInvoiceWindow(_invoiceService)
            {
                Owner = this
            };

            if (win.ShowDialog() == true)
            {
                LoadInvoiceIntoPOS(win.SelectedInvoice!);
            }
        }
        private void LoadInvoiceIntoPOS(InvoiceReadDto invoice)
        {
            _invoiceLines.Clear();

            foreach (var line in invoice.InvoiceLines)
            {
                _invoiceLines.Add(new InvoiceLineWriteDto
                {
                    ProductId = line.ProductId,
                    ProductName = line.ProductName,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    ProductUnitId = line.ProductUnitId
                });
            }

            _currentInvoice = new InvoiceWriteDto
            {
                Id = invoice.Id,              // 👈 VERY IMPORTANT
                InvoiceNumber = invoice.InvoiceNumber,
                Status = InvoiceStatus.Draft,
                IsPOS = true,
                OpenedAt = invoice.OpenedAt
            };

            InvoiceGrid.Items.Refresh();
            RecalculateTotals();
        }

        #endregion
        //delete invoice line
        private void DeleteItemBtn_Click(object sender, RoutedEventArgs e)
        {
            // Get row from CurrentCell or from SelectedCells
            InvoiceLineWriteDto? selectedRow = null;

            if (InvoiceGrid.CurrentCell.Item is InvoiceLineWriteDto cellItem)
                selectedRow = cellItem;
            else if (InvoiceGrid.SelectedCells.Count > 0)
                selectedRow = InvoiceGrid.SelectedCells[0].Item as InvoiceLineWriteDto;

            if (selectedRow == null)
            {
                MessageBox.Show("يرجى تحديد مادة أولاً", "تنبيه");
                return;
            }

            var confirm = MessageBox.Show(
                $"هل تريد حذف المادة:\n{selectedRow.ProductName} ؟",
                "تأكيد الحذف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            _invoiceLines.Remove(selectedRow);

            RecalculateTotals();
            InvoiceGrid.Items.Refresh();

            BarcodeTextBox.Focus();
        }


        private void InvoiceGrid_CellGotFocus(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is DataGridCell cell && cell.DataContext is InvoiceLineWriteDto row)
            {
                // Force row selection
                InvoiceGrid.SelectedItem = row;

                // Set current cell explicitly
                InvoiceGrid.CurrentCell = new DataGridCellInfo(row, cell.Column);
            }
        }

        //replace Item 
        private async void ExchangeItemBtn_Click(object sender, RoutedEventArgs e)
        {

            InvoiceLineWriteDto? selectedRow = null;
            if (InvoiceGrid.CurrentCell.Item is InvoiceLineWriteDto cellItem)
                selectedRow = cellItem;
            else if (InvoiceGrid.SelectedCells.Count > 0)
                selectedRow = InvoiceGrid.SelectedCells[0].Item as InvoiceLineWriteDto;

            if (selectedRow == null)
            {
                MessageBox.Show("يرجى تحديد مادة للاستبدال");
                return;
            }

            var win = new ExchangeInvoiceWindow(_invoiceService);
            if (win.ShowDialog() != true)
                return;

            CreateNewInvoice(
                InvoiceType.Exchange,
                win.OriginalInvoiceId
            );

            // إزالة المادة القديمة
            _invoiceLines.Remove(selectedRow);

            MessageBox.Show("امسح المادة الجديدة بالباركود");
            BarcodeTextBox.Focus();
        }

        //returns 
        private async void ReturnItemBtn_Click(object sender, RoutedEventArgs e)
        {

            InvoiceLineWriteDto? selectedRow = null;
            if (InvoiceGrid.CurrentCell.Item is InvoiceLineWriteDto cellItem)
                selectedRow = cellItem;
            else if (InvoiceGrid.SelectedCells.Count > 0)
                selectedRow = InvoiceGrid.SelectedCells[0].Item as InvoiceLineWriteDto;

            if (selectedRow == null)
            {
                MessageBox.Show("يرجى تحديد مادة لإرجاعها");
                return;
            }

            var win = new ReturnInvoiceWindow(_invoiceService);
            if (win.ShowDialog() != true)
                return;

            // جلب الفاتورة الأصلية
            var result = await _invoiceService
                .GetAllWriteDtoWithFilteringAndIncludeAsync(
                    i => i.InvoiceNumber == win.OriginalInvoiceId,
                    i => i.InvoiceLines
                );

            var originalInvoice = result.Data.FirstOrDefault();
            if (originalInvoice == null)
            {
                MessageBox.Show("الفاتورة غير موجودة");
                return;
            }

            bool exists = originalInvoice.InvoiceLines
                .Any(l => l.ProductId == selectedRow.ProductId);

            if (!exists)
            {
                MessageBox.Show("لا يمكن إرجاع مادة غير موجودة في الفاتورة الأصلية");
                return;
            }

            // 🟢 إنشاء فاتورة مرتجع جديدة
            CreateNewInvoice(
                InvoiceType.Return,
                win.OriginalInvoiceId
            );


            // 🟢 إضافة السطر المرتجع
            _invoiceLines.Add(new InvoiceLineWriteDto
            {
                ProductId = selectedRow.ProductId,
                ProductName = selectedRow.ProductName,
                ProductUnitId = selectedRow.ProductUnitId,
                Quantity = -Math.Abs(selectedRow.Quantity),
                UnitPrice = selectedRow.UnitPrice,
                OriginalInvoiceId = win.OriginalInvoiceId
            });
            _currentInvoice.InvoiceType = InvoiceType.Return;

            RecalculateTotals();

            InvoiceGrid.Items.Refresh();
        }


        //==========================
        //payment method handler
        //==========================
        private async void CashPaymentBtn_Click(object sender, RoutedEventArgs e)
        {
            await ProcessPaymentAsync(PaymentType.Cash);
        }

        private async void VisaPaymentBtn_Click(object sender, RoutedEventArgs e)
        {
            await ProcessPaymentAsync(PaymentType.Visa);
        }

        private async void MasterCardPaymentBtn_Click(object sender, RoutedEventArgs e)
        {
            await ProcessPaymentAsync(PaymentType.Master);

        }

        private async void CreditPaymentBtn_Click(object sender, RoutedEventArgs e)
        {
            await ProcessPaymentAsync(PaymentType.Credit);
        }


        /*  private async Task ProcessPaymentAsync(PaymentType paymentType)
          {
              try
              {
                  _currentInvoice.PaymentType = paymentType;

                  if (!CanSaveInvoice())
                      return;

                  PrepareInvoiceForSave();

                  var result = await _invoiceService.CreateAsync(_currentInvoice);

                  if (!result.Success)
                  {
                      MessageBox.Show(result.Message ?? "فشل حفظ الفاتورة", "خطأ");
                      return;
                  }

                  MessageBox.Show("تم حفظ الفاتورة بنجاح ✅", "نجاح");

                  _lastSavedInvoice =
                      await _invoiceService.GetFullInvoiceByIdAsync(result.Data.Id);

                  await UpdateStockAfterSaleAsync();
                  ResetPOS();
              }
              catch (Exception ex)
              {
                  MessageBox.Show(ex.Message, "خطأ");
              }
          }
  */


        //new INvoice 
        private void NewInvoiceBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_invoiceLines.Count > 0)
            {
                var confirm = MessageBox.Show(
                    "سيتم مسح الفاتورة الحالية.\nهل تريد المتابعة؟",
                    "فاتورة جديدة",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes)
                    return;
            }

            ResetPOS();
        }

        //Cancel Invoice
        private async void CancelInvoiceBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_invoiceLines.Count == 0)
            {
                ResetPOS();
                return;
            }

            var confirm = MessageBox.Show(
                "هل تريد إلغاء الفاتورة الحالية؟",
                "إلغاء",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            // If invoice was saved before (Held)
            if (_currentInvoice.Id > 0)
            {
                _currentInvoice.Status = InvoiceStatus.Cancelled;
                _currentInvoice.ClosedAt = DateTime.Now;

                await _invoiceService.UpdateAsync(_currentInvoice);
            }

            ResetPOS();
        }


        //print Invoice 
        private void PrintBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_lastSavedInvoice == null)
            {
                MessageBox.Show(
                    "لا توجد فاتورة للطباعة.\nيرجى إنهاء البيع أولاً.",
                    "تنبيه",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            SaveSalesInvoicePdf(_lastSavedInvoice);
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

        private void OpenReceipt_Click(object sender, RoutedEventArgs e)
        {
            var sessionId = _userSession.CurrentCashierSession.Id;
            var cashierId = _userSession.CurrentCashierSession.CashierId;

            var win = new ReceiptWindow(_financialService, sessionId, cashierId)
            {
                Owner = this
            };

            win.ShowDialog();
        }


        private void OpenPayment_Click(object sender, RoutedEventArgs e)
        {
            var sessionId = _userSession.CurrentCashierSession.Id;
            var cashierId = _userSession.CurrentCashierSession.CashierId;

            var win = new PaymentWindow(_financialService, sessionId, cashierId)
            {
                Owner = this
            };

            win.ShowDialog();
            /*            WindowManager.ShowDialog<PaymentWindow>(WindowSizeType.SmallRectangle);
            */
        }


        /*private void ProductNameTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Move focus to next cell
                if (sender is TextBox tb)
                {
                    var dg = FindVisualParent<DataGrid>(tb);
                    if (dg != null)
                    {
                        dg.CommitEdit(DataGridEditingUnit.Cell, true);
                        dg.CommitEdit(); // commit row
                        dg.Focus();
                    }
                }
                e.Handled = true;
            }
        }*/

        #region Search
        // Keep track of the current Popup for the editing cell
        public ObservableCollection<ProductReadDto> ProductSuggestions { get; set; }
    = new();
        private readonly SemaphoreSlim _searchLock = new(1, 1);

        private void ProductCombo_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                if (sender is ComboBox combo)
                {
                    if (combo.IsDropDownOpen && combo.SelectedItem != null)
                    {
                        e.Handled = true; // stop DataGrid from moving to new row
                    }
                }
            }
        }




        private async void ProductCombo_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (sender is not ComboBox combo)
                return;

            // Ignore navigation keys
            if (e.Key is Key.Down or Key.Up or Key.Enter or Key.Escape)
                return;

            var text = combo.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text) || text.Length < 2)
            {
                combo.IsDropDownOpen = false;
                return;
            }

            // Cancel previous search
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                await Task.Delay(300, token); // debounce

                await _searchLock.WaitAsync(token); // 🔒 prevent concurrent DbContext use

                var result = await _stockService.GetAllWithFilteringAndIncludeAsync(
                    s => s.Quantity > 10 &&
                         (s.Product.Name.Contains(text) ||
                          s.Product.ITEMCODE.ToString().Contains(text)),
                    new Expression<Func<Stock, object>>[]
                    {
                s => s.Product,
                s => s.Product.ProductUnits
                    });

                if (token.IsCancellationRequested)
                    return;

                ProductSuggestions.Clear();
                ProductSuggestions.Clear();
                foreach (var item in result.Data.Select(s => s.Product).Distinct())
                {
                    ProductSuggestions.Add(item);
                }

                combo.IsDropDownOpen = ProductSuggestions.Any();
            }
            catch (TaskCanceledException)
            {
                // expected when typing fast
            }
            finally
            {
                if (_searchLock.CurrentCount == 0)
                    _searchLock.Release();
            }
        }
        private void ProductCombo_KeyUp(object sender, KeyEventArgs e)
        {
            if (sender is not ComboBox combo) return;

            string text = combo.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(text))
            {
                ProductSuggestions.Clear();
                foreach (var p in Products)
                    ProductSuggestions.Add(p);
                combo.IsDropDownOpen = true;
                return;
            }

            var filtered = Products
                .Where(p => (p.Name?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false)
                         || p.ITEMCODE.ToString().Contains(text))
                .ToList();

            ProductSuggestions.Clear();
            foreach (var p in filtered)
                ProductSuggestions.Add(p);

            combo.IsDropDownOpen = filtered.Any();
        }



        private void ProductCombo_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox combo)
            {
                if (combo.Template.FindName("PART_EditableTextBox", combo) is TextBox tb)
                {
                    tb.TextChanged += ProductCombo_TextBox_TextChanged;
                }
            }
        }

        private void ProductCombo_DropDownOpened(object sender, EventArgs e)
        {
            if (sender is ComboBox combo)
            {
                // Initialize ProductSuggestions with all products if empty
                if (!ProductSuggestions.Any() && Products.Any())
                {
                    ProductSuggestions.Clear();
                    foreach (var p in Products)
                        ProductSuggestions.Add(p);
                }

                // Find and wire up the DataGrid events
                Dispatcher.BeginInvoke(() =>
                {
                    var popup = combo.Template?.FindName("Popup", combo) as Popup;
                    if (popup?.Child is Border border)
                    {
                        var scrollViewer = border.Child as ScrollViewer;
                        var grid = scrollViewer?.Content as DataGrid;
                        if (grid != null)
                        {
                            grid.MouseDoubleClick -= ProductGrid_MouseDoubleClick;
                            grid.PreviewKeyDown -= ProductGrid_PreviewKeyDown;
                            grid.MouseDoubleClick += ProductGrid_MouseDoubleClick;
                            grid.PreviewKeyDown += ProductGrid_PreviewKeyDown;
                        }
                    }
                }, DispatcherPriority.Loaded);
            }
        }

        private void ProductGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid grid && grid.SelectedItem is ProductReadDto product)
            {
                SelectProductFromGrid(product, grid);
            }
        }

        private void ProductGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is DataGrid grid)
            {
                if (e.Key == Key.Enter && grid.SelectedItem is ProductReadDto product)
                {
                    e.Handled = true;
                    SelectProductFromGrid(product, grid);
                }
                else if (e.Key == Key.Escape)
                {
                    e.Handled = true;
                    var combo = FindParent<ComboBox>(grid);
                    if (combo != null)
                    {
                        combo.IsDropDownOpen = false;
                    }
                }
            }
        }

        private void SelectProductFromGrid(ProductReadDto product, DataGrid grid)
        {
            // Find the ComboBox that contains this DataGrid
            var combo = FindParent<ComboBox>(grid);
            if (combo == null) return;

            // Find the InvoiceLineWriteDto from the ComboBox's DataContext
            if (combo.DataContext is not InvoiceLineWriteDto line) return;

            // Set the selected product
            line.SelectedProduct = product;
            line.ProductId = product.Id;
            line.ProductName = product.Name ?? string.Empty;
            line.ProductUnitId = product.ProductUnits?.FirstOrDefault()?.Id ?? 0;
            line.UnitPrice = product.ProductUnits?.FirstOrDefault()?.SalePrice ?? 0;
            if (line.Quantity <= 0)
                line.Quantity = 1;

            // Update ComboBox selection and text
            combo.SelectedItem = product;
            combo.Text = product.Name ?? string.Empty;

            // Close the dropdown
            combo.IsDropDownOpen = false;

            // Commit the edit and move to quantity cell
            var dataGrid = FindParent<DataGrid>(combo);
            if (dataGrid != null)
            {
                try
                {
                    // Commit cell edit
                    dataGrid.CommitEdit(DataGridEditingUnit.Cell, true);

                    // Commit row edit
                    dataGrid.CommitEdit(DataGridEditingUnit.Row, true);

                    Dispatcher.BeginInvoke(() =>
                    {
                        var qtyColumn = dataGrid.Columns.FirstOrDefault(c => c.Header?.ToString() == "الكمية");
                        if (qtyColumn != null)
                        {
                            dataGrid.CurrentCell = new DataGridCellInfo(line, qtyColumn);
                            dataGrid.ScrollIntoView(line, qtyColumn);
                            dataGrid.BeginEdit();
                        }
                        RecalculateTotals();
                    }, DispatcherPriority.Background);
                }
                catch (Exception ex)
                {
                    // If commit fails, cancel edit
                    try
                    {
                        dataGrid.CancelEdit(DataGridEditingUnit.Cell);
                        dataGrid.CancelEdit(DataGridEditingUnit.Row);
                    }
                    catch { }
                    System.Diagnostics.Debug.WriteLine($"SelectProductFromGrid commit error: {ex.Message}");
                }
            }
        }

        private async void ProductCombo_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox) return;

            // Find the ComboBox parent
            var combo = FindParent<ComboBox>(textBox);
            if (combo == null) return;

            var text = textBox.Text?.Trim();

            // If empty or too short, show all products
            if (string.IsNullOrWhiteSpace(text) || text.Length < 1)
            {
                ProductSuggestions.Clear();
                foreach (var p in Products)
                    ProductSuggestions.Add(p);
                return;
            }

            // Cancel previous search
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                await Task.Delay(300, token); // debounce

                await _searchLock.WaitAsync(token);

                var result = await _stockService.GetAllWithFilteringAndIncludeAsync(
                    s => s.Quantity > 10 &&
                         ((s.Product.Name != null && s.Product.Name.Contains(text)) ||
                          (s.Product.ITEMCODE != null && s.Product.ITEMCODE.ToString().Contains(text))),
                    new Expression<Func<Stock, object>>[]
                    {
                        s => s.Product,
                        s => s.Product.SubCategory,
                        s => s.Product.Brand,
                        s => s.Product.ProductUnits
                    });

                if (token.IsCancellationRequested)
                    return;

                ProductSuggestions.Clear();

                foreach (var item in result.Data.Select(s => s.Product).Distinct())
                {
                    ProductSuggestions.Add(item);
                }

                // Open dropdown if there are suggestions
                if (ProductSuggestions.Any())
                {
                    combo.IsDropDownOpen = true;
                }
            }
            catch (TaskCanceledException)
            {
                // Expected when typing fast
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في البحث: {ex.Message}", "خطأ");
            }
            finally
            {
                if (_searchLock.CurrentCount == 0)
                    _searchLock.Release();
            }
        }

        private void ProductCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox combo) return;
            if (combo.SelectedItem is not ProductReadDto product) return;

            if (combo.DataContext is not InvoiceLineWriteDto line) return;

            line.SelectedProduct = product;
            line.ProductId = product.Id;
            line.ProductName = product.Name;
            line.ProductUnitId = product.ProductUnits.FirstOrDefault()?.Id ?? 0;
            line.UnitPrice = product.ProductUnits.FirstOrDefault()?.SalePrice ?? 0;
            line.Quantity = 1;

            FocusQuantityCell(combo);
        }

        private void ApplyProduct(ComboBox combo)
        {
            if (combo.SelectedItem is not ProductReadDto product)
                return;

            if (combo.DataContext is not InvoiceLineWriteDto line)
                return;

            var unit = product.ProductUnits.FirstOrDefault();
            if (unit == null) return;

            line.SelectedProduct = product;
            line.ProductId = product.Id;
            line.ProductName = product.Name;
            line.ProductUnitId = unit.Id;
            line.UnitPrice = unit.SalePrice;
            line.Quantity = 1;

            FocusQuantityCell(combo);
        }

        private void FocusQuantityCell(ComboBox combo)
        {
            var grid = FindParent<DataGrid>(combo);
            if (grid == null) return;

            grid.CommitEdit(DataGridEditingUnit.Cell, true);

            grid.Dispatcher.BeginInvoke(() =>
            {
                grid.CurrentCell = new DataGridCellInfo(
                    grid.SelectedItem,
                    grid.Columns.First(c => c.Header.ToString() == "الكمية")
                );

                grid.BeginEdit();
            }, DispatcherPriority.Background);
        }
        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent) return parent;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }



        private void ProductNameTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                _currentEditingTextBox = tb; // store reference
                                             // Find the Popup in the same template
                var parentGrid = VisualTreeHelper.GetParent(tb);
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parentGrid); i++)
                {
                    var child = VisualTreeHelper.GetChild(parentGrid, i);
                    if (child is Popup popup && popup.Name == "ProductSuggestionsPopup")
                    {
                        _currentPopup = popup;
                        break;
                    }
                }
            }
        }


        private async void ProductNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox tb) return;
            if (tb.DataContext is not InvoiceLineWriteDto line) return;

            string text = tb.Text.Trim();
            if (text.Length < 2)
            {
                if (_currentPopup != null) _currentPopup.IsOpen = false;
                return;
            }

            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                await Task.Delay(300, token); // debounce

                var result = await _stockService.GetAllWithFilteringAndIncludeAsync(
                    s => s.Product.Name.Contains(text) || s.Product.ITEMCODE.ToString().Contains(text),
                    new Expression<Func<Stock, object>>[] { s => s.Product, s => s.Product.ProductUnits });

                if (token.IsCancellationRequested) return;

                var suggestions = result.Data.Select(s => s.Product).Distinct().ToList();

                if (_currentPopup != null && _currentPopup.Child is Border border && border.Child is ListBox listBox)
                {
                    listBox.ItemsSource = suggestions;
                    if (suggestions.Any())
                    {
                        listBox.SelectedIndex = 0;
                        _currentPopup.IsOpen = true;
                    }
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception ex) { MessageBox.Show(ex.Message, "خطأ"); }
        }

        private async void ProductSuggestionsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            /*if (sender is not ListBox lb || lb.SelectedItem is not ProductReadDto selectedProduct)
                return;

            if (_currentEditingTextBox == null)
                return;

            if (_currentEditingTextBox.DataContext is not InvoiceLineWriteDto line)
                return;

            try
            {
                // Lookup full product info from stock service
                var result = await _stockService.GetAllWithFilteringAndIncludeAsync(
                    s => s.Product.Id == selectedProduct.Id,
                    new Expression<Func<Stock, object>>[]
                    {
                            s => s.Product,
                            s => s.Product.SubCategory,
                            s => s.Product.Brand,
                            s => s.Product.ProductUnits
                    });

                if (result == null || result.Data == null || !result.Data.Any())
                {
                    MessageBox.Show("الصنف غير موجود", "تنبيه");
                    return;
                }

                var stockItem = result.Data.First().Product;

                // Fill current line (like AddProductToInvoice, but update existing line)
                line.ProductId = stockItem.Id;
                line.ProductName = stockItem.Name;
                line.UnitPrice = stockItem.ProductUnits.FirstOrDefault()?.SalePrice ?? 0;
                line.ProductUnitId = stockItem.ProductUnits.FirstOrDefault()?.Id ?? 0;
                line.Quantity = 1; // default quantity

                // Close popup
                _currentPopup.IsOpen = false;

                // Commit current edits
                InvoiceGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                InvoiceGrid.CommitEdit(DataGridEditingUnit.Row, true);

                // Move focus to Quantity column
                var quantityColumn = InvoiceGrid.Columns.FirstOrDefault(c => c.Header.ToString().Contains("الكمية"));
                if (quantityColumn != null)
                {
                    InvoiceGrid.CurrentCell = new DataGridCellInfo(line, quantityColumn);
                    InvoiceGrid.ScrollIntoView(line, quantityColumn);
                    InvoiceGrid.Focus();
                    InvoiceGrid.BeginEdit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ");
            }*/
        }
        private async void ProductSuggestionsListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not ListBox lb) return;

            if (e.Key == Key.Enter && lb.SelectedItem is ProductReadDto product)
            {
                e.Handled = true;
                await SelectProduct(product);
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                _currentPopup.IsOpen = false;
                _currentEditingTextBox?.Focus();
            }
        }
        private async void ProductSuggestionsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox lb && lb.SelectedItem is ProductReadDto product)
            {
                await SelectProduct(product);
            }
        }


        private void ProductNameTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_currentPopup == null || !_currentPopup.IsOpen)
                return;

            if (_currentPopup.Child is not Border border ||
                border.Child is not ListBox listBox ||
                listBox.Items.Count == 0)
                return;

            switch (e.Key)
            {
                case Key.Down:
                    e.Handled = true;
                    listBox.Focus();
                    listBox.SelectedIndex = Math.Min(
                        listBox.SelectedIndex + 1,
                        listBox.Items.Count - 1);
                    listBox.ScrollIntoView(listBox.SelectedItem);
                    break;

                case Key.Up:
                    e.Handled = true;
                    listBox.Focus();
                    listBox.SelectedIndex = Math.Max(
                        listBox.SelectedIndex - 1,
                        0);
                    listBox.ScrollIntoView(listBox.SelectedItem);
                    break;

                case Key.Enter:
                    e.Handled = true;
                    if (listBox.SelectedItem != null)
                    {
                        // Force selection
                        ProductSuggestionsListBox_SelectionChanged(
                            listBox,
                            new SelectionChangedEventArgs(
                                Selector.SelectionChangedEvent,
                                new List<object>(),
                                new List<object> { listBox.SelectedItem }));
                    }
                    break;

                case Key.Escape:
                    e.Handled = true;
                    _currentPopup.IsOpen = false;
                    break;
            }
        }


        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) return t;
                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }
        private void InvoiceGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            if (e.Column.Header.ToString() == "الصنف")
            {
                if (e.EditingElement is ComboBox combo)
                {
                    // Set the Text property based on ProductName
                    if (e.Row.DataContext is InvoiceLineWriteDto line)
                    {
                        if (line.SelectedProduct != null)
                        {
                            combo.Text = line.SelectedProduct.Name;
                            combo.SelectedItem = line.SelectedProduct;
                        }
                        else if (!string.IsNullOrEmpty(line.ProductName))
                        {
                            combo.Text = line.ProductName;
                        }
                        else
                        {
                            combo.Text = string.Empty;
                        }
                    }

                    // Open dropdown after a short delay to allow text to be set
                    Dispatcher.BeginInvoke(() =>
                    {
                        combo.IsDropDownOpen = true;
                        combo.Focus();
                    }, DispatcherPriority.Loaded);
                }
            }
        }
        private void ProductCombo_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox combo)
            {
                ApplyProduct(combo); // sets SelectedProduct, ProductId, etc.
                combo.IsDropDownOpen = false;
            }
        }

        private async Task SelectProduct(ProductReadDto selectedProduct)
        {
            if (_currentEditingTextBox?.DataContext is not InvoiceLineWriteDto line)
                return;

            var result = await _stockService.GetAllWithFilteringAndIncludeAsync(
                s => s.Product.Id == selectedProduct.Id,
                new Expression<Func<Stock, object>>[]
                {
            s => s.Product,
            s => s.Product.ProductUnits
                });

            var product = result.Data.First().Product;

            line.ProductId = product.Id;
            line.ProductName = product.Name;
            line.UnitPrice = product.ProductUnits.FirstOrDefault()?.SalePrice ?? 0;
            line.ProductUnitId = product.ProductUnits.FirstOrDefault()?.Id ?? 0;
            line.Quantity = 1;

            _currentPopup.IsOpen = false;

            InvoiceGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            InvoiceGrid.CommitEdit(DataGridEditingUnit.Row, true);

            var qtyColumn = InvoiceGrid.Columns
                .First(c => c.Header.ToString().Contains("الكمية"));

            InvoiceGrid.CurrentCell = new DataGridCellInfo(line, qtyColumn);
            InvoiceGrid.Focus();
            InvoiceGrid.BeginEdit();
        }

        //search by Name Cell 

        #endregion
        #region financialhandle 

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

        private async Task ProcessPaymentAsync(PaymentType paymentType)
        {
            try
            {
                _currentInvoice.PaymentType = paymentType;

                if (!CanSaveInvoice())
                    return;

                PrepareInvoiceForSave();

                var result = await _invoiceService.CreateAsync(_currentInvoice);
                if (!result.Success)
                {
                    MessageBox.Show(result.Message ?? "فشل حفظ الفاتورة", "خطأ");
                    return;
                }

                var savedInvoiceId = result.Data.Id;

                // ✅ 1) Post Financial Transaction
                var postDto = new FinancialPostDto
                {
                    Direction = TransactionDirection.In,
                    Method = MapPaymentMethod(paymentType),
                    Amount = _currentInvoice.TotalAmount,
                    TransactionDate = DateTime.Now,

                    SourceType = FinancialSourceType.PosSaleInvoice,
                    SourceId = savedInvoiceId,

                    CashierSessionId = _userSession.CurrentCashierSession.Id,
                    CashierId = _userSession.CurrentCashierSession.CashierId,

                    Notes = $"POS Invoice #{_currentInvoice.InvoiceNumber}"
                };

                var postResult = await _financialService.PostAsync(postDto); if (!postResult.Success)
                {
                    MessageBox.Show(postResult.Message ?? "تم حفظ الفاتورة لكن فشل تسجيل الحركة المالية", "تحذير");
                    // هون قرارك: يا ترجع تلغي الفاتورة؟ أو تتركها وتعرض تنبيه
                    return;
                }

                MessageBox.Show("تم حفظ الفاتورة وتسجيل الحركة المالية ✅", "نجاح");

                _lastSavedInvoice = await _invoiceService.GetFullInvoiceByIdAsync(savedInvoiceId);

                await UpdateStockAfterSaleAsync();
                ResetPOS();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ");
            }
        }
        private FinancialSourceType MapSourceTypeByInvoiceType(InvoiceType invoiceType)
        {
            return invoiceType switch
            {
                InvoiceType.Sale => FinancialSourceType.PosSaleInvoice,
                InvoiceType.Return => FinancialSourceType.SaleReturn,
                InvoiceType.Exchange => FinancialSourceType.PosSaleInvoice, // أو SaleInvoice إذا بتحب
                _ => FinancialSourceType.Manual
            };
        }

        private TransactionDirection ResolveDirection(InvoiceType invoiceType, decimal totalAmount)
        {
            // Return دائماً Refund = OUT
            if (invoiceType == InvoiceType.Return)
                return TransactionDirection.Out;

            // Exchange يعتمد على الإشارة: + يعني الزبون دفع، - يعني رجعت له
            if (invoiceType == InvoiceType.Exchange)
                return totalAmount >= 0 ? TransactionDirection.In : TransactionDirection.Out;

            // Sale
            return TransactionDirection.In;
        }

        private async Task PostFinancialForInvoiceAsync(int invoiceId)
        {
            var total = _currentInvoice.TotalAmount;

            // إذا صفر ما في حركة مالية
            if (total == 0)
                return;

            var direction = ResolveDirection(_currentInvoice.InvoiceType, total);
            var amount = Math.Abs(total);

            // طريقة الدفع من الفاتورة (أو خليها Cash إذا ما عندك اختيار)
            var method = MapPaymentMethod(_currentInvoice.PaymentType.Value);

            var postDto = new FinancialPostDto
            {
                Direction = direction,
                Method = method,
                Amount = amount,
                TransactionDate = DateTime.Now,

                SourceType = MapSourceTypeByInvoiceType(_currentInvoice.InvoiceType),
                SourceId = invoiceId,

                CashierSessionId = _userSession.CurrentCashierSession?.Id,
                CashierId = _userSession.CurrentCashierSession?.CashierId,

                Notes = $"{_currentInvoice.InvoiceType} Invoice #{_currentInvoice.InvoiceNumber}"
            };

            var postResult = await _financialService.PostAsync(postDto);
            if (!postResult.Success)
                throw new Exception(postResult.Message ?? "فشل تسجيل الحركة المالية");
        }


        #endregion
        private void BarcodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        #region SessionManagement
        private void RefreshSessionButtons()
        {
            bool hasSession = _userSession.CurrentCashierSession != null;

            OpenSessionBtn.Visibility = hasSession ? Visibility.Collapsed : Visibility.Visible;
            CloseSessionBtn.Visibility = hasSession ? Visibility.Visible : Visibility.Collapsed;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshSessionButtons();
        }
        private void OpenSessionBtn_Click(object sender, RoutedEventArgs e)
        {
            var win = new StartCashierSessionWindow(_cashierSessionService, _financialService, _userSession);
            if (win.ShowDialog() == true)
            {
                RefreshSessionButtons();
            }
        }

        private void CloseSessionBtn_Click(object sender, RoutedEventArgs e)
        {
            var win = new CloseCashierSessionWindow(_cashierSessionService, _financialService, _userSession);
            if (win.ShowDialog() == true)
            {
                RefreshSessionButtons();
            }
        }
        #endregion


    }
}



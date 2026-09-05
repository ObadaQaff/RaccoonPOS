#region Usings
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Cashers;
using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.ProductUnits;
using RaccoonWarehouse.Application.Service.Sales;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.StockTransactions;
using RaccoonWarehouse.Application.Service.StockDocuments;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Auth;
using RaccoonWarehouse.Common;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse;
using RaccoonWarehouse.Domain.Cashiers;
using RaccoonWarehouse.Domain.Cashiers.DTOs;
using RaccoonWarehouse.Domain.Checks.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.FinancialTransactions.DTOs;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using RaccoonWarehouse.Domain.POS.DTOs;
using RaccoonWarehouse.Domain.Stock;
using RaccoonWarehouse.Domain.Stock.DTOs;
using RaccoonWarehouse.Domain.StockTransactions.DTOs;
using RaccoonWarehouse.Domain.StockDocuments.DTOs;
using RaccoonWarehouse.Domain.StockItems.DTOs;
using RaccoonWarehouse.Domain.SubCategories.DTOs;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Domain.Units.DTOs;
using RaccoonWarehouse.FinancialTransactions;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Helpers.Pdf;
using RaccoonWarehouse.Helpers.Pdf.Reports;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.POS;
using RaccoonWarehouse.Products;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Threading;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Markup;
using System.Windows.Shapes;
using System.Windows.Threading;
#endregion
namespace RaccoonWarehouse.Invoices
{
    public partial class POS : Window
    {
        private sealed class ReturnInvoiceLine : InvoiceLineWriteDto
        {
            private decimal _displayQuantity;
            private decimal _returnedQuantity;

            public new decimal Quantity
            {
                get => _displayQuantity;
                set
                {
                    if (_displayQuantity == value)
                        return;

                    _displayQuantity = value;
                    OnPropertyChanged();
                }
            }

            public decimal ReturnedQuantity
            {
                get => _returnedQuantity;
                set
                {
                    if (_returnedQuantity == value)
                        return;

                    _returnedQuantity = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LineTotal));
                }
            }

            public decimal ReturnableQuantity { get; set; }
            // Return quantities are entered as positive user input, but the
            // displayed line amount must clearly represent an outgoing return.
            public new decimal LineTotal => -(ReturnedQuantity * UnitPrice);

            public static ReturnInvoiceLine FromSnapshot(InvoiceLineWriteDto snapshot, decimal originalQuantity, decimal returnableQuantity)
            {
                var line = new ReturnInvoiceLine
                {
                    ProductId = snapshot.ProductId,
                    ProductName = snapshot.ProductName,
                    ProductUnitId = snapshot.ProductUnitId,
                    ProductUnit = snapshot.ProductUnit,
                    Product = snapshot.Product,
                    SelectedProduct = snapshot.SelectedProduct,
                    QuantityPerUnitSnapshot = snapshot.QuantityPerUnitSnapshot,
                    UnitPrice = snapshot.UnitPrice,
                    UnitCost = snapshot.UnitCost,
                    AvailableQuantitySnapshot = snapshot.AvailableQuantitySnapshot,
                    UnitNameSnapshot = snapshot.UnitNameSnapshot,
                    UnitName = snapshot.UnitName,
                    TaxExempt = snapshot.TaxExempt,
                    TaxRate = snapshot.TaxRate,
                    ExpiryDate = snapshot.ExpiryDate,
                    OriginalInvoiceId = snapshot.OriginalInvoiceId,
                    ReturnableQuantity = returnableQuantity
                };

                // Keep the original invoice quantity visible, but require the
                // user to enter the quantity that should actually be returned.
                var baseLine = (InvoiceLineWriteDto)line;
                baseLine.Quantity = originalQuantity;
                baseLine.LineSubTotal = 0m;
                baseLine.TaxAmount = 0m;
                baseLine.ProfitBeforeTax = 0m;
                baseLine.Profit = 0m;
                line.baseQuantityForReturn = originalQuantity *
                    (line.QuantityPerUnitSnapshot > 0 ? line.QuantityPerUnitSnapshot : 1m);
                line.Quantity = originalQuantity;
                line.ReturnedQuantity = 0m;
                return line;
            }

            private decimal baseQuantityForReturn
            {
                set => base.BaseQuantity = value;
            }
        }

        private readonly IServiceProvider _serviceProvider;
        private readonly IInvoiceService _invoiceService;
        private readonly IProductService _productService;
        private readonly IProductUnitService _productUnitService;
        private readonly IUserService _userService;
        private readonly IStockService _stockService;
        private readonly IStockTransactionService _stockTransactionService;
        private readonly ICashierSessionService _cashierSessionService;
        private readonly IUserSession _userSession;
        private readonly IFinancialTransactionService _financialService;
        private readonly ISaleCheckoutService _saleCheckoutService;
        private bool _isProcessingPayment;
        private bool _isHoldingInvoice;

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

        private InvoiceLineWriteDto? GetSelectedInvoiceLine()
        {
            if (InvoiceGrid.CurrentCell.Item is InvoiceLineWriteDto cellItem)
                return cellItem;

            if (InvoiceGrid.SelectedCells.Count > 0)
                return InvoiceGrid.SelectedCells[0].Item as InvoiceLineWriteDto;

            return InvoiceGrid.SelectedItem as InvoiceLineWriteDto;
        }

        private void FocusBarcodeInput()
        {
            BarcodeTextBox.Focus();
            Keyboard.Focus(BarcodeTextBox);
            BarcodeTextBox.SelectAll();
        }

        private void FocusBarcodeInputDeferred()
        {
            Dispatcher.BeginInvoke(new Action(FocusBarcodeInput), System.Windows.Threading.DispatcherPriority.Input);
        }

        private DataGridColumn? GetBarcodeColumn(DataGrid grid)
        {
            return grid.Columns.FirstOrDefault(column =>
                column.Header?.ToString()?.Contains("Barcode", StringComparison.OrdinalIgnoreCase) == true);
        }

        private static bool IsEmptyBarcodeLine(InvoiceLineWriteDto line)
        {
            return line.ProductId <= 0 &&
                   line.ProductUnitId <= 0 &&
                   string.IsNullOrWhiteSpace(line.ProductName) &&
                   line.UnitPrice == 0m &&
                   line.UnitCost == 0m;
        }

        private InvoiceLineWriteDto EnsureEmptyBarcodeLine()
        {
            var emptyLine = _invoiceLines.FirstOrDefault(IsEmptyBarcodeLine);

            if (emptyLine != null)
                return emptyLine;

            emptyLine = new InvoiceLineWriteDto();
            _invoiceLines.Add(emptyLine);
            return emptyLine;
        }

        private void FocusBarcodeGridCellDeferred()
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (_currentInvoice.InvoiceType is InvoiceType.Return or InvoiceType.PurchaseReturn)
                {
                    FocusFirstReturnQuantityCell();
                    return;
                }

                var barcodeColumn = GetBarcodeColumn(InvoiceGrid);
                if (barcodeColumn == null)
                    return;

                var emptyLine = EnsureEmptyBarcodeLine();
                MoveGridFocusToCell(InvoiceGrid, emptyLine, barcodeColumn);
            }, System.Windows.Threading.DispatcherPriority.Input);
        }

        private void RefreshInvoiceGridAfterEdit()
        {
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    if (InvoiceGrid.Items is IEditableCollectionView editableView &&
                        (editableView.IsAddingNew || editableView.IsEditingItem))
                    {
                        return;
                    }

                    InvoiceGrid.Items.Refresh();
                }
                catch (InvalidOperationException)
                {
                    // The collection can still be completing an AddNew/EditItem
                    // transaction after the product-search callback returns.
                    // The collection change is already visible, so no refresh is required.
                }
            }, DispatcherPriority.ContextIdle);
        }

        private void AddEmptyBarcodeLineAndFocus(DataGrid grid)
        {
            var emptyLine = new InvoiceLineWriteDto();
            _invoiceLines.Add(emptyLine);

            var barcodeColumn = GetBarcodeColumn(grid);
            if (barcodeColumn != null)
                MoveGridFocusToCell(grid, emptyLine, barcodeColumn);
        }

        private static bool HeaderMatches(DataGridColumn column, string arabicHeader)
        {
            var headerText = column.Header?.ToString();
            if (string.IsNullOrWhiteSpace(headerText))
                return false;

            return headerText.Contains(arabicHeader, StringComparison.Ordinal)
                || headerText.Contains(UiText.Translate(arabicHeader), StringComparison.Ordinal);
        }

        private static DataGridColumn? FindColumnByHeader(DataGrid grid, string arabicHeader)
        {
            return grid.Columns.FirstOrDefault(column => HeaderMatches(column, arabicHeader));
        }

        private InvoiceReadDto? _lastSavedInvoice;


        private InvoiceWriteDto _currentInvoice;
        private ObservableCollection<UserReadDto> _allCustomers;
        private bool _isFilteringCustomers;
        private bool _isNavigatingCustomerChoices;
        private int _falconValidationVersion;
        private string? _lastFalconDuplicateMessageValue;
        private readonly SemaphoreSlim _falconValidationGate = new(1, 1);
        private List<ProductWriteDto> _invoiceProducts;
        private readonly ILoadingService _loading;
        private ObservableCollection<ProductReadDto> Products { get; set; }
            = new ObservableCollection<ProductReadDto>();
        public ObservableCollection<SubCategoryReadDto> SubCategories { get; } = new();
        public ObservableCollection<ProductReadDto> FilteredProducts { get; } = new();
        private const decimal MinimumSellableQuantity = 10m;
        private const int ProductDropdownPageSize = 10;
        private const int ProductStockFetchSize = 40;
        private const int BrowsePageSize = 72;
        private int _browsePageNumber = 1;
        private bool _browseHasMore = true;
        private bool _browseIsLoading;
        private bool _pendingBrowseReload;
        private CancellationTokenSource? _browseLoadCts;
        private readonly SemaphoreSlim _posDataOperationSemaphore = new(1, 1);
        private readonly DispatcherTimer _browseSearchDebounceTimer = new() { Interval = TimeSpan.FromMilliseconds(280) };
        private InvoiceLineWriteDto? _pendingFefoEditedLine;
        private bool _hasPendingFefoSplit;
        private bool _isProcessingPendingFefoSplit;
        private int _posResetVersion;
        private readonly HashSet<int> _loadedProductIds = new();
        private readonly Dictionary<(int ProductId, int ProductUnitId), StockReadDto> _stockLookup = new();
        private readonly Dictionary<int, List<ProductUnitReadDto>> _hydratedProductUnits = new();
        private readonly Dictionary<int, ProductReadDto> _hydratedProducts = new();
        private readonly SemaphoreSlim _addProductGate = new(1, 1);
        private ScrollViewer? _productDropdownScrollViewer;
        private readonly DispatcherTimer _productSearchResetTimer = new() { Interval = TimeSpan.FromSeconds(1.2) };
        private string _productSearchText = string.Empty;
        private string _browseSearchText = string.Empty;
        private int? _selectedBrowseSubCategoryId;
        private int _comboSearchVersion;
        private int _popupSearchVersion;
        private bool _suppressUnitSelectionChanged;
        private bool _isLoadingHeldInvoice;
        private bool _isLoadingReturnInvoice;
        private ObservableCollection<InvoiceLineWriteDto> _invoiceLines
            = new ObservableCollection<InvoiceLineWriteDto>();

        public POS(
        #region ctor            
                   IServiceProvider serviceProvider, IProductService productService,
                   IProductUnitService productUnitService,
                   IStockService stockService, IUserService userService,
                   IInvoiceService invoiceService, ILoadingService loading,
                   IStockTransactionService stockTransactionService,
                   ICashierSessionService cashierSessionService,
                   IUserSession userSession,
                   IFinancialTransactionService financialService,
                   ISaleCheckoutService saleCheckoutService
        #endregion
            )
        {
            #region initialization
            _serviceProvider = serviceProvider;
            _productService = productService;
            _productUnitService = productUnitService;
            _invoiceService = invoiceService;
            _userService = userService;
            _stockService = stockService;
            _stockTransactionService = stockTransactionService;
            _loading = loading;
            _cashierSessionService = cashierSessionService;
            _userSession = userSession;
            _financialService = financialService;
            _saleCheckoutService = saleCheckoutService;
            #endregion

            InitializeComponent();
            this.DataContext = this;
            UiText.ApplyWindow(this);
            _productSearchResetTimer.Tick += ProductSearchResetTimer_Tick;
            _browseSearchDebounceTimer.Tick += BrowseSearchDebounceTimer_Tick;
            Loaded += POS_Loaded;
            Closed += POS_Closed;
        }


        // ===================== LOAD DATA =====================
        private async void POS_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                UiText.ApplyTranslations(this);
                _loading.Show();
                if (!TryGetActiveCashierSession(out var session))
                {
                    Close();
                    return;
                }

                CreateNewInvoice();

                CurrentCasherName = session.CashierName;
                await LoadCustomersAsync();
                InvoiceGrid.ItemsSource = _invoiceLines;
                CashierName.Text = CurrentCasherName.ToString();

                // The product-card/category browse area is hidden. Products for
                // the invoice selector are loaded lazily when its dropdown opens.
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء تحميل البيانات", "An error occurred while loading data")}: {ex.Message}", UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _loading.Hide();
                FocusBarcodeGridCellDeferred();

            }
        }

        private async Task LoadCustomersAsync(int? selectCustomerId = null)
        {
            var result = await _userService.GetAllAsync();
            _allCustomers = new ObservableCollection<UserReadDto>(result?.Data ?? new List<UserReadDto>());
            CustomerComboBox.ItemsSource = _allCustomers;
            CustomerComboBox.SelectedIndex = -1;

            if (selectCustomerId.HasValue)
            {
                var selectedCustomer = _allCustomers.FirstOrDefault(customer => customer.Id == selectCustomerId.Value);
                if (selectedCustomer != null)
                {
                    CustomerComboBox.SelectedItem = selectedCustomer;
                    _currentInvoice.CustomerId = selectedCustomer.Id;
                }
            }
        }

        private async Task<bool> EnsureCustomerCreditAllowedAsync(decimal invoiceAmount)
        {
            if (CustomerComboBox.SelectedItem is not UserReadDto customer)
            {
                ShowPaymentValidationMessage(UiText.T("يرجى اختيار الزبون.", "Please choose the customer."), UiText.T("تنبيه", "Notice"));
                return false;
            }

            if (customer.Role != UserRole.Customer)
                return true;

            if (customer.CreditStatus is CreditStatus.Blocked or CreditStatus.Suspended)
            {
                ShowPaymentValidationMessage(
                    UiText.T(
                        "حساب هذا الزبون الائتماني موقوف. لا يمكن إنشاء فاتورة آجل له.",
                        "This customer credit account is blocked. A credit invoice cannot be created."),
                    UiText.T("تنبيه", "Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            if (customer.CreditLimit <= 0m)
                return true;

            var statementService = _serviceProvider.GetRequiredService<UserStatementService>();
            var currentBalance = await statementService.GetCurrentBalanceAsync(customer.Id);
            var previousCreditAmount = _currentInvoice.Id > 0 && _lastSavedInvoice?.PaymentType == PaymentType.Credit
                ? -(_lastSavedInvoice?.TotalAmount ?? 0m)
                : 0m;
            var projectedBalance = currentBalance + invoiceAmount + previousCreditAmount;

            if (projectedBalance > customer.CreditLimit)
            {
                ShowPaymentValidationMessage(
                    UiText.T(
                        $"الحد الائتماني للزبون تم تجاوزه.\nالرصيد الحالي: {currentBalance:N5}\nالحد الائتماني: {customer.CreditLimit:N5}\nالرصيد المتوقع بعد الفاتورة: {projectedBalance:N5}",
                        $"The customer credit limit was exceeded.\nCurrent balance: {currentBalance:N5}\nCredit limit: {customer.CreditLimit:N5}\nProjected balance after invoice: {projectedBalance:N5}"),
                    UiText.T("تنبيه", "Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void POS_Closed(object? sender, EventArgs e)
        {
            _productSearchResetTimer.Tick -= ProductSearchResetTimer_Tick;
            _browseSearchDebounceTimer.Tick -= BrowseSearchDebounceTimer_Tick;
            _browseLoadCts?.Cancel();
            _browseLoadCts?.Dispose();
            CancelFalconValidation();
        }
        private string GenerateInvoiceNumber()
        {
            return (DateTime.Now.Ticks % 90000 + 10000).ToString();
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
            if (!TryGetActiveCashierSession(out var session))
                return;

            _invoiceLines.Clear();
            _invoiceLines.Add(new InvoiceLineWriteDto());

            _currentInvoice = new InvoiceWriteDto
            {
                InvoiceNumber = GenerateInvoiceNumber(),
                FalconInvoiceNumber = string.Empty,
                InvoiceType = invoiceType,
                OriginalInvoiceId = originalInvoiceNumber,
                OpenedAt = DateTime.Now,
                InvoiceLines = _invoiceLines,
                TotalAmount = 0,
                IsPOS = true,
                CasherId = session.CashierId,
                CashierSessionId = session.Id
            };

            // ✅ UI
            CurrentDateTextBlock.Text = DateTime.Now.ToString("yyyy/MM/dd");

            ConfigureReturnGrid(false);
            RecalculateTotals();
        }

        private DataGridColumn? GetReturnQuantityColumn(DataGrid grid)
        {
            return grid.Columns.FirstOrDefault(column =>
                column.Header?.ToString()?.Contains("Returned Quantity", StringComparison.OrdinalIgnoreCase) == true);
        }

        private void ConfigureReturnGrid(bool returnMode)
        {
            var returnColumn = GetReturnQuantityColumn(InvoiceGrid);
            if (returnColumn != null)
                returnColumn.Visibility = returnMode ? Visibility.Visible : Visibility.Collapsed;

            foreach (var column in InvoiceGrid.Columns)
            {
                if (column == returnColumn)
                {
                    column.IsReadOnly = !returnMode;
                    continue;
                }

                if (!returnMode)
                {
                    // Restore the normal sale grid after leaving return/exchange mode.
                    // These calculated/display-only columns remain read-only.
                    column.IsReadOnly = HeaderMatches(column, "الصنف") ||
                                        HeaderMatches(column, "المتوفر") ||
                                        HeaderMatches(column, "الإجمالي");
                    continue;
                }

                if (returnMode)
                {
                    column.IsReadOnly = true;
                }
            }

            InvoiceGrid.CanUserDeleteRows = !returnMode;
        }

        private void FocusFirstReturnQuantityCell()
        {
            var column = GetReturnQuantityColumn(InvoiceGrid);
            var line = _invoiceLines.OfType<ReturnInvoiceLine>().FirstOrDefault();
            if (column != null && line != null)
                MoveGridFocusToCell(InvoiceGrid, line, column);
        }

        private void PrepareGridForReturnLoad()
        {
            // A return replaces the current draft. Cancel only the active grid edit
            // so the existing normal-sale edit/save flow is not changed.
            try
            {
                InvoiceGrid.CancelEdit(DataGridEditingUnit.Cell);
                InvoiceGrid.CancelEdit(DataGridEditingUnit.Row);
            }
            catch (InvalidOperationException)
            {
                // The grid may already be outside an edit transaction.
            }
        }

        private void HandleReturnGridKey(DataGrid grid, KeyEventArgs e)
        {
            if (grid.CurrentCell.Item is not ReturnInvoiceLine line)
                return;

            var returnColumn = GetReturnQuantityColumn(grid);
            if (returnColumn == null)
                return;

            e.Handled = true;
            if (grid.CurrentCell.Column != returnColumn)
            {
                MoveGridFocusToCell(grid, line, returnColumn);
                return;
            }

            try
            {
                grid.CommitEdit(DataGridEditingUnit.Cell, true);
                grid.CommitEdit(DataGridEditingUnit.Row, true);
            }
            catch { }

            var rowIndex = grid.Items.IndexOf(line);
            var targetRowIndex = e.Key == Key.Up ? rowIndex - 1 : rowIndex + 1;
            if (e.Key is Key.Enter or Key.Down or Key.Up)
            {
                if (targetRowIndex >= 0 && targetRowIndex < grid.Items.Count && grid.Items[targetRowIndex] is ReturnInvoiceLine targetLine)
                    MoveGridFocusToCell(grid, targetLine, returnColumn);
            }
        }
        #region useabellty 
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_isLoadingHeldInvoice)
            {
                e.Handled = true;
                return;
            }

            if (e.Key == Key.F1)
            {
                SearchProductBtn_Click(sender, new RoutedEventArgs());
                e.Handled = true;
                return;
            }
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.D1:
                    case Key.NumPad1:
                        CashPaymentBtn_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        return;
                    case Key.D2:
                    case Key.NumPad2:
                        VisaPaymentBtn_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        return;
                    case Key.D3:
                    case Key.NumPad3:
                        MasterCardPaymentBtn_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        return;
                    case Key.D4:
                    case Key.NumPad4:
                        DebitPaymentBtn_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        return;
                    case Key.D5:
                    case Key.NumPad5:
                        CheckPaymentBtn_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        return;
                    case Key.D6:
                    case Key.NumPad6:
                        MobilePaymentBtn_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        return;
                    case Key.D7:
                    case Key.NumPad7:
                        CreditPaymentBtn_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        return;
                }
            }

            switch (e.Key)
            {
                case Key.F1:
                    SearchProductBtn_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.F2:
                    FinishSaleBtn_Click(this, null);
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

                case Key.F7:
                    OpenReceipt_Click(this, null);
                    e.Handled = true;
                    break;

                case Key.F8:
                    OpenPayment_Click(this, null);
                    e.Handled = true;
                    break;

                case Key.F9:
                    PrintBtn_Click(this, null);
                    e.Handled = true;
                    break;

                case Key.F10:
                    DailyReportBtn_Click(this, null);
                    e.Handled = true;
                    break;

                case Key.F11:
                    DiscountBtn_Click(this, null);
                    e.Handled = true;
                    break;

                case Key.F12:
                    CreditPaymentBtn_Click(this, null);
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
            InvoiceGrid_PreviewKeyDownUpdated(sender, e);
            return;

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
                var currentColumnHeader = grid.CurrentCell.Column?.Header?.ToString();
                var currentLine = grid.CurrentCell.Item as InvoiceLineWriteDto;
                var isQuantityColumn = grid.CurrentCell.Column != null && HeaderMatches(grid.CurrentCell.Column, "الكمية");

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

                var quantityHeader = UiText.Translate("الكمية");
                var priceHeader = UiText.Translate("السعر");

                if (currentLine != null && grid.CurrentCell.Column != null && HeaderMatches(grid.CurrentCell.Column, "Ø§Ù„ØµÙ†Ù"))
                {
                    MoveGridFocusToNextEditableCell(grid, currentLine, colIndex, isRtl);
                }

                if (isQuantityColumn && currentLine != null)
                {
                    _pendingFefoEditedLine = currentLine;
                    _hasPendingFefoSplit = true;

                    grid.Dispatcher.BeginInvoke(async () =>
                    {
                        var targetLine = await ProcessPendingFefoSplitAsync();
                        MoveGridFocusToColumn(grid, targetLine ?? currentLine, priceHeader);
                    }, DispatcherPriority.Background);
                }
                else if (grid.CurrentCell.Column != null && HeaderMatches(grid.CurrentCell.Column, "السعر"))
                {
                    FocusBarcodeInput();
                }

                e.Handled = true;
                return;
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

        private async void InvoiceGrid_PreviewKeyDownUpdated(object sender, KeyEventArgs e)
        {
            if (sender is not DataGrid grid ||
                e.Key is not (Key.Enter or Key.Left or Key.Right or Key.Up or Key.Down) ||
                grid.CurrentCell.Item == null ||
                grid.CurrentCell.Column == null)
            {
                return;
            }

            var currentItem = grid.CurrentCell.Item;
            var currentColumn = grid.CurrentCell.Column;
            var rowIndex = grid.Items.IndexOf(currentItem);

            if (_currentInvoice.InvoiceType is InvoiceType.Return or InvoiceType.PurchaseReturn)
            {
                HandleReturnGridKey(grid, e);
                return;
            }

            if (e.Key == Key.Enter)
            {
                if (currentItem is InvoiceLineWriteDto barcodeLine &&
                    currentColumn.Header?.ToString()?.Contains("Barcode", StringComparison.OrdinalIgnoreCase) == true)
                {
                    e.Handled = true;
                    var barcodeCell = FindCell(grid, barcodeLine, currentColumn);
                    var barcodeEditor = Keyboard.FocusedElement as TextBox
                        ?? (barcodeCell == null ? null : FindVisualChild<TextBox>(barcodeCell));
                    var barcode = barcodeEditor?.Text.Trim();
                    if (string.IsNullOrWhiteSpace(barcode))
                        return;

                    if (!await ProcessBarcodeAsync(barcode, barcodeLine))
                        return;

                    grid.CommitEdit(DataGridEditingUnit.Cell, true);
                    grid.CommitEdit(DataGridEditingUnit.Row, true);
                    if (barcodeLine.ProductId <= 0)
                        _invoiceLines.Remove(barcodeLine);
                    AddEmptyBarcodeLineAndFocus(grid);
                    return;
                }

                try
                {
                    grid.CommitEdit(DataGridEditingUnit.Cell, true);
                    grid.CommitEdit(DataGridEditingUnit.Row, true);
                }
                catch { }

                RecalculateTotals();

                if (currentItem is InvoiceLineWriteDto currentLine &&
                    HeaderMatches(currentColumn, "Ø§Ù„ÙƒÙ…ÙŠØ©"))
                {
                    _pendingFefoEditedLine = currentLine;
                    _hasPendingFefoSplit = true;
                    grid.Dispatcher.BeginInvoke(async () =>
                    {
                        var targetLine = await ProcessPendingFefoSplitAsync() ?? currentLine;
                        MoveGridFocusAfterEnter(grid, targetLine, grid.Items.IndexOf(targetLine), currentColumn);
                    }, DispatcherPriority.Background);
                }
                else
                {
                    MoveGridFocusAfterEnter(grid, currentItem, rowIndex, currentColumn);
                }

                e.Handled = true;
                return;
            }

            if (e.Key is Key.Left or Key.Right)
            {
                try
                {
                    grid.CommitEdit(DataGridEditingUnit.Cell, true);
                    grid.CommitEdit(DataGridEditingUnit.Row, true);
                }
                catch { }

                var nextColumn = FindAdjacentEditableColumn(grid, currentColumn, e.Key == Key.Left);
                if (nextColumn != null)
                    MoveGridFocusToCell(grid, currentItem, nextColumn);

                e.Handled = true;
                return;
            }

            var targetRowIndex = e.Key == Key.Up ? rowIndex - 1 : rowIndex + 1;
            if (targetRowIndex >= 0 && targetRowIndex < grid.Items.Count)
            {
                var targetItem = grid.Items[targetRowIndex];
                var targetColumn = currentColumn.IsReadOnly
                    ? GetEditableColumns(grid).FirstOrDefault()
                    : currentColumn;
                if (targetColumn != null)
                    MoveGridFocusToCell(grid, targetItem, targetColumn);
            }

            e.Handled = true;
        }

        private void MoveGridFocusAfterEnter(DataGrid grid, object item, int rowIndex, DataGridColumn currentColumn)
        {
            if (rowIndex < 0)
                return;

            var nextColumn = FindAdjacentEditableColumn(grid, currentColumn, moveLeft: true);
            var targetRowIndex = rowIndex;
            if (nextColumn == null)
            {
                targetRowIndex++;
                if (targetRowIndex >= grid.Items.Count)
                {
                    AddEmptyBarcodeLineAndFocus(grid);
                    return;
                }

                var editableColumns = GetEditableColumns(grid);
                nextColumn = grid.FlowDirection == FlowDirection.RightToLeft
                    ? editableColumns.LastOrDefault()
                    : editableColumns.FirstOrDefault();
            }

            if (nextColumn != null)
                MoveGridFocusToCell(grid, grid.Items[targetRowIndex], nextColumn);
        }

        private DataGridColumn? FindAdjacentEditableColumn(DataGrid grid, DataGridColumn currentColumn, bool moveLeft)
        {
            var columns = GetEditableColumns(grid);
            var currentIndex = columns.IndexOf(currentColumn);
            if (currentIndex < 0)
                return columns.FirstOrDefault();

            var step = grid.FlowDirection == FlowDirection.RightToLeft
                ? (moveLeft ? 1 : -1)
                : (moveLeft ? -1 : 1);
            var targetIndex = currentIndex + step;
            return targetIndex >= 0 && targetIndex < columns.Count
                ? columns[targetIndex]
                : null;
        }

        private static List<DataGridColumn> GetEditableColumns(DataGrid grid)
        {
            return grid.Columns
                .Where(column => column.Visibility == Visibility.Visible && !column.IsReadOnly)
                .OrderBy(column => column.DisplayIndex)
                .ToList();
        }

        private void MoveGridFocusToCell(DataGrid grid, object item, DataGridColumn column)
        {
            grid.Dispatcher.BeginInvoke(() =>
            {
                var targetCell = new DataGridCellInfo(item, column);
                grid.SelectedCells.Clear();
                grid.SelectedCells.Add(targetCell);
                grid.SelectedIndex = grid.Items.IndexOf(item);
                grid.CurrentCell = targetCell;
                grid.ScrollIntoView(item, column);
                grid.UpdateLayout();
                grid.Focus();
                grid.BeginEdit();
                grid.UpdateLayout();

                var cell = FindCell(grid, item, column);
                var textBox = cell == null ? null : FindVisualChild<TextBox>(cell);
                if (textBox != null)
                {
                    textBox.Focus();
                    textBox.SelectAll();
                    return;
                }

                var comboBox = cell == null ? null : FindVisualChild<ComboBox>(cell);
                comboBox?.Focus();
            }, DispatcherPriority.Background);
        }

        private static DataGridCell? FindCell(DataGrid grid, object item, DataGridColumn column)
        {
            if (grid.ItemContainerGenerator.ContainerFromItem(item) is not DataGridRow row)
                return null;

            return FindVisualChildren<DataGridCell>(row)
                .FirstOrDefault(cell => cell.Column == column);
        }

        #endregion
        private void RequestBrowseReload()
        {
            _pendingBrowseReload = true;
            if (!_browseIsLoading)
            {
                _ = ReloadBrowseAsync();
            }
        }

        private async Task LoadBrowseSubCategoriesAsync()
        {
            await _posDataOperationSemaphore.WaitAsync();
            try
            {
                var result = await _stockService.GetPosBrowseSubCategoriesAsync();
                if (result == null || !result.Success || result.Data == null)
                    return;

                SubCategories.Clear();
                foreach (var subCategory in result.Data)
                    SubCategories.Add(subCategory);

                SyncCategoryTabSelection();
            }
            catch
            {
                // Keep POS usable even if subcategory loading fails.
            }
            finally
            {
                _posDataOperationSemaphore.Release();
            }
        }

        private void BrowseSearchDebounceTimer_Tick(object? sender, EventArgs e)
        {
            _browseSearchDebounceTimer.Stop();
            RequestBrowseReload();
        }

        private async Task ReloadBrowseAsync()
        {
            _pendingBrowseReload = false;
            _browsePageNumber = 1;
            _browseHasMore = true;

            Products.Clear();
            ProductSuggestions.Clear();
            FilteredProducts.Clear();
            _loadedProductIds.Clear();

            await LoadNextBrowsePageAsync(reset: true);
        }

        private async Task LoadNextBrowsePageAsync(bool reset = false)
        {
            if (_browseIsLoading || !_browseHasMore)
                return;

            _browseIsLoading = true;
            var loadingShown = false;
            await _posDataOperationSemaphore.WaitAsync();

            _browseLoadCts?.Cancel();
            _browseLoadCts?.Dispose();
            _browseLoadCts = new CancellationTokenSource();
            var token = _browseLoadCts.Token;

            try
            {
                if (reset)
                {
                    _loading.Show();
                    loadingShown = true;
                }

                var result = await _stockService.GetPosBrowsePageAsync(
                    _browsePageNumber,
                    BrowsePageSize,
                    _browseSearchText,
                    _selectedBrowseSubCategoryId);

                if (token.IsCancellationRequested)
                    return;

                if (result == null || !result.Success || result.Data == null)
                {
                    MessageBox.Show(result?.Message ?? UiText.T("خطأ عند تحميل المنتجات", "Error loading products"), UiText.T("خطأ", "Error"));
                    _browseHasMore = false;
                    return;
                }

                var items = result.Data.Items?.ToList() ?? new List<PosBrowseItemDto>();
                foreach (var item in items)
                {
                    if (item.ProductId <= 0 || !_loadedProductIds.Add(item.ProductId))
                        continue;

                    var product = new ProductReadDto
                    {
                        Id = item.ProductId,
                        Name = item.Name,
                        ITEMCODE = item.ItemCode,
                        SubCategoryId = item.SubCategoryId,
                        TaxExempt = item.TaxExempt,
                        TaxRate = item.TaxRate,
                        CurrentStockQuantity = item.AvailableQuantity,
                        CurrentSalePrice = item.CurrentSalePrice,
                        LastCostIncludingTax = item.LastCostIncludingTax,
                        AverageCostIncludingTax = item.AverageCostIncludingTax
                    };

                    Products.Add(product);
                    FilteredProducts.Add(product);
                    ProductSuggestions.Add(product);
                }

                _browseHasMore = (_browsePageNumber * BrowsePageSize) < result.Data.TotalCount &&
                                items.Count == BrowsePageSize;

                if (items.Count > 0)
                    _browsePageNumber++;
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                    MessageBox.Show($"{UiText.T("خطأ عند تحميل المنتجات", "Error loading products")}: {ex.Message}", UiText.T("خطأ", "Error"));
            }
            finally
            {
                _browseIsLoading = false;
                if (loadingShown)
                    _loading.Hide();

                if (_pendingBrowseReload && !_browseIsLoading)
                    _ = ReloadBrowseAsync();

                _posDataOperationSemaphore.Release();
            }
        }

        private void RefreshProductBrowseState()
        {
            // Browse is server-driven in phase 1. Keep subcategory tabs as-is (already bound from stock load elsewhere if needed).
            SyncCategoryTabSelection();
        }

        private void SyncCategoryTabSelection()
        {
            foreach (var toggleButton in FindVisualChildren<ToggleButton>(CategoryTabsControl))
            {
                if (toggleButton.Tag is SubCategoryReadDto subCategory)
                {
                    toggleButton.IsChecked = _selectedBrowseSubCategoryId.HasValue &&
                                             subCategory.Id == _selectedBrowseSubCategoryId.Value;
                }
                else
                {
                    toggleButton.IsChecked = false;
                }
            }
        }

        private void CategoryTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleButton toggleButton || toggleButton.Tag is not SubCategoryReadDto subCategory)
                return;

            _selectedBrowseSubCategoryId = toggleButton.IsChecked == true ? subCategory.Id : null;
            SyncCategoryTabSelection();
            RequestBrowseReload();
        }

        private void ProductSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _browseSearchText = ProductSearchBox.Text.Trim();
            _browseSearchDebounceTimer.Stop();
            _browseSearchDebounceTimer.Start();
        }

        private async void ProductCardsScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Infinite scroll: load the next page when the user is near the bottom.
            if (e.ExtentHeight <= 0 || e.ViewportHeight <= 0)
                return;

            if (e.VerticalOffset + e.ViewportHeight < e.ExtentHeight - 240)
                return;

            await LoadNextBrowsePageAsync();
        }

        private async void ProductCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not ProductReadDto product)
                return;

            await AddProductToInvoiceAsync(product, moveFocusToQuantity: false);
            FocusBarcodeGridCellDeferred();
        }

        private void ResetProductDropdownState()
        {
            _loadedProductIds.Clear();
            Products.Clear();
            ProductSuggestions.Clear();
        }

        private async Task LoadSellableProductsAsync()
        {
            // Phase 1: use the lightweight POS browse API instead of loading full stock graphs.
            var result = await _stockService.GetPosBrowsePageAsync(1, BrowsePageSize, null, null);
            if (result == null || !result.Success || result.Data == null)
                return;

            Products.Clear();
            ProductSuggestions.Clear();
            _loadedProductIds.Clear();

            foreach (var item in result.Data.Items ?? Array.Empty<PosBrowseItemDto>())
            {
                if (item.ProductId <= 0 || !_loadedProductIds.Add(item.ProductId))
                    continue;

                var product = new ProductReadDto
                {
                    Id = item.ProductId,
                    Name = item.Name,
                    ITEMCODE = item.ItemCode,
                    SubCategoryId = item.SubCategoryId,
                    TaxExempt = item.TaxExempt,
                    TaxRate = item.TaxRate,
                    CurrentStockQuantity = item.AvailableQuantity,
                    CurrentSalePrice = item.CurrentSalePrice,
                    LastCostIncludingTax = item.LastCostIncludingTax,
                    AverageCostIncludingTax = item.AverageCostIncludingTax
                };

                Products.Add(product);
                ProductSuggestions.Add(product);
            }
        }

        private async Task EnsureSelectedProductVisibleAsync(ProductReadDto? selectedProduct)
        {
            if (selectedProduct == null || _loadedProductIds.Contains(selectedProduct.Id))
                return;

            _loadedProductIds.Add(selectedProduct.Id);
            Products.Add(selectedProduct);
            ProductSuggestions.Add(selectedProduct);
            await Task.CompletedTask;
        }
        private void RecalculateTotals()
        {
            var isReturnInvoice = _currentInvoice.InvoiceType is InvoiceType.Return or InvoiceType.PurchaseReturn;

            decimal GetSignedLineTotal(InvoiceLineWriteDto line)
                => line is ReturnInvoiceLine returnLine
                    ? -(returnLine.ReturnedQuantity * line.UnitPrice)
                    : line.Quantity * line.UnitPrice;

            decimal GetSignedLineSubtotal(InvoiceLineWriteDto line)
            {
                if (line is not ReturnInvoiceLine returnLine)
                    return line.LineSubTotal;

                var signedTotal = -(returnLine.ReturnedQuantity * line.UnitPrice);
                var divisor = line.TaxExempt ? 1m : 1m + Math.Max(0m, line.TaxRate) / 100m;
                return line.TaxExempt || divisor <= 0m
                    ? signedTotal
                    : Math.Round(signedTotal / divisor, 3);
            }

            var grossSales = isReturnInvoice
                ? _invoiceLines.Sum(GetSignedLineTotal)
                : _invoiceLines.Sum(l => l.Quantity * l.UnitPrice);
            _currentInvoice.SubTotal = isReturnInvoice
                ? _invoiceLines.Sum(GetSignedLineSubtotal)
                : _invoiceLines.Sum(l => l.LineSubTotal);
            _currentInvoice.TotalTax = isReturnInvoice
                ? _invoiceLines.Sum(line => GetSignedLineTotal(line) - GetSignedLineSubtotal(line))
                : _invoiceLines.Sum(l => l.TaxAmount);
            _currentInvoice.TotalCOGS = isReturnInvoice
                ? _invoiceLines.Sum(line => line is ReturnInvoiceLine returnLine
                    ? -(returnLine.ReturnedQuantity * line.UnitCost)
                    : line.Quantity * line.UnitCost)
                : _invoiceLines.Sum(l => l.Quantity * l.UnitCost);
            var discount = grossSales > 0m
                ? Math.Clamp(_currentInvoice.DiscountAmount ?? 0m, 0m, grossSales)
                : 0m;
            _currentInvoice.DiscountAmount = discount;
            _currentInvoice.NetSales = _currentInvoice.SubTotal - discount;
            _currentInvoice.GrossProfit = _currentInvoice.NetSales - _currentInvoice.TotalCOGS;
            _currentInvoice.TotalAmount = grossSales - discount;

            TotalTextBlock.Text = _currentInvoice.TotalAmount.ToString("0.00000");
            DiscountTextBox.Text = discount.ToString("0.00000");
            DiscountSummaryText.Text = discount.ToString("0.00000");
            NetTotalText.Text = _currentInvoice.TotalAmount.ToString("0.00000");
        }
        private ProductReadDto? FindProductForLine(InvoiceLineWriteDto line)
        {
            if (line.SelectedProduct != null)
                return line.SelectedProduct;

            return Products.FirstOrDefault(p => p.Id == line.ProductId);
        }

        private void CacheStocks(IEnumerable<StockReadDto> stocks)
        {
            var groupedStocks = stocks
                .Where(s => s != null && s.ProductId > 0 && s.ProductUnitId > 0)
                .GroupBy(s => s.ProductId);

            foreach (var group in groupedStocks)
            {
                foreach (var stock in group)
                {
                    _stockLookup[(stock.ProductId, stock.ProductUnitId)] = stock;

                    var productUnit = stock.Product?.ProductUnits?.FirstOrDefault(unit => unit.Id == stock.ProductUnitId);
                    if (productUnit != null)
                    {
                        productUnit.PurchasePrice = stock.PurchasePrice;
                    }
                }

                var firstStock = ResolvePreferredStock(group);

                if (firstStock?.Product != null)
                {
                    firstStock.Product.CurrentSalePrice = firstStock.Product.DefaultSalePrice;
                    firstStock.Product.CurrentPurchasePrice = firstStock.PurchasePrice;
                }
            }
        }

        private async Task<List<StockReadDto>> GetAvailableStocksForProductAsync(int productId)
        {
            var result = await _stockService.GetAllWithFilteringAndIncludeAsync(
                s => s.ProductId == productId && s.Quantity > 0,
                new Expression<Func<Stock, object>>[]
                {
                    s => s.Product,
                    s => s.Product.SubCategory,
                    s => s.Product.Brand,
                    s => s.Product.ProductUnits,
                    s => s.ProductUnit,
                    s => s.ProductUnit.Unit
                });

            var stocks = result?.Data?
                .Where(stock => stock.Product != null && stock.ProductUnit != null && stock.Quantity > 0)
                .ToList()
                ?? new List<StockReadDto>();

            CacheStocks(stocks);
            return stocks;
        }

        private static ProductUnitWriteDto MapAvailableUnit(StockReadDto stock)
        {
            return new ProductUnitWriteDto
            {
                Id = stock.ProductUnitId,
                ProductId = stock.ProductId,
                UnitId = stock.ProductUnit?.UnitId ?? 0,
                Unit = stock.ProductUnit?.Unit == null
                    ? null
                    : new UnitWriteDto
                    {
                        Id = stock.ProductUnit.Unit.Id,
                        Name = stock.ProductUnit.Unit.Name,
                        CreatedDate = stock.ProductUnit.Unit.CreatedDate,
                        UpdatedDate = stock.ProductUnit.Unit.UpdatedDate
                    },
                QuantityPerUnit = stock.ProductUnit?.QuantityPerUnit ?? 1m,
                PurchasePrice = stock.PurchasePrice,
                SalePrice = stock.SalePrice,
                IsDefaultSaleUnit = stock.ProductUnit?.IsDefaultSaleUnit ?? false,
                IsDefaultPurchaseUnit = stock.ProductUnit?.IsDefaultPurchaseUnit ?? false
            };
        }

        private static ProductUnitWriteDto MapProductUnit(ProductUnitReadDto unit)
        {
            return new ProductUnitWriteDto
            {
                Id = unit.Id,
                ProductId = unit.ProductId,
                UnitId = unit.UnitId,
                Unit = unit.Unit == null
                    ? null
                    : new UnitWriteDto
                    {
                        Id = unit.Unit.Id,
                        Name = unit.Unit.Name ?? string.Empty,
                        CreatedDate = unit.Unit.CreatedDate,
                        UpdatedDate = unit.Unit.UpdatedDate
                    },
                QuantityPerUnit = unit.QuantityPerUnit,
                PurchasePrice = unit.PurchasePrice,
                SalePrice = unit.SalePrice,
                IsBaseUnit = unit.IsBaseUnit,
                IsDefaultSaleUnit = unit.IsDefaultSaleUnit,
                IsDefaultPurchaseUnit = unit.IsDefaultPurchaseUnit
            };
        }

        private static bool HasHydratedUnits(ProductReadDto? product)
        {
            return product?.ProductUnits?.Any(unit => unit.Unit != null && !string.IsNullOrWhiteSpace(unit.Unit.Name)) == true;
        }

        private ProductReadDto? ResolveProductForUnits(int productId)
        {
            var product = Products.FirstOrDefault(p => p.Id == productId);
            if (HasHydratedUnits(product))
                return product;

            return null;
        }

        private async Task<List<ProductUnitReadDto>> LoadHydratedUnitsAsync(int productId)
        {
            if (_hydratedProductUnits.TryGetValue(productId, out var cachedUnits))
                return cachedUnits;

            var result = await _productUnitService.GetAllWithFilteringAndIncludeAsync(
                unit => unit.ProductId == productId,
                unit => unit.Unit);

            var units = result.Data ?? new List<ProductUnitReadDto>();
            _hydratedProductUnits[productId] = units;
            return units;
        }

        private async Task<ProductReadDto?> ResolveProductForUnitsAsync(int productId)
        {
            if (_hydratedProducts.TryGetValue(productId, out var hydratedProduct))
                return hydratedProduct;

            var cachedProduct = ResolveProductForUnits(productId);
            if (cachedProduct != null)
            {
                _hydratedProducts[productId] = cachedProduct;
                return cachedProduct;
            }

            if (productId <= 0)
                return null;

            try
            {
                var result = await _productService.GetByIdWithUnitsAsync(productId);
                var product = result?.Data;                if (product == null)
                    return null;

                _hydratedProducts[productId] = product;
                if (product.ProductUnits != null)
                    _hydratedProductUnits[productId] = product.ProductUnits.ToList();
                return product;
            }
            catch
            {
                return null;
            }
        }

        private async Task<ProductReadDto?> ResolveProductWithUnitsAsync(ProductReadDto product)
        {
            if (product.Id <= 0)
                return product;

            return await ResolveProductForUnitsAsync(product.Id) ?? product;
        }

        private async Task<List<ProductUnitWriteDto>> GetAvailableUnitsForProductAsync(int productId)
        {
            var product = await ResolveProductForUnitsAsync(productId);
            return product?.ProductUnits?
                .Select(MapProductUnit)
                .OrderByDescending(unit => unit.IsDefaultSaleUnit)
                .ThenBy(unit => unit.DisplayName)
                .ToList()
                ?? new List<ProductUnitWriteDto>();
        }

        private StockReadDto? ResolvePreferredStock(IEnumerable<StockReadDto> stocks, int? preferredUnitId = null)
        {
            var stockList = stocks?.Where(stock => stock.Quantity > 0).ToList() ?? new List<StockReadDto>();
            if (stockList.Count == 0)
                return null;

            if (preferredUnitId.HasValue)
            {
                var preferred = stockList.FirstOrDefault(stock => stock.ProductUnitId == preferredUnitId.Value);
                if (preferred != null)
                    return preferred;
            }

            var product = stockList.First().Product;
            var defaultUnitId = ProductUnitSelector.GetDefaultSaleUnit(product?.ProductUnits)?.Id;
            if (defaultUnitId.HasValue)
            {
                var defaultStock = stockList.FirstOrDefault(stock => stock.ProductUnitId == defaultUnitId.Value);
                if (defaultStock != null)
                    return defaultStock;
            }

            return stockList
                .OrderByDescending(stock => stock.Quantity)
                .ThenBy(stock => stock.ProductUnit?.Unit?.Name)
                .FirstOrDefault();
        }

        private decimal GetDefaultSalePrice(InvoiceLineWriteDto line)
        {
            var product = FindProductForLine(line);
            var unit = product?.ProductUnits?.FirstOrDefault(u => u.Id == line.ProductUnitId)
                       ?? ProductUnitSelector.GetDefaultSaleUnit(product?.ProductUnits);

            if (unit != null)
                return unit.SalePrice;

            return _stockLookup.TryGetValue((line.ProductId, line.ProductUnitId), out var stock)
                ? stock.SalePrice
                : line.UnitPrice;
        }

        private void ResetPriceBelowCost(InvoiceLineWriteDto line, decimal enteredPrice, TextBox? editor = null)
        {
            var defaultPrice = GetDefaultSalePrice(line);
            ShowPaymentValidationMessage(
                UiText.T(
                    $"لا يمكن بيع الصنف {line.ProductName} بسعر أقل من التكلفة. السعر المدخل: {enteredPrice:0.00000}، التكلفة: {line.UnitCost:0.00000}. سيتم إعادة السعر الافتراضي: {defaultPrice:0.00000}.",
                    $"Cannot sell {line.ProductName} below cost. Entered price: {enteredPrice:0.00000}, cost: {line.UnitCost:0.00000}. The default price will be restored: {defaultPrice:0.00000}."),
                UiText.T("تنبيه", "Notice"));

            line.UnitPrice = defaultPrice;
            if (editor != null)
                editor.Text = defaultPrice.ToString("0.00000");
        }

        private void RecalculateLineFromCurrentValues(InvoiceLineWriteDto line)
        {
            if (line.Quantity == 0)
                return;

            var lineTotal = line.Quantity * line.UnitPrice;
            var costTotal = line.Quantity * line.UnitCost;
            var product = FindProductForLine(line);
            var taxExempt = product?.TaxExempt ?? line.TaxExempt;
            var taxRate = taxExempt ? 0m : Math.Max(0m, product?.TaxRate ?? line.TaxRate);
            var divisor = 1m + taxRate / 100m;
            var lineSubTotal = taxExempt || divisor <= 0m
                ? lineTotal
                : Math.Round(lineTotal / divisor, 3);

            line.TaxExempt = taxExempt;
            line.TaxRate = taxRate;
            line.LineSubTotal = lineSubTotal;
            line.TaxAmount = Math.Round(lineTotal - lineSubTotal, 3);
            line.ProfitBeforeTax = lineSubTotal - costTotal;
            line.Profit = line.ProfitBeforeTax;
            line.BaseQuantity = line.Quantity * (line.QuantityPerUnitSnapshot > 0 ? line.QuantityPerUnitSnapshot : 1m);
        }

        private static InvoiceLineWriteDto CloneLineSnapshot(InvoiceLineWriteDto source, decimal quantity, string? originalInvoiceId = null)
        {
            var divisor = source.Quantity == 0 ? 1 : Math.Abs(source.Quantity);

            return new InvoiceLineWriteDto
            {
                ProductId = source.ProductId,
                ProductName = source.ProductName,
                ProductUnitId = source.ProductUnitId,
                QuantityPerUnitSnapshot = source.QuantityPerUnitSnapshot,
                BaseQuantity = (source.BaseQuantity / divisor) * quantity,
                UnitPrice = source.UnitPrice,
                UnitCost = source.UnitCost,
                AvailableQuantitySnapshot = source.AvailableQuantitySnapshot,
                UnitNameSnapshot = source.UnitNameSnapshot,
                TaxExempt = source.TaxExempt,
                TaxRate = source.TaxRate,
                Quantity = quantity,
                LineSubTotal = (source.LineSubTotal / divisor) * quantity,
                TaxAmount = (source.TaxAmount / divisor) * quantity,
                ProfitBeforeTax = (source.ProfitBeforeTax / divisor) * quantity,
                Profit = (source.Profit / divisor) * quantity,
                OriginalInvoiceId = originalInvoiceId ?? source.OriginalInvoiceId
            };
        }
        private async Task<bool> ApplyLinePricingFromProductAsync(InvoiceLineWriteDto line, ProductReadDto product, int? preferredUnitId = null)
        {
            if (!HasHydratedUnits(product))
                product = await ResolveProductWithUnitsAsync(product);

            var productUnits = product.ProductUnits?.ToList() ?? new List<ProductUnitReadDto>();
            if (productUnits.Count == 0)
                return false;

            var selectedUnit = preferredUnitId.HasValue
                ? productUnits.FirstOrDefault(unit => unit.Id == preferredUnitId.Value)
                : ProductUnitSelector.GetDefaultSaleUnit(productUnits) ?? productUnits.FirstOrDefault();

            if (selectedUnit == null)
                return false;

            var stocks = await GetAvailableStocksForProductAsync(product.Id);
            var stock = stocks.FirstOrDefault(item => item.ProductUnitId == selectedUnit.Id);

            var quantityPerUnit = selectedUnit.QuantityPerUnit > 0 ? selectedUnit.QuantityPerUnit : 1m;
            var unitPrice = selectedUnit.SalePrice;
            var lineTotal = line.Quantity * unitPrice;
            var unitCost = stock?.PurchasePrice ?? selectedUnit.PurchasePrice;
            var costTotal = line.Quantity * unitCost;
            var taxExempt = product.TaxExempt ?? false;
            var taxRate = taxExempt ? 0m : Math.Max(0m, product.TaxRate ?? 0m);
            var divisor = 1m + taxRate / 100m;
            var lineSubTotal = taxExempt || divisor <= 0m
                ? lineTotal
                : Math.Round(lineTotal / divisor, 3);

            line.SelectedProduct = product;
            line.ProductId = product.Id;
            line.ProductName = product.Name;
            line.ProductUnitId = selectedUnit.Id;
            line.ProductUnit = MapProductUnit(selectedUnit);
            line.QuantityPerUnitSnapshot = quantityPerUnit;
            line.BaseQuantity = line.Quantity * line.QuantityPerUnitSnapshot;
            line.UnitPrice = unitPrice;
            line.UnitCost = unitCost;
            line.TaxExempt = taxExempt;
            line.TaxRate = taxRate;
            line.LineSubTotal = lineSubTotal;
            line.TaxAmount = Math.Round(lineTotal - lineSubTotal, 3);
            line.ProfitBeforeTax = lineSubTotal - costTotal;
            line.Profit = line.ProfitBeforeTax;
            line.AvailableQuantitySnapshot = product.CurrentStockQuantity;
            line.UnitNameSnapshot = selectedUnit.Unit?.Name;

            return true;
        }
        private async void InvoiceGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit || e.Row.Item is not InvoiceLineWriteDto line)
                return;

            if (e.EditingElement is not TextBox textBox)
                return;

            var header = e.Column.Header?.ToString();
            if (string.IsNullOrWhiteSpace(header))
                return;

            if (_currentInvoice.InvoiceType is InvoiceType.Return or InvoiceType.PurchaseReturn &&
                e.Row.Item is ReturnInvoiceLine returnLine &&
                header.Contains("Returned Quantity", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseDecimalInput(textBox.Text, out var returnedQuantity) || returnedQuantity < 0 || returnedQuantity > returnLine.ReturnableQuantity)
                {
                    MessageBox.Show(
                        UiText.T($"الكمية المرتجعة يجب أن تكون بين صفر و{returnLine.ReturnableQuantity:0.00000}.", $"Returned quantity must be between zero and {returnLine.ReturnableQuantity:0.00000}."),
                        UiText.T("تنبيه", "Notice"));
                    returnLine.ReturnedQuantity = 0m;
                    line.Quantity = 0m;
                }
                else
                {
                    returnLine.ReturnedQuantity = returnedQuantity;
                    line.Quantity = -returnedQuantity;
                    line.BaseQuantity = -returnedQuantity * (line.QuantityPerUnitSnapshot > 0 ? line.QuantityPerUnitSnapshot : 1m);
                    RecalculateLineFromCurrentValues(line);
                }

                RecalculateTotals();
                return;
            }

            if (header.Contains("الكمية") || header.Contains(UiText.Translate("الكمية")))
            {
                if (!TryParseDecimalInput(textBox.Text, out var quantity) || quantity <= 0)
                {
                    MessageBox.Show(UiText.T("يرجى إدخال كمية صحيحة أكبر من صفر.", "Please enter a valid quantity greater than zero."), UiText.T("تنبيه", "Notice"));
                    line.Quantity = _currentInvoice.InvoiceType is InvoiceType.Return or InvoiceType.Exchange
                        ? -Math.Max(Math.Abs(line.Quantity), 1m)
                        : 1m;
                }
                else
                {
                    if (_currentInvoice.InvoiceType is InvoiceType.Return or InvoiceType.Exchange)
                    {
                        line.Quantity = -quantity;
                        line.BaseQuantity = -quantity * (line.QuantityPerUnitSnapshot > 0 ? line.QuantityPerUnitSnapshot : 1m);
                        RecalculateTotals();
                        InvoiceGrid.Items.Refresh();
                        return;
                    }

                    line.Quantity = quantity;

                    var availableQuantity = await GetAvailableQuantityForProductUnitAsync(line.ProductId, line.ProductUnitId);
                    line.AvailableQuantitySnapshot = availableQuantity;
                    if (availableQuantity <= 0)
                    {
                        MessageBox.Show(
                            UiText.T(
                                $"الصنف {line.ProductName} غير متوفر حالياً في المخزون، وتمت إزالته من الفاتورة.",
                                $"The item {line.ProductName} is currently unavailable in stock and was removed from the invoice."),
                            UiText.T("تنبيه", "Notice"));

                        _invoiceLines.Remove(line);
                        InvoiceGrid.Items.Refresh();
                        RecalculateTotals();
                        return;
                    }

                    if (quantity > availableQuantity)
                    {
                        MessageBox.Show(
                            UiText.T(
                                $"الكمية المطلوبة للصنف {line.ProductName} أكبر من الكمية المتوفرة. الكمية المطلوبة: {quantity:0.00000}، الكمية المتوفرة: {availableQuantity:0.00000}. سيتم تعديل الكمية إلى الكمية المتوفرة.",
                                $"The requested quantity for {line.ProductName} is greater than available stock. Requested: {quantity:0.00000}, available: {availableQuantity:0.00000}. The quantity will be adjusted to the available quantity."),
                            UiText.T("تنبيه", "Notice"));

                        line.Quantity = availableQuantity;
                        textBox.Text = availableQuantity.ToString("0.00000");
                    }
                }
            }
            else if (header.Contains("السعر") || header.Contains(UiText.Translate("السعر")))
            {
                var originalUnitPrice = line.UnitPrice;

                if (!TryParseDecimalInput(textBox.Text, out var unitPrice))
                {
                    MessageBox.Show(UiText.T("يرجى إدخال سعر صحيح.", "Please enter a valid price."), UiText.T("تنبيه", "Notice"));
                }
                else
                {
                    line.UnitPrice = unitPrice;
                }
            }
            else
            {
                return;
            }

            if (header.Contains("الكمية") || header.Contains(UiText.Translate("الكمية")))
            {
                _pendingFefoEditedLine = line;
                _hasPendingFefoSplit = true;
                return;
            }

            RecalculateLineFromCurrentValues(line);
            RecalculateTotals();
        }

        private async void InvoiceGrid_CurrentCellChanged(object? sender, EventArgs e)
        {
            await ProcessPendingFefoSplitAsync();
        }

        private async Task<InvoiceLineWriteDto?> ProcessPendingFefoSplitAsync()
        {
            if (!_hasPendingFefoSplit || _pendingFefoEditedLine == null || _isProcessingPendingFefoSplit)
                return null;

            var pendingLine = _pendingFefoEditedLine;
            var resetVersion = _posResetVersion;
            _pendingFefoEditedLine = null;
            _hasPendingFefoSplit = false;
            _isProcessingPendingFefoSplit = true;

            try
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    InvoiceGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                    InvoiceGrid.CommitEdit(DataGridEditingUnit.Row, true);
                }, DispatcherPriority.Background);

                if (resetVersion != _posResetVersion)
                    return null;

                return await SplitEditedLineByFefoAsync(pendingLine);
            }
            finally
            {
                _isProcessingPendingFefoSplit = false;
            }
        }
        private bool TryGetActiveCashierSession(out CashierSessionReadDto? session)
        {
            session = _userSession.CurrentCashierSession;
            if (session != null)
                return true;

            ShowPaymentValidationMessage(
                UiText.T("لا توجد جلسة كاشير مفتوحة. الرجاء فتح جلسة أولاً.", "There is no open cashier session. Please open a session first."),
                UiText.T("خطأ", "Error"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            RefreshSessionButtons();
            return false;
        }


        private async void BarcodeTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            var barcode = BarcodeTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(barcode)) return;

            e.Handled = true;
            BarcodeTextBox.Clear();
            try
            {
                await ProcessBarcodeAsync(barcode);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, UiText.T("Ø®Ø·Ø£", "Error"));
            }
            finally
            {
                EnsureEmptyBarcodeLine();
                FocusBarcodeInput();
            }
            return;

            BarcodeTextBox.Clear();
            await _addProductGate.WaitAsync();
            try
            {
                //var result = await _productService.(barcode);
                var result = await _productService.GetAllWithFilteringAndIncludeAsync(
                            p => p.ITEMCODE.ToString() == barcode ||
                                 p.ProductUnits!.Any(unit => unit.AlternateBarcode == barcode),
                            new Expression<Func<Product, object>>[]
                            {
                                p => p.SubCategory,
                                p => p.Brand,
                                p => p.ProductUnits
                            });
                if (result == null || result.Data == null || result.Data.Count == 0)
                {
                    var createProductPrompt = MessageBox.Show(
                        UiText.T(
                            "الصنف غير موجود. هل تريد إنشاءه الآن؟",
                            "The item was not found. Do you want to create it now?"),
                        UiText.T("تنبيه", "Notice"),
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (createProductPrompt == MessageBoxResult.Yes)
                    {
                        var createProductWindow = _serviceProvider.GetRequiredService<CreateProduct>();
                        createProductWindow.Owner = this;
                        createProductWindow.InitialItemCode = barcode;
                        createProductWindow.ShowDialog();
                    }

                    return;
                }
                var product = result.Data.FirstOrDefault();
                if (product == null)
                {
                    MessageBox.Show(UiText.T("الصنف غير موجود.", "The item was not found."), UiText.T("تنبيه", "Notice"));
                    return;
                }

                await AddProductToInvoiceCoreAsync(product, moveFocusToQuantity: false);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, UiText.T("خطأ", "Error"));
            }
            finally
            {
                _addProductGate.Release();
                EnsureEmptyBarcodeLine();
                FocusBarcodeInput();
            }
        }

        private async Task<ProductReadDto?> FindProductByBarcodeAsync(string barcode)
        {
            var result = await _productService.GetAllWithFilteringAndIncludeAsync(
                p => p.ITEMCODE.ToString() == barcode ||
                     p.ProductUnits!.Any(unit => unit.AlternateBarcode == barcode),
                new Expression<Func<Product, object>>[]
                {
                    p => p.SubCategory,
                    p => p.Brand,
                    p => p.ProductUnits
                });

            return result?.Data?.FirstOrDefault();
        }

        private async Task<bool> ProcessBarcodeAsync(string barcode, InvoiceLineWriteDto? targetLine = null)
        {
            await _addProductGate.WaitAsync();
            try
            {
                var product = await FindProductByBarcodeAsync(barcode);
                if (product == null)
                {
                    var localizedCreateProductPrompt = MessageBox.Show(
                        UiText.T(
                            "الصنف غير موجود. هل تريد إنشاءه الآن؟",
                            "The item was not found. Do you want to create it now?"),
                        UiText.T("تنبيه", "Notice"),
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (localizedCreateProductPrompt == MessageBoxResult.Yes)
                    {
                        var createProductWindow = _serviceProvider.GetRequiredService<CreateProduct>();
                        createProductWindow.Owner = this;
                        createProductWindow.InitialItemCode = barcode;
                        createProductWindow.ShowDialog();
                    }

                    return false;

                    var createProductPrompt = MessageBox.Show(
                        UiText.T("Ø§Ù„ØµÙ†Ù ØºÙŠØ± Ù…ÙˆØ¬ÙˆØ¯. Ù‡Ù„ ØªØ±ÙŠØ¯ Ø¥Ù†Ø´Ø§Ø¡Ù‡ Ø§Ù„Ø¢Ù†ØŸ", "The item was not found. Do you want to create it now?"),
                        UiText.T("ØªÙ†Ø¨ÙŠÙ‡", "Notice"),
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (createProductPrompt == MessageBoxResult.Yes)
                    {
                        var createProductWindow = _serviceProvider.GetRequiredService<CreateProduct>();
                        createProductWindow.Owner = this;
                        createProductWindow.InitialItemCode = barcode;
                        createProductWindow.ShowDialog();
                    }

                    return false;
                }

                var preferredUnitId = product.ProductUnits?
                    .FirstOrDefault(unit =>
                        !string.IsNullOrWhiteSpace(unit.AlternateBarcode) &&
                        string.Equals(unit.AlternateBarcode.Trim(), barcode.Trim(), StringComparison.OrdinalIgnoreCase))?
                    .Id;

                return await AddProductToInvoiceCoreAsync(
                    product,
                    moveFocusToQuantity: false,
                    preferredUnitId: preferredUnitId);
            }
            finally
            {
                _addProductGate.Release();
            }
        }

        private async void ProductBarcodeTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || sender is not TextBox textBox ||
                textBox.DataContext is not InvoiceLineWriteDto line)
                return;

            var barcode = textBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(barcode))
                return;

            e.Handled = true;
            if (!await ProcessBarcodeAsync(barcode, line))
                return;

            InvoiceGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            InvoiceGrid.CommitEdit(DataGridEditingUnit.Row, true);
            if (line.ProductId <= 0)
                _invoiceLines.Remove(line);
            AddEmptyBarcodeLineAndFocus(InvoiceGrid);
            return;

            try
            {
                var product = await FindProductByBarcodeAsync(barcode);
                if (product == null)
                {
                    MessageBox.Show(
                        UiText.T("الصنف غير موجود.", "The item was not found."),
                        UiText.T("تنبيه", "Notice"));
                    return;
                }

                line.Quantity = 1m;
                if (!await ApplyLinePricingFromProductAsync(line, product))
                {
                    MessageBox.Show(
                        UiText.T("لا توجد وحدات معرفة لهذا الصنف.", "There are no units defined for this item."),
                        UiText.T("تنبيه", "Notice"));
                    return;
                }

                RecalculateTotals();
                InvoiceGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                InvoiceGrid.CommitEdit(DataGridEditingUnit.Row, true);

                AddEmptyBarcodeLineAndFocus(InvoiceGrid);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, UiText.T("خطأ", "Error"));
            }
        }

        private async Task<bool> AddProductToInvoiceAsync(ProductReadDto product, bool moveFocusToQuantity = true, int? preferredUnitId = null, decimal quantity = 1m, decimal? unitPriceOverride = null)
        {
            await _addProductGate.WaitAsync();
            try
            {
                return await AddProductToInvoiceCoreAsync(product, moveFocusToQuantity, preferredUnitId, quantity, unitPriceOverride);
            }
            finally
            {
                _addProductGate.Release();
            }
        }

        private async Task<bool> AddProductToInvoiceCoreAsync(ProductReadDto product, bool moveFocusToQuantity = true, int? preferredUnitId = null, decimal quantity = 1m, decimal? unitPriceOverride = null)
        {
            if (product == null || quantity <= 0)
            {
                if (quantity <= 0)
                    MessageBox.Show(UiText.T("الكمية يجب أن تكون أكبر من صفر.", "Quantity must be greater than zero."), UiText.T("تنبيه", "Notice"));
                return false;
            }

            if (_currentInvoice.InvoiceType is InvoiceType.Return or InvoiceType.PurchaseReturn)
                return await AddProductToReturnInvoiceCoreAsync(product, preferredUnitId, quantity);

            var timing = System.Diagnostics.Stopwatch.StartNew();
            var stepTiming = System.Diagnostics.Stopwatch.StartNew();
            LogPosTiming("add item start", timing, stepTiming);

            product = await ResolveProductWithUnitsAsync(product);
            LogPosTiming("add item resolve product and units", timing, stepTiming);

            var productUnits = product.ProductUnits?.ToList() ?? new List<ProductUnitReadDto>();
            var selectedUnit = (preferredUnitId.HasValue ? productUnits.FirstOrDefault(unit => unit.Id == preferredUnitId.Value) : null) ?? ProductUnitSelector.GetDefaultSaleUnit(productUnits) ?? productUnits.FirstOrDefault();
            if (selectedUnit == null)
            {
                MessageBox.Show(
                    UiText.T(
                        "الصنف موجود في النظام لكن لا توجد له وحدات معرفة.",
                        "The item exists in the system, but no units are defined for it."),
                    UiText.T("تنبيه", "Notice"));
                return false;
            }

            var existingLine = _invoiceLines
                .FirstOrDefault(l => l.ProductId == product.Id && l.ProductUnitId == selectedUnit.Id);

            if (existingLine != null)
            {
                existingLine.Quantity += quantity;
                existingLine.UnitNameSnapshot =
                    selectedUnit.Unit?.Name
                    ?? existingLine.UnitNameSnapshot
                    ?? existingLine.ProductUnit?.Unit?.Name;
            }
            else
            {
                var line = _invoiceLines.FirstOrDefault(l =>
                    l.ProductId <= 0 &&
                    l.ProductUnitId <= 0 &&
                    string.IsNullOrWhiteSpace(l.ProductName) &&
                    l.UnitPrice == 0m &&
                    l.UnitCost == 0m);

                if (line == null)
                {
                    line = new InvoiceLineWriteDto();
                    _invoiceLines.Add(line);
                }

                line.Quantity = quantity;
                if (!await ApplyLinePricingFromProductAsync(line, product, selectedUnit.Id))
                {
                    MessageBox.Show(UiText.T("لا توجد وحدات معرفة لهذا الصنف.", "There are no units defined for this item."), UiText.T("تنبيه", "Notice"));
                    return false;
                }
                if (unitPriceOverride.HasValue)
                    line.UnitPrice = unitPriceOverride.Value;

                LogPosTiming("add item apply pricing", timing, stepTiming);
            }
            var targetUnitId = selectedUnit.Id;
            var availableAfterAllocation = await SplitDraftLinesByFefoAsync(product.Id, targetUnitId);
            LogPosTiming("add item FEFO allocation", timing, stepTiming);
            var refreshedAvailableQuantity = availableAfterAllocation
                ?? await GetAvailableQuantityForProductUnitAsync(product.Id, targetUnitId);
            LogPosTiming("add item available quantity", timing, stepTiming);
            foreach (var invoiceLine in _invoiceLines.Where(l => l.ProductId == product.Id && l.ProductUnitId == targetUnitId))
            {
                invoiceLine.AvailableQuantitySnapshot = refreshedAvailableQuantity;
                invoiceLine.UnitNameSnapshot =
                    selectedUnit.Unit?.Name
                    ?? invoiceLine.UnitNameSnapshot
                    ?? invoiceLine.ProductUnit?.Unit?.Name;
            }

            RecalculateTotals();
LogPosTiming("add item UI refresh and totals", timing, stepTiming);
            PosPerformanceLogger.Write("add item total", timing.ElapsedMilliseconds, timing.ElapsedMilliseconds);

            if (moveFocusToQuantity)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (InvoiceGrid.Items.Count == 0)
                        return;

                    var targetLine = _invoiceLines
                        .LastOrDefault(l => l.ProductId == product.Id && l.ProductUnitId == targetUnitId)
                        ?? _invoiceLines.LastOrDefault();
                    if (targetLine == null)
                        return;

                    MoveGridFocusToColumn(InvoiceGrid, targetLine, "الكمية");
                }), System.Windows.Threading.DispatcherPriority.Input);
            }

            return true;
        }

        private async Task<decimal?> SplitDraftLinesByFefoAsync(int productId, int productUnitId)
        {
            var matchingLines = _invoiceLines
                .Where(l => l.ProductId == productId && l.ProductUnitId == productUnitId && l.Quantity > 0)
                .ToList();

            if (!matchingLines.Any())
                return null;

            var templateLine = matchingLines.First();
            var insertIndex = _invoiceLines.IndexOf(templateLine);
            var totalQuantity = matchingLines.Sum(l => l.Quantity);

            var allocationResult = await _stockService.AllocateOutgoingAsync(new[]
            {
                new StockAllocationRequestDto
                {
                    ProductId = productId,
                    ProductUnitId = productUnitId,
                    Quantity = totalQuantity
                }
            });

            if (!allocationResult.Success || allocationResult.Data == null || allocationResult.Data.Count == 0)
                return null;

            foreach (var line in matchingLines)
                _invoiceLines.Remove(line);

            foreach (var allocation in allocationResult.Data)
            {
                var splitLine = new InvoiceLineWriteDto
                {
                    SelectedProduct = templateLine.SelectedProduct,
                    ProductId = templateLine.ProductId,
                    ProductName = templateLine.ProductName,
                    ProductUnitId = templateLine.ProductUnitId,
                    Quantity = allocation.Quantity,
                    QuantityPerUnitSnapshot = allocation.QuantityPerUnitSnapshot,
                    BaseQuantity = allocation.BaseQuantity,
                    UnitPrice = templateLine.UnitPrice,
                    UnitCost = allocation.PurchasePrice,
                    TaxExempt = templateLine.TaxExempt,
                    TaxRate = templateLine.TaxRate,
                    ExpiryDate = allocation.ExpiryDate ?? templateLine.ExpiryDate,
                    OriginalInvoiceId = templateLine.OriginalInvoiceId,
                    CreatedDate = templateLine.CreatedDate,
                    UpdatedDate = DateTime.Now
                };

                RecalculateLineFromCurrentValues(splitLine);
                _invoiceLines.Insert(insertIndex++, splitLine);
            }

            var availableAfterAllocation = allocationResult.Data
                .Select(allocation => allocation.AvailableQuantityAfterAllocation)
                .FirstOrDefault();
            return availableAfterAllocation;
        }

        private async Task<InvoiceLineWriteDto?> SplitEditedLineByFefoAsync(InvoiceLineWriteDto sourceLine)
        {
            if (sourceLine.ProductId <= 0 || sourceLine.ProductUnitId <= 0 || sourceLine.Quantity <= 0)
            {
                RecalculateLineFromCurrentValues(sourceLine);
                RecalculateTotals();
                return sourceLine;
            }

            var matchingLines = _invoiceLines
                .Where(l => l.ProductId == sourceLine.ProductId && l.ProductUnitId == sourceLine.ProductUnitId && l.Quantity > 0)
                .ToList();

            if (!matchingLines.Any())
            {
                RecalculateLineFromCurrentValues(sourceLine);
                RecalculateTotals();
                return sourceLine;
            }

            var templateLine = matchingLines.FirstOrDefault(l => ReferenceEquals(l, sourceLine)) ?? matchingLines.First();
            var insertIndex = _invoiceLines.IndexOf(matchingLines.First());
            var totalQuantity = matchingLines.Sum(l => l.Quantity);

            var allocationResult = await AllocateInvoiceLinesAsync(sourceLine.ProductId, sourceLine.ProductUnitId, totalQuantity);

            if (!allocationResult.Success || allocationResult.Data == null || allocationResult.Data.Count == 0)
            {
                var availableQuantity = await GetAvailableQuantityForProductUnitAsync(sourceLine.ProductId, sourceLine.ProductUnitId);
                if (availableQuantity > 0)
                {
                    var adjustedResult = await AllocateInvoiceLinesAsync(sourceLine.ProductId, sourceLine.ProductUnitId, availableQuantity);
                    if (adjustedResult.Success && adjustedResult.Data != null && adjustedResult.Data.Count > 0)
                    {
                        MessageBox.Show(
                            UiText.T(
                                $"الكمية المطلوبة للصنف {sourceLine.ProductName} غير متوفرة. تم تعديل الكمية إلى الحد الأقصى المتاح: {availableQuantity:0.00000}",
                                $"The requested quantity for {sourceLine.ProductName} is not available. The quantity was adjusted to the maximum available: {availableQuantity:0.00000}"),
                            UiText.T("تنبيه", "Notice"));

                        return ReplaceInvoiceLinesFromAllocations(matchingLines, templateLine, insertIndex, adjustedResult.Data);
                    }
                }

                foreach (var line in matchingLines)
                    _invoiceLines.Remove(line);

                InvoiceGrid.Items.Refresh();
                RecalculateTotals();
                MessageBox.Show(
                    UiText.T(
                        $"الصنف {sourceLine.ProductName} غير متوفر حالياً في المخزون، وتمت إزالته من الفاتورة.",
                        $"The item {sourceLine.ProductName} is currently unavailable in stock and was removed from the invoice."),
                    UiText.T("تنبيه", "Notice"));
                return null;
            }

            return ReplaceInvoiceLinesFromAllocations(matchingLines, templateLine, insertIndex, allocationResult.Data);
        }

        private async Task<Result<List<StockLotAllocationDto>>> AllocateInvoiceLinesAsync(int productId, int productUnitId, decimal quantity)
        {
            return await _stockService.AllocateOutgoingAsync(new[]
            {
                new StockAllocationRequestDto
                {
                    ProductId = productId,
                    ProductUnitId = productUnitId,
                    Quantity = quantity
                }
            });
        }

        private async Task<decimal> GetAvailableQuantityForProductUnitAsync(int productId, int productUnitId)
        {
            if (productId <= 0 || productUnitId <= 0)
                return 0m;

            var result = await _stockService.GetAvailableQuantityInUnitAsync(productId, productUnitId);
            return result.Success ? result.Data : 0m;
        }

        private InvoiceLineWriteDto? ReplaceInvoiceLinesFromAllocations(
            IEnumerable<InvoiceLineWriteDto> matchingLines,
            InvoiceLineWriteDto templateLine,
            int insertIndex,
            IEnumerable<StockLotAllocationDto> allocations)
        {
            foreach (var line in matchingLines)
                _invoiceLines.Remove(line);

            InvoiceLineWriteDto? targetLine = null;
            foreach (var allocation in allocations)
            {
                var hasStockLookup = _stockLookup.TryGetValue((allocation.ProductId, allocation.ProductUnitId), out var stockSnapshot);
                var splitLine = new InvoiceLineWriteDto
                {
                    SelectedProduct = templateLine.SelectedProduct,
                    ProductId = templateLine.ProductId,
                    ProductName = templateLine.ProductName,
                    ProductUnitId = templateLine.ProductUnitId,
                    Quantity = allocation.Quantity,
                    QuantityPerUnitSnapshot = allocation.QuantityPerUnitSnapshot,
                    BaseQuantity = allocation.BaseQuantity,
                    UnitPrice = templateLine.UnitPrice,
                    UnitCost = allocation.PurchasePrice,
                    TaxExempt = templateLine.TaxExempt,
                    TaxRate = templateLine.TaxRate,
                    ExpiryDate = allocation.ExpiryDate ?? templateLine.ExpiryDate,
                    OriginalInvoiceId = templateLine.OriginalInvoiceId,
                    CreatedDate = templateLine.CreatedDate,
                    UpdatedDate = DateTime.Now,
                    AvailableQuantitySnapshot = hasStockLookup
                        ? Math.Max(stockSnapshot!.Quantity, 0m)
                        : templateLine.AvailableQuantitySnapshot,
                    UnitNameSnapshot = hasStockLookup
                        ? stockSnapshot!.ProductUnit?.Unit?.Name ?? templateLine.UnitNameSnapshot
                        : templateLine.UnitNameSnapshot
                };

                RecalculateLineFromCurrentValues(splitLine);
                _invoiceLines.Insert(insertIndex++, splitLine);
                targetLine ??= splitLine;
            }

            InvoiceGrid.Items.Refresh();
            RecalculateTotals();
            return targetLine;
        }
        private void InvoiceGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private async void FinishSaleBtn_Click(object sender, RoutedEventArgs e)
        {
            await ProcessPaymentAsync(PaymentType.Cash);
        }
        private static TransactionType MapStockTransactionType(InvoiceType invoiceType)
        {
            return invoiceType switch
            {
                InvoiceType.Purchase => TransactionType.Purchase,
                InvoiceType.Return => TransactionType.Return,
                InvoiceType.PurchaseReturn => TransactionType.Return,
                _ => TransactionType.Sale
            };
        }

        private static TransactionType MapStockTransactionType(InvoiceType invoiceType, decimal quantity)
        {
            if (invoiceType == InvoiceType.Return)
                return quantity < 0 ? TransactionType.Return : TransactionType.Sale;

            if (invoiceType == InvoiceType.Exchange)
                return quantity < 0 ? TransactionType.Return : TransactionType.Sale;

            return MapStockTransactionType(invoiceType);
        }

        private IEnumerable<StockMovementPostDto> BuildPosStockMovements(IEnumerable<InvoiceLineWriteDto> lines, int invoiceId, CashierSessionReadDto session)
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
                        Quantity = _currentInvoice.InvoiceType == InvoiceType.PurchaseReturn
                            ? line.Quantity
                            : -line.Quantity,
                        QuantityPerUnitSnapshot = quantityPerUnit,
                        BaseQuantity = _currentInvoice.InvoiceType == InvoiceType.PurchaseReturn
                            ? baseQuantity
                            : -baseQuantity,
                        UnitPrice = line.UnitPrice,
                        PurchasePrice = line.UnitCost,
                        SalePrice = line.UnitPrice,
                        ExpiryDate = line.ExpiryDate,
                        TransactionType = MapStockTransactionType(_currentInvoice.InvoiceType, line.Quantity),
                        InvoiceId = invoiceId,
                        CustomerId = _currentInvoice.CustomerId,
                        CasherId = session.CashierId,
                        CashierSessionId = session.Id,
                        TransactionDate = DateTime.Now,
                        Notes = $"POS {_currentInvoice.InvoiceType} #{_currentInvoice.InvoiceNumber}"
                    };
                });
        }
        private async Task<bool> ValidateStockAvailabilityAsync()
        {
            var sellableLines = _invoiceLines.Where(l => l.Quantity > 0).ToList();
            var availabilityResult = await _stockService.GetAvailableQuantitiesInUnitsAsync(
                sellableLines.Select(line => new StockAllocationRequestDto
                {
                    ProductId = line.ProductId,
                    ProductUnitId = line.ProductUnitId,
                    Quantity = line.Quantity
                }));
            var availabilityByKey = (availabilityResult.Data ?? new List<StockAvailabilityDto>())
                .ToDictionary(x => (x.ProductId, x.ProductUnitId), x => x.AvailableQuantity);
            var productIds = sellableLines.Select(line => line.ProductId).Distinct().ToList();
            var productUnitIds = sellableLines.Select(line => line.ProductUnitId).Distinct().ToList();
            var stockSnapshotResult = await _stockService.GetAllWriteDtoWithFilteringAndIncludeAsync(
                stock => productIds.Contains(stock.ProductId) && productUnitIds.Contains(stock.ProductUnitId));
            var stockByKey = (stockSnapshotResult.Data ?? new List<StockWriteDto>())
                .GroupBy(stock => (stock.ProductId, stock.ProductUnitId))
                .ToDictionary(group => group.Key, group => group.First());

            foreach (var line in sellableLines)
            {
                stockByKey.TryGetValue((line.ProductId, line.ProductUnitId), out var stock);
                if (stock != null)
                {
                    _stockLookup[(stock.ProductId, stock.ProductUnitId)] = new StockReadDto
                    {
                        ProductId = stock.ProductId,
                        ProductUnitId = stock.ProductUnitId,
                        Quantity = stock.Quantity,
                        PurchasePrice = stock.PurchasePrice,
                        SalePrice = stock.SalePrice
                    };

                    line.UnitCost = stock.PurchasePrice;
                }

                var availableQuantity = availabilityByKey.TryGetValue(
                    (line.ProductId, line.ProductUnitId), out var available)
                    ? available
                    : 0m;
                if (availableQuantity <= 0)
                {
                    ShowPaymentValidationMessage(
                        UiText.T(
                            $"الصنف {line.ProductName} غير موجود في المخزون. لن يتم حفظ الفاتورة.",
                            $"The item {line.ProductName} was not found in stock. The invoice will not be saved."),
                        UiText.T("تنبيه", "Notice"));
                    return false;
                }

                if (availableQuantity >= line.Quantity)
                {
                    RecalculateLineFromCurrentValues(line);
                    continue;
                }

                if (availableQuantity > 0)
                {
                    line.Quantity = availableQuantity;
                    RecalculateLineFromCurrentValues(line);
                    ShowPaymentValidationMessage(
                        UiText.T(
                            $"الكمية المطلوبة للصنف {line.ProductName} غير متوفرة. تم تعديل الكمية إلى الحد الأقصى المتاح: {availableQuantity:0.00000}",
                            $"The requested quantity for {line.ProductName} is not available. The quantity was adjusted to the maximum available: {availableQuantity:0.00000}"),
                        UiText.T("تنبيه", "Notice"));
                }
                else
                {
                    _invoiceLines.Remove(line);
                    ShowPaymentValidationMessage(
                        UiText.T(
                            $"الصنف {line.ProductName} غير متوفر حالياً في المخزون، وتمت إزالته من الفاتورة.",
                            $"The item {line.ProductName} is currently unavailable in stock and was removed from the invoice."),
                        UiText.T("تنبيه", "Notice"));
                }

                RecalculateTotals();
                InvoiceGrid.Items.Refresh();
                return false;
            }

            return true;
        }

        private async Task<List<InvoiceLineWriteDto>?> ExpandInvoiceLinesByFefoAsync(IEnumerable<InvoiceLineWriteDto> sourceLines)
        {
            var expandedLines = new List<InvoiceLineWriteDto>();
            var positiveLines = sourceLines.Where(line => line.Quantity > 0).ToList();
            var allocationResult = await _stockService.AllocateOutgoingAsync(
                positiveLines.Select((line, index) => new StockAllocationRequestDto
                {
                    RequestIndex = index,
                    ProductId = line.ProductId,
                    ProductUnitId = line.ProductUnitId,
                    Quantity = line.Quantity
                }));

            if (!allocationResult.Success || allocationResult.Data == null)
            {
                ShowPaymentValidationMessage(
                    allocationResult.Message ?? UiText.T("تعذر تخصيص المخزون.", "Could not allocate stock."),
                    UiText.T("تنبيه", "Notice"));
                return null;
            }
            var allocationsByRequest = allocationResult.Data
                .GroupBy(allocation => allocation.RequestIndex)
                .ToDictionary(group => group.Key, group => group.ToList());

            foreach (var returnLine in sourceLines.Where(line => line.Quantity < 0))
            {
                var signedLine = CloneLineSnapshot(returnLine, returnLine.Quantity, returnLine.OriginalInvoiceId);
                RecalculateLineFromCurrentValues(signedLine);
                expandedLines.Add(signedLine);
            }

            for (var index = 0; index < positiveLines.Count; index++)
            {
                var sourceLine = positiveLines[index];

                if (!allocationsByRequest.TryGetValue(index, out var lineAllocations) || lineAllocations.Count == 0)
                {
                    ShowPaymentValidationMessage(
                        UiText.T($"تعذر تخصيص المخزون للصنف {sourceLine.ProductName}.", $"Could not allocate stock for item {sourceLine.ProductName}."),
                        UiText.T("تنبيه", "Notice"));
                    return null;
                }

                foreach (var allocation in lineAllocations)
                {
                    var splitLine = new InvoiceLineWriteDto
                    {
                        ProductId = sourceLine.ProductId,
                        ProductName = sourceLine.ProductName,
                        ProductUnitId = sourceLine.ProductUnitId,
                        UnitNameSnapshot = sourceLine.UnitNameSnapshot ?? sourceLine.ProductUnit?.Unit?.Name,
                        Quantity = allocation.Quantity,
                        QuantityPerUnitSnapshot = allocation.QuantityPerUnitSnapshot,
                        BaseQuantity = allocation.BaseQuantity,
                        UnitPrice = sourceLine.UnitPrice,
                        UnitCost = allocation.PurchasePrice,
                        TaxExempt = sourceLine.TaxExempt,
                        TaxRate = sourceLine.TaxRate,
                        ExpiryDate = allocation.ExpiryDate ?? sourceLine.ExpiryDate,
                        OriginalInvoiceId = sourceLine.OriginalInvoiceId,
                        CreatedDate = sourceLine.CreatedDate,
                        UpdatedDate = DateTime.Now
                    };

                    RecalculateLineFromCurrentValues(splitLine);
                    expandedLines.Add(splitLine);
                }
            }

            return expandedLines;
        }


        private bool CanSaveInvoice()
        {
            if (string.IsNullOrWhiteSpace(FalconInvoiceNumberTextBox.Text))
            {
                ShowPaymentValidationMessage(
                    UiText.T("رقم فالكون مطلوب.", "Falcon invoice number is required."),
                    UiText.T("تنبيه", "Notice"));
                FalconInvoiceNumberTextBox.Focus();
                return false;
            }

            if (_currentInvoice.InvoiceType is InvoiceType.Return or InvoiceType.PurchaseReturn)
            {
                if (!_invoiceLines.OfType<ReturnInvoiceLine>().Any(line => line.ReturnedQuantity > 0))
                {
                    ShowPaymentValidationMessage(UiText.T("يرجى إدخال كمية مرتجعة واحدة على الأقل.", "Enter at least one returned quantity."), UiText.T("تنبيه", "Notice"));
                    return false;
                }

                return true;
            }

            foreach (var emptyLine in _invoiceLines.Where(IsEmptyBarcodeLine).ToList())
                _invoiceLines.Remove(emptyLine);

            if (_invoiceLines.Count == 0)
            {
                ShowPaymentValidationMessage(UiText.T("لا يوجد أصناف في الفاتورة.", "There are no items in the invoice."), UiText.T("تنبيه", "Notice"));
                return false;
            }
            
            if (_invoiceLines.Any(l => l.ProductId <= 0 || l.ProductUnitId <= 0 || l.Quantity == 0))
            {
                ShowPaymentValidationMessage(UiText.T("يوجد صنف ببيانات غير مكتملة أو كمية غير صالحة.", "There is an item with incomplete data or an invalid quantity."), UiText.T("تنبيه", "Notice"));
                return false;
            }

            /* if (CustomerComboBox.SelectedItem == null)
             {
                 MessageBox.Show("يرجى اختيار العميل", "تنبيه");
                 return false;
             }*/

            return true;
        }

        private async void FalconInvoiceNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var validationVersion = ++_falconValidationVersion;
            var value = FalconInvoiceNumberTextBox.Text?.Trim();

            FalconInvoiceValidationText.Text = string.Empty;
            FalconInvoiceValidationText.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(value))
                return;

            try
            {
                await Task.Delay(250);
                if (validationVersion != _falconValidationVersion ||
                    !string.Equals(value, FalconInvoiceNumberTextBox.Text?.Trim(), StringComparison.Ordinal))
                    return;

                await _falconValidationGate.WaitAsync();
                InvoiceReadDto? duplicate;
                try
                {
                    if (validationVersion != _falconValidationVersion ||
                        !string.Equals(value, FalconInvoiceNumberTextBox.Text?.Trim(), StringComparison.Ordinal))
                        return;

                    duplicate = await FindFalconInvoiceForValidationAsync(
                        value,
                        _currentInvoice.Id > 0 ? _currentInvoice.Id : null);
                }
                finally
                {
                    _falconValidationGate.Release();
                }

                if (validationVersion != _falconValidationVersion || !string.Equals(value, FalconInvoiceNumberTextBox.Text?.Trim(), StringComparison.Ordinal))
                    return;

                if (duplicate == null)
                    return;

                FalconInvoiceValidationText.Text = UiText.T(
                    $"رقم فالكون مستخدم في الفاتورة {duplicate.InvoiceNumber} بتاريخ {duplicate.CreatedDate:yyyy-MM-dd}.",
                    $"Falcon number is already used by invoice {duplicate.InvoiceNumber} dated {duplicate.CreatedDate:yyyy-MM-dd}.");
                FalconInvoiceValidationText.Visibility = Visibility.Visible;

                if (!string.Equals(_lastFalconDuplicateMessageValue, value, StringComparison.Ordinal))
                {
                    _lastFalconDuplicateMessageValue = value;
                    MessageBox.Show(
                        FalconInvoiceValidationText.Text,
                        UiText.T("تنبيه", "Notice"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                if (validationVersion != _falconValidationVersion)
                    return;

                FalconInvoiceValidationText.Text = UiText.T(
                    $"تعذر التحقق من رقم فالكون: {ex.Message}",
                    $"Falcon number validation failed: {ex.Message}");
                FalconInvoiceValidationText.Visibility = Visibility.Visible;
            }
        }

        private async Task<bool> ValidateFalconNumberBeforeSaveAsync()
        {
            var value = FalconInvoiceNumberTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(value))
                return false;

            // Stop the debounce check and wait for any already-running lookup so the
            // save-time validation never uses the shared DbContext concurrently.
            CancelFalconValidation();
            await _falconValidationGate.WaitAsync();
            InvoiceReadDto? duplicate;
            try
            {
                duplicate = await FindFalconInvoiceForValidationAsync(
                    value,
                    _currentInvoice.Id > 0 ? _currentInvoice.Id : null);
            }
            finally
            {
                _falconValidationGate.Release();
            }
            if (duplicate == null)
                return true;

            var message = UiText.T(
                $"رقم فالكون مستخدم في الفاتورة {duplicate.InvoiceNumber} بتاريخ {duplicate.CreatedDate:yyyy-MM-dd}.",
                $"Falcon number is already used by invoice {duplicate.InvoiceNumber} dated {duplicate.CreatedDate:yyyy-MM-dd}.");
            FalconInvoiceValidationText.Text = message;
            FalconInvoiceValidationText.Visibility = Visibility.Visible;
            ShowPaymentValidationMessage(message, UiText.T("تنبيه", "Notice"));
            FalconInvoiceNumberTextBox.Focus();
            return false;
        }

        private async Task<InvoiceReadDto?> FindFalconInvoiceForValidationAsync(
            string value,
            int? excludeInvoiceId)
        {
            // Falcon validation is read-only. Use a dedicated scope/context so it
            // cannot collide with product loading or another POS operation that is
            // still using the window's shared context.
            using var scope = _serviceProvider.CreateScope();
            var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
            return await invoiceService.FindPOSInvoiceByFalconNumberAsync(value, excludeInvoiceId);
        }

        private void CancelFalconValidation()
        {
            Interlocked.Increment(ref _falconValidationVersion);
        }

        private async Task<InvoiceWriteDto?> LoadOriginalInvoiceForReturnOrExchangeAsync(string? invoiceNumber)
        {
            if (string.IsNullOrWhiteSpace(invoiceNumber))
                return null;

            var result = await _invoiceService.GetAllWriteDtoWithFilteringAndIncludeAsync(
                invoice => invoice.InvoiceNumber == invoiceNumber,
                invoice => invoice.InvoiceLines);

            var invoice = result?.Data?.FirstOrDefault();
            if (invoice != null)
                return invoice;

            var stockDocumentService = _serviceProvider.GetService<IStockDocumentService>();
            var stockDocument = stockDocumentService == null
                ? null
                : (await stockDocumentService.GetDocumentWithItemsAsync(invoiceNumber)).FirstOrDefault();

            return stockDocument == null
                ? null
                : ConvertStockDocumentToReturnSource(stockDocument, invoiceNumber);
        }

        private bool IsNegativeLineAllowedByOriginalInvoice(
            InvoiceLineWriteDto line,
            InvoiceWriteDto originalInvoice,
            IEnumerable<InvoiceLineWriteDto> negativeLines,
            out string message)
        {
            message = string.Empty;

            var matchingOriginalLines = originalInvoice.InvoiceLines?
                .Where(originalLine =>
                    originalLine.ProductId == line.ProductId &&
                    originalLine.ProductUnitId == line.ProductUnitId)
                .ToList() ?? new List<InvoiceLineWriteDto>();

            if (!matchingOriginalLines.Any())
            {
                message = UiText.T(
                    $"لا يمكن إرجاع أو استبدال الصنف {line.ProductName} لأنه غير موجود في الفاتورة الأصلية.",
                    $"Item {line.ProductName} cannot be returned or exchanged because it does not exist in the original invoice.");
                return false;
            }

            var originalQuantity = matchingOriginalLines.Sum(originalLine => Math.Abs(originalLine.Quantity));
            var requestedQuantity = negativeLines
                .Where(invoiceLine =>
                    invoiceLine.ProductId == line.ProductId &&
                    invoiceLine.ProductUnitId == line.ProductUnitId)
                .Sum(invoiceLine => Math.Abs(invoiceLine.Quantity));

            if (requestedQuantity > originalQuantity)
            {
                message = UiText.T(
                    $"كمية الصنف {line.ProductName} أكبر من الكمية الموجودة في الفاتورة الأصلية. الكمية الأصلية: {originalQuantity:0.00000}، الكمية المطلوبة: {requestedQuantity:0.00000}.",
                    $"Item {line.ProductName} quantity exceeds the original invoice quantity. Original: {originalQuantity:0.00000}, requested: {requestedQuantity:0.00000}.");
                return false;
            }

            return true;
        }

        private async Task<bool> ValidateReturnOrExchangeAgainstOriginalInvoiceAsync()
        {
            if (_currentInvoice.InvoiceType is not (InvoiceType.Return or InvoiceType.PurchaseReturn or InvoiceType.Exchange))
                return true;

            var configuredReturnLines = _invoiceLines.OfType<ReturnInvoiceLine>().ToList();
            foreach (var configuredLine in configuredReturnLines)
            {
                if (configuredLine.ReturnedQuantity > configuredLine.ReturnableQuantity)
                {
                    ShowPaymentValidationMessage(
                        UiText.T($"الكمية المرتجعة للصنف {configuredLine.ProductName} أكبر من الكمية المتاحة للإرجاع.", $"The returned quantity for {configuredLine.ProductName} exceeds the remaining returnable quantity."),
                        UiText.T("تنبيه", "Notice"));
                    return false;
                }

                var baseLine = (InvoiceLineWriteDto)configuredLine;
                baseLine.Quantity = -configuredLine.ReturnedQuantity;
                baseLine.BaseQuantity = -configuredLine.ReturnedQuantity *
                    (baseLine.QuantityPerUnitSnapshot > 0 ? baseLine.QuantityPerUnitSnapshot : 1m);
                RecalculateLineFromCurrentValues(baseLine);
            }

            var negativeLines = _invoiceLines.Where(line => line.Quantity < 0).ToList();
            if (!negativeLines.Any())
            {
                ShowPaymentValidationMessage(UiText.T("لا يوجد صنف مرجع أو مستبدل في الفاتورة.", "There is no returned or exchanged item in the invoice."), UiText.T("تنبيه", "Notice"));
                return false;
            }

            var originalInvoice = await LoadOriginalInvoiceForReturnOrExchangeAsync(_currentInvoice.OriginalInvoiceId);
            if (originalInvoice?.InvoiceLines == null || !originalInvoice.InvoiceLines.Any())
            {
                ShowPaymentValidationMessage(UiText.T("الفاتورة الأصلية غير موجودة أو لا تحتوي على أصناف.", "The original invoice was not found or has no items."), UiText.T("تنبيه", "Notice"));
                return false;
            }

            if (originalInvoice.PaymentType == PaymentType.Credit)
            {
                if (_currentInvoice.PaymentType != PaymentType.Credit)
                {
                    ShowPaymentValidationMessage(
                        UiText.T(
                            "هذه الفاتورة الأصلية آجلة. يجب إنهاء المرتجع كفاتورة آجل ولا يمكن استخدام الدفع النقدي أو أي طريقة أخرى.",
                            "The original invoice is a credit invoice. This return must be completed as credit; cash or another payment method is not allowed."),
                        UiText.T("تنبيه", "Notice"));
                    return false;
                }

                _currentInvoice.CustomerId = originalInvoice.CustomerId;
                SelectInvoiceCustomer(originalInvoice.CustomerId);
            }

            foreach (var line in negativeLines)
            {
                if (!IsNegativeLineAllowedByOriginalInvoice(line, originalInvoice, negativeLines, out var message))
                {
                    ShowPaymentValidationMessage(message, UiText.T("تنبيه", "Notice"));
                    return false;
                }
            }

            return true;
        }

        private async Task<bool> AddProductToReturnInvoiceCoreAsync(
            ProductReadDto product,
            int? preferredUnitId,
            decimal quantity)
        {
            var resolvedProduct = await ResolveProductWithUnitsAsync(product);
            var productUnits = resolvedProduct.ProductUnits?.ToList() ?? new List<ProductUnitReadDto>();
            var selectedUnit = (preferredUnitId.HasValue
                    ? productUnits.FirstOrDefault(unit => unit.Id == preferredUnitId.Value)
                    : null)
                ?? ProductUnitSelector.GetDefaultSaleUnit(productUnits)
                ?? productUnits.FirstOrDefault();

            if (selectedUnit == null)
            {
                MessageBox.Show(
                    UiText.T(
                        "الصنف موجود في النظام لكن لا توجد له وحدات معرفة.",
                        "The item exists in the system, but no units are defined for it."),
                    UiText.T("تنبيه", "Notice"));
                return false;
            }

            var originalInvoice = await LoadOriginalInvoiceForReturnOrExchangeAsync(_currentInvoice.OriginalInvoiceId);
            var matchingOriginalLines = originalInvoice?.InvoiceLines?
                .Where(line => line.ProductId == resolvedProduct.Id && line.ProductUnitId == selectedUnit.Id)
                .ToList() ?? new List<InvoiceLineWriteDto>();

            if (matchingOriginalLines.Count == 0)
            {
                if (_currentInvoice.InvoiceType == InvoiceType.PurchaseReturn)
                {
                    MessageBox.Show(
                        UiText.T(
                            $"لا يمكن إرجاع الصنف {resolvedProduct.Name} لأنه غير موجود في الفاتورة الأصلية.",
                            $"Item {resolvedProduct.Name} cannot be returned because it is not in the original invoice."),
                        UiText.T("تنبيه", "Notice"));
                    return false;
                }

                var newSaleLine = _invoiceLines.FirstOrDefault(line =>
                    line is not ReturnInvoiceLine &&
                    line.ProductId == resolvedProduct.Id &&
                    line.ProductUnitId == selectedUnit.Id);

                if (newSaleLine == null)
                {
                    newSaleLine = new InvoiceLineWriteDto
                    {
                        ProductId = resolvedProduct.Id,
                        ProductName = resolvedProduct.Name,
                        ProductUnitId = selectedUnit.Id,
                        Quantity = quantity,
                        SelectedProduct = resolvedProduct
                    };

                    if (!await ApplyLinePricingFromProductAsync(newSaleLine, resolvedProduct, selectedUnit.Id))
                    {
                        MessageBox.Show(
                            UiText.T("لا توجد وحدات معرفة لهذا الصنف.", "There are no units defined for this item."),
                            UiText.T("تنبيه", "Notice"));
                        return false;
                    }

                    _invoiceLines.Add(newSaleLine);
                }
                else
                {
                    newSaleLine.Quantity += quantity;
                }

                var availableAfterAllocation = await SplitDraftLinesByFefoAsync(
                    resolvedProduct.Id,
                    selectedUnit.Id);

                foreach (var saleLine in _invoiceLines.Where(line =>
                             line is not ReturnInvoiceLine &&
                             line.ProductId == resolvedProduct.Id &&
                             line.ProductUnitId == selectedUnit.Id))
                {
                    saleLine.AvailableQuantitySnapshot = availableAfterAllocation
                        ?? await GetAvailableQuantityForProductUnitAsync(resolvedProduct.Id, selectedUnit.Id);
                    saleLine.UnitNameSnapshot = selectedUnit.Unit?.Name
                        ?? saleLine.UnitNameSnapshot
                        ?? saleLine.ProductUnit?.Unit?.Name;
                }

                RecalculateTotals();
                RefreshInvoiceGridAfterEdit();
                FocusBarcodeGridCellDeferred();
                return true;
            }

            var existingLine = _invoiceLines.OfType<ReturnInvoiceLine>()
                .FirstOrDefault(line => line.ProductId == resolvedProduct.Id && line.ProductUnitId == selectedUnit.Id);

            var originalQuantity = matchingOriginalLines.Sum(line => Math.Abs(line.Quantity));
            var previouslyReturned = existingLine == null
                ? (await LoadPreviouslyReturnedQuantitiesAsync(_currentInvoice.OriginalInvoiceId))
                    .GetValueOrDefault((resolvedProduct.Id, selectedUnit.Id))
                : 0m;
            var returnableQuantity = existingLine?.ReturnableQuantity
                ?? Math.Max(0m, originalQuantity - previouslyReturned);
            var currentRequestedQuantity = existingLine?.ReturnedQuantity ?? 0m;

            if (currentRequestedQuantity + quantity > returnableQuantity)
            {
                MessageBox.Show(
                    UiText.T(
                        $"الكمية المرتجعة للصنف {resolvedProduct.Name} أكبر من الكمية المتاحة للإرجاع.",
                        $"The requested return quantity for {resolvedProduct.Name} exceeds the remaining returnable quantity."),
                    UiText.T("تنبيه", "Notice"));
                return false;
            }

            if (existingLine != null)
            {
                existingLine.ReturnedQuantity += quantity;
            }
            else
            {
                var sourceLine = matchingOriginalLines.First();
                var snapshot = CloneLineSnapshot(sourceLine, 0m, _currentInvoice.OriginalInvoiceId);
                snapshot.ProductName = resolvedProduct.Name;
                snapshot.SelectedProduct = resolvedProduct;
                snapshot.ProductUnitId = selectedUnit.Id;
                snapshot.ProductUnit = MapProductUnit(selectedUnit);
                snapshot.UnitName = selectedUnit.Unit?.Name;
                snapshot.UnitNameSnapshot = selectedUnit.Unit?.Name;

                var newReturnLine = ReturnInvoiceLine.FromSnapshot(
                    snapshot,
                    originalQuantity,
                    returnableQuantity);
                newReturnLine.ReturnedQuantity = quantity;
                _invoiceLines.Add(newReturnLine);
            }

            RecalculateTotals();
            RefreshInvoiceGridAfterEdit();
            FocusFirstReturnQuantityCell();
            return true;
        }

        private void ApplyOriginalCreditTerms(InvoiceWriteDto originalInvoice)
        {
            if (originalInvoice.PaymentType != PaymentType.Credit)
                return;

            _currentInvoice.PaymentType = PaymentType.Credit;
            _currentInvoice.CustomerId = originalInvoice.CustomerId;
            SelectInvoiceCustomer(originalInvoice.CustomerId);
        }

        private void SelectInvoiceCustomer(int? customerId)
        {
            if (!customerId.HasValue || _allCustomers == null)
                return;

            var customer = _allCustomers.FirstOrDefault(x => x.Id == customerId.Value);
            if (customer != null)
                CustomerComboBox.SelectedItem = customer;
        }

        private void PrepareInvoiceForSave()
        {
            if (TryGetActiveCashierSession(out var session))
            {
                _currentInvoice.CasherId = session.CashierId;
                _currentInvoice.CashierSessionId = session.Id;
            }

            var customer = CustomerComboBox.SelectedItem as UserReadDto;
            if (customer != null)
                _currentInvoice.CustomerId = customer.Id;
            else if (string.IsNullOrWhiteSpace(CustomerComboBox.Text))
                _currentInvoice.CustomerId = null;
            _currentInvoice.IsPOS = true;
            _currentInvoice.FalconInvoiceNumber = FalconInvoiceNumberTextBox.Text.Trim();
            _currentInvoice.Status = InvoiceStatus.Completed;
            _currentInvoice.ClosedAt = DateTime.Now;
            RecalculateTotals();
        }
        private void ResetPOS()
        {
            CancelFalconValidation();
            Interlocked.Increment(ref _posResetVersion);
            _pendingFefoEditedLine = null;
            _hasPendingFefoSplit = false;
            _isProcessingPendingFefoSplit = false;

            _invoiceLines.Clear();
            InvoiceGrid.SelectedItem = null;
            InvoiceGrid.SelectedCells.Clear();
            InvoiceGrid.CurrentCell = new DataGridCellInfo();
            InvoiceGrid.Items.Refresh();

            TotalTextBlock.Text = "0.00000";
            DiscountTextBox.Text = "0";
            DiscountSummaryText.Text = "0.00000";
            NetTotalText.Text = "0.00000";

            BarcodeTextBox.Clear();
            CustomerComboBox.SelectedIndex = -1;
            CustomerComboBox.SelectedItem = null;
            CustomerComboBox.Text = string.Empty;
            FalconInvoiceNumberTextBox.Clear();
            _lastSavedInvoice = null;

            // Drop all state from the held/previous invoice before creating the next one.
            // This is especially important after updating an existing held invoice: its Id
            // must not remain attached to the next invoice and cause another update.
            _currentInvoice = new InvoiceWriteDto
            {
                Id = 0,
                InvoiceNumber = GenerateInvoiceNumber(),
                FalconInvoiceNumber = string.Empty,
                InvoiceType = InvoiceType.Sale,
                Status = InvoiceStatus.Draft,
                IsPOS = true,
                InvoiceLines = _invoiceLines,
                Payments = new List<InvoicePaymentWriteDto>()
            };

            CreateNewInvoice();

            InvoiceGrid.Items.Refresh();
            FocusBarcodeGridCellDeferred();
        }

        private void SearchProductBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var disabledKeys = _invoiceLines
                    .Select(l => $"{l.ProductId}:{l.ProductUnitId}")
                    .ToList();

                var searchWindow = new ProductSearchWindow(
                    _stockService,
                    _productUnitService,
                    async row =>
                    {
                        if (row == null || row.Quantity <= 0)
                            return false;

                        var added = await AddProductToInvoiceAsync(row.Product, true, row.SelectedUnit?.Id, row.Quantity, row.SalePrice);
                        FocusBarcodeGridCellDeferred();
                        return added;
                    },
                    disabledKeys)
                {
                    Owner = this
                };

                searchWindow.ShowDialog();
                FocusBarcodeGridCellDeferred();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"تعذر فتح نافذة البحث: {ex.Message}", "خطأ");
                FocusBarcodeInput();
            }
        }

        private void CustomerComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not ComboBox combo)
                return;

            switch (e.Key)
            {
                case Key.Enter:
                    e.Handled = true;
                    var selectedCustomer = combo.SelectedItem as UserReadDto
                        ?? combo.Items.OfType<UserReadDto>().FirstOrDefault()
                        ?? _allCustomers?.FirstOrDefault(customer =>
                            string.Equals(customer.Name, combo.Text?.Trim(), StringComparison.CurrentCultureIgnoreCase));
                    if (selectedCustomer != null)
                    {
                        combo.SelectedItem = selectedCustomer;
                        _currentInvoice.CustomerId = selectedCustomer.Id;
                    }
                    FocusBarcodeInput();
                    break;

                case Key.Escape:
                    e.Handled = true;
                    combo.IsDropDownOpen = false;
                    FocusBarcodeInput();
                    break;

                case Key.Down:
                case Key.Up:
                    if (!combo.IsDropDownOpen)
                        combo.IsDropDownOpen = true;

                    var typedCustomerText = combo.Text ?? string.Empty;
                    var nextIndex = combo.SelectedIndex;
                    nextIndex = e.Key == Key.Down
                        ? Math.Min(nextIndex + 1, combo.Items.Count - 1)
                        : Math.Max(nextIndex - 1, 0);

                    if (combo.Items.Count > 0)
                    {
                        _isNavigatingCustomerChoices = true;
                        try { combo.SelectedIndex = nextIndex; }
                        finally { _isNavigatingCustomerChoices = false; }

                        combo.Text = typedCustomerText;
                        if (combo.Template.FindName("PART_EditableTextBox", combo) is TextBox editableTextBox)
                            editableTextBox.CaretIndex = editableTextBox.Text.Length;
                    }

                    e.Handled = true;
                    break;
            }
        }

        private void CustomerComboBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is not ComboBox combo)
                return;

            combo.IsDropDownOpen = true;
        }

        private void CustomerComboBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            CustomerComboBox.SelectedItem = null;
            Dispatcher.BeginInvoke(new Action(() => FilterCustomerList(CustomerComboBox.Text)),
                System.Windows.Threading.DispatcherPriority.Input);
        }

        private void CustomerComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back || e.Key == Key.Delete)
            {
                CustomerComboBox.SelectedItem = null;
                FilterCustomerList(CustomerComboBox.Text);
            }
        }

        private void FilterCustomerList(string? text)
        {
            var searchText = text ?? string.Empty;
            var filtered = string.IsNullOrWhiteSpace(searchText)
                ? _allCustomers?.ToList() ?? new List<UserReadDto>()
                : _allCustomers?
                    .Where(customer => (customer.Name ?? string.Empty)
                        .Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                    .ToList() ?? new List<UserReadDto>();

            _isFilteringCustomers = true;
            try
            {
                CustomerComboBox.ItemsSource = filtered;
                CustomerComboBox.SelectedItem = null;
                CustomerComboBox.SelectedValue = null;
                CustomerComboBox.SelectedIndex = -1;
                CustomerComboBox.Text = searchText;
                CustomerComboBox.IsDropDownOpen = filtered.Count > 0;
                if (CustomerComboBox.Template.FindName("PART_EditableTextBox", CustomerComboBox) is TextBox textBox)
                    textBox.CaretIndex = textBox.Text.Length;
            }
            finally
            {
                _isFilteringCustomers = false;
            }
        }

        private void CustomerComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFilteringCustomers || _isNavigatingCustomerChoices)
                return;

            var textBox = e.OriginalSource as TextBox ?? Keyboard.FocusedElement as TextBox;
            if (textBox == null || !textBox.IsKeyboardFocusWithin)
                return;

            FilterCustomerList(textBox.Text);
        }

        private async void AddCustomerBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int? createdCustomerId = null;
                var initialName = CustomerComboBox.Text?.Trim();

                WindowManager.ShowDialog<CreateUser>(WindowSizeType.MediumRectangle, window =>
                {
                    window.InitializeForCustomerQuickCreate(initialName, null);
                    window.Closed += (_, __) => createdCustomerId = window.CreatedUserId;
                });

                if (createdCustomerId.HasValue)
                {
                    await LoadCustomersAsync(createdCustomerId);
                    CustomerComboBox.IsDropDownOpen = false;
                }

                FocusBarcodeInputDeferred();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("تعذر إضافة العميل", "Could not add the customer")}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                FocusBarcodeInputDeferred();
            }
        }

        private async void ChangePaymentMethodBtn_Click(object sender, RoutedEventArgs e)
        {
            var loadingShown = false;
            void HideLoadingForDialog()
            {
                if (loadingShown)
                {
                    _loading.Hide();
                    loadingShown = false;
                }
            }
            try
            {
                if (!TryGetActiveCashierSession(out var session))
                    return;

                var searchWindow = new SearchSalesInvoiceWindow(
                    _invoiceService,
                    _allCustomers ?? Enumerable.Empty<UserReadDto>(),
                    null,
                    true)
                {
                    Owner = this
                };

                if (searchWindow.ShowDialog() != true || searchWindow.Result == null)
                    return;

                var invoice = searchWindow.Result;
                if (invoice.Status is InvoiceStatus.Draft or InvoiceStatus.OnHold or InvoiceStatus.Cancelled)
                {
                    MessageBox.Show(UiText.T("لا يمكن تغيير طريقة دفع فاتورة غير مرحّلة.", "Only posted invoices can change payment method."), UiText.T("تنبيه", "Notice"));
                    return;
                }

                var paymentWindow = new ChangeInvoicePaymentWindow(
                    invoice.PaymentType,
                    _allCustomers ?? Enumerable.Empty<UserReadDto>(),
                    invoice.CustomerId,
                    Math.Abs(invoice.TotalAmount),
                    invoice.Payments)
                { Owner = this };
                if (paymentWindow.ShowDialog() != true || paymentWindow.SelectedPayments.Count == 0)
                    return;

                var oldPaymentType = invoice.PaymentType ?? PaymentType.Cash;
                var oldPayments = invoice.Payments?.Where(payment => payment.Amount > 0m).ToList()
                    ?? new List<InvoicePaymentReadDto>();
                if (oldPayments.Count == 0)
                    oldPayments.Add(new InvoicePaymentReadDto { PaymentType = oldPaymentType, Amount = Math.Abs(invoice.TotalAmount) });
                var newPayments = paymentWindow.SelectedPayments.ToList();
                var newPaymentType = newPayments[0].PaymentType;
                var oldInvoice = BuildPaymentChangeInvoice(invoice);
                var updatedInvoice = BuildPaymentChangeInvoice(invoice);
                updatedInvoice.PaymentType = newPaymentType;
                updatedInvoice.Payments = newPayments.Select(payment => new InvoicePaymentWriteDto
                {
                    PaymentType = payment.PaymentType,
                    Amount = payment.Amount
                }).ToList();

                if (newPayments.Any(payment => payment.PaymentType == PaymentType.Credit))
                {
                    updatedInvoice.CustomerId = paymentWindow.SelectedCustomerId;
                }

                var checkPayment = newPayments.FirstOrDefault(payment => payment.PaymentType == PaymentType.Check);
                if (checkPayment != null)
                {
                    var checkWindow = new CheckDetailsWindow(checkPayment.Amount) { Owner = this };
                    if (checkWindow.ShowDialog() != true)
                        return;
                    updatedInvoice.Checks = checkWindow.ResultChecks.ToList();
                }
                else
                {
                    updatedInvoice.Checks = new List<CheckWriteDto>();
                }

                if (MessageBox.Show(
                        UiText.T($"هل تريد تغيير طريقة دفع الفاتورة {invoice.InvoiceNumber}؟", $"Change payment method for invoice {invoice.InvoiceNumber}?"),
                        UiText.T("تأكيد", "Confirm"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

                _loading.Show();
                loadingShown = true;
                var updateResult = await _invoiceService.UpdateAsync(updatedInvoice);
                if (!updateResult.Success)
                {
                    HideLoadingForDialog();
                    MessageBox.Show(updateResult.Message ?? UiText.T("فشل تحديث الفاتورة.", "Invoice update failed."), UiText.T("خطأ", "Error"));
                    return;
                }

                var sourceType = GetPaymentChangeSourceType(invoice.InvoiceType);
                if (oldPayments.Any(payment => payment.PaymentType != PaymentType.Credit))
                {
                    var voidResult = await _financialService.VoidBySourceAsync(sourceType, invoice.Id, $"Payment method changed from {oldPaymentType} to {newPaymentType}");
                    if (!voidResult.Success)
                    {
                        await _invoiceService.UpdateAsync(oldInvoice);
                        HideLoadingForDialog();
                        MessageBox.Show(voidResult.Message ?? UiText.T("فشل إلغاء الحركة المالية القديمة.", "Could not void the old financial transaction."), UiText.T("خطأ", "Error"));
                        return;
                    }
                }

                if (newPayments.Any(payment => payment.PaymentType != PaymentType.Credit))
                {
                    foreach (var payment in newPayments.Where(payment => payment.PaymentType != PaymentType.Credit))
                    {
                        var postResult = await _financialService.PostAsync(BuildPaymentChangeFinancialPost(invoice, payment.PaymentType, payment.Amount, sourceType, session, "POS payment method changed"));
                    if (!postResult.Success)
                    {
                        await _invoiceService.UpdateAsync(oldInvoice);
                        if (oldPayments.Any(payment => payment.PaymentType != PaymentType.Credit))
                            foreach (var oldPayment in oldPayments.Where(payment => payment.PaymentType != PaymentType.Credit))
                                await _financialService.PostAsync(BuildPaymentChangeFinancialPost(invoice, oldPayment.PaymentType, oldPayment.Amount, sourceType, session, "Restored original POS payment method"));
                        HideLoadingForDialog();
                        MessageBox.Show(postResult.Message ?? UiText.T("فشل تسجيل طريقة الدفع الجديدة.", "Could not post the new payment transaction."), UiText.T("خطأ", "Error"));
                        return;
                        }
                    }
                }

                HideLoadingForDialog();
                MessageBox.Show(UiText.T("تم تغيير طريقة الدفع وتحديث الحسابات المرتبطة بنجاح.", "Payment method and related accounts were updated successfully."), UiText.T("تم", "Done"));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("تعذر تغيير طريقة الدفع", "Could not change payment method")}: {ex.Message}", UiText.T("خطأ", "Error"));
            }
            finally
            {
                HideLoadingForDialog();
                FocusBarcodeInput();
            }
        }

        private static InvoiceWriteDto BuildPaymentChangeInvoice(InvoiceReadDto invoice)
        {
            return new InvoiceWriteDto
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                FalconInvoiceNumber = invoice.FalconInvoiceNumber,
                OriginalInvoiceId = invoice.OriginalInvoiceId,
                InvoiceType = invoice.InvoiceType,
                PaymentType = invoice.PaymentType,
                CasherId = invoice.CasherId,
                SupplierId = invoice.SupplierId,
                CustomerId = invoice.CustomerId,
                DelegateId = invoice.DelegateId,
                DelegateName = invoice.DelegateName,
                VoucherId = invoice.VoucherId,
                TotalAmount = invoice.TotalAmount,
                Notes = invoice.Notes,
                Status = invoice.Status,
                IsPOS = invoice.IsPOS,
                OpenedAt = invoice.OpenedAt,
                ClosedAt = invoice.ClosedAt,
                HeldColor = invoice.HeldColor,
                DiscountAmount = invoice.DiscountAmount,
                SubTotal = invoice.SubTotal,
                TotalTax = invoice.TotalTax,
                TotalCOGS = invoice.TotalCOGS,
                GrossProfit = invoice.GrossProfit,
                NetSales = invoice.NetSales,
                CreatedDate = invoice.CreatedDate,
                UpdatedDate = DateTime.Now,
                InvoiceLines = (invoice.InvoiceLines ?? Array.Empty<InvoiceLineReadDto>()).Select(line => new InvoiceLineWriteDto
                {
                    Id = 0,
                    ProductId = line.ProductId,
                    ProductName = line.ProductName,
                    ProductUnitId = line.ProductUnitId,
                    Quantity = line.Quantity,
                    QuantityPerUnitSnapshot = line.QuantityPerUnitSnapshot,
                    BaseQuantity = line.BaseQuantity,
                    UnitPrice = line.UnitPrice,
                    UnitCost = line.UnitCost,
                    TaxExempt = line.TaxExempt,
                    TaxRate = line.TaxRate,
                    TaxAmount = line.TaxAmount,
                    LineSubTotal = line.LineSubTotal,
                    ProfitBeforeTax = line.ProfitBeforeTax,
                    Profit = line.Profit,
                    ExpiryDate = line.ExpiryDate,
                    OriginalInvoiceId = line.OriginalInvoiceId,
                    CreatedDate = line.CreatedDate,
                    UpdatedDate = DateTime.Now
                }).ToList(),
                Payments = (invoice.Payments ?? Array.Empty<InvoicePaymentReadDto>()).Select(payment => new InvoicePaymentWriteDto
                {
                    Id = payment.Id,
                    PaymentType = payment.PaymentType,
                    Amount = payment.Amount,
                    CreatedDate = payment.CreatedDate,
                    UpdatedDate = DateTime.Now
                }).ToList(),
                Checks = (invoice.Checks ?? Array.Empty<CheckReadDto>()).Select(check => new CheckWriteDto
                {
                    Id = check.Id,
                    CheckNumber = check.CheckNumber,
                    BankName = check.BankName,
                    DueDate = check.DueDate,
                    Amount = check.Amount,
                    Status = check.Status,
                    Notes = check.Notes,
                    CreatedDate = check.CreatedDate,
                    UpdatedDate = DateTime.Now
                }).ToList()
            };
        }

        private static FinancialSourceType GetPaymentChangeSourceType(InvoiceType invoiceType) => invoiceType switch
        {
            InvoiceType.Return => FinancialSourceType.SaleReturn,
            InvoiceType.PurchaseReturn => FinancialSourceType.PurchaseReturn,
            _ => FinancialSourceType.PosSaleInvoice
        };

        private static TransactionDirection GetPaymentChangeDirection(InvoiceType invoiceType) =>
            invoiceType == InvoiceType.Return ? TransactionDirection.Out : TransactionDirection.In;

        private static PaymentMethod GetPaymentChangeMethod(PaymentType paymentType) => paymentType switch
        {
            PaymentType.Visa => PaymentMethod.Visa,
            PaymentType.Master => PaymentMethod.Master,
            PaymentType.Debit => PaymentMethod.BankTransfer,
            PaymentType.Check => PaymentMethod.Check,
            PaymentType.MobilePayment => PaymentMethod.MobilePayment,
            PaymentType.Credit => PaymentMethod.Credit,
            _ => PaymentMethod.Cash
        };

        private static FinancialPostDto BuildPaymentChangeFinancialPost(
            InvoiceReadDto invoice,
            PaymentType paymentType,
            decimal amount,
            FinancialSourceType sourceType,
            CashierSessionReadDto session,
            string notes)
        {
            return new FinancialPostDto
            {
                Direction = GetPaymentChangeDirection(invoice.InvoiceType),
                Method = GetPaymentChangeMethod(paymentType),
                Amount = Math.Abs(amount),
                TransactionDate = DateTime.Now,
                SourceType = sourceType,
                SourceId = invoice.Id,
                CashierSessionId = session.Id,
                CashierId = session.CashierId,
                Notes = notes
            };
        }

        private void DailyReportBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!TryGetActiveCashierSession(out var session))
                    return;

                var reportWindow = new DailySalesReport(_serviceProvider, session.Id, session.CashierId);
                reportWindow.Owner = this;
                reportWindow.ShowDialog();
                FocusBarcodeInput();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("تعذر فتح تقرير المبيعات", "Could not open the sales report")}: {ex.Message}", UiText.T("خطأ", "Error"));
                FocusBarcodeInput();
            }
        }


        #region OnHold
        private async void HoldSaleBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isHoldingInvoice || _isProcessingPayment || _isLoadingHeldInvoice)
                return;

            var linesToHold = _invoiceLines
                .Where(line => line.ProductId > 0 && line.ProductUnitId > 0 && line.Quantity > 0)
                .ToList();

            if (linesToHold.Count == 0)
            {
                MessageBox.Show(UiText.T("لا توجد مواد لحفظها", "There are no items to hold."), UiText.T("تنبيه", "Notice"));
                return;
            }

            _isHoldingInvoice = true;
            await _posDataOperationSemaphore.WaitAsync();
            try
            {
                var heldInvoicesResult = await _invoiceService.GetHeldPOSInvoicesAsync();
                if (!heldInvoicesResult.Success)
                {
                    MessageBox.Show(
                        heldInvoicesResult.Message ?? UiText.T("تعذر تحميل ألوان الفواتير المعلقة.", "Could not load held-invoice colors."),
                        UiText.T("خطأ", "Error"));
                    return;
                }

                var heldColor = ResumeHeldInvoiceWindow.ChooseAvailableColor(
                    this,
                    heldInvoicesResult.Data ?? new List<InvoiceReadDto>(),
                    _currentInvoice.Id);
                if (string.IsNullOrWhiteSpace(heldColor))
                    return;

                _currentInvoice.InvoiceLines = linesToHold;
                _currentInvoice.Status = InvoiceStatus.OnHold;
                _currentInvoice.IsPOS = true;
                _currentInvoice.FalconInvoiceNumber = FalconInvoiceNumberTextBox.Text.Trim();
                _currentInvoice.ClosedAt = null;
                _currentInvoice.HeldColor = heldColor;
                _currentInvoice.DeferAccountingPosting = true;
                RecalculateTotals();
                
                var result = _currentInvoice.Id > 0
                    ? await _invoiceService.UpdateAsync(_currentInvoice)
                    : await _invoiceService.CreateAsync(_currentInvoice);

                if (!result.Success)
                {
                    MessageBox.Show(result.Message ?? UiText.T("فشل حفظ الفاتورة في وضع الانتظار.", "Failed to save the invoice on hold."), UiText.T("خطأ", "Error"));
                    return;
                }

                MessageBox.Show(UiText.T("تم حفظ الفاتورة في وضع الانتظار", "The invoice was saved on hold."), UiText.T("تم", "Done"));

                ResetPOS(); //clear
                FocusBarcodeInput();
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

                MessageBox.Show(details, UiText.T("خطأ", "Error"));
            }
            finally
            {
                _posDataOperationSemaphore.Release();
                _isHoldingInvoice = false;
                FocusBarcodeInput();
            }
        }
        //resume held invoice
        private async void ResumeHoldBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var win = new ResumeHeldInvoiceWindow(_invoiceService)
                {
                    Owner = this
                };

                if (win.ShowDialog() != true)
                    return;

                if (win.SelectedInvoice == null)
                {
                    MessageBox.Show(UiText.T("لم يتم اختيار فاتورة معلقة.", "No held invoice was selected."), UiText.T("تنبيه", "Notice"));
                    return;
                }

                await _posDataOperationSemaphore.WaitAsync();
                try
                {
                    _isLoadingHeldInvoice = true;
                    _loading.Show();
                    try
                    {
                        var latestHeldInvoice = await _invoiceService.GetFullInvoiceByIdAsync(win.SelectedInvoice.Id);
                        if (latestHeldInvoice == null || latestHeldInvoice.Status != InvoiceStatus.OnHold)
                        {
                            MessageBox.Show(
                                UiText.T("الفاتورة المعلقة غير موجودة أو تم استئنافها مسبقاً.", "The held invoice no longer exists or has already been resumed."),
                                UiText.T("تنبيه", "Notice"),
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            return;
                        }

                        await LoadInvoiceIntoPOSAsync(latestHeldInvoice);
                        FocusBarcodeInput();
                    }
                    finally
                    {
                        _loading.Hide();
                        _isLoadingHeldInvoice = false;
                    }
                }
                finally
                {
                    _posDataOperationSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("تعذر استئناف الفاتورة المعلقة", "Could not resume the held invoice")}: {ex.Message}", UiText.T("خطأ", "Error"));
                FocusBarcodeInput();
            }
        }
        private async Task LoadInvoiceIntoPOSAsync(InvoiceReadDto invoice)
        {
            _invoiceLines.Clear();

            var heldLines = (invoice.InvoiceLines ?? Enumerable.Empty<InvoiceLineReadDto>()).ToList();
            var availabilityResult = await _stockService.GetAvailableQuantitiesInUnitsAsync(
                heldLines
                    .Where(line => line.ProductId > 0 && line.ProductUnitId > 0)
                    .Select(line => new StockAllocationRequestDto
                    {
                        ProductId = line.ProductId,
                        ProductUnitId = line.ProductUnitId,
                        Quantity = line.Quantity
                    }));
            var availabilityByKey = (availabilityResult.Data ?? new List<StockAvailabilityDto>())
                .ToDictionary(item => (item.ProductId, item.ProductUnitId), item => item.AvailableQuantity);

            foreach (var line in heldLines)
            {
                var restoredProduct = await ResolveProductForUnitsAsync(line.ProductId);
                var restoredUnit = restoredProduct?.ProductUnits?
                    .FirstOrDefault(unit => unit.Id == line.ProductUnitId);
                var availableQuantity = availabilityByKey.TryGetValue(
                    (line.ProductId, line.ProductUnitId),
                    out var available)
                    ? available
                    : 0m;
                var restoredLine = new InvoiceLineWriteDto
                {
                    ProductId = line.ProductId,
                    ProductName = string.IsNullOrWhiteSpace(line.ProductName)
                        ? restoredProduct?.Name ?? $"#{line.ProductId}"
                        : line.ProductName,
                    SelectedProduct = restoredProduct,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    ProductUnitId = line.ProductUnitId,
                    ProductUnit = restoredUnit == null ? null : MapProductUnit(restoredUnit),
                    QuantityPerUnitSnapshot = line.QuantityPerUnitSnapshot,
                    BaseQuantity = line.BaseQuantity,
                    UnitCost = line.UnitCost,
                    TaxExempt = line.TaxExempt,
                    TaxRate = line.TaxRate,
                    TaxAmount = line.TaxAmount,
                    LineSubTotal = line.LineSubTotal,
                    ProfitBeforeTax = line.ProfitBeforeTax,
                    Profit = line.Profit,
                    AvailableQuantitySnapshot = availableQuantity,
                    UnitNameSnapshot = line.ProductUnit?.Unit?.Name ?? restoredUnit?.Unit?.Name,
                    OriginalInvoiceId = line.OriginalInvoiceId
                };

                // Rebuild calculated values from the restored quantity/price/tax data.
                // Held invoices created by older versions may contain stale or zero totals.
                RecalculateLineFromCurrentValues(restoredLine);
                _invoiceLines.Add(restoredLine);
                restoredLine.RefreshCalculatedProperties();
            }

            _currentInvoice = new InvoiceWriteDto
            {
                Id = invoice.Id,              // 👈 VERY IMPORTANT
                InvoiceNumber = invoice.InvoiceNumber,
                FalconInvoiceNumber = invoice.FalconInvoiceNumber,
                Status = InvoiceStatus.Draft,
                IsPOS = true,
                OpenedAt = invoice.OpenedAt,
                CustomerId = invoice.CustomerId,
                SupplierId = invoice.SupplierId,
                PaymentType = invoice.PaymentType,
                Payments = invoice.Payments?.Select(payment => new InvoicePaymentWriteDto
                {
                    PaymentType = payment.PaymentType,
                    Amount = payment.Amount
                }).ToList() ?? new List<InvoicePaymentWriteDto>(),
                DiscountAmount = invoice.DiscountAmount ?? 0m,
                InvoiceType = invoice.InvoiceType,
                HeldColor = invoice.HeldColor
            };
            FalconInvoiceNumberTextBox.Text = invoice.FalconInvoiceNumber ?? string.Empty;
            _currentInvoice.InvoiceLines = _invoiceLines;

            InvoiceGrid.Items.Refresh();
            InvoiceGrid.UpdateLayout();
            RecalculateTotals();
        }

        #endregion
        //delete invoice line
        private void DeleteItemBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedRow = GetSelectedInvoiceLine();

            if (selectedRow == null)
            {
                MessageBox.Show(UiText.T("يرجى تحديد مادة أولاً", "Please select an item first."), UiText.T("تنبيه", "Notice"));
                return;
            }

            var confirm = MessageBox.Show(
                UiText.T(
                    $"هل تريد حذف المادة:\n{selectedRow.ProductName} ؟",
                    $"Do you want to remove the item:\n{selectedRow.ProductName}?"),
                UiText.T("تأكيد الحذف", "Confirm deletion"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            _invoiceLines.Remove(selectedRow);

            RecalculateTotals();
            InvoiceGrid.Items.Refresh();

            FocusBarcodeInput();
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
            var loadingShown = false;
            try
            {
                var selectedRow = GetSelectedInvoiceLine();

                if (selectedRow == null)
                {
                    MessageBox.Show(UiText.T("يرجى تحديد مادة للاستبدال", "Please select an item to exchange."), UiText.T("تنبيه", "Notice"));
                    return;
                }

                var win = new ExchangeInvoiceWindow(_invoiceService);
                if (win.ShowDialog() != true)
                    return;

                _loading.Show();
                loadingShown = true;

                var originalInvoice = await LoadOriginalInvoiceForReturnOrExchangeAsync(win.OriginalInvoiceId);
                if (originalInvoice?.InvoiceLines == null || !originalInvoice.InvoiceLines.Any())
                {
                    _loading.Hide();
                    loadingShown = false;
                    MessageBox.Show(UiText.T("الفاتورة الأصلية غير موجودة أو لا تحتوي على أصناف.", "The original invoice was not found or has no items."), UiText.T("تنبيه", "Notice"));
                    return;
                }

                var exchangeLine = CloneLineSnapshot(
                    selectedRow,
                    -Math.Abs(selectedRow.Quantity),
                    win.OriginalInvoiceId);

                if (!IsNegativeLineAllowedByOriginalInvoice(exchangeLine, originalInvoice, new[] { exchangeLine }, out var validationMessage))
                {
                    _loading.Hide();
                    loadingShown = false;
                    MessageBox.Show(validationMessage, UiText.T("تنبيه", "Notice"));
                    return;
                }

                _loading.Hide();
                loadingShown = false;

                CreateNewInvoice(
                    InvoiceType.Exchange,
                    win.OriginalInvoiceId
                );

                ApplyOriginalCreditTerms(originalInvoice);

                _invoiceLines.Add(exchangeLine);

                MessageBox.Show(UiText.T("امسح المادة الجديدة بالباركود", "Scan the new item barcode."), UiText.T("تنبيه", "Notice"));
                RecalculateTotals();
                InvoiceGrid.Items.Refresh();
                FocusBarcodeInput();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("تعذر تنفيذ الاستبدال", "Could not complete the exchange")}: {ex.Message}", UiText.T("خطأ", "Error"));
                FocusBarcodeInput();
            }
            finally
            {
                if (loadingShown)
                    _loading.Hide();
            }
        }

        private static InvoiceWriteDto ConvertStockDocumentToReturnSource(StockDocumentReadDto document, string documentNumber)
        {
            return new InvoiceWriteDto
            {
                InvoiceNumber = documentNumber,
                InvoiceType = InvoiceType.Purchase,
                SupplierId = document.SupplierId,
                PaymentType = document.PaymentType,
                InvoiceLines = (document.Items ?? new List<StockItemReadDto>())
                    .Select(item => new InvoiceLineWriteDto
                    {
                        ProductId = item.ProductId,
                        ProductName = item.Product?.Name,
                        ProductUnitId = item.ProductUnitId,
                        Quantity = item.Quantity,
                        QuantityPerUnitSnapshot = item.QuantityPerUnitSnapshot,
                        BaseQuantity = item.BaseQuantity,
                        UnitPrice = item.PurchasePrice,
                        UnitCost = item.PurchasePrice,
                        ExpiryDate = item.ExpiryDate ?? DateTime.Today,
                        UnitNameSnapshot = item.ProductUnit?.Unit?.Name
                    })
                    .ToList()
            };
        }

        private async Task<Dictionary<(int ProductId, int ProductUnitId), decimal>> LoadPreviouslyReturnedQuantitiesAsync(string originalDocumentNumber)
        {
            var result = await _invoiceService.GetAllWriteDtoWithFilteringAndIncludeAsync(
                invoice => invoice.OriginalInvoiceId == originalDocumentNumber,
                invoice => invoice.InvoiceLines);

            return (result?.Data ?? new List<InvoiceWriteDto>())
                .SelectMany(invoice => invoice.InvoiceLines ?? Array.Empty<InvoiceLineWriteDto>())
                .Where(line => line.ProductId > 0 && line.ProductUnitId > 0 && line.Quantity < 0)
                .GroupBy(line => (line.ProductId, line.ProductUnitId))
                .ToDictionary(group => group.Key, group => group.Sum(line => Math.Abs(line.Quantity)));
        }

        //returns 
        private async void ReturnItemBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoadingReturnInvoice)
                return;

            _isLoadingReturnInvoice = true;
            var loadingShown = false;
            try
            {
                PrepareGridForReturnLoad();

                var win = new ReturnInvoiceWindow(_invoiceService);
                if (win.ShowDialog() != true)
                    return;

                _loading.Show();
                loadingShown = true;

                // جلب الفاتورة الأصلية
                var result = await _invoiceService
                    .GetAllWriteDtoWithFilteringAndIncludeAsync(
                        i => i.InvoiceNumber == win.OriginalInvoiceId,
                        i => i.InvoiceLines
                    );

                var originalInvoice = result?.Data?.FirstOrDefault();
                if (originalInvoice == null)
                {
                    var stockDocumentService = _serviceProvider.GetService<IStockDocumentService>();
                    var stockDocument = stockDocumentService == null
                        ? null
                        : (await stockDocumentService.GetDocumentWithItemsAsync(win.OriginalInvoiceId)).FirstOrDefault();
                    if (stockDocument != null)
                        originalInvoice = ConvertStockDocumentToReturnSource(stockDocument, win.OriginalInvoiceId);

                    if (originalInvoice != null)
                        goto ReturnSourceLoaded;

                    _loading.Hide();
                    loadingShown = false;
                    MessageBox.Show(UiText.T("الفاتورة غير موجودة", "The invoice was not found."), UiText.T("تنبيه", "Notice"));
                    return;
                }

            ReturnSourceLoaded:
                if (originalInvoice.InvoiceLines == null)
                {
                    _loading.Hide();
                    loadingShown = false;
                    MessageBox.Show(UiText.T("بيانات الفاتورة الأصلية غير مكتملة.", "The original invoice data is incomplete."), UiText.T("تنبيه", "Notice"));
                    return;
                }

                var originalLines = originalInvoice.InvoiceLines
                    .Where(line => line.Quantity != 0 && line.ProductId > 0 && line.ProductUnitId > 0)
                    .ToList();

                if (originalLines.Count == 0)
                {
                    _loading.Hide();
                    loadingShown = false;
                    MessageBox.Show(UiText.T("الفاتورة الأصلية لا تحتوي على أصناف قابلة للإرجاع.", "The original invoice has no returnable items."), UiText.T("تنبيه", "Notice"));
                    return;
                }

                var previouslyReturned = await LoadPreviouslyReturnedQuantitiesAsync(win.OriginalInvoiceId);

                _loading.Hide();
                loadingShown = false;

                var returnType = originalInvoice.InvoiceType == InvoiceType.Purchase
                    ? InvoiceType.PurchaseReturn
                    : InvoiceType.Return;

                // Create a new linked return invoice; the original invoice remains unchanged.
                PrepareGridForReturnLoad();
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.DataBind);
                CreateNewInvoice(returnType, win.OriginalInvoiceId);
                _invoiceLines.Clear();

                if (returnType == InvoiceType.PurchaseReturn)
                {
                    _currentInvoice.SupplierId = originalInvoice.SupplierId;
                    _currentInvoice.PaymentType = originalInvoice.PaymentType;
                    SelectInvoiceCustomer(originalInvoice.SupplierId);
                }
                else
                {
                    ApplyOriginalCreditTerms(originalInvoice);
                }


                // Load original lines as read-only snapshots with zero returned quantity.
                foreach (var originalLine in originalLines)
                {
                    var snapshot = CloneLineSnapshot(originalLine, 0m, win.OriginalInvoiceId);
                    var hydratedProduct = await ResolveProductForUnitsAsync(originalLine.ProductId);
                    var hydratedUnit = hydratedProduct?.ProductUnits?
                        .FirstOrDefault(unit => unit.Id == originalLine.ProductUnitId);

                    if (hydratedProduct != null)
                    {
                        snapshot.ProductName = string.IsNullOrWhiteSpace(snapshot.ProductName)
                            ? hydratedProduct.Name
                            : snapshot.ProductName;
                        snapshot.SelectedProduct = hydratedProduct;
                        snapshot.AvailableQuantitySnapshot =
                            await GetAvailableQuantityForProductUnitAsync(
                                originalLine.ProductId,
                                originalLine.ProductUnitId);
                    }

                    if (hydratedUnit != null)
                    {
                        snapshot.ProductUnit = MapProductUnit(hydratedUnit);
                        snapshot.QuantityPerUnitSnapshot = hydratedUnit.QuantityPerUnit > 0
                            ? hydratedUnit.QuantityPerUnit
                            : snapshot.QuantityPerUnitSnapshot;
                        snapshot.UnitNameSnapshot = hydratedUnit.Unit?.Name;
                        snapshot.UnitName = hydratedUnit.Unit?.Name;
                    }

                    var originalQuantity = Math.Abs(originalLine.Quantity);
                    var returnedBefore = previouslyReturned.TryGetValue(
                        (originalLine.ProductId, originalLine.ProductUnitId), out var priorQuantity)
                        ? priorQuantity
                        : 0m;
                    var returnableQuantity = Math.Max(0m, originalQuantity - returnedBefore);
                    _invoiceLines.Add(ReturnInvoiceLine.FromSnapshot(
                        snapshot,
                        originalQuantity,
                        returnableQuantity));
                }

                _currentInvoice.InvoiceType = returnType;

                RecalculateTotals();
                ConfigureReturnGrid(true);
                FocusFirstReturnQuantityCell();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("تعذر تنفيذ الإرجاع", "Could not complete the return")}: {ex.Message}", UiText.T("خطأ", "Error"));
            }
            finally
            {
                if (loadingShown)
                    _loading.Hide();
                _isLoadingReturnInvoice = false;
                if (_currentInvoice.InvoiceType is not (InvoiceType.Return or InvoiceType.PurchaseReturn))
                    FocusBarcodeInput();
            }
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

        private async void DebitPaymentBtn_Click(object sender, RoutedEventArgs e)
        {
            await ProcessPaymentAsync(PaymentType.Debit);
        }

        private async void CheckPaymentBtn_Click(object sender, RoutedEventArgs e)
        {
            await ProcessPaymentAsync(PaymentType.Check);
        }

        private async void MobilePaymentBtn_Click(object sender, RoutedEventArgs e)
        {
            await ProcessPaymentAsync(PaymentType.MobilePayment);
        }

        private async void CreditPaymentBtn_Click(object sender, RoutedEventArgs e)
        {
            await ProcessPaymentAsync(PaymentType.Credit);
        }

        private async void SplitPaymentBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_invoiceLines == null || _invoiceLines.Count == 0)
                return;

            PrepareInvoiceForSave();
            var total = Math.Abs(_invoiceLines.Sum(line => line.Quantity * line.UnitPrice)
                - (_currentInvoice.DiscountAmount ?? 0m));
            var paymentWindow = new ChangeInvoicePaymentWindow(
                null,
                _allCustomers ?? Enumerable.Empty<UserReadDto>(),
                _currentInvoice.CustomerId,
                total)
            { Owner = this };

            if (paymentWindow.ShowDialog() != true || paymentWindow.SelectedPayments.Count == 0)
                return;

            _currentInvoice.Payments = paymentWindow.SelectedPayments.Select(payment => new InvoicePaymentWriteDto
            {
                PaymentType = payment.PaymentType,
                Amount = payment.Amount
            }).ToList();
            _currentInvoice.PaymentType = paymentWindow.SelectedPayments[0].PaymentType;
            if (paymentWindow.SelectedCustomerId.HasValue)
            {
                _currentInvoice.CustomerId = paymentWindow.SelectedCustomerId;
                SelectInvoiceCustomer(paymentWindow.SelectedCustomerId);
            }

            await ProcessPaymentAsync(_currentInvoice.PaymentType.Value);
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
                    UiText.T("سيتم مسح الفاتورة الحالية.\nهل تريد المتابعة؟", "The current invoice will be cleared.\nDo you want to continue?"),
                    UiText.T("فاتورة جديدة", "New invoice"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes)
                    return;
            }

            ResetPOS();
            FocusBarcodeInputDeferred();
        }

        //Cancel Invoice
        private async void CancelInvoiceBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_invoiceLines.Count == 0)
                {
                    ResetPOS();
                    return;
                }

                // Never allow a stale loading overlay to cover the confirmation dialog.
                _loading.Hide();
                var confirm = MessageBox.Show(
                    UiText.T("هل تريد إلغاء الفاتورة الحالية؟", "Do you want to cancel the current invoice?"),
                    UiText.T("إلغاء", "Cancel"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes)
                    return;

                _loading.Show();

                // If invoice was saved before (Held)
                if (_currentInvoice.Id > 0)
                {
                    _currentInvoice.InvoiceLines = _invoiceLines
                        .Where(line => line.ProductId > 0 && line.ProductUnitId > 0 && line.Quantity != 0)
                        .ToList();
                    _currentInvoice.Status = InvoiceStatus.Cancelled;
                    _currentInvoice.ClosedAt = DateTime.Now;

                    var updateResult = await _invoiceService.UpdateAsync(_currentInvoice);
                    if (!updateResult.Success)
                    {
                        MessageBox.Show(updateResult.Message ?? UiText.T("فشل إلغاء الفاتورة", "Failed to cancel the invoice."), UiText.T("خطأ", "Error"));
                        return;
                    }
                }

                ResetPOS();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء إلغاء الفاتورة", "An error occurred while cancelling the invoice")}: {ex.Message}", UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _loading.Hide();
                FocusBarcodeInput();
            }
        }


        //print Invoice 
        private async Task PrintSavedSmallInvoiceAsync(int invoiceId)
        {
            if (invoiceId <= 0)
                return;

            var loadingShown = false;
            try
            {
                _loading.Show();
                loadingShown = true;

                var invoice = await _invoiceService.GetFullInvoiceByIdAsync(invoiceId);
                if (invoice == null)
                {
                    MessageBox.Show(
                        UiText.T("تعذر تحميل الفاتورة للطباعة.", "The invoice could not be loaded for printing."),
                        UiText.T("تنبيه", "Notice"));
                    return;
                }

                _lastSavedInvoice = invoice;

                // The preview is modal and can remain open while the user reviews it.
                // Do not leave the application loading window visible behind it.
                _loading.Hide();
                loadingShown = false;
                ReportPrintService.PrintSmallInvoice(invoice, this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("تعذر طباعة الفاتورة", "Could not print the invoice")}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                if (loadingShown)
                    _loading.Hide();
            }
        }

        private void PrintBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_lastSavedInvoice == null || string.IsNullOrWhiteSpace(_lastSavedInvoice.InvoiceNumber))
                {
                    MessageBox.Show(
                        UiText.T("لا توجد فاتورة للطباعة.\nيرجى إنهاء البيع أولاً.", "There is no invoice to print.\nPlease finish the sale first."),
                        UiText.T("تنبيه", "Notice"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }

                SaveSalesInvoicePdf(_lastSavedInvoice);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("تعذر طباعة الفاتورة", "Could not print the invoice")}: {ex.Message}", UiText.T("خطأ", "Error"));
            }
        }

        private void DiscountBtn_Click(object sender, RoutedEventArgs e)
        {
            DiscountTextBox.Focus();
            DiscountTextBox.SelectAll();
        }

        private void DiscountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded || _currentInvoice == null)
                return;

            if (decimal.TryParse(DiscountTextBox.Text, out var discount))
                _currentInvoice.DiscountAmount = Math.Max(0m, discount);

            RecalculateTotals();
        }

        private void InvoiceGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentInvoice?.HeldColor))
            {
                e.Row.ClearValue(Control.BackgroundProperty);
                e.Row.ClearValue(Control.ForegroundProperty);
                return;
            }

            try
            {
                e.Row.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_currentInvoice.HeldColor)!);
                e.Row.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(ResumeHeldInvoiceWindow.GetTextColor(_currentInvoice.HeldColor))!);
            }
            catch
            {
                e.Row.ClearValue(Control.BackgroundProperty);
                e.Row.ClearValue(Control.ForegroundProperty);
            }
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

                MessageBox.Show(UiText.T("تم حفظ ملف PDF بنجاح.", "The PDF file was saved successfully."), UiText.T("تم الحفظ", "Saved"), MessageBoxButton.OK, MessageBoxImage.Information);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
        }

        private void OpenReceipt_Click(object sender, RoutedEventArgs e)
        {
            var loadingShown = false;
            try
            {
                if (!TryGetActiveCashierSession(out var session))
                    return;

                var sessionId = session.Id;
                var cashierId = session.CashierId;

                _loading.Show();
                loadingShown = true;

                var win = new ReceiptWindow(_financialService, sessionId, cashierId)
                {
                    Owner = this
                };

                _loading.Hide();
                loadingShown = false;

                win.ShowDialog();
                FocusBarcodeInput();
            }
            catch (Exception ex)
            {
                if (loadingShown)
                {
                    _loading.Hide();
                    loadingShown = false;
                }
                MessageBox.Show($"{UiText.T("تعذر فتح نافذة المقبوضات", "Could not open the receipts window")}: {ex.Message}", UiText.T("خطأ", "Error"));
                FocusBarcodeInput();
            }
            finally
            {
                if (loadingShown)
                    _loading.Hide();
            }
        }


        private void OpenPayment_Click(object sender, RoutedEventArgs e)
        {
            var loadingShown = false;
            try
            {
                if (!TryGetActiveCashierSession(out var session))
                    return;

                var sessionId = session.Id;
                var cashierId = session.CashierId;

                _loading.Show();
                loadingShown = true;

                var win = new PaymentWindow(_financialService, sessionId, cashierId)
                {
                    Owner = this
                };

                _loading.Hide();
                loadingShown = false;

                win.ShowDialog();
                FocusBarcodeInput();
            }
            catch (Exception ex)
            {
                if (loadingShown)
                {
                    _loading.Hide();
                    loadingShown = false;
                }
                MessageBox.Show($"{UiText.T("تعذر فتح نافذة المدفوعات", "Could not open the payments window")}: {ex.Message}", UiText.T("خطأ", "Error"));
                FocusBarcodeInput();
            }
            finally
            {
                if (loadingShown)
                    _loading.Hide();
            }
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

        private void ProductSearchResetTimer_Tick(object? sender, EventArgs e)
        {
            _productSearchResetTimer.Stop();
            _productSearchText = string.Empty;
        }

        private async void ProductCombo_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not ComboBox combo)
                return;

            if (e.Key == Key.Back)
            {
                e.Handled = true;

                if (string.IsNullOrEmpty(_productSearchText))
                    return;

                _productSearchText = _productSearchText[..^1];
                _ = SearchProductsForComboAsync(combo, _productSearchText);
                RestartProductSearchResetTimer();
                return;
            }

            if (e.Key == Key.Down)
            {
                if (!combo.IsDropDownOpen)
                {
                    combo.IsDropDownOpen = true;
                    FocusProductSuggestionGrid(combo, selectFirstWhenInactive: true);
                    e.Handled = true;
                    return;
                }

                if (combo.IsDropDownOpen)
                {
                    FocusProductSuggestionGrid(combo, selectFirstWhenInactive: true);
                    e.Handled = true; // stop DataGrid from moving to new row
                }
            }

            if (e.Key == Key.Enter)
            {
                e.Handled = true;

                if (combo.SelectedItem is ProductReadDto selectedProduct)
                {
                    if (combo.DataContext is InvoiceLineWriteDto line)
                    {
                        line.SelectedProduct = selectedProduct;
                        if (!await ApplyLinePricingFromProductAsync(line, selectedProduct))
                        {
                        MessageBox.Show(UiText.T("لا توجد وحدات معرفة لهذا الصنف.", "There are no units defined for this item."), UiText.T("تنبيه", "Notice"));
                        return;
                    }
                        RecalculateTotals();
                    }

                    combo.IsDropDownOpen = false;
                    var grid = FindParent<DataGrid>(combo);
                    if (grid != null && combo.DataContext is InvoiceLineWriteDto focusLine)
                    {
                        var currentColumn = grid.CurrentCell.Column ?? grid.Columns.FirstOrDefault();
                        if (currentColumn != null)
                            MoveGridFocusAfterEnter(grid, focusLine, grid.Items.IndexOf(focusLine), currentColumn);
                    }
                    return;
                }

                if (combo.IsDropDownOpen)
                    return;
            }

            if (e.Key == Key.Escape)
            {
                combo.IsDropDownOpen = false;
                _productSearchText = string.Empty;
                _productSearchResetTimer.Stop();
                e.Handled = true;
            }
        }

        private void ProductCombo_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not ComboBox combo || string.IsNullOrWhiteSpace(e.Text))
                return;

            _productSearchText += e.Text;
            RestartProductSearchResetTimer();
            _ = SearchProductsForComboAsync(combo, _productSearchText);
            e.Handled = true;
        }

        private void RestartProductSearchResetTimer()
        {
            _productSearchResetTimer.Stop();
            _productSearchResetTimer.Start();
        }

        private async Task SearchProductsForComboAsync(ComboBox combo, string searchText)
        {
            var text = searchText?.Trim();
            var searchVersion = ++_comboSearchVersion;
            var currentGrid = GetProductSuggestionsGrid(combo);
            var selectedProductId = (currentGrid?.SelectedItem as ProductReadDto)?.Id
                ?? (combo.SelectedItem as ProductReadDto)?.Id;
            var currentColumnIndex = currentGrid?.CurrentCell.Column?.DisplayIndex;

            if (string.IsNullOrWhiteSpace(text))
            {
                ProductSuggestions.Clear();
                FocusProductComboSearchBox(combo);
                foreach (var product in Products)
                {
                    ProductSuggestions.Add(product);
                    FocusProductComboSearchBox(combo);
                }

                combo.IsDropDownOpen = ProductSuggestions.Any();
                RestoreProductSuggestionSelection(combo, selectedProductId, currentColumnIndex);
                FocusProductComboSearchBox(combo);
                return;
            }

            var lockTaken = false;
            try
            {
                await Task.Delay(110);
                if (searchVersion != _comboSearchVersion)
                    return;

                await _searchLock.WaitAsync();
                lockTaken = true;

                if (searchVersion != _comboSearchVersion)
                    return;

                var matches = new List<ProductReadDto>();
                var pageNumber = 1;
                var hasMore = true;

                while (matches.Count < ProductDropdownPageSize && hasMore)
                {
                    var page = await _stockService.GetReadDtoPagedListAsync(
                        pageNumber,
                        ProductStockFetchSize,
                        s => s.Quantity > 0 &&
                             s.Product != null &&
                             ((s.Product.Name != null && s.Product.Name.Contains(text)) ||
                              s.Product.ITEMCODE.ToString().Contains(text)),
                        q => q.OrderBy(s => s.Product.Name).ThenBy(s => s.ProductId),
                        s => s.Product,
                        s => s.Product.SubCategory,
                        s => s.Product.Brand,
                        s => s.Product.ProductUnits);

                    var pageItems = page?.Items?.Where(s => s.Product != null).ToList() ?? new List<StockReadDto>();
                    if (pageItems.Count == 0)
                        break;

                    CacheStocks(pageItems);

                    foreach (var stock in pageItems)
                    {
                        var product = stock.Product;
                        if (product == null || matches.Any(p => p.Id == product.Id))
                            continue;

                        matches.Add(product);
                        if (matches.Count >= ProductDropdownPageSize)
                            break;
                    }

                    pageNumber++;
                    hasMore = pageItems.Count == ProductStockFetchSize &&
                              (page?.TotalCount ?? 0) > ((pageNumber - 1) * ProductStockFetchSize);
                }

                if (searchVersion != _comboSearchVersion)
                    return;

                ProductSuggestions.Clear();
                FocusProductComboSearchBox(combo);
                foreach (var product in matches.OrderBy(p => p.Name))
                {
                    ProductSuggestions.Add(product);
                    FocusProductComboSearchBox(combo);
                }

                combo.IsDropDownOpen = ProductSuggestions.Any();
                RestoreProductSuggestionSelection(combo, selectedProductId, currentColumnIndex);
                FocusProductComboSearchBox(combo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("تعذر البحث عن الصنف", "Could not search for the item")}: {ex.Message}", UiText.T("خطأ", "Error"));
            }
            finally
            {
                if (lockTaken)
                    _searchLock.Release();
            }
        }

        private static DataGrid? GetProductSuggestionsGrid(ComboBox combo)
        {
            var popup = combo.Template?.FindName("Popup", combo) as Popup;
            return popup?.Child == null
                ? null
                : FindVisualChildren<DataGrid>(popup.Child).FirstOrDefault();
        }

        private void FocusProductSuggestionGrid(ComboBox combo, bool selectFirstWhenInactive)
        {
            Dispatcher.BeginInvoke(() =>
            {
                var grid = GetProductSuggestionsGrid(combo);
                if (grid == null || !ProductSuggestions.Any())
                    return;

                var hasActiveCell = grid.IsKeyboardFocusWithin &&
                                    grid.SelectedItem != null &&
                                    grid.CurrentCell.Column != null;

                if (selectFirstWhenInactive && !hasActiveCell)
                    grid.SelectedIndex = 0;

                if (grid.SelectedItem is not ProductReadDto selectedProduct)
                    return;

                var column = grid.CurrentCell.Column ?? grid.Columns.FirstOrDefault();
                if (column != null)
                {
                    grid.CurrentCell = new DataGridCellInfo(selectedProduct, column);
                    grid.ScrollIntoView(selectedProduct, column);
                }

                grid.Focus();
                Keyboard.Focus(grid);
            }, DispatcherPriority.Input);
        }

        private void FocusProductComboSearchBox(ComboBox combo)
        {
            Dispatcher.BeginInvoke(() =>
            {
                var searchBox = combo.Template?.FindName("PART_EditableTextBox", combo) as TextBox;
                if (searchBox != null)
                {
                    searchBox.Focus();
                    Keyboard.Focus(searchBox);
                    return;
                }

                combo.Focus();
                Keyboard.Focus(combo);
            }, DispatcherPriority.ContextIdle);
        }

        private void RestoreProductSuggestionSelection(
            ComboBox combo,
            int? selectedProductId,
            int? currentColumnIndex)
        {
            var selectedProduct = selectedProductId.HasValue
                ? ProductSuggestions.FirstOrDefault(product => product.Id == selectedProductId.Value)
                : null;
            selectedProduct ??= ProductSuggestions.FirstOrDefault();
            if (selectedProduct == null)
                return;

            combo.SelectedItem = selectedProduct;

            var grid = GetProductSuggestionsGrid(combo);
            if (grid == null)
                return;

            grid.SelectedItem = selectedProduct;
            grid.ScrollIntoView(selectedProduct);

            var column = currentColumnIndex.HasValue
                ? grid.Columns.FirstOrDefault(item => item.DisplayIndex == currentColumnIndex.Value)
                : grid.Columns.FirstOrDefault();
            if (column != null)
                grid.CurrentCell = new DataGridCellInfo(selectedProduct, column);
        }

        private async Task LoadNextProductPageAsync()
        {
            await LoadSellableProductsAsync();

            ProductSuggestions.Clear();
            foreach (var product in Products.Take(ProductDropdownPageSize))
                ProductSuggestions.Add(product);
        }




        private void ProductCombo_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox combo && combo.SelectedItem is ProductReadDto selectedProduct)
                _ = EnsureSelectedProductVisibleAsync(selectedProduct);
        }

        private async void ProductCombo_DropDownOpened(object sender, EventArgs e)
        {
            if (sender is ComboBox combo)
            {
                _productSearchText = string.Empty;
                _productSearchResetTimer.Stop();

                if (!ProductSuggestions.Any())
                {
                    await LoadNextProductPageAsync();
                }

                Dispatcher.BeginInvoke(() =>
                {
                    var popup = combo.Template?.FindName("Popup", combo) as Popup;
                    if (popup?.Child is Border border)
                    {
                        if (_productDropdownScrollViewer != null)
                            _productDropdownScrollViewer.ScrollChanged -= ProductDropdownScrollViewer_ScrollChanged;

                        var scrollViewer = border.Child as ScrollViewer;
                        _productDropdownScrollViewer = scrollViewer;
                        if (_productDropdownScrollViewer != null)
                            _productDropdownScrollViewer.ScrollChanged += ProductDropdownScrollViewer_ScrollChanged;

                        var grid = scrollViewer?.Content as DataGrid;
                        if (grid != null)
                        {
                            grid.MouseDoubleClick -= ProductGrid_MouseDoubleClick;
                            grid.PreviewKeyDown -= ProductGrid_PreviewKeyDown;
                            grid.MouseDoubleClick += ProductGrid_MouseDoubleClick;
                            grid.PreviewKeyDown += ProductGrid_PreviewKeyDown;

                            if (grid.SelectedItem == null && ProductSuggestions.Any())
                                grid.SelectedIndex = 0;

                            if (grid.SelectedItem is ProductReadDto selectedProduct && grid.Columns.Count > 0)
                            {
                                grid.CurrentCell = new DataGridCellInfo(selectedProduct, grid.Columns[0]);
                                grid.ScrollIntoView(selectedProduct, grid.Columns[0]);
                                grid.Focus();
                                Keyboard.Focus(grid);
                            }
                        }
                    }
                }, DispatcherPriority.Loaded);
            }
        }

        private async void ProductDropdownScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange <= 0)
                return;

            if (e.VerticalOffset + e.ViewportHeight < e.ExtentHeight - 40)
                return;

            await LoadNextProductPageAsync();
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
                        combo.Focus();
                    }
                }
            }
        }

        private async void SelectProductFromGrid(ProductReadDto product, DataGrid grid)
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
            if (line.Quantity <= 0)
                line.Quantity = 1;

            if (!await ApplyLinePricingFromProductAsync(line, product))
            {
                MessageBox.Show(UiText.T("لا توجد وحدات معرفة لهذا الصنف.", "There are no units defined for this item."), UiText.T("تنبيه", "Notice"));
                return;
            }

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
                        var qtyColumn = FindColumnByHeader(dataGrid, "الكمية");
                        if (qtyColumn != null)
                        {
                            dataGrid.CurrentCell = new DataGridCellInfo(line, qtyColumn);
                            dataGrid.ScrollIntoView(line, qtyColumn);
                            dataGrid.SelectedItem = line;
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

        private async void ProductCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            return;

            if (sender is not ComboBox combo) return;
            if (combo.SelectedItem is not ProductReadDto product) return;

            if (combo.DataContext is not InvoiceLineWriteDto line) return;

            line.SelectedProduct = product;
            line.Quantity = 1;
            if (!await ApplyLinePricingFromProductAsync(line, product))
            {
                MessageBox.Show(UiText.T("لا توجد وحدات معرفة لهذا الصنف.", "There are no units defined for this item."), UiText.T("تنبيه", "Notice"));
                return;
            }

            FocusBarcodeInputDeferred();
        }

        private async void ApplyProduct(ComboBox combo)
        {
            if (combo.SelectedItem is not ProductReadDto product)
                return;

            if (combo.DataContext is not InvoiceLineWriteDto line)
                return;

            line.Quantity = 1;
            if (!await ApplyLinePricingFromProductAsync(line, product))
            {
                MessageBox.Show("لا توجد وحدات معرفة لهذا الصنف.", "تنبيه");
                return;
            }
            RecalculateTotals();

            FocusBarcodeInputDeferred();
        }

        private void FocusQuantityCell(ComboBox combo)
        {
            var grid = FindParent<DataGrid>(combo);
            if (grid == null) return;

            grid.CommitEdit(DataGridEditingUnit.Cell, true);

            grid.Dispatcher.BeginInvoke(() =>
            {
                grid.CurrentCell = new DataGridCellInfo(
                    combo.DataContext,
                    FindColumnByHeader(grid, "الكمية")!
                );

                grid.ScrollIntoView(combo.DataContext, FindColumnByHeader(grid, "الكمية"));
                grid.Focus();
                grid.BeginEdit();
            }, DispatcherPriority.Background);
        }

        private void MoveGridFocusToColumn(DataGrid grid, object item, string headerText)
        {
            var targetColumn = grid.Columns.FirstOrDefault(c => string.Equals(c.Header?.ToString(), headerText, StringComparison.Ordinal));
            if (targetColumn == null)
                return;

            grid.Dispatcher.BeginInvoke(() =>
            {
                grid.CurrentCell = new DataGridCellInfo(item, targetColumn);
                grid.ScrollIntoView(item, targetColumn);
                grid.Focus();
                grid.BeginEdit();
            }, DispatcherPriority.Background);
        }

        private void MoveGridFocusToNextEditableCell(DataGrid grid, object item, int currentColumnIndex, bool isRtl)
        {
            var nextColumnIndex = currentColumnIndex + (isRtl ? 1 : -1);
            while (nextColumnIndex >= 0 && nextColumnIndex < grid.Columns.Count && grid.Columns[nextColumnIndex].IsReadOnly)
                nextColumnIndex += isRtl ? 1 : -1;

            if (nextColumnIndex < 0 || nextColumnIndex >= grid.Columns.Count)
                return;

            var nextCell = new DataGridCellInfo(item, grid.Columns[nextColumnIndex]);
            grid.Dispatcher.BeginInvoke(() =>
            {
                grid.CurrentCell = nextCell;
                grid.ScrollIntoView(item, nextCell.Column);
                grid.Focus();
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

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                yield break;

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    yield return typedChild;

                foreach (var descendant in FindVisualChildren<T>(child))
                    yield return descendant;
            }
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
            var searchVersion = ++_popupSearchVersion;
            if (text.Length < 2)
            {
                if (_currentPopup != null) _currentPopup.IsOpen = false;
                return;
            }

            try
            {
                await Task.Delay(300); // debounce
                if (searchVersion != _popupSearchVersion)
                    return;

                var result = await _stockService.GetAllWithFilteringAndIncludeAsync(
                    s => s.Product.Name.Contains(text) || s.Product.ITEMCODE.ToString().Contains(text),
                    new Expression<Func<Stock, object>>[] { s => s.Product, s => s.Product.ProductUnits });

                if (searchVersion != _popupSearchVersion) return;

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
            catch (Exception ex) { MessageBox.Show(ex.Message, UiText.T("خطأ", "Error")); }
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
                var defaultSaleUnit = ProductUnitSelector.GetDefaultSaleUnit(stockItem.ProductUnits);
                line.UnitPrice = defaultSaleUnit?.SalePrice ?? 0;
                line.ProductUnitId = defaultSaleUnit?.Id ?? 0;
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
                        _ = SelectProduct((ProductReadDto)listBox.SelectedItem);
                    }
                    break;

                case Key.Escape:
                    e.Handled = true;
                    _currentPopup.IsOpen = false;
                    _currentEditingTextBox?.Focus();
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
        private async void InvoiceGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            if (HeaderMatches(e.Column, "الصنف"))
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

            if (HeaderMatches(e.Column, "الوحدة") &&
                e.EditingElement is ComboBox unitCombo &&
                e.Row.DataContext is InvoiceLineWriteDto unitLine)
            {
                var units = await GetAvailableUnitsForProductAsync(unitLine.ProductId);

                _suppressUnitSelectionChanged = true;
                unitCombo.ItemsSource = units;
                unitCombo.SelectedValue = unitLine.ProductUnitId > 0
                    ? unitLine.ProductUnitId
                    : ProductUnitSelector.GetDefaultSaleUnit(units)?.Id ?? 0;

                Dispatcher.BeginInvoke(() =>
                {
                    _suppressUnitSelectionChanged = false;
                    unitCombo.IsDropDownOpen = true;
                    unitCombo.Focus();
                }, DispatcherPriority.Loaded);
            }

            if ((HeaderMatches(e.Column, "الكمية") ||
                 HeaderMatches(e.Column, "السعر")) &&
                e.EditingElement is TextBox numericTextBox)
            {
                numericTextBox.FlowDirection = FlowDirection.LeftToRight;
                numericTextBox.TextAlignment = TextAlignment.Left;
                numericTextBox.Language = XmlLanguage.GetLanguage("en-US");
                numericTextBox.PreviewKeyDown -= NumericEditor_PreviewKeyDown;
                numericTextBox.PreviewKeyDown += NumericEditor_PreviewKeyDown;
                numericTextBox.PreviewTextInput -= NumericEditor_PreviewTextInput;
                numericTextBox.PreviewTextInput += NumericEditor_PreviewTextInput;
            }
        }

        private void NumericEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;

            if (e.Key is Key.Decimal or Key.OemPeriod or Key.OemComma)
            {
                InsertDecimalSeparator(textBox);
                e.Handled = true;
            }
        }

        private void NumericEditor_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;

            if (!string.IsNullOrWhiteSpace(e.Text) &&
                e.Text.Any(ch => !char.IsDigit(ch)))
            {
                InsertDecimalSeparator(textBox);
                e.Handled = true;
            }
        }

        private static void InsertDecimalSeparator(TextBox textBox)
        {
            var text = textBox.Text ?? string.Empty;
            var selectionStart = textBox.SelectionStart;
            var selectionLength = textBox.SelectionLength;
            var decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            if (string.IsNullOrEmpty(decimalSeparator))
                decimalSeparator = ".";

            var textWithoutSelection = selectionLength > 0
                ? text.Remove(selectionStart, selectionLength)
                : text;

            if (textWithoutSelection.Contains('.') ||
                textWithoutSelection.Contains(',') ||
                textWithoutSelection.Contains(decimalSeparator))
            {
                return;
            }

            var updatedText = selectionLength > 0
                ? text.Remove(selectionStart, selectionLength).Insert(selectionStart, decimalSeparator)
                : text.Insert(selectionStart, decimalSeparator);

            textBox.Text = updatedText;
            textBox.SelectionStart = selectionStart + decimalSeparator.Length;
            textBox.SelectionLength = 0;
        }

        private ProductReadDto? ResolveProductForLine(InvoiceLineWriteDto line)
        {
            return line.SelectedProduct
                   ?? Products.FirstOrDefault(product => product.Id == line.ProductId);
        }

        private async Task<ProductReadDto?> ResolveProductForLineAsync(InvoiceLineWriteDto line)
        {
            var cachedProduct = ResolveProductForLine(line);
            if (cachedProduct != null)
                return cachedProduct;

            if (line.ProductId <= 0)
                return null;

            try
            {
                var result = await _productService.GetByIdAsync(line.ProductId);
                return result?.Data;
            }
            catch
            {
                return null;
            }
        }

        private async void InvoiceUnitCombo_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ComboBox combo ||
                combo.DataContext is not InvoiceLineWriteDto line)
            {
                return;
            }

            var units = await GetAvailableUnitsForProductAsync(line.ProductId);

            _suppressUnitSelectionChanged = true;
            combo.ItemsSource = units;
            combo.SelectedValue = line.ProductUnitId > 0
                ? line.ProductUnitId
                : ProductUnitSelector.GetDefaultSaleUnit(units)?.Id ?? 0;
            _suppressUnitSelectionChanged = false;
        }

        private async void InvoiceUnitCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox combo ||
                combo.DataContext is not InvoiceLineWriteDto line)
            {
                return;
            }

            if (_suppressUnitSelectionChanged)
                return;

            if (combo.SelectedValue is not int selectedUnitId || selectedUnitId <= 0)
                return;

            var product = ResolveProductForLine(line)
                         ?? await ResolveProductForLineAsync(line);
            if (product == null)
                return;

            try
            {
                if (!await ApplyLinePricingFromProductAsync(line, product, selectedUnitId))
                    return;

                RecalculateTotals();
                InvoiceGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                InvoiceGrid.CommitEdit(DataGridEditingUnit.Row, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("تعذر تحديث الوحدة", "Could not update the unit")}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ProductCombo_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox combo)
            {
                combo.IsDropDownOpen = false;
            }
        }

        private async Task SelectProduct(ProductReadDto selectedProduct)
        {
            if (_currentEditingTextBox?.DataContext is not InvoiceLineWriteDto line)
                return;

            line.Quantity = 1;
            if (!await ApplyLinePricingFromProductAsync(line, selectedProduct))
            {
                MessageBox.Show(UiText.T("لا توجد وحدات معرفة لهذا الصنف.", "There are no units defined for this item."), UiText.T("تنبيه", "Notice"));
                return;
            }
            RecalculateTotals();

            _currentPopup.IsOpen = false;

            InvoiceGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            InvoiceGrid.CommitEdit(DataGridEditingUnit.Row, true);

            FocusBarcodeInputDeferred();
        }

        //search by Name Cell 

        #endregion
        #region financialhandle 

        private void ShowPaymentValidationMessage(
            string message,
            string title,
            MessageBoxButton buttons = MessageBoxButton.OK,
            MessageBoxImage image = MessageBoxImage.Warning)
        {
            _loading.Hide();
            MessageBox.Show(message, title, buttons, image);
        }

        private async Task ProcessPaymentAsync(PaymentType paymentType)
        {
            if (_isProcessingPayment)
                return;

            _isProcessingPayment = true;
            var loadingShown = false;
            try
            {
                var timing = System.Diagnostics.Stopwatch.StartNew();
                var stepTiming = System.Diagnostics.Stopwatch.StartNew();

                void HideLoadingForDialog()
                {
                    if (loadingShown)
                    {
                        _loading.Hide();
                        loadingShown = false;
                    }
                }

                void ShowLoadingForWork()
                {
                    _loading.Show();
                    loadingShown = true;
                }

                void StopForValidationMessage()
                {
                    HideLoadingForDialog();
                }

                _loading.Show();
                loadingShown = true;
                LogPosTiming("click to processing indicator", timing, stepTiming);
                _currentInvoice.PaymentType = paymentType;

                if (!await ValidateFalconNumberBeforeSaveAsync())
                {
                    StopForValidationMessage();
                    return;
                }

                if (!CanSaveInvoice())
                {
                    StopForValidationMessage();
                    return;
                }

                StopForValidationMessage();
                if (MessageBox.Show(
                        UiText.T("هل تريد حفظ فاتورة البيع؟", "Do you want to save the sales invoice?"),
                        UiText.T("تأكيد الحفظ", "Confirm save"),
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

                ShowLoadingForWork();
                if (!await ValidateReturnOrExchangeAgainstOriginalInvoiceAsync())
                {
                    StopForValidationMessage();
                    return;
                }

                ShowLoadingForWork();
                if (!TryGetActiveCashierSession(out var session))
                {
                    StopForValidationMessage();
                    return;
                }

                ShowLoadingForWork();
                LogPosTiming("payment validation", timing, stepTiming);
                if (!await ValidateStockAvailabilityAsync())
                {
                    StopForValidationMessage();
                    return;
                }

                ShowLoadingForWork();
                LogPosTiming("stock validation", timing, stepTiming);

                PrepareInvoiceForSave();
                var expandedLines = await ExpandInvoiceLinesByFefoAsync(_invoiceLines);
                if (expandedLines == null)
                {
                    StopForValidationMessage();
                    return;
                }

                ShowLoadingForWork();
                LogPosTiming("FEFO expansion", timing, stepTiming);
                _currentInvoice.InvoiceLines = expandedLines;
                var signlessPurchaseReturn = _currentInvoice.InvoiceType == InvoiceType.PurchaseReturn;
                _currentInvoice.SubTotal = signlessPurchaseReturn
                    ? Math.Abs(expandedLines.Sum(l => l.LineSubTotal))
                    : expandedLines.Sum(l => l.LineSubTotal);
                _currentInvoice.TotalTax = signlessPurchaseReturn
                    ? Math.Abs(expandedLines.Sum(l => l.TaxAmount))
                    : expandedLines.Sum(l => l.TaxAmount);
                _currentInvoice.TotalCOGS = signlessPurchaseReturn
                    ? Math.Abs(expandedLines.Sum(l => l.Quantity * l.UnitCost))
                    : expandedLines.Sum(l => l.Quantity * l.UnitCost);
                _currentInvoice.NetSales = _currentInvoice.SubTotal - (_currentInvoice.DiscountAmount ?? 0m);
                _currentInvoice.GrossProfit = _currentInvoice.NetSales - _currentInvoice.TotalCOGS;
                var calculatedTotal = expandedLines.Sum(l => l.Quantity * l.UnitPrice) - (_currentInvoice.DiscountAmount ?? 0m);
                _currentInvoice.TotalAmount = signlessPurchaseReturn ? Math.Abs(calculatedTotal) : calculatedTotal;

                var checkAllocation = _currentInvoice.Payments?.FirstOrDefault(payment => payment.PaymentType == PaymentType.Check);
                if (checkAllocation != null)
                {
                    HideLoadingForDialog();
                    if (!await CaptureCheckDetailsAsync(checkAllocation.Amount))
                        return;

                    ShowLoadingForWork();
                }

                if (paymentType == PaymentType.Credit ||
                    _currentInvoice.Payments?.Any(payment => payment.PaymentType == PaymentType.Credit && payment.Amount > 0m) == true)
                {
                    if (CustomerComboBox.SelectedItem is not UserReadDto)
                    {
                        ShowPaymentValidationMessage(
                            UiText.T("يرجى اختيار الزبون قبل إنشاء فاتورة آجل.", "Please select a customer before creating a credit invoice."),
                            UiText.T("تنبيه", "Notice"));
                        return;
                    }

                    ShowLoadingForWork();
                    if (!await EnsureCustomerCreditAllowedAsync(_currentInvoice.TotalAmount))
                        return;

                    ShowLoadingForWork();
                }

                _currentInvoice.DeferAccountingPosting = true;
                var checkoutResult = await _saleCheckoutService.CompleteAsync(new SaleCheckoutRequest
                {
                    Invoice = _currentInvoice,
                    Session = session,
                    StockMovementsFactory = savedInvoiceId => BuildPosStockMovements(expandedLines, savedInvoiceId, session)
                });
                LogPosTiming("sale checkout", timing, stepTiming);
                if (!checkoutResult.Success || checkoutResult.Data == null)
                {
                    HideLoadingForDialog();
                    MessageBox.Show(checkoutResult.Message ?? UiText.T("فشل حفظ الفاتورة", "Failed to save the invoice."), UiText.T("خطأ", "Error"));
                    return;
                }

                _lastSavedInvoice = null;

                HideLoadingForDialog();
                MessageBox.Show(
                    paymentType == PaymentType.Credit
                        ? UiText.T("تم حفظ الفاتورة الآجلة بنجاح ✅", "The credit invoice was saved successfully.")
                        : UiText.T("تم حفظ الفاتورة، وسيتم تسجيل الحركة المالية تلقائياً ✅", "The invoice was saved; the financial transaction will be posted automatically."),
                    UiText.T("نجاح", "Success"));

                var printChoice = MessageBox.Show(
                    UiText.T("هل تريد طباعة الفاتورة الصغيرة؟", "Do you want to print the small invoice?"),
                    UiText.T("طباعة الفاتورة", "Print invoice"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (printChoice == MessageBoxResult.Yes)
                    await PrintSavedSmallInvoiceAsync(checkoutResult.Data.SavedInvoice.Id);

                ResetPOS();
            }
            catch (Exception ex)
            {
                if (loadingShown)
                {
                    _loading.Hide();
                    loadingShown = false;
                }
                MessageBox.Show($"{UiText.T("تعذر إتمام عملية الدفع", "Could not complete the payment")}: {ex.Message}", UiText.T("خطأ", "Error"));
            }
            finally
            {
                if (loadingShown)
                    _loading.Hide();

                _isProcessingPayment = false;
            }
        }

        private static void LogPosTiming(
            string step,
            System.Diagnostics.Stopwatch totalTiming,
            System.Diagnostics.Stopwatch stepTiming)
        {
            var stepMilliseconds = stepTiming.ElapsedMilliseconds;
            var totalMilliseconds = totalTiming.ElapsedMilliseconds;
            PosPerformanceLogger.Write(step, stepMilliseconds, totalMilliseconds);
            System.Diagnostics.Debug.WriteLine($"[POS timing] {step}: {stepMilliseconds} ms (total {totalMilliseconds} ms)");
            stepTiming.Restart();
        }

        private async Task<bool> CaptureCheckDetailsAsync(decimal invoiceAmount)
        {
            var dialog = new CheckDetailsWindow(invoiceAmount, _currentInvoice.Checks?.ToList())
            {
                Owner = this
            };

            if (dialog.ShowDialog() != true)
                return false;

            _currentInvoice.Checks = dialog.ResultChecks.ToList();
            await Task.CompletedTask;
            return true;
        }
        #if false
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
            if (!TryGetActiveCashierSession(out var session))
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

                CashierSessionId = session.Id,
                CashierId = session.CashierId,

                Notes = $"{_currentInvoice.InvoiceType} Invoice #{_currentInvoice.InvoiceNumber}"
            };

            var postResult = await _financialService.PostAsync(postDto);
            if (!postResult.Success)
                throw new Exception(postResult.Message ?? UiText.T("فشل تسجيل الحركة المالية", "Failed to post the financial transaction."));
        }


        #endif
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
            try
            {
                var win = _serviceProvider.GetRequiredService<StartCashierSessionWindow>();
                if (win.ShowDialog() == true)
                {
                    RefreshSessionButtons();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("تعذر فتح جلسة الكاشير", "Could not open the cashier session")}: {ex.Message}", UiText.T("خطأ", "Error"));
            }
        }

        private void CloseSessionBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var win = _serviceProvider.GetRequiredService<CloseCashierSessionWindow>();
                if (win.ShowDialog() == true)
                {
                    RefreshSessionButtons();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("تعذر إغلاق جلسة الكاشير", "Could not close the cashier session")}: {ex.Message}", UiText.T("خطأ", "Error"));
            }
        }

        private static bool TryParseDecimalInput(string? text, out decimal value)
        {
            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
                || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }
        #endregion


    }
}



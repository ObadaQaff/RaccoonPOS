#region Usings
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Cashers;
using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.StockTransactions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Auth;
using RaccoonWarehouse.Common;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Domain.Cashiers;
using RaccoonWarehouse.Domain.Cashiers.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.FinancialTransactions.DTOs;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.Stock;
using RaccoonWarehouse.Domain.Stock.DTOs;
using RaccoonWarehouse.Domain.StockTransactions.DTOs;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.FinancialTransactions;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.POS;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
using System.Windows.Markup;
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
        private readonly IStockTransactionService _stockTransactionService;
        private readonly ICashierSessionService _cashierSessionService;
        private readonly IUserSession _userSession;
        private readonly IFinancialTransactionService _financialService;

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
        private List<ProductWriteDto> _invoiceProducts;
        private readonly ILoadingService _loading;
        private ObservableCollection<ProductReadDto> Products { get; set; }
            = new ObservableCollection<ProductReadDto>();
        private const decimal MinimumSellableQuantity = 10m;
        private const int ProductDropdownPageSize = 10;
        private const int ProductStockFetchSize = 40;
        private int _nextProductStockPage = 1;
        private bool _hasMoreProductPages = true;
        private bool _isLoadingProductPage;
        private InvoiceLineWriteDto? _pendingFefoEditedLine;
        private bool _hasPendingFefoSplit;
        private bool _isProcessingPendingFefoSplit;
        private readonly HashSet<int> _loadedProductIds = new();
        private readonly Dictionary<(int ProductId, int ProductUnitId), StockReadDto> _stockLookup = new();
        private ScrollViewer? _productDropdownScrollViewer;
        private readonly DispatcherTimer _productSearchResetTimer = new() { Interval = TimeSpan.FromSeconds(1.2) };
        private string _productSearchText = string.Empty;
        private int _comboSearchVersion;
        private int _popupSearchVersion;
        private ObservableCollection<InvoiceLineWriteDto> _invoiceLines
            = new ObservableCollection<InvoiceLineWriteDto>();

        public POS(
        #region ctor            
                   IServiceProvider serviceProvider, IProductService productService,
                   IStockService stockService, IUserService userService,
                   IInvoiceService invoiceService, ILoadingService loading,
                   IStockTransactionService stockTransactionService,
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
            _stockTransactionService = stockTransactionService;
            _loading = loading;
            _cashierSessionService = cashierSessionService;
            _userSession = userSession;
            _financialService = financialService;
            #endregion

            InitializeComponent();
            this.DataContext = this;
            UiText.ApplyWindow(this);
            _productSearchResetTimer.Tick += ProductSearchResetTimer_Tick;
            Loaded += POS_Loaded;
            Closed += POS_Closed;
            CatalogRefreshNotifier.CatalogChanged += CatalogRefreshNotifier_CatalogChanged;
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
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء تحميل البيانات", "An error occurred while loading data")}: {ex.Message}", UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _loading.Hide();

            }
        }
        private async void CatalogRefreshNotifier_CatalogChanged(object? sender, EventArgs e)
        {
            if (!IsLoaded)
                return;

            await LoadProductsAsync();
        }

        private void POS_Closed(object? sender, EventArgs e)
        {
            _productSearchResetTimer.Tick -= ProductSearchResetTimer_Tick;
            CatalogRefreshNotifier.CatalogChanged -= CatalogRefreshNotifier_CatalogChanged;
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
            if (!TryGetActiveCashierSession(out var session))
                return;

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
                CasherId = session.CashierId,
                CashierSessionId = session.Id
            };

            // ✅ UI
            CurrentDateTextBlock.Text = DateTime.Now.ToString("yyyy/MM/dd");

            RecalculateTotals();
        }
        #region useabellty 
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
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

        #endregion
        private async Task LoadProductsAsync()
        {
            try
            {
                ResetProductDropdownState();
                await LoadSellableProductsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("خطأ عند تحميل المنتجات", "Error loading products")}: {ex.Message}", UiText.T("خطأ", "Error"));
            }
        }

        private void ResetProductDropdownState()
        {
            _nextProductStockPage = 1;
            _hasMoreProductPages = true;
            _loadedProductIds.Clear();
            Products.Clear();
            ProductSuggestions.Clear();
        }

        private async Task LoadSellableProductsAsync()
        {
            if (_isLoadingProductPage)
                return;

            _isLoadingProductPage = true;

            try
            {
                var result = await _stockService.GetAllWithFilteringAndIncludeAsync(
                    s => s.Quantity > 0,
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
                    .ToList() ?? new List<StockReadDto>();

                CacheStocks(stocks);

                var products = stocks
                    .GroupBy(stock => stock.ProductId)
                    .Where(group => group.Sum(stock => stock.Quantity) > MinimumSellableQuantity)
                    .Select(group => group.First().Product!)
                    .OrderBy(product => product.Name)
                    .ToList();

                foreach (var product in products)
                {
                    if (_loadedProductIds.Add(product.Id))
                    {
                        Products.Add(product);
                        ProductSuggestions.Add(product);
                    }
                }
            }
            finally
            {
                _isLoadingProductPage = false;
                _hasMoreProductPages = false;
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
            var grossSales = _invoiceLines.Sum(l => l.Quantity * l.UnitPrice);
            _currentInvoice.SubTotal = _invoiceLines.Sum(l => l.LineSubTotal);
            _currentInvoice.TotalTax = _invoiceLines.Sum(l => l.TaxAmount);
            _currentInvoice.TotalCOGS = _invoiceLines.Sum(l => l.Quantity * l.UnitCost);
            _currentInvoice.NetSales = _currentInvoice.SubTotal - (_currentInvoice.DiscountAmount ?? 0m);
            _currentInvoice.GrossProfit = _currentInvoice.NetSales - _currentInvoice.TotalCOGS;
            _currentInvoice.TotalAmount = grossSales - (_currentInvoice.DiscountAmount ?? 0m);

            TotalTextBlock.Text = _currentInvoice.TotalAmount.ToString("0.000");
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
                        productUnit.SalePrice = stock.SalePrice;
                    }
                }

                var firstStock = ResolvePreferredStock(group);

                if (firstStock?.Product != null)
                {
                    firstStock.Product.CurrentSalePrice = firstStock.SalePrice;
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
            if (_stockLookup.TryGetValue((line.ProductId, line.ProductUnitId), out var stock))
                return stock.SalePrice;

            var product = FindProductForLine(line);
            var unit = product?.ProductUnits?.FirstOrDefault(u => u.Id == line.ProductUnitId)
                       ?? ProductUnitSelector.GetDefaultSaleUnit(product?.ProductUnits);

            return unit?.SalePrice ?? line.UnitPrice;
        }

        private void RecalculateLineFromCurrentValues(InvoiceLineWriteDto line)
        {
            if (line.Quantity <= 0)
                line.Quantity = 1;

            var lineTotal = line.Quantity * line.UnitPrice;
            var divisor = 1m + (line.TaxRate / 100m);
            var lineSubTotal = line.TaxExempt || divisor <= 0m
                ? lineTotal
                : Math.Round(lineTotal / divisor, 3);
            var taxAmount = Math.Round(lineTotal - lineSubTotal, 3);
            var costTotal = line.Quantity * line.UnitCost;

            line.LineSubTotal = lineSubTotal;
            line.TaxAmount = taxAmount;
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
            var stocks = await GetAvailableStocksForProductAsync(product.Id);
            if (stocks.Sum(stock => stock.Quantity) <= MinimumSellableQuantity)
                return false;

            var stock = ResolvePreferredStock(stocks, preferredUnitId);
            if (stock == null)
                return false;

            var taxExempt = product.TaxExempt ?? false;
            var taxRate = taxExempt ? 0m : (product.TaxRate ?? 0m);
            var quantityPerUnit = stock.ProductUnit?.QuantityPerUnit > 0 ? stock.ProductUnit.QuantityPerUnit : 1m;
            var unitPrice = stock.SalePrice;
            var lineTotal = line.Quantity * unitPrice;
            var divisor = 1m + (taxRate / 100m);
            var lineSubTotal = taxExempt || divisor <= 0m
                ? lineTotal
                : Math.Round(lineTotal / divisor, 3);
            var taxAmount = Math.Round(lineTotal - lineSubTotal, 3);
            var costTotal = line.Quantity * stock.PurchasePrice;

            line.SelectedProduct = product;
            line.ProductId = product.Id;
            line.ProductName = product.Name;
            line.ProductUnitId = stock.ProductUnitId;
            line.QuantityPerUnitSnapshot = quantityPerUnit;
            line.BaseQuantity = line.Quantity * line.QuantityPerUnitSnapshot;
            line.UnitPrice = unitPrice;
            line.UnitCost = stock.PurchasePrice;
            line.TaxExempt = taxExempt;
            line.TaxRate = taxRate;
            line.LineSubTotal = lineSubTotal;
            line.TaxAmount = taxAmount;
            line.ProfitBeforeTax = lineSubTotal - costTotal;
            line.Profit = line.ProfitBeforeTax;

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

            if (header.Contains("الكمية") || header.Contains(UiText.Translate("الكمية")))
            {
                if (!TryParseDecimalInput(textBox.Text, out var quantity) || quantity <= 0)
                {
                    MessageBox.Show(UiText.T("يرجى إدخال كمية صحيحة أكبر من صفر.", "Please enter a valid quantity greater than zero."), UiText.T("تنبيه", "Notice"));
                    line.Quantity = 1;
                }
                else
                {
                    line.Quantity = quantity;
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

                if (line.UnitPrice != originalUnitPrice && line.UnitPrice < line.UnitCost)
                {
                    var defaultPrice = GetDefaultSalePrice(line);
                    MessageBox.Show(UiText.T("سعر البيع لا يمكن أن يكون أقل من التكلفة. سيتم إعادة السعر الافتراضي.", "Sale price cannot be lower than cost. The default price will be restored."), UiText.T("تنبيه", "Notice"));
                    line.UnitPrice = defaultPrice;
                    textBox.Text = defaultPrice.ToString("0.000");
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

            MessageBox.Show(UiText.T("لا توجد جلسة كاشير مفتوحة. الرجاء فتح جلسة أولاً.", "There is no open cashier session. Please open a session first."), UiText.T("خطأ", "Error"));
            RefreshSessionButtons();
            return false;
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
                            s => s.Quantity > 0 && s.Product.ITEMCODE.ToString() == barcode,
                            new Expression<Func<Stock, object>>[]
                            {
                                s => s.Product,
                                s => s.Product.SubCategory,
                                s => s.Product.Brand,
                                s => s.Product.ProductUnits
                            });
                if (result == null || result.Data == null || result.Data.Count == 0)
                {
                    MessageBox.Show(UiText.T("الصنف غير موجود.", "The item was not found."), UiText.T("تنبيه", "Notice"));
                    return;
                }
                var product = result.Data.FirstOrDefault()?.Product;
                if (product == null)
                {
                    MessageBox.Show(UiText.T("الصنف غير موجود.", "The item was not found."), UiText.T("تنبيه", "Notice"));
                    return;
                }

                await AddProductToInvoiceAsync(product);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, UiText.T("خطأ", "Error"));
            }
            finally
            {
                BarcodeTextBox.Clear();
                BarcodeTextBox.Focus();
            }
        }

        private async Task<bool> AddProductToInvoiceAsync(ProductReadDto product)
        {
            if (product == null)
                return false;

            var stocks = await GetAvailableStocksForProductAsync(product.Id);
            var stock = ResolvePreferredStock(stocks);
            if (stock == null)
            {
                MessageBox.Show(UiText.T("لا يوجد مخزون متاح لهذا الصنف.", "There is no available stock for this item."), UiText.T("تنبيه", "Notice"));
                return false;
            }

            var existingLine = _invoiceLines
                .FirstOrDefault(l => l.ProductId == product.Id && l.ProductUnitId == stock.ProductUnitId);

            if (existingLine != null)
            {
                existingLine.Quantity += 1;
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

                line.Quantity = 1;
                if (!await ApplyLinePricingFromProductAsync(line, product, stock.ProductUnitId))
                {
                    MessageBox.Show(UiText.T("لا يوجد مخزون متاح لهذا الصنف.", "There is no available stock for this item."), UiText.T("تنبيه", "Notice"));
                    return false;
                }
            }

            var targetUnitId = stock.ProductUnitId;
            await SplitDraftLinesByFefoAsync(product.Id, targetUnitId);

            RecalculateTotals();

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

            return true;
        }

        private async Task SplitDraftLinesByFefoAsync(int productId, int productUnitId)
        {
            var matchingLines = _invoiceLines
                .Where(l => l.ProductId == productId && l.ProductUnitId == productUnitId && l.Quantity > 0)
                .ToList();

            if (!matchingLines.Any())
                return;

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
                return;

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
                    UnitPrice = allocation.SalePrice,
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

            InvoiceGrid.Items.Refresh();
            RecalculateTotals();
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
                                $"الكمية المطلوبة للصنف {sourceLine.ProductName} غير متوفرة. تم تعديل الكمية إلى الحد الأقصى المتاح: {availableQuantity:0.###}",
                                $"The requested quantity for {sourceLine.ProductName} is not available. The quantity was adjusted to the maximum available: {availableQuantity:0.###}"),
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
            var stocks = await GetAvailableStocksForProductAsync(productId);
            return stocks
                .Where(stock => stock.ProductUnitId == productUnitId)
                .Sum(stock => Math.Max(stock.Quantity, 0m));
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
                var splitLine = new InvoiceLineWriteDto
                {
                    SelectedProduct = templateLine.SelectedProduct,
                    ProductId = templateLine.ProductId,
                    ProductName = templateLine.ProductName,
                    ProductUnitId = templateLine.ProductUnitId,
                    Quantity = allocation.Quantity,
                    QuantityPerUnitSnapshot = allocation.QuantityPerUnitSnapshot,
                    BaseQuantity = allocation.BaseQuantity,
                    UnitPrice = allocation.SalePrice,
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
                        Quantity = -line.Quantity,
                        QuantityPerUnitSnapshot = quantityPerUnit,
                        BaseQuantity = -baseQuantity,
                        UnitPrice = line.UnitPrice,
                        PurchasePrice = line.UnitCost,
                        SalePrice = line.UnitPrice,
                        ExpiryDate = line.ExpiryDate,
                        TransactionType = MapStockTransactionType(_currentInvoice.InvoiceType),
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
            foreach (var line in _invoiceLines.Where(l => l.Quantity > 0).ToList())
            {
                var existingStock = await _stockService.GetAllWriteDtoWithFilteringAndIncludeAsync(
                    s => s.ProductId == line.ProductId && s.ProductUnitId == line.ProductUnitId);

                if (existingStock?.Data == null || existingStock.Data.Count == 0)
                {
                    MessageBox.Show(
                        UiText.T(
                            $"الصنف {line.ProductName} غير موجود في المخزون. لن يتم حفظ الفاتورة.",
                            $"The item {line.ProductName} was not found in stock. The invoice will not be saved."),
                        UiText.T("تنبيه", "Notice"));
                    return false;
                }

                var stock = existingStock.Data.First();
                _stockLookup[(stock.ProductId, stock.ProductUnitId)] = new StockReadDto
                {
                    ProductId = stock.ProductId,
                    ProductUnitId = stock.ProductUnitId,
                    Quantity = stock.Quantity,
                    PurchasePrice = stock.PurchasePrice,
                    SalePrice = stock.SalePrice
                };

                line.UnitPrice = stock.SalePrice;
                line.UnitCost = stock.PurchasePrice;
                if (stock.Quantity >= line.Quantity)
                {
                    RecalculateLineFromCurrentValues(line);
                    continue;
                }

                var availableQuantity = Math.Max(stock.Quantity, 0m);

                if (availableQuantity > 0)
                {
                    line.Quantity = availableQuantity;
                    RecalculateLineFromCurrentValues(line);
                    MessageBox.Show(
                        UiText.T(
                            $"الكمية المطلوبة للصنف {line.ProductName} غير متوفرة. تم تعديل الكمية إلى الحد الأقصى المتاح: {availableQuantity:0.###}",
                            $"The requested quantity for {line.ProductName} is not available. The quantity was adjusted to the maximum available: {availableQuantity:0.###}"),
                        UiText.T("تنبيه", "Notice"));
                }
                else
                {
                    _invoiceLines.Remove(line);
                    MessageBox.Show(
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

        private async Task<List<InvoiceLineWriteDto>> ExpandInvoiceLinesByFefoAsync(IEnumerable<InvoiceLineWriteDto> sourceLines)
        {
            var expandedLines = new List<InvoiceLineWriteDto>();

            foreach (var sourceLine in sourceLines.Where(line => line.Quantity > 0))
            {
                var allocationResult = await _stockService.AllocateOutgoingAsync(new[]
                {
                    new StockAllocationRequestDto
                    {
                        ProductId = sourceLine.ProductId,
                        ProductUnitId = sourceLine.ProductUnitId,
                        Quantity = sourceLine.Quantity
                    }
                });

                if (!allocationResult.Success || allocationResult.Data == null || allocationResult.Data.Count == 0)
                    throw new InvalidOperationException(allocationResult.Message ?? UiText.T($"تعذر تخصيص المخزون للصنف {sourceLine.ProductName}.", $"Could not allocate stock for item {sourceLine.ProductName}."));

                foreach (var allocation in allocationResult.Data)
                {
                    var splitLine = new InvoiceLineWriteDto
                    {
                        ProductId = sourceLine.ProductId,
                        ProductName = sourceLine.ProductName,
                        ProductUnitId = sourceLine.ProductUnitId,
                        Quantity = allocation.Quantity,
                        QuantityPerUnitSnapshot = allocation.QuantityPerUnitSnapshot,
                        BaseQuantity = allocation.BaseQuantity,
                        UnitPrice = allocation.SalePrice,
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
            if (_invoiceLines.Count == 0)
            {
                MessageBox.Show(UiText.T("لا يوجد أصناف في الفاتورة.", "There are no items in the invoice."), UiText.T("تنبيه", "Notice"));
                return false;
            }
            
            if (_invoiceLines.Any(l => l.ProductId <= 0 || l.ProductUnitId <= 0 || l.Quantity <= 0))
            {
                MessageBox.Show(UiText.T("يوجد صنف ببيانات غير مكتملة أو كمية غير صالحة.", "There is an item with incomplete data or an invalid quantity."), UiText.T("تنبيه", "Notice"));
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
            if (TryGetActiveCashierSession(out var session))
            {
                _currentInvoice.CasherId = session.CashierId;
                _currentInvoice.CashierSessionId = session.Id;
            }

            var customer = CustomerComboBox.SelectedItem as UserReadDto;
            _currentInvoice.CustomerId = customer?.Id;
            _currentInvoice.IsPOS = true;
            _currentInvoice.Status = InvoiceStatus.Completed;
            _currentInvoice.ClosedAt = DateTime.Now;
            RecalculateTotals();
        }
        private void ResetPOS()
        {
            _invoiceLines.Clear();
            InvoiceGrid.SelectedItem = null;
            InvoiceGrid.SelectedCells.Clear();
            InvoiceGrid.CurrentCell = new DataGridCellInfo();
            InvoiceGrid.Items.Refresh();

            TotalTextBlock.Text = "0.000";

            BarcodeTextBox.Clear();
            CustomerComboBox.SelectedIndex = -1;
            CustomerComboBox.SelectedItem = null;
            CustomerComboBox.Text = string.Empty;
            _lastSavedInvoice = null;

            CreateNewInvoice();

            InvoiceGrid.Items.Refresh();
            BarcodeTextBox.Focus();
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
                    async product =>
                    {
                        if (product == null)
                            return false;

                        return await AddProductToInvoiceAsync(product);
                    },
                    disabledKeys)
                {
                    Owner = this
                };

                searchWindow.ShowDialog();
                FocusBarcodeInput();
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
                    _currentInvoice.CustomerId = (combo.SelectedItem as UserReadDto)?.Id;
                    FocusBarcodeInput();
                    break;

                case Key.Escape:
                    e.Handled = true;
                    combo.IsDropDownOpen = false;
                    FocusBarcodeInput();
                    break;

                case Key.Down:
                    if (!combo.IsDropDownOpen)
                        combo.IsDropDownOpen = true;
                    break;
            }
        }

        private void CustomerComboBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is not ComboBox combo)
                return;

            combo.IsDropDownOpen = true;
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
            if (_invoiceLines.Count == 0)
            {
                MessageBox.Show(UiText.T("لا توجد مواد لحفظها", "There are no items to hold."), UiText.T("تنبيه", "Notice"));
                return;
            }

            try
            {
                _currentInvoice.Status = InvoiceStatus.OnHold;
                _currentInvoice.IsPOS = true;
                _currentInvoice.ClosedAt = null;
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
                FocusBarcodeInput();
            }
        }
        //resume held invoice
        private void ResumeHoldBtn_Click(object sender, RoutedEventArgs e)
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

                LoadInvoiceIntoPOS(win.SelectedInvoice);
                FocusBarcodeInput();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("تعذر استئناف الفاتورة المعلقة", "Could not resume the held invoice")}: {ex.Message}", UiText.T("خطأ", "Error"));
                FocusBarcodeInput();
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
                    ProductUnitId = line.ProductUnitId,
                    QuantityPerUnitSnapshot = line.QuantityPerUnitSnapshot,
                    BaseQuantity = line.BaseQuantity,
                    UnitCost = line.UnitCost,
                    TaxExempt = line.TaxExempt,
                    TaxRate = line.TaxRate,
                    TaxAmount = line.TaxAmount,
                    LineSubTotal = line.LineSubTotal,
                    ProfitBeforeTax = line.ProfitBeforeTax,
                    Profit = line.Profit,
                    OriginalInvoiceId = line.OriginalInvoiceId
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
        private void ExchangeItemBtn_Click(object sender, RoutedEventArgs e)
        {
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

                CreateNewInvoice(
                    InvoiceType.Exchange,
                    win.OriginalInvoiceId
                );

                _invoiceLines.Add(CloneLineSnapshot(
                    selectedRow,
                    -Math.Abs(selectedRow.Quantity),
                    win.OriginalInvoiceId));

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
        }

        //returns 
        private async void ReturnItemBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedRow = GetSelectedInvoiceLine();

                if (selectedRow == null)
                {
                    MessageBox.Show(UiText.T("يرجى تحديد مادة لإرجاعها", "Please select an item to return."), UiText.T("تنبيه", "Notice"));
                    return;
                }

                var win = new ReturnInvoiceWindow(_invoiceService);
                if (win.ShowDialog() != true)
                    return;

                _loading.Show();

                // جلب الفاتورة الأصلية
                var result = await _invoiceService
                    .GetAllWriteDtoWithFilteringAndIncludeAsync(
                        i => i.InvoiceNumber == win.OriginalInvoiceId,
                        i => i.InvoiceLines
                    );

                var originalInvoice = result?.Data?.FirstOrDefault();
                if (originalInvoice == null)
                {
                    MessageBox.Show(UiText.T("الفاتورة غير موجودة", "The invoice was not found."), UiText.T("تنبيه", "Notice"));
                    return;
                }

                if (originalInvoice.InvoiceLines == null)
                {
                    MessageBox.Show(UiText.T("بيانات الفاتورة الأصلية غير مكتملة.", "The original invoice data is incomplete."), UiText.T("تنبيه", "Notice"));
                    return;
                }

                bool exists = originalInvoice.InvoiceLines
                    .Any(l => l.ProductId == selectedRow.ProductId && l.ProductUnitId == selectedRow.ProductUnitId);

                if (!exists)
                {
                    MessageBox.Show(UiText.T("لا يمكن إرجاع مادة غير موجودة في الفاتورة الأصلية", "You cannot return an item that does not exist in the original invoice."), UiText.T("تنبيه", "Notice"));
                    return;
                }

                // 🟢 إنشاء فاتورة مرتجع جديدة
                CreateNewInvoice(
                    InvoiceType.Return,
                    win.OriginalInvoiceId
                );


                // 🟢 إضافة السطر المرتجع
                _invoiceLines.Add(CloneLineSnapshot(
                    selectedRow,
                    -Math.Abs(selectedRow.Quantity),
                    win.OriginalInvoiceId));
                _currentInvoice.InvoiceType = InvoiceType.Return;

                RecalculateTotals();
                InvoiceGrid.Items.Refresh();
                FocusBarcodeInput();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("تعذر تنفيذ الإرجاع", "Could not complete the return")}: {ex.Message}", UiText.T("خطأ", "Error"));
            }
            finally
            {
                _loading.Hide();
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
            FocusBarcodeInput();
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
            MessageBox.Show(UiText.T("ميزة الخصم في شاشة نقاط البيع غير مفعلة حالياً. لن يتم تنفيذ أي تغيير حتى تتم إضافتها بشكل آمن.", "The discount feature in POS is not enabled yet. No change will be applied until it is added safely."), UiText.T("تنبيه", "Notice"));
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
            try
            {
                if (!TryGetActiveCashierSession(out var session))
                    return;

                var sessionId = session.Id;
                var cashierId = session.CashierId;

                var win = new ReceiptWindow(_financialService, sessionId, cashierId)
                {
                    Owner = this
                };

                win.ShowDialog();
                FocusBarcodeInput();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("تعذر فتح نافذة المقبوضات", "Could not open the receipts window")}: {ex.Message}", UiText.T("خطأ", "Error"));
                FocusBarcodeInput();
            }
        }


        private void OpenPayment_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!TryGetActiveCashierSession(out var session))
                    return;

                var sessionId = session.Id;
                var cashierId = session.CashierId;

                var win = new PaymentWindow(_financialService, sessionId, cashierId)
                {
                    Owner = this
                };

                win.ShowDialog();
                FocusBarcodeInput();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("تعذر فتح نافذة المدفوعات", "Could not open the payments window")}: {ex.Message}", UiText.T("خطأ", "Error"));
                FocusBarcodeInput();
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
                    e.Handled = true;
                    return;
                }

                if (combo.IsDropDownOpen && combo.SelectedItem != null)
                {
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
                            MessageBox.Show(UiText.T("لا يوجد مخزون متاح لهذا الصنف.", "There is no available stock for this item."), UiText.T("تنبيه", "Notice"));
                            return;
                        }
                        RecalculateTotals();
                    }

                    combo.IsDropDownOpen = false;
                    FocusQuantityCell(combo);
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

            if (string.IsNullOrWhiteSpace(text))
            {
                ProductSuggestions.Clear();
                foreach (var product in Products)
                    ProductSuggestions.Add(product);

                combo.IsDropDownOpen = ProductSuggestions.Any();
                if (ProductSuggestions.Any())
                    combo.SelectedIndex = 0;
                return;
            }

            var lockTaken = false;
            try
            {
                await Task.Delay(180);
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
                foreach (var product in matches.OrderBy(p => p.Name))
                    ProductSuggestions.Add(product);

                combo.IsDropDownOpen = ProductSuggestions.Any();
                if (ProductSuggestions.Any())
                    combo.SelectedIndex = 0;
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

        private async Task LoadNextProductPageAsync()
        {
            if (_isLoadingProductPage)
                return;

            if (!_hasMoreProductPages && Products.Any())
            {
                ProductSuggestions.Clear();
                foreach (var product in Products.Take(ProductDropdownPageSize))
                    ProductSuggestions.Add(product);
                return;
            }

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
                MessageBox.Show(UiText.T("لا يوجد مخزون متاح لهذا الصنف.", "There is no available stock for this item."), UiText.T("تنبيه", "Notice"));
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
            if (sender is not ComboBox combo) return;
            if (combo.SelectedItem is not ProductReadDto product) return;

            if (combo.DataContext is not InvoiceLineWriteDto line) return;

            line.SelectedProduct = product;
            line.Quantity = 1;
            if (!await ApplyLinePricingFromProductAsync(line, product))
            {
                MessageBox.Show(UiText.T("لا يوجد مخزون متاح لهذا الصنف.", "There is no available stock for this item."), UiText.T("تنبيه", "Notice"));
                return;
            }

            FocusQuantityCell(combo);
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
                MessageBox.Show("لا يوجد مخزون متاح لهذا الصنف.", "تنبيه");
                return;
            }
            RecalculateTotals();

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
        private void InvoiceGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
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

            line.Quantity = 1;
            if (!await ApplyLinePricingFromProductAsync(line, selectedProduct))
            {
                MessageBox.Show(UiText.T("لا يوجد مخزون متاح لهذا الصنف.", "There is no available stock for this item."), UiText.T("تنبيه", "Notice"));
                return;
            }
            RecalculateTotals();

            _currentPopup.IsOpen = false;

            InvoiceGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            InvoiceGrid.CommitEdit(DataGridEditingUnit.Row, true);

            var qtyColumn = InvoiceGrid.Columns
                .First(c => HeaderMatches(c, "الكمية"));

            InvoiceGrid.CurrentCell = new DataGridCellInfo(line, qtyColumn);
            InvoiceGrid.ScrollIntoView(line, qtyColumn);
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
                PaymentType.Debit => PaymentMethod.BankTransfer,
                PaymentType.Check => PaymentMethod.Check,
                PaymentType.MobilePayment => PaymentMethod.MobilePayment,
                PaymentType.Credit => PaymentMethod.Credit,
                _ => PaymentMethod.Cash
            };
        }

        private async Task ProcessPaymentAsync(PaymentType paymentType)
        {
            var loadingShown = false;
            try
            {
                _currentInvoice.PaymentType = paymentType;

                if (!CanSaveInvoice())
                    return;
                if (!TryGetActiveCashierSession(out var session))
                    return;
                if (!await ValidateStockAvailabilityAsync())
                    return;

                PrepareInvoiceForSave();
                var expandedLines = await ExpandInvoiceLinesByFefoAsync(_invoiceLines);
                _currentInvoice.InvoiceLines = expandedLines;
                _currentInvoice.SubTotal = expandedLines.Sum(l => l.LineSubTotal);
                _currentInvoice.TotalTax = expandedLines.Sum(l => l.TaxAmount);
                _currentInvoice.TotalCOGS = expandedLines.Sum(l => l.Quantity * l.UnitCost);
                _currentInvoice.NetSales = _currentInvoice.SubTotal - (_currentInvoice.DiscountAmount ?? 0m);
                _currentInvoice.GrossProfit = _currentInvoice.NetSales - _currentInvoice.TotalCOGS;
                _currentInvoice.TotalAmount = expandedLines.Sum(l => l.Quantity * l.UnitPrice) - (_currentInvoice.DiscountAmount ?? 0m);

                _loading.Show();
                loadingShown = true;

                var result = _currentInvoice.Id > 0
                    ? await _invoiceService.UpdateAsync(_currentInvoice)
                    : await _invoiceService.CreateAsync(_currentInvoice);
                if (!result.Success || result.Data == null)
                {
                    _loading.Hide();
                    loadingShown = false;
                    MessageBox.Show(result.Message ?? UiText.T("فشل حفظ الفاتورة", "Failed to save the invoice."), UiText.T("خطأ", "Error"));
                    return;
                }

                var savedInvoiceId = result.Data.Id > 0 ? result.Data.Id : _currentInvoice.Id;

                _lastSavedInvoice = await _invoiceService.GetFullInvoiceByIdAsync(savedInvoiceId);

                var movementResult = await _stockService.PostMovementsAsync(BuildPosStockMovements(expandedLines, savedInvoiceId, session));
                if (!movementResult.Success)
                {
                    _loading.Hide();
                    loadingShown = false;
                    MessageBox.Show(movementResult.Message ?? UiText.T("فشل تحديث المخزون.", "Failed to update stock."), UiText.T("خطأ", "Error"));
                    return;
                }

                if (paymentType != PaymentType.Credit)
                {
                    var postDto = new FinancialPostDto
                    {
                        Direction = TransactionDirection.In,
                        Method = MapPaymentMethod(paymentType),
                        Amount = _currentInvoice.TotalAmount,
                        TransactionDate = DateTime.Now,

                        SourceType = FinancialSourceType.PosSaleInvoice,
                        SourceId = savedInvoiceId,

                        CashierSessionId = session.Id,
                        CashierId = session.CashierId,

                        Notes = $"POS Invoice #{_currentInvoice.InvoiceNumber}"
                    };

                    var postResult = await _financialService.PostAsync(postDto);
                    if (!postResult.Success)
                    {
                        _loading.Hide();
                        loadingShown = false;
                        MessageBox.Show(postResult.Message ?? UiText.T("تم حفظ الفاتورة وتحديث المخزون لكن فشل تسجيل الحركة المالية", "The invoice was saved and stock was updated, but posting the financial transaction failed."), UiText.T("تحذير", "Warning"));
                        return;
                    }
                }

                _loading.Hide();
                loadingShown = false;
                MessageBox.Show(
                    paymentType == PaymentType.Credit
                        ? UiText.T("تم حفظ الفاتورة الآجلة بنجاح ✅", "The credit invoice was saved successfully.")
                        : UiText.T("تم حفظ الفاتورة وتسجيل الحركة المالية ✅", "The invoice was saved and the financial transaction was posted successfully."),
                    UiText.T("نجاح", "Success"));
                ResetPOS();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("تعذر إتمام عملية الدفع", "Could not complete the payment")}: {ex.Message}", UiText.T("خطأ", "Error"));
            }
            finally
            {
                if (loadingShown)
                    _loading.Hide();
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



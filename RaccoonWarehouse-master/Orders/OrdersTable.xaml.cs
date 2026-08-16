using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Orders;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Orders.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Integration;
using RaccoonWarehouse.Navigation;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.Orders
{
    public partial class OrdersTable : Window
    {
        private const int SearchDelayMs = 300;
        private readonly ApplicationDbContext _context;
        private readonly IPandaOrderSyncService _orderSyncService;
        private readonly ILoadingService _loadingService;
        private readonly ObservableCollection<OrderInvoiceRow> _orders = new();
        private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
        private bool _isSynchronizing;
        private bool _suppressFilterEvents;
        private int _filterVersion;

        public OrdersTable(
            ApplicationDbContext context,
            IPandaOrderSyncService orderSyncService,
            ILoadingService loadingService)
        {
            InitializeComponent();
            _context = context;
            _orderSyncService = orderSyncService;
            _loadingService = loadingService;
            OrdersGrid.ItemsSource = _orders;
            Loaded += OrdersTable_Loaded;
            Closed += OrdersTable_Closed;
            _orderSyncService.OrderImported += OrderSyncService_OrderImported;
        }

        private void OrdersTable_Closed(object? sender, EventArgs e)
        {
            _orderSyncService.OrderImported -= OrderSyncService_OrderImported;
        }

        private void OrderSyncService_OrderImported(object? sender, OrderImportedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(async () => await LoadOrdersAsync()));
        }

        private async void OrdersTable_Loaded(object sender, RoutedEventArgs e)
        {
            UiText.ApplyWindow(this);
            InitializeStatusFilter();
            await RefreshOrdersAsync();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await RefreshOrdersAsync();
        }

        private async void OrdersGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (OrdersGrid.SelectedItem is not OrderInvoiceRow selected)
                return;

            WindowManager.ShowDialog<OrderInvoiceDetails>(
                WindowSizeType.LargeRectangle,
                window => window.SetInvoiceId(selected.Id));

            await LoadOrdersAsync();
        }

        private void InitializeStatusFilter()
        {
            _suppressFilterEvents = true;
            StatusFilterComboBox.Items.Clear();
            StatusFilterComboBox.Items.Add(new ComboBoxItem
            {
                Content = UiText.T("الكل", "All"),
                Tag = null
            });

            foreach (var status in GetEndpointOrderStatuses())
            {
                StatusFilterComboBox.Items.Add(new ComboBoxItem
                {
                    Content = GetStatusText(status),
                    Tag = status
                });
            }

            StatusFilterComboBox.SelectedIndex = 0;
            _suppressFilterEvents = false;
        }

        private static string GetStatusText(InvoiceStatus status)
        {
            return status switch
            {
                InvoiceStatus.Draft => UiText.T("مسودة", "Draft"),
                InvoiceStatus.Completed => UiText.T("مكتمل", "Completed"),
                InvoiceStatus.Posted => UiText.T("مرحّل", "Posted"),
                InvoiceStatus.Cancelled => UiText.T("ملغي", "Cancelled"),
                InvoiceStatus.Returned => UiText.T("مرتجع", "Returned"),
                InvoiceStatus.OnHold => UiText.T("قيد الانتظار", "On hold"),
                InvoiceStatus.Unknown => UiText.T("غير معروف", "Unknown"),
                InvoiceStatus.InProcess => UiText.T("قيد التجهيز", "In Process"),
                _ => status.ToString()
            };
        }

        private static InvoiceStatus[] GetEndpointOrderStatuses()
        {
            return
            [
                InvoiceStatus.Unknown,
                InvoiceStatus.InProcess,
                InvoiceStatus.Completed,
                InvoiceStatus.Cancelled
            ];
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded || _suppressFilterEvents)
                return;

            var filterVersion = Interlocked.Increment(ref _filterVersion);
            _ = DebouncedLoadOrdersAsync(filterVersion);
        }

        private async Task DebouncedLoadOrdersAsync(int filterVersion)
        {
            try
            {
                await Task.Delay(SearchDelayMs);
                if (filterVersion != _filterVersion)
                    return;

                await LoadOrdersAsync(filterVersion);
            }
            catch (Exception ex)
            {
                if (filterVersion != _filterVersion)
                    return;

                MessageBox.Show(
                    $"{UiText.T("تعذر البحث في الطلبيات.", "Failed to search orders.")} {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void StatusFilterComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (!IsLoaded || _suppressFilterEvents)
                return;

            var filterVersion = Interlocked.Increment(ref _filterVersion);
            await LoadOrdersAsync(filterVersion);
        }

        private async void ApplyFilters_Click(object sender, RoutedEventArgs e)
        {
            if (FromDatePicker.SelectedDate.HasValue &&
                ToDatePicker.SelectedDate.HasValue &&
                FromDatePicker.SelectedDate.Value.Date > ToDatePicker.SelectedDate.Value.Date)
            {
                MessageBox.Show(
                    UiText.T(
                        "يجب أن يكون تاريخ البداية قبل تاريخ النهاية أو مساوياً له.",
                        "The from date must be before or equal to the to date."),
                    UiText.T("تنبيه", "Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var filterVersion = Interlocked.Increment(ref _filterVersion);
            await LoadOrdersAsync(filterVersion);
        }

        private async void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            _suppressFilterEvents = true;
            SearchTextBox.Clear();
            StatusFilterComboBox.SelectedIndex = 0;
            FromDatePicker.SelectedDate = null;
            ToDatePicker.SelectedDate = null;
            _suppressFilterEvents = false;

            var filterVersion = Interlocked.Increment(ref _filterVersion);
            await LoadOrdersAsync(filterVersion);
        }

        #region Temporary Box API Integration

        private async Task RefreshOrdersAsync()
        {
            if (_isSynchronizing)
                return;

            var loadingShown = false;

            try
            {
                _isSynchronizing = true;
                _loadingService.Show();
                loadingShown = true;
                await _loadSemaphore.WaitAsync();
                // Orders now come from the Raccoon database. The live synchronization
                // service imports Panda events independently of this window.
                var importResult = new
                {
                    Success = true,
                    Message = (string?)null,
                    Data = new BoxOrderImportResultDto()
                };
                await LoadOrdersCoreAsync();

                if (!importResult.Success)
                {
                    MessageBox.Show(
                        importResult.Message ?? UiText.T("تعذر استيراد الطلبات الجديدة.", "Failed to import new orders."),
                        UiText.T("تنبيه", "Notice"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                else if (importResult.Data?.Errors.Count > 0)
                {
                    MessageBox.Show(
                        string.Join(Environment.NewLine, importResult.Data.Errors),
                        UiText.T("تنبيه", "Notice"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                await LoadOrdersCoreAsync();
                MessageBox.Show(
                    $"{UiText.T("تعذر تحميل الطلبات.", "Failed to load orders.")} {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                _loadSemaphore.Release();

                if (loadingShown)
                    _loadingService.Hide();

                _isSynchronizing = false;
            }
        }

        #endregion

        private async Task LoadOrdersAsync(int? filterVersion = null)
        {
            await _loadSemaphore.WaitAsync();
            try
            {
                await LoadOrdersCoreAsync(filterVersion);
            }
            catch (Exception ex)
            {
                StatusText.Text = UiText.T("تعذر تحميل الطلبيات.", "Failed to load orders.");
                MessageBox.Show(
                    $"{UiText.T("تعذر تحميل الطلبيات", "Failed to load orders")}: {ex.Message}",
                    UiText.T("خطأ", "Error"));
            }
            finally
            {
                _loadSemaphore.Release();
            }
        }

        private async Task LoadOrdersCoreAsync(int? filterVersion = null)
        {
            if (filterVersion.HasValue && filterVersion.Value != _filterVersion)
                return;

            StatusText.Text = UiText.T("جاري تحميل الطلبيات...", "Loading orders...");

            var searchText = SearchTextBox.Text?.Trim();
            var selectedStatus = StatusFilterComboBox.SelectedItem is ComboBoxItem statusItem &&
                                 statusItem.Tag is InvoiceStatus status
                ? status
                : (InvoiceStatus?)null;
            var fromDate = FromDatePicker.SelectedDate?.Date;
            var toDateExclusive = ToDatePicker.SelectedDate?.Date.AddDays(1);

            var query = _context.Set<RaccoonWarehouse.Domain.Invoices.Invoice>()
                .AsNoTracking()
                .Include(invoice => invoice.InvoiceLines)
                .Where(invoice => invoice.InvoiceType == InvoiceType.appCart);

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(invoice =>
                    invoice.InvoiceNumber.Contains(searchText) ||
                    (invoice.CustomerId.HasValue &&
                     _context.Set<RaccoonWarehouse.Domain.Users.User>()
                         .Any(user =>
                             user.Id == invoice.CustomerId.Value &&
                             user.Name.Contains(searchText))));
            }

            if (selectedStatus.HasValue)
                query = query.Where(invoice => invoice.Status == selectedStatus.Value);

            if (fromDate.HasValue)
                query = query.Where(invoice => invoice.CreatedDate >= fromDate.Value);

            if (toDateExclusive.HasValue)
                query = query.Where(invoice => invoice.CreatedDate < toDateExclusive.Value);

            var rows = await query
                .OrderByDescending(invoice => invoice.CreatedDate)
                .Select(invoice => new OrderInvoiceRow
                {
                    Id = invoice.Id,
                    InvoiceNumber = invoice.InvoiceNumber,
                    CustomerName = _context.Set<RaccoonWarehouse.Domain.Users.User>()
                        .Where(user => user.Id == invoice.CustomerId)
                        .Select(user => user.Name)
                        .FirstOrDefault() ?? string.Empty,
                    CreatedDate = invoice.CreatedDate,
                    DocumentDate = invoice.DocumentDate,
                    StatusValue = invoice.Status,
                    TotalAmount = invoice.TotalAmount,
                    ItemsCount = invoice.InvoiceLines != null ? invoice.InvoiceLines.Count : 0
                })
                .ToListAsync();

            if (filterVersion.HasValue && filterVersion.Value != _filterVersion)
                return;

            foreach (var row in rows)
            {
                row.Status = row.StatusValue.HasValue
                    ? GetStatusText(row.StatusValue.Value)
                    : string.Empty;
            }

            _orders.Clear();
            foreach (var row in rows)
                _orders.Add(row);

            StatusText.Text = _orders.Count == 0
                ? UiText.T("لا توجد طلبيات لعرضها.", "There are no orders to display.")
                : string.Format(UiText.T("عدد الطلبيات: {0}", "Orders count: {0}"), _orders.Count);
        }
    }
}

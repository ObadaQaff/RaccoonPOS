using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using System.Collections.ObjectModel;
using System.Windows;

namespace RaccoonWarehouse.Orders
{
    public partial class OrdersTable : Window
    {
        private readonly ApplicationDbContext _context;
        private readonly ObservableCollection<OrderInvoiceRow> _orders = new();
        private bool _isLoading;

        public OrdersTable(ApplicationDbContext context)
        {
            InitializeComponent();
            _context = context;
            OrdersGrid.ItemsSource = _orders;
            Loaded += OrdersTable_Loaded;
        }

        private async void OrdersTable_Loaded(object sender, RoutedEventArgs e)
        {
            UiText.ApplyWindow(this);
            await LoadOrdersAsync();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadOrdersAsync();
        }

        private async void OrdersGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (OrdersGrid.SelectedItem is not OrderInvoiceRow selected)
            {
                return;
            }

            WindowManager.ShowDialog<OrderInvoiceDetails>(
                WindowSizeType.LargeRectangle,
                window => window.SetInvoiceId(selected.Id));

            await LoadOrdersAsync();
        }

        private async Task LoadOrdersAsync()
        {
            if (_isLoading)
            {
                return;
            }

            try
            {
                _isLoading = true;
                StatusText.Text = UiText.T("جاري تحميل الطلبيات...", "Loading orders...");

                var rows = await _context.Set<RaccoonWarehouse.Domain.Invoices.Invoice>()
                    .AsNoTracking()
                    .Include(invoice => invoice.InvoiceLines)
                    .Where(invoice => invoice.InvoiceType == InvoiceType.EndpointOrder)
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
                        Status = invoice.Status != null ? invoice.Status.ToString() : string.Empty,
                        TotalAmount = invoice.TotalAmount,
                        ItemsCount = invoice.InvoiceLines != null ? invoice.InvoiceLines.Count : 0
                    })
                    .ToListAsync();

                _orders.Clear();

                foreach (var row in rows)
                {
                    _orders.Add(row);
                }

                StatusText.Text = _orders.Count == 0
                    ? UiText.T("لا توجد طلبيات لعرضها.", "There are no orders to display.")
                    : string.Format(UiText.T("عدد الطلبيات: {0}", "Orders count: {0}"), _orders.Count);
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
                _isLoading = false;
            }
        }
    }
}

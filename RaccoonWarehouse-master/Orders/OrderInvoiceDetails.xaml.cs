using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Helpers.Localization;
using System.Collections.ObjectModel;
using System.Windows;

namespace RaccoonWarehouse.Orders
{
    public partial class OrderInvoiceDetails : Window
    {
        private readonly ApplicationDbContext _context;
        private readonly ObservableCollection<OrderInvoiceLineRow> _lines = new();
        private int _invoiceId;
        private bool _isSavingStatus;

        public OrderInvoiceDetails(ApplicationDbContext context)
        {
            InitializeComponent();
            _context = context;
            LinesGrid.ItemsSource = _lines;
            Loaded += OrderInvoiceDetails_Loaded;
        }

        public void SetInvoiceId(int invoiceId)
        {
            _invoiceId = invoiceId;
        }

        private async void OrderInvoiceDetails_Loaded(object sender, RoutedEventArgs e)
        {
            UiText.ApplyWindow(this);
            StatusComboBox.ItemsSource = Enum.GetValues(typeof(InvoiceStatus));
            await LoadInvoiceAsync();
        }

        private async Task LoadInvoiceAsync()
        {
            if (_invoiceId <= 0)
            {
                MessageBox.Show(UiText.T("لم يتم تحديد الفاتورة.", "No invoice was selected."), UiText.T("خطأ", "Error"));
                Close();
                return;
            }

            var invoice = await _context.Set<RaccoonWarehouse.Domain.Invoices.Invoice>()
                .AsNoTracking()
                .Include(x => x.InvoiceLines)
                    .ThenInclude(line => line.Product)
                .Include(x => x.InvoiceLines)
                    .ThenInclude(line => line.ProductUnit)
                        .ThenInclude(productUnit => productUnit.Unit)
                .FirstOrDefaultAsync(x => x.Id == _invoiceId);

            if (invoice == null)
            {
                MessageBox.Show(UiText.T("لم يتم العثور على الفاتورة.", "Invoice was not found."), UiText.T("خطأ", "Error"));
                Close();
                return;
            }

            var customerName = invoice.CustomerId.HasValue
                ? await _context.Set<RaccoonWarehouse.Domain.Users.User>()
                    .AsNoTracking()
                    .Where(user => user.Id == invoice.CustomerId.Value)
                    .Select(user => user.Name)
                    .FirstOrDefaultAsync()
                : null;

            InvoiceTitleText.Text = string.Format(
                UiText.T("الفاتورة رقم {0}", "Invoice #{0}"),
                invoice.InvoiceNumber);
            InvoiceNumberText.Text = invoice.InvoiceNumber;
            CustomerText.Text = string.IsNullOrWhiteSpace(customerName) ? UiText.T("غير محدد", "Unspecified") : customerName;
            StatusComboBox.SelectedItem = invoice.Status ?? InvoiceStatus.OnHold;
            TotalAmountText.Text = invoice.TotalAmount.ToString("N2");

            _lines.Clear();
            foreach (var line in (invoice.InvoiceLines ?? Array.Empty<RaccoonWarehouse.Domain.InvoiceLines.InvoiceLine>())
                .OrderBy(line => line.Id))
            {
                _lines.Add(new OrderInvoiceLineRow
                {
                    ProductId = line.ProductId,
                    ProductName = line.Product?.Name ?? string.Empty,
                    UnitName = line.ProductUnit?.Unit?.Name ?? string.Empty,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    TaxAmount = line.TaxAmount,
                    LineTotal = line.LineTotal
                });
            }
        }

        private async void SaveStatus_Click(object sender, RoutedEventArgs e)
        {
            if (_isSavingStatus)
            {
                return;
            }

            if (StatusComboBox.SelectedItem is not InvoiceStatus selectedStatus)
            {
                MessageBox.Show(UiText.T("يرجى اختيار حالة صحيحة.", "Please select a valid status."), UiText.T("تنبيه", "Notice"));
                return;
            }

            try
            {
                _isSavingStatus = true;
                var invoice = await _context.Set<RaccoonWarehouse.Domain.Invoices.Invoice>()
                    .FirstOrDefaultAsync(x => x.Id == _invoiceId);

                if (invoice == null)
                {
                    MessageBox.Show(UiText.T("لم يتم العثور على الفاتورة.", "Invoice was not found."), UiText.T("خطأ", "Error"));
                    return;
                }

                invoice.Status = selectedStatus;
                invoice.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync();

                MessageBox.Show(UiText.T("تم حفظ حالة الفاتورة.", "Invoice status was saved."), UiText.T("تم", "Done"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("تعذر حفظ حالة الفاتورة", "Failed to save invoice status")}: {ex.Message}",
                    UiText.T("خطأ", "Error"));
            }
            finally
            {
                _isSavingStatus = false;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public sealed class OrderInvoiceLineRow
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal LineTotal { get; set; }
    }
}

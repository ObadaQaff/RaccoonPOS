using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Orders;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Orders.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.Orders
{
    public partial class OrderInvoiceDetails : Window
    {
        private readonly ApplicationDbContext _context;
        private readonly IEndpointOrderStatusService _endpointOrderStatusService;
        private readonly ILoadingService _loadingService;
        private readonly ObservableCollection<OrderInvoiceLineRow> _lines = new();
        private readonly ObservableCollection<OrderProductOption> _products = new();
        private List<OrderLineDetailsSnapshot> _loadedLineDetails = new();
        private int _invoiceId;
        private bool _isSavingStatus;
        private bool _isSavingDetails;

        public OrderInvoiceDetails(
            ApplicationDbContext context,
            IEndpointOrderStatusService endpointOrderStatusService,
            ILoadingService loadingService)
        {
            InitializeComponent();
            _context = context;
            _endpointOrderStatusService = endpointOrderStatusService;
            _loadingService = loadingService;
            LinesGrid.ItemsSource = _lines;
            ProductComboBox.ItemsSource = _products;
            Loaded += OrderInvoiceDetails_Loaded;
        }

        public void SetInvoiceId(int invoiceId)
        {
            _invoiceId = invoiceId;
        }

        private async void OrderInvoiceDetails_Loaded(object sender, RoutedEventArgs e)
        {
            UiText.ApplyWindow(this);
            InitializeStatusOptions();
            await LoadProductsAsync();
            await LoadInvoiceAsync();
        }

        private async Task LoadProductsAsync()
        {
            var products = await _context.Set<RaccoonWarehouse.Domain.Products.Product>()
                .AsNoTracking()
                .Include(product => product.ProductUnits)
                    .ThenInclude(productUnit => productUnit.Unit)
                .Where(product => !product.IsDeleted)
                .OrderBy(product => product.Name)
                .ToListAsync();

            _products.Clear();
            foreach (var product in products)
            {
                _products.Add(new OrderProductOption
                {
                    Id = product.Id,
                    Name = product.Name ?? string.Empty,
                    Units = (product.ProductUnits ?? Array.Empty<RaccoonWarehouse.Domain.ProductUnits.ProductUnit>())
                        .Select(unit => new OrderUnitOption
                        {
                            Id = unit.Id,
                            Name = unit.Unit?.Name ?? string.Empty,
                            SalePrice = unit.SalePrice
                        })
                        .OrderBy(unit => unit.Name)
                        .ToList()
                });
            }
        }

        private void InitializeStatusOptions()
        {
            StatusComboBox.Items.Clear();
            AddStatusOption(InvoiceStatus.Unknown, UiText.T("غير معروف", "Unknown"));
            AddStatusOption(InvoiceStatus.InProcess, UiText.T("قيد التجهيز", "In Process"));
            AddStatusOption(InvoiceStatus.Completed, UiText.T("مكتمل", "Completed"));
            AddStatusOption(InvoiceStatus.Cancelled, UiText.T("ملغي", "Cancelled"));
        }

        private void AddStatusOption(InvoiceStatus status, string text)
        {
            StatusComboBox.Items.Add(new ComboBoxItem
            {
                Tag = status,
                Content = text
            });
        }

        private async Task LoadInvoiceAsync()
        {
            if (_invoiceId <= 0)
            {
                MessageBox.Show(
                    UiText.T("لم يتم تحديد الفاتورة.", "No invoice was selected."),
                    UiText.T("خطأ", "Error"));
                Close();
                return;
            }

            var invoice = await _context.Set<RaccoonWarehouse.Domain.Invoices.Invoice>()
                .AsNoTracking()
                .Include(item => item.InvoiceLines)
                    .ThenInclude(line => line.Product)
                .Include(item => item.InvoiceLines)
                    .ThenInclude(line => line.ProductUnit)
                        .ThenInclude(productUnit => productUnit.Unit)
                .FirstOrDefaultAsync(item => item.Id == _invoiceId);

            if (invoice == null)
            {
                MessageBox.Show(
                    UiText.T("لم يتم العثور على الفاتورة.", "Invoice was not found."),
                    UiText.T("خطأ", "Error"));
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
            CustomerText.Text = string.IsNullOrWhiteSpace(customerName)
                ? UiText.T("غير محدد", "Unspecified")
                : customerName;
            var currentStatus = invoice.Status ?? InvoiceStatus.Unknown;
            var displayedStatus = currentStatus == InvoiceStatus.OnHold
                ? InvoiceStatus.Unknown
                : currentStatus;
            StatusComboBox.SelectedItem = StatusComboBox.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(item => item.Tag is InvoiceStatus status && status == displayedStatus);
            TotalAmountText.Text = invoice.TotalAmount.ToString("N2");
            var canEditDetails = currentStatus is InvoiceStatus.Unknown
                or InvoiceStatus.InProcess
                or InvoiceStatus.OnHold;
            LinesGrid.IsReadOnly = !canEditDetails;
            SaveDetailsButton.IsEnabled = canEditDetails;
            ProductComboBox.IsEnabled = canEditDetails;
            UnitComboBox.IsEnabled = canEditDetails;
            QuantityTextBox.IsEnabled = canEditDetails;
            UnitPriceTextBox.IsEnabled = canEditDetails;
            AddReplaceLineButton.IsEnabled = canEditDetails;
            DeleteLineButton.IsEnabled = canEditDetails;

            _lines.Clear();
            foreach (var line in (invoice.InvoiceLines ??
                                  Array.Empty<RaccoonWarehouse.Domain.InvoiceLines.InvoiceLine>())
                     .OrderBy(line => line.Id))
            {
                _lines.Add(new OrderInvoiceLineRow
                {
                    InvoiceLineId = line.Id,
                    ProductId = line.ProductId,
                    ProductUnitId = line.ProductUnitId,
                    ProductName = line.Product?.Name ?? string.Empty,
                    UnitName = line.ProductUnit?.Unit?.Name ?? string.Empty,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    TaxAmount = line.TaxAmount,
                    LineTotal = line.LineTotal
                });
            }

            _loadedLineDetails = _lines.Select(OrderLineDetailsSnapshot.FromRow).ToList();

            ClearLineEditor();
        }

        private void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductComboBox.SelectedItem is not OrderProductOption product)
            {
                UnitComboBox.ItemsSource = null;
                return;
            }

            UnitComboBox.ItemsSource = product.Units;
            UnitComboBox.SelectedItem = product.Units.FirstOrDefault();
        }

        private void UnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UnitComboBox.SelectedItem is OrderUnitOption unit &&
                string.IsNullOrWhiteSpace(UnitPriceTextBox.Text))
            {
                UnitPriceTextBox.Text = unit.SalePrice.ToString("0.###");
            }
        }

        private void LinesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LinesGrid.SelectedItem is not OrderInvoiceLineRow line)
                return;

            ProductComboBox.SelectedItem = _products.FirstOrDefault(product => product.Id == line.ProductId);
            if (ProductComboBox.SelectedItem is OrderProductOption product)
                UnitComboBox.SelectedItem = product.Units.FirstOrDefault(unit => unit.Id == line.ProductUnitId);

            QuantityTextBox.Text = line.Quantity.ToString("0.###");
            UnitPriceTextBox.Text = line.UnitPrice.ToString("0.###");
        }

        private void AddReplaceLine_Click(object sender, RoutedEventArgs e)
        {
            if (ProductComboBox.SelectedItem is not OrderProductOption product ||
                UnitComboBox.SelectedItem is not OrderUnitOption unit ||
                !decimal.TryParse(QuantityTextBox.Text, out var quantity) ||
                quantity <= 0 ||
                !decimal.TryParse(UnitPriceTextBox.Text, out var unitPrice) ||
                unitPrice < 0)
            {
                MessageBox.Show(
                    UiText.T(
                        "يرجى اختيار صنف ووحدة وإدخال كمية وسعر صحيحين.",
                        "Select a product and unit and enter a valid quantity and price."),
                    UiText.T("تنبيه", "Notice"));
                return;
            }

            var existing = LinesGrid.SelectedItem as OrderInvoiceLineRow;
            var line = existing ?? new OrderInvoiceLineRow();
            line.ProductId = product.Id;
            line.ProductUnitId = unit.Id;
            line.ProductName = product.Name;
            line.UnitName = unit.Name;
            line.Quantity = quantity;
            line.UnitPrice = unitPrice;

            if (existing == null)
                _lines.Add(line);

            LinesGrid.Items.Refresh();
            ClearLineEditor();
            UpdateDisplayedTotal();
        }

        private void DeleteLine_Click(object sender, RoutedEventArgs e)
        {
            if (LinesGrid.SelectedItem is not OrderInvoiceLineRow line)
                return;

            _lines.Remove(line);
            ClearLineEditor();
            UpdateDisplayedTotal();
        }

        private void ClearLineEditor()
        {
            LinesGrid.SelectedItem = null;
            ProductComboBox.SelectedItem = null;
            UnitComboBox.ItemsSource = null;
            QuantityTextBox.Clear();
            UnitPriceTextBox.Clear();
        }

        private void UpdateDisplayedTotal()
        {
            TotalAmountText.Text = _lines.Sum(line => line.LineTotal).ToString("N2");
        }

        private async void SaveDetails_Click(object sender, RoutedEventArgs e)
        {
            if (_isSavingDetails)
                return;

            LinesGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            LinesGrid.CommitEdit(DataGridEditingUnit.Row, true);

            if (!TryApplyPendingSelectedLine())
                return;

            if (!HaveLineDetailsChanged())
            {
                MessageBox.Show(
                    UiText.T(
                        "لم يتم اكتشاف أي تغييرات لحفظها.",
                        "No changes were detected to save."),
                    UiText.T("تنبيه", "Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (_lines.Count == 0 ||
                _lines.Any(line =>
                    line.ProductId <= 0 ||
                    line.ProductUnitId <= 0 ||
                    line.Quantity <= 0 ||
                    line.UnitPrice < 0))
            {
                MessageBox.Show(
                    UiText.T(
                        "يجب أن يحتوي الطلب على صنف واحد على الأقل، وأن تكون الكمية أكبر من صفر والسعر صفراً أو أكبر.",
                        "The order must contain at least one product; quantity must be positive and price zero or greater."),
                    UiText.T("تنبيه", "Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var loadingShown = false;
            try
            {
                _isSavingDetails = true;
                SaveDetailsButton.IsEnabled = false;
                _loadingService.Show();
                loadingShown = true;

                var result = await _endpointOrderStatusService.UpdateDetailsAsync(new EndpointOrderEditDto
                {
                    InvoiceId = _invoiceId,
                    Lines = _lines.Select(line => new EndpointOrderLocalLineDto
                    {
                        InvoiceLineId = line.InvoiceLineId,
                        ProductId = line.ProductId,
                        ProductUnitId = line.ProductUnitId,
                        Quantity = line.Quantity,
                        UnitPrice = line.UnitPrice
                    }).ToList()
                });

                if (!result.Success)
                {
                    _loadingService.Hide();
                    loadingShown = false;
                    await LoadInvoiceAsync();
                    MessageBox.Show(
                        result.Message,
                        UiText.T("خطأ", "Error"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                await LoadInvoiceAsync();
                _loadingService.Hide();
                loadingShown = false;
                MessageBox.Show(
                    UiText.T(
                        "تم تحديث تفاصيل الطلب المحلي والمخزون.",
                        "Local order details and stock were updated."),
                    UiText.T("تم", "Done"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("تعذر تحديث تفاصيل الطلب", "Failed to update order details")}: {ex.Message}",
                    UiText.T("خطأ", "Error"));
            }
            finally
            {
                if (loadingShown)
                    _loadingService.Hide();

                _isSavingDetails = false;
                SaveDetailsButton.IsEnabled = LinesGrid.IsReadOnly == false;
            }
        }

        private bool HaveLineDetailsChanged()
        {
            if (_lines.Count != _loadedLineDetails.Count)
                return true;

            return _lines
                .Select(OrderLineDetailsSnapshot.FromRow)
                .Where((current, index) => current != _loadedLineDetails[index])
                .Any();
        }

        private bool TryApplyPendingSelectedLine()
        {
            if (LinesGrid.SelectedItem is not OrderInvoiceLineRow selectedLine)
                return true;

            if (ProductComboBox.SelectedItem is not OrderProductOption product ||
                UnitComboBox.SelectedItem is not OrderUnitOption unit ||
                !decimal.TryParse(QuantityTextBox.Text, out var quantity) ||
                quantity <= 0 ||
                !decimal.TryParse(UnitPriceTextBox.Text, out var unitPrice) ||
                unitPrice < 0)
            {
                MessageBox.Show(
                    UiText.T(
                        "يرجى اختيار صنف ووحدة وإدخال كمية وسعر صحيحين.",
                        "Select a product and unit and enter a valid quantity and price."),
                    UiText.T("تنبيه", "Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            selectedLine.ProductId = product.Id;
            selectedLine.ProductUnitId = unit.Id;
            selectedLine.ProductName = product.Name;
            selectedLine.UnitName = unit.Name;
            selectedLine.Quantity = quantity;
            selectedLine.UnitPrice = unitPrice;
            selectedLine.LineTotal = quantity * unitPrice;
            LinesGrid.Items.Refresh();
            UpdateDisplayedTotal();
            return true;
        }

        private async void SaveStatus_Click(object sender, RoutedEventArgs e)
        {
            if (_isSavingStatus)
                return;

            if (StatusComboBox.SelectedItem is not ComboBoxItem statusItem ||
                statusItem.Tag is not InvoiceStatus selectedStatus)
            {
                MessageBox.Show(
                    UiText.T("يرجى اختيار حالة صحيحة.", "Please select a valid status."),
                    UiText.T("تنبيه", "Notice"));
                return;
            }

            var loadingShown = false;
            try
            {
                _isSavingStatus = true;
                _loadingService.Show();
                loadingShown = true;

                var result = await _endpointOrderStatusService.ApplyStatusAsync(_invoiceId, selectedStatus);
                if (!result.Success)
                {
                    _loadingService.Hide();
                    loadingShown = false;
                    MessageBox.Show(
                        result.Message ?? UiText.T("تعذر حفظ حالة الفاتورة.", "Failed to save invoice status."),
                        UiText.T("خطأ", "Error"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                await LoadInvoiceAsync();
                _loadingService.Hide();
                loadingShown = false;
                MessageBox.Show(
                    UiText.T(
                        "تم حفظ حالة الفاتورة وتحديث المخزون والقيود المرتبطة.",
                        "Invoice status, stock, and related accounting were updated."),
                    UiText.T("تم", "Done"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("تعذر حفظ حالة الفاتورة", "Failed to save invoice status")}: {ex.Message}",
                    UiText.T("خطأ", "Error"));
            }
            finally
            {
                if (loadingShown)
                    _loadingService.Hide();

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
        public int InvoiceLineId { get; set; }
        public int ProductId { get; set; }
        public int ProductUnitId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal LineTotal { get; set; }
    }

    internal sealed record OrderLineDetailsSnapshot(
        int ProductId,
        int ProductUnitId,
        decimal Quantity,
        decimal UnitPrice)
    {
        public static OrderLineDetailsSnapshot FromRow(OrderInvoiceLineRow row) =>
            new(row.ProductId, row.ProductUnitId, row.Quantity, row.UnitPrice);
    }

    public sealed class OrderProductOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<OrderUnitOption> Units { get; set; } = new();
    }

    public sealed class OrderUnitOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal SalePrice { get; set; }
    }
}

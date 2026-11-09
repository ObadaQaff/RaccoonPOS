using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.PurchaseRequests;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.PurchaseRequests.DTOs;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Invoices;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.PurchaseRequests;

public partial class PurchaseRequestEditorWindow : Window
{
    private readonly IPurchaseRequestService _service;
    private readonly ILoadingService _loading;
    private readonly IProductService _productService;
    private readonly IStockService _stockService;
    private readonly IUserService _userService;
    private readonly IUserSession _userSession;
    private readonly ObservableCollection<PendingLine> _lines = new();
    private bool _suppliersLoaded;
    private bool _isSaving;
    private int? _editingId;

    public PurchaseRequestEditorWindow(
        IPurchaseRequestService service,
        ILoadingService loading,
        IProductService productService,
        IStockService stockService,
        IUserService userService,
        IUserSession userSession)
    {
        _service = service;
        _loading = loading;
        _productService = productService;
        _stockService = stockService;
        _userService = userService;
        _userSession = userSession;
        
        InitializeComponent();
        LinesGrid.ItemsSource = _lines;
        RequestDatePicker.SelectedDate = DateTime.Today;
        RequesterTextBox.Text = _userSession.CurrentUser?.Name ?? string.Empty;
        CategoryComboBox.SelectedIndex = 0;
        UiText.ApplyWindow(this);
    }

    public void InitializeForEdit(int requestId)
    {
        _editingId = requestId;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadSuppliersAsync();
        if (_editingId.HasValue)
            await LoadRequestAsync(_editingId.Value);
        else
            ResetForNewRequest();
    }

    private async Task LoadSuppliersAsync()
    {
        if (_suppliersLoaded)
            return;

        try
        {
            var result = await _userService.GetAllAsync();
            var suppliers = result.Data?.Where(x => x.Role == UserRole.Supplier).OrderBy(x => x.Name).ToList() ?? new();
            suppliers.Insert(0, new UserReadDto { Id = 0, Name = "All suppliers" });
            SupplierFilterComboBox.ItemsSource = suppliers;
            SupplierFilterComboBox.SelectedIndex = 0;
            _suppliersLoaded = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not load suppliers: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadRequestAsync(int requestId)
    {
        _loading.Show();
        try
        {
            var result = await _service.GetByIdAsync(requestId);
            if (!result.Success || result.Data == null)
            {
                MessageBox.Show(result.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            var request = result.Data;
            RequestNumberTextBox.Text = request.RequestNumber;
            RequestDatePicker.SelectedDate = request.RequestDate == default ? DateTime.Today : request.RequestDate;
            RequesterTextBox.Text = request.RequesterName ?? string.Empty;
            DepartmentTextBox.Text = request.Department ?? string.Empty;
            PriorityComboBox.SelectedIndex = request.Priority?.ToUpperInvariant() switch
            {
                "HIGH" => 0,
                "LOW" => 2,
                _ => 1
            };
            CategoryComboBox.SelectedIndex = request.Category == PurchaseRequestCategory.Other ? 1 : 0;
            StatusText.Text = request.Status.ToString();

            _lines.Clear();
            foreach (var line in request.Lines ?? new List<PurchaseRequestLineWriteDto>())
                AddLine(PendingLine.FromDto(line));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not load the purchase request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
        finally
        {
            _loading.Hide();
        }
    }

    private void ResetForNewRequest()
    {
        _editingId = null;
        RequestNumberTextBox.Text = "Generated on save";
        RequestDatePicker.SelectedDate = DateTime.Today;
        RequesterTextBox.Text = _userSession.CurrentUser?.Name ?? string.Empty;
        DepartmentTextBox.Clear();
        PriorityComboBox.SelectedIndex = 1;
        CategoryComboBox.SelectedIndex = 0;
        StatusText.Text = "Draft";
        _lines.Clear();
        UpdateTotalAmount();
    }

    private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SelectProductsButton == null)
            return;

        var drugs = GetSelectedCategory() == PurchaseRequestCategory.Drugs;
        SelectProductsButton.Visibility = drugs ? Visibility.Visible : Visibility.Collapsed;
        SupplierFilterComboBox.IsEnabled = drugs;
        OtherLineEditor.Visibility = drugs ? Visibility.Collapsed : Visibility.Visible;
        AddOtherLineButton.Visibility = drugs ? Visibility.Collapsed : Visibility.Visible;
        ModeHintText.Text = drugs
            ? "Drugs mode: choose products from the catalog. Filter by supplier, name, or barcode."
            : "Other mode: enter free-text request lines without linking them to the product catalog.";
    }

    private PurchaseRequestCategory GetSelectedCategory()
    {
        return (CategoryComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() == nameof(PurchaseRequestCategory.Other)
            ? PurchaseRequestCategory.Other
            : PurchaseRequestCategory.Drugs;
    }

    private int? SelectedSupplierId =>
        SupplierFilterComboBox.SelectedItem is UserReadDto { Id: > 0 } supplier ? supplier.Id : null;

    private void SelectProducts_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var searchWindow = new PurchaseProductSearchWindow(
                _productService,
                _stockService,
                AddProductLineAsync,
                supplierId: SelectedSupplierId)
            {
                Owner = this
            };
            searchWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open the product selector: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private Task<bool> AddProductLineAsync(PurchaseProductSearchWindow.PurchaseProductSearchRow row)
    {
        if (row.ProductUnitId <= 0)
        {
            MessageBox.Show("The selected product has no purchase unit.", "Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
            return Task.FromResult(false);
        }

        var existing = _lines.FirstOrDefault(x => x.ProductId == row.Product.Id && x.ProductUnitId == row.ProductUnitId);
        if (existing != null)
        {
            existing.RequestedQuantity += row.Quantity;
            LinesGrid.Items.Refresh();
            return Task.FromResult(true);
        }

        var selectedUnit = row.Product.ProductUnits?.FirstOrDefault(x => x.Id == row.ProductUnitId);
        AddLine(new PendingLine
        {
            ProductId = row.Product.Id,
            ProductUnitId = row.ProductUnitId,
            Barcode = row.ItemCode,
            ItemName = row.ProductName,
            UnitName = row.UnitName,
            UnitPrice = row.PurchasePrice,
            RequestedQuantity = row.Quantity,
            SupplierId = row.Product.SupplierId,
            SupplierName = row.Product.Supplier?.Name,
            AvailableStockSnapshot = row.Product.CurrentStockQuantity,
            Notes = selectedUnit == null ? null : $"Quantity per unit: {selectedUnit.QuantityPerUnit:N3}"
        });
        return Task.FromResult(true);
    }

    private void AddOtherLine_Click(object sender, RoutedEventArgs e)
    {
        var itemName = OtherNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(itemName) || !TryParseDecimal(OtherQuantityTextBox.Text, out var quantity) || quantity <= 0)
        {
            MessageBox.Show("Enter an item name and a positive quantity.", "Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryParseDecimal(OtherPriceTextBox.Text, out var price) || price < 0)
        {
            MessageBox.Show("Enter a valid non-negative unit price.", "Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        AddLine(new PendingLine
        {
            ItemName = itemName,
            UnitName = OtherUnitTextBox.Text.Trim(),
            UnitPrice = price,
            RequestedQuantity = quantity,
            SupplierName = OtherSupplierTextBox.Text.Trim(),
            Notes = OtherNotesTextBox.Text.Trim()
        });
        OtherNameTextBox.Clear();
        OtherUnitTextBox.Clear();
        OtherPriceTextBox.Text = "0";
        OtherQuantityTextBox.Text = "1";
        OtherSupplierTextBox.Clear();
        OtherNotesTextBox.Clear();
    }

    private void RemoveLine_Click(object sender, RoutedEventArgs e)
    {
        if (LinesGrid.SelectedItem is PendingLine line)
        {
            _lines.Remove(line);
            line.PropertyChanged -= PendingLine_PropertyChanged;
            UpdateTotalAmount();
        }
    }

    private async void SaveDraft_Click(object sender, RoutedEventArgs e)
    {
        if (_isSaving)
            return;
        if (_lines.Count == 0)
        {
            MessageBox.Show("Add at least one request line first.", "Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _isSaving = true;
        _loading.Show();
        try
        {
            var dto = new PurchaseRequestWriteDto
            {
                Id = _editingId ?? 0,
                Category = GetSelectedCategory(),
                RequestDate = RequestDatePicker.SelectedDate ?? DateTime.Today,
                RequesterId = _userSession.CurrentUser?.Id,
                RequesterName = _userSession.CurrentUser?.Name,
                Priority = (PriorityComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                Department = DepartmentTextBox.Text.Trim(),
                Lines = _lines.Select(x => x.ToDto()).ToList()
            };

            var result = _editingId.HasValue
                ? await _service.UpdateAsync(dto)
                : await _service.CreateAsync(dto);

            if (!result.Success)
            {
                _loading.Hide();
                MessageBox.Show(result.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _editingId = result.Data?.Id ?? _editingId;
            RequestNumberTextBox.Text = result.Data?.RequestNumber ?? RequestNumberTextBox.Text;
            StatusText.Text = "Draft";
            _loading.Hide();
            MessageBox.Show(result.Message, "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _loading.Hide();
            MessageBox.Show($"Could not save the purchase request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _loading.Hide();
            _isSaving = false;
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void AddLine(PendingLine line)
    {
        line.PropertyChanged += PendingLine_PropertyChanged;
        _lines.Add(line);
        UpdateTotalAmount();
    }

    private void PendingLine_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PendingLine.UnitPrice) ||
            e.PropertyName == nameof(PendingLine.RequestedQuantity))
            UpdateTotalAmount();
    }

    private void UpdateTotalAmount()
    {
        if (TotalAmountText != null)
            TotalAmountText.Text = _lines.Sum(x => x.UnitPrice * x.RequestedQuantity).ToString("N3");
    }

    private static bool TryParseDecimal(string value, out decimal number)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out number) ||
               decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out number);
    }

    private sealed class PendingLine : INotifyPropertyChanged
    {
        private decimal _requestedQuantity;
        private decimal _unitPrice;

        public int? ProductId { get; set; }
        public int? ProductUnitId { get; set; }
        public string? Barcode { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? UnitName { get; set; }
        public decimal UnitPrice
        {
            get => _unitPrice;
            set { _unitPrice = value; PropertyChanged?.Invoke(this, new(nameof(UnitPrice))); }
        }
        public decimal RequestedQuantity
        {
            get => _requestedQuantity;
            set
            {
                _requestedQuantity = value;
                PropertyChanged?.Invoke(this, new(nameof(RequestedQuantity)));
                PropertyChanged?.Invoke(this, new(nameof(NeedBeyondStock)));
            }
        }
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public decimal AvailableStockSnapshot { get; set; }
        public decimal NeedBeyondStock => Math.Max(0m, RequestedQuantity - AvailableStockSnapshot);
        public string? Notes { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;

        public PurchaseRequestLineWriteDto ToDto() => new()
        {
            ProductId = ProductId,
            ProductUnitId = ProductUnitId,
            Barcode = Barcode,
            ItemName = ItemName,
            UnitName = UnitName,
            UnitPrice = UnitPrice,
            RequestedQuantity = RequestedQuantity,
            ApprovedQuantity = RequestedQuantity,
            SupplierId = SupplierId,
            SupplierName = SupplierName,
            AvailableStockSnapshot = AvailableStockSnapshot,
            Notes = Notes
        };

        public static PendingLine FromDto(PurchaseRequestLineWriteDto line) => new()
        {
            ProductId = line.ProductId,
            ProductUnitId = line.ProductUnitId,
            Barcode = line.Barcode,
            ItemName = line.ItemName,
            UnitName = line.UnitName,
            UnitPrice = line.UnitPrice,
            RequestedQuantity = line.RequestedQuantity,
            SupplierId = line.SupplierId,
            SupplierName = line.SupplierName,
            AvailableStockSnapshot = line.AvailableStockSnapshot,
            Notes = line.Notes
        };
    }
}

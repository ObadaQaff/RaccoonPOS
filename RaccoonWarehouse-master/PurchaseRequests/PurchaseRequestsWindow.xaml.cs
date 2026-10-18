using RaccoonWarehouse.Application.Service.PurchaseRequests;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.PurchaseRequests.DTOs;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.PurchaseOrders;
using RaccoonWarehouse.Helpers.Localization;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.PurchaseRequests;

public partial class PurchaseRequestsWindow : Window
{
    private readonly IPurchaseRequestService _service;
    private readonly ILoadingService _loading;
    private readonly IProductService _productService;
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly ObservableCollection<PurchaseRequestReadDto> _requests = new();
    private readonly ObservableCollection<PurchaseRequestLineDisplay> _selectedLines = new();

    public PurchaseRequestsWindow(IPurchaseRequestService service, ILoadingService loading, IProductService productService, IPurchaseOrderService purchaseOrderService)
    { _service = service; _loading = loading; _productService = productService; _purchaseOrderService = purchaseOrderService; InitializeComponent(); RequestsGrid.ItemsSource = _requests; LinesGrid.ItemsSource = _selectedLines; RequestDatePicker.SelectedDate = DateTime.Today; UiText.ApplyWindow(this); }
    private async void Window_Loaded(object sender, RoutedEventArgs e) { await LoadAsync(); }
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();
    private async Task LoadAsync()
    { _loading.Show(); try { var result = await _service.GetAllAsync(); if (!result.Success) { MessageBox.Show(result.Message); return; } _requests.Clear(); foreach (var request in result.Data ?? new()) _requests.Add(request); } finally { _loading.Hide(); } }
    private async void Create_Click(object sender, RoutedEventArgs e)
    {
        var typedProductName = ProductNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(typedProductName) || !decimal.TryParse(QuantityTextBox.Text, out var quantity) || quantity <= 0) { MessageBox.Show("Type an existing product name and enter a positive quantity."); return; }
        var result = await _service.CreateAsync(new PurchaseRequestWriteDto
        {
            RequestDate = RequestDatePicker.SelectedDate ?? DateTime.Today,
            Priority = (PriorityComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString(),
            Department = DepartmentTextBox.Text.Trim(),
            Justification = JustificationTextBox.Text.Trim(),
            Notes = string.IsNullOrWhiteSpace(SupplierTextBox.Text) ? null : $"Supplier: {SupplierTextBox.Text.Trim()}",
            Lines = new List<PurchaseRequestLineWriteDto>
            {
                new() { ItemName = typedProductName, RequestedQuantity = quantity, ApprovedQuantity = quantity, Description = JustificationTextBox.Text.Trim() }
            }
        });
        MessageBox.Show(result.Message);
        if (result.Success)
        {
            ProductNameTextBox.Clear();
            QuantityTextBox.Text = "1";
            SupplierTextBox.Clear();
            JustificationTextBox.Clear();
            await LoadAsync();
        }
    }
    private async void RequestsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RequestsGrid.SelectedItem is not PurchaseRequestReadDto request)
        {
            _selectedLines.Clear();
            SelectedStatusText.Text = "PR status | الحالة: No request selected";
            RequestNumberTextBox.Text = "Auto generated";
            return;
        }

        _selectedLines.Clear();
        foreach (var line in request.Lines ?? new List<PurchaseRequestLineWriteDto>())
        {
            ProductReadDto? product = null;
            if (line.ProductId.HasValue)
            {
                var productResult = await _productService.GetByIdWithUnitsAsync(line.ProductId.Value);
                product = productResult.Data;
            }
            _selectedLines.Add(new PurchaseRequestLineDisplay(line, product));
        }

        SelectedStatusText.Text = $"PR status | الحالة: {request.Status}";
        RequestNumberTextBox.Text = string.IsNullOrWhiteSpace(request.RequestNumber) ? "Auto generated" : request.RequestNumber;
        RequestDatePicker.SelectedDate = request.RequestDate == default ? DateTime.Today : request.RequestDate;
        DepartmentTextBox.Text = request.Department ?? string.Empty;
        JustificationTextBox.Text = request.Justification ?? string.Empty;
        PriorityComboBox.SelectedValue = request.Priority;
    }

    private sealed class PurchaseRequestLineDisplay
    {
        private readonly PurchaseRequestLineWriteDto _line;
        public int? ProductId => _line.ProductId;
        public string ProductName { get; }
        public string Description { get; }
        public decimal RequestedQuantity => _line.RequestedQuantity;
        public decimal AvailableStockSnapshot => _line.AvailableStockSnapshot;
        public decimal ApprovedQuantity => _line.ApprovedQuantity;
        public DateTime? ExpectedStockoutDate => _line.ExpectedStockoutDate;
        public string? Notes => _line.Notes;

        public PurchaseRequestLineDisplay(PurchaseRequestLineWriteDto line, ProductReadDto? product)
        {
            _line = line;
            ProductName = string.IsNullOrWhiteSpace(line.ItemName) ? product?.Name ?? string.Empty : line.ItemName;
            Description = line.Description ?? product?.Description ?? string.Empty;
        }
    }
    private async void Submit_Click(object sender, RoutedEventArgs e) => await ExecuteSelectedAsync(request => _service.SubmitAsync(request.Id));
    private async void Approve_Click(object sender, RoutedEventArgs e) => await ExecuteSelectedAsync(request => _service.ApproveAsync(request.Id));
    private async void CreatePo_Click(object sender, RoutedEventArgs e)
    {
        if (RequestsGrid.SelectedItem is not PurchaseRequestReadDto request)
        {
            MessageBox.Show("Select a purchase request first.");
            return;
        }
        if (request.Status != RaccoonWarehouse.Domain.Enums.PurchaseRequestStatus.Approved)
        {
            MessageBox.Show("Only approved purchase requests can generate a purchase order.");
            return;
        }
        var result = await _purchaseOrderService.CreateFromPurchaseRequestAsync(request.Id);
        MessageBox.Show(result.Message);
    }
    private async void Reject_Click(object sender, RoutedEventArgs e) => await ExecuteSelectedAsync(request => _service.RejectAsync(request.Id, "Rejected from purchasing screen."));
    private async Task ExecuteSelectedAsync(Func<PurchaseRequestReadDto, Task<RaccoonWarehouse.Core.Common.Result<PurchaseRequestWriteDto>>> action)
    { if (RequestsGrid.SelectedItem is not PurchaseRequestReadDto request) { MessageBox.Show("Select a purchase request first."); return; } var result = await action(request); MessageBox.Show(result.Message); if (result.Success) await LoadAsync(); }
}

using RaccoonWarehouse.Application.Service.GoodsReceipts;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.GoodsReceipts.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System.Collections.ObjectModel;
using System.Windows;

namespace RaccoonWarehouse.GoodsReceipts;

public partial class GoodsReceiptsWindow : Window
{
    private readonly IGoodsReceiptService _service;
    private readonly ILoadingService _loading;
    private readonly ObservableCollection<GoodsReceiptReadDto> _receipts = new();

    public GoodsReceiptsWindow(IGoodsReceiptService service, ILoadingService loading)
    { _service = service; _loading = loading; InitializeComponent(); ReceiptsGrid.ItemsSource = _receipts; UiText.ApplyWindow(this); }
    private async void Window_Loaded(object sender, RoutedEventArgs e) => await LoadAsync();
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();
    private async Task LoadAsync()
    { _loading.Show(); try { var result = await _service.GetAllAsync(); if (!result.Success) { MessageBox.Show(result.Message); return; } _receipts.Clear(); foreach (var receipt in result.Data ?? new()) _receipts.Add(receipt); } finally { _loading.Hide(); } }
    private async void Create_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(PurchaseOrderIdTextBox.Text, out var poId) || !int.TryParse(PurchaseOrderLineIdTextBox.Text, out var poLineId) || !int.TryParse(ProductIdTextBox.Text, out var productId) || !int.TryParse(ProductUnitIdTextBox.Text, out var unitId) || !decimal.TryParse(ReceivedTextBox.Text, out var received) || !decimal.TryParse(AcceptedTextBox.Text, out var accepted)) { MessageBox.Show("Enter valid IDs and quantities."); return; }
        var result = await _service.CreateAsync(new GoodsReceiptWriteDto { PurchaseOrderId = poId, Lines = new List<GoodsReceiptLineWriteDto> { new() { PurchaseOrderLineId = poLineId, ProductId = productId, ProductUnitId = unitId, QuantityReceived = received, QuantityAccepted = accepted, QuantityRejected = received - accepted } } });
        MessageBox.Show(result.Message); if (result.Success) await LoadAsync();
    }
    private async void Confirm_Click(object sender, RoutedEventArgs e)
    { if (ReceiptsGrid.SelectedItem is not GoodsReceiptReadDto receipt) { MessageBox.Show("Select a receipt first."); return; } var result = await _service.ConfirmAsync(receipt.Id); MessageBox.Show(result.Message); if (result.Success) await LoadAsync(); }
}

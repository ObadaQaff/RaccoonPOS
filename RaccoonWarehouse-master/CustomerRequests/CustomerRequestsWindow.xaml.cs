using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.CustomerRequests;
using RaccoonWarehouse.Domain.CustomerRequests;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.CustomerRequests;

public partial class CustomerRequestsWindow : Window
{
    private readonly ICustomerRequestService _service;
    private readonly IServiceProvider _provider;
    private List<CustomerRequestDto> _all = new();

    public CustomerRequestsWindow(ICustomerRequestService service, IServiceProvider provider)
    {
        _service = service;
        _provider = provider;
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var result = await _service.GetAllAsync();
        _all = result.Data ?? new();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (RequestsGrid == null) return;
        var text = SearchText?.Text?.Trim() ?? string.Empty;
        var tag = (StatusFilter?.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        int.TryParse(tag, out var status);
        var from = FromDate?.SelectedDate?.Date;
        var to = ToDate?.SelectedDate?.Date.AddDays(1);
        var filtered = _all.Where(x =>
            (!from.HasValue || x.CreatedDate >= from.Value) &&
            (!to.HasValue || x.CreatedDate < to.Value) &&
            (string.IsNullOrWhiteSpace(text) ||
             x.RequestNumber.Contains(text, StringComparison.CurrentCultureIgnoreCase) ||
             x.CustomerName.Contains(text, StringComparison.CurrentCultureIgnoreCase) ||
             (x.CustomerPhone?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false)) &&
            (status < 0 || (int)x.Status == status)).ToList();
        RequestsGrid.ItemsSource = filtered;
        TotalCountText.Text = filtered.Count.ToString();
        SubmittedCountText.Text = filtered.Count(x => x.Status == CustomerRequestStatus.Submitted).ToString();
        ApprovedCountText.Text = filtered.Count(x => x.Status == CustomerRequestStatus.Approved).ToString();
        CancelledCountText.Text = filtered.Count(x => x.Status == CustomerRequestStatus.Cancelled).ToString();
        PartialPaidCountText.Text = filtered.Count(x => x.PaymentStatus.Contains("جزئ", StringComparison.CurrentCultureIgnoreCase)).ToString();
        NotSoldCountText.Text = filtered.Count(x => x.SalesStatus == CustomerRequestSalesStatus.NotSold).ToString();
    }

    private CustomerRequestDto? Selected => RequestsGrid?.SelectedItem as CustomerRequestDto;
    private void SearchText_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilter();
    private void DateFilter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilter();

    private void SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var row = Selected;
        if (row == null)
        {
            SelectionSummary.Text = "اختر طلباً لعرض الإجراءات المتاحة";
            ApproveButton.Visibility = Visibility.Collapsed;
            CancelButton.Visibility = Visibility.Collapsed;
            ReceiveButton.Visibility = Visibility.Collapsed;
            SaleButton.Visibility = Visibility.Collapsed;
            DetailsButton.Visibility = Visibility.Collapsed;
            return;
        }

        SelectionSummary.Text = $"{row.RequestNumber}  •  {row.CustomerName}  •  {row.CustomerPhone}  •  {row.StatusText}  •  {row.SalesStatusText}";
        ApproveButton.Visibility = row.Status == CustomerRequestStatus.Submitted ? Visibility.Visible : Visibility.Collapsed;
        CancelButton.Visibility = row.Status != CustomerRequestStatus.Cancelled && row.SalesStatus != CustomerRequestSalesStatus.Sold ? Visibility.Visible : Visibility.Collapsed;
        ReceiveButton.Visibility = row.Status == CustomerRequestStatus.Approved && row.ReceivingStatus != CustomerRequestReceivingStatus.Received ? Visibility.Visible : Visibility.Collapsed;
        SaleButton.Visibility = row.Status == CustomerRequestStatus.Approved && row.SalesStatus != CustomerRequestSalesStatus.Sold && HasSellableLines(row) ? Visibility.Visible : Visibility.Collapsed;
        DetailsButton.Visibility = Visibility.Visible;
    }

    private static bool HasSellableLines(CustomerRequestDto row) => row.Lines.Any(x => x.ProductId.HasValue && x.ProductUnitId.HasValue && x.Quantity > x.SoldQuantity);

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        var window = _provider.GetRequiredService<CustomerRequestCreateWindow>();
        window.Owner = this;
        if (window.ShowDialog() == true) _ = LoadAsync();
    }

    private async void Approve_Click(object sender, RoutedEventArgs e)
    {
        if (Selected == null) return;
        var result = await _service.ApproveAsync(Selected.Id);
        if (!result.Success) MessageBox.Show(result.Message);
        await LoadAsync();
    }

    private async void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (Selected == null) return;
        var result = await _service.CancelAsync(Selected.Id);
        if (!result.Success) MessageBox.Show(result.Message);
        await LoadAsync();
    }

    private void Receive_Click(object sender, RoutedEventArgs e)
    {
        if (Selected == null) return;
        var window = _provider.GetRequiredService<CustomerRequestReceivingWindow>();
        window.Owner = this;
        window.Initialize(Selected);
        if (window.ShowDialog() == true) _ = LoadAsync();
    }

    private void Report_Click(object sender, RoutedEventArgs e)
    {
        var window = _provider.GetRequiredService<CustomerRequestsReportWindow>();
        window.Owner = this;
        window.ShowDialog();
    }

    private void Details_Click(object sender, RoutedEventArgs e)
    {
        if (Selected == null) return;
        var window = _provider.GetRequiredService<CustomerRequestDetailsWindow>();
        window.Owner = this;
        window.Initialize(Selected);
        window.ShowDialog();
    }

    private void Sale_Click(object sender, RoutedEventArgs e)
    {
        var row = Selected;
        if (row == null) return;
        var lines = row.Lines
            .Where(x => x.ProductId.HasValue && x.ProductUnitId.HasValue && x.Quantity > x.SoldQuantity)
            .Select(x => new CustomerRequestSaleLine(x.Id, x.ProductId!.Value, x.ProductUnitId!.Value, x.Quantity - x.SoldQuantity, x.UnitPrice))
            .ToList();
        if (lines.Count == 0)
        {
            MessageBox.Show("لا توجد كميات قابلة للبيع.");
            return;
        }
        CustomerRequestSaleContext.Set(new CustomerRequestSaleDraft(row.Id, row.CustomerId, lines));
        var pos = _provider.GetRequiredService<RaccoonWarehouse.Invoices.POS>();
        pos.Owner = this;
        pos.ShowDialog();
        _ = LoadAsync();
    }
}

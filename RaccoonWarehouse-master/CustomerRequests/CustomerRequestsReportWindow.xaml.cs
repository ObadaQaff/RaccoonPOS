using RaccoonWarehouse.Application.Service.CustomerRequests;
using RaccoonWarehouse.Domain.CustomerRequests;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.CustomerRequests;

public partial class CustomerRequestsReportWindow : Window
{
    private readonly ICustomerRequestService _service;
    private List<CustomerRequestDto> _all = new();
    public CustomerRequestsReportWindow(ICustomerRequestService service) { _service = service; InitializeComponent(); Loaded += async (_, _) => { var result = await _service.GetAllAsync(); _all = result.Data ?? new(); ApplyFilter(); }; }
    private void ApplyFilter() { var text = SearchText?.Text?.Trim() ?? ""; var tag = (StatusFilter?.SelectedItem as ComboBoxItem)?.Tag?.ToString(); int.TryParse(tag, out var status); ReportGrid.ItemsSource = _all.Where(x => (string.IsNullOrWhiteSpace(text) || x.RequestNumber.Contains(text, StringComparison.CurrentCultureIgnoreCase) || x.CustomerName.Contains(text, StringComparison.CurrentCultureIgnoreCase) || (x.CustomerPhone?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false)) && (status < 0 || (int)x.Status == status)).ToList(); }
    private void SearchText_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilter();
    private void Print_Click(object sender, RoutedEventArgs e) { var dialog = new PrintDialog(); if (dialog.ShowDialog() == true) dialog.PrintVisual(ReportGrid, Title); }
}

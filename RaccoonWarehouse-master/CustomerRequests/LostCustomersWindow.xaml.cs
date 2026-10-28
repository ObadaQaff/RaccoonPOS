using RaccoonWarehouse.Application.Service.LostCustomers;
using RaccoonWarehouse.Domain.LostCustomers;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RaccoonWarehouse.LostCustomers;

public partial class LostCustomersWindow : Window
{
    private readonly ILostCustomerService _service; private readonly IServiceProvider _provider; private readonly ObservableCollection<LostCustomerRow> _rows = new();
    public LostCustomersWindow(ILostCustomerService service, IServiceProvider provider) { _service = service; _provider = provider; InitializeComponent(); Grid.ItemsSource = _rows; Loaded += async (_, _) => await LoadAsync(); }
    private async Task LoadAsync() { _rows.Clear(); var result = await _service.GetAllAsync(); if (result.Data != null) foreach (var x in result.Data) _rows.Add(new(x)); ApplyFilter(); }
    private void ApplyFilter()
    {
        var text = SearchBox.Text.Trim(); var from = FromDate.SelectedDate?.Date; var to = ToDate.SelectedDate?.Date.AddDays(1);
        var filtered = _rows.Where(x => (!from.HasValue || x.CreatedDate >= from.Value) && (!to.HasValue || x.CreatedDate < to.Value) && (string.IsNullOrWhiteSpace(text) || x.Search.Contains(text, StringComparison.OrdinalIgnoreCase))).ToList();
        Grid.ItemsSource = filtered;
        TotalCountText.Text = filtered.Count.ToString();
        MissingProductCountText.Text = filtered.Count(x => x.Dto.Type == LostCustomerType.MissingProduct).ToString();
        OtherReasonCountText.Text = filtered.Count(x => x.Dto.Type == LostCustomerType.GeneralReason).ToString();
        TotalQuantityText.Text = filtered.Sum(x => x.Quantity ?? 0).ToString("0.###");
        TodayCountText.Text = filtered.Count(x => x.CreatedDate.Date == DateTime.Today).ToString();
    }
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
    private void DateFilter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilter();
    private LostCustomerRow? Selected => Grid.SelectedItem as LostCustomerRow;
    private void Add_Click(object sender, RoutedEventArgs e) { var w = (LostCustomerCreateWindow)_provider.GetService(typeof(LostCustomerCreateWindow))!; w.Owner = this; if (w.ShowDialog() == true) _ = LoadAsync(); }
    private void Edit_Click(object sender, RoutedEventArgs e) { if (Selected == null) return; var w = (LostCustomerCreateWindow)_provider.GetService(typeof(LostCustomerCreateWindow))!; w.Initialize(Selected.Dto); w.Owner = this; if (w.ShowDialog() == true) _ = LoadAsync(); }
    private async void Delete_Click(object sender, RoutedEventArgs e) { if (Selected == null || MessageBox.Show("هل تريد حذف السجل؟", "تأكيد", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return; var result = await _service.DeleteAsync(Selected.Dto.Id); if (!result.Success) MessageBox.Show(result.Message ?? "تعذر الحذف."); else await LoadAsync(); }
    private void Grid_MouseDoubleClick(object sender, MouseButtonEventArgs e) => Edit_Click(sender, e);
    private void Report_Click(object sender, RoutedEventArgs e) { var w = (LostCustomersReportWindow)_provider.GetService(typeof(LostCustomersReportWindow))!; w.Owner = this; w.ShowDialog(); }
    private sealed class LostCustomerRow { public LostCustomerDto Dto { get; } public string TypeLabel => Dto.Type == LostCustomerType.MissingProduct ? "منتج غير متوفر" : "سبب آخر"; public string ReasonLabel => Dto.Reason == LostCustomerReason.OutOfStock ? "غير متوفر بالمخزون" : Dto.Reason == LostCustomerReason.ProductNotRegistered ? "غير مسجل بالنظام" : "سبب آخر"; public string ProductName => Dto.ProductName; public decimal? Quantity => Dto.Quantity; public string? Notes => Dto.Message ?? Dto.Notes; public string CreatedByName => Dto.CreatedByName; public DateTime CreatedDate => Dto.CreatedDate; public string Search => $"{TypeLabel} {ReasonLabel} {ProductName} {Dto.Message} {Dto.Notes} {CreatedByName}"; public LostCustomerRow(LostCustomerDto dto) => Dto = dto; }
}

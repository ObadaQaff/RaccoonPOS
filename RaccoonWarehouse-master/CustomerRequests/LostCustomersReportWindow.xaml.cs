using RaccoonWarehouse.Application.Service.LostCustomers;
using RaccoonWarehouse.Domain.LostCustomers;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.LostCustomers;

public partial class LostCustomersReportWindow : Window
{
    private readonly ILostCustomerService _service; private List<LostCustomerDto> _all = new();
    public LostCustomersReportWindow(ILostCustomerService service) { _service = service; InitializeComponent(); TypeFilter.SelectedIndex = 0; ReasonFilter.SelectedIndex = 0; Loaded += async (_, _) => { var result = await _service.GetAllAsync(); _all = result.Data ?? new(); Apply(); }; }
    private void Apply_Click(object sender, RoutedEventArgs e) => Apply();
    private void Apply()
    { var from = FromDate.SelectedDate?.Date; var to = ToDate.SelectedDate?.Date.AddDays(1).AddTicks(-1); var type = (TypeFilter.SelectedItem as ComboBoxItem)?.Tag?.ToString(); var reason = (ReasonFilter.SelectedItem as ComboBoxItem)?.Tag?.ToString(); var search = SearchBox.Text.Trim(); var rows = _all.Where(x => (!from.HasValue || x.CreatedDate >= from) && (!to.HasValue || x.CreatedDate <= to) && (type == "-1" || string.IsNullOrEmpty(type) || (int)x.Type == int.Parse(type)) && (reason == "-1" || string.IsNullOrEmpty(reason) || (int)x.Reason == int.Parse(reason)) && (string.IsNullOrWhiteSpace(search) || $"{x.ProductName} {x.Message} {x.Notes} {x.CreatedByName}".Contains(search, StringComparison.OrdinalIgnoreCase))).ToList(); Grid.ItemsSource = rows.Select(x => new ReportRow(x)).ToList(); var missing = rows.Where(x => x.Type == LostCustomerType.MissingProduct).ToList(); var most = missing.GroupBy(x => x.ProductName).OrderByDescending(g => g.Sum(x => x.Quantity ?? 0)).FirstOrDefault(); var reasonBreakdown = string.Join("، ", rows.GroupBy(x => x.Reason).Select(g => $"{ReasonLabel(g.Key)}: {g.Count()}")); SummaryText.Text = $"إجمالي السجلات: {rows.Count}   |   المنتجات غير المتوفرة: {missing.Count}   |   إجمالي الكميات: {missing.Sum(x => x.Quantity ?? 0):0.###}   |   الأكثر طلباً: {most?.Key ?? "—"}\nالتوزيع حسب السبب: {reasonBreakdown}"; }
    private static string ReasonLabel(LostCustomerReason reason) => reason == LostCustomerReason.OutOfStock ? "غير متوفر بالمخزون" : reason == LostCustomerReason.ProductNotRegistered ? "غير مسجل بالنظام" : "سبب آخر";
    private void Print_Click(object sender, RoutedEventArgs e) { var dialog = new PrintDialog(); if (dialog.ShowDialog() == true) dialog.PrintVisual(Grid, Title); }
    private sealed class ReportRow { private readonly LostCustomerDto D; public string TypeLabel => D.Type == LostCustomerType.MissingProduct ? "منتج غير متوفر" : "سبب آخر"; public string ReasonLabel => D.Reason == LostCustomerReason.OutOfStock ? "غير متوفر بالمخزون" : D.Reason == LostCustomerReason.ProductNotRegistered ? "غير مسجل بالنظام" : "سبب آخر"; public string ProductName => D.ProductName; public decimal? Quantity => D.Quantity; public string? Message => D.Message; public string? Notes => D.Notes; public string CreatedByName => D.CreatedByName; public DateTime CreatedDate => D.CreatedDate; public ReportRow(LostCustomerDto d) => D = d; }
}

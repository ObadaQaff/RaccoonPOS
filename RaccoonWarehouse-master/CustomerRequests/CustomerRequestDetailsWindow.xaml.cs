using RaccoonWarehouse.Domain.CustomerRequests;
using System.Windows;

namespace RaccoonWarehouse.CustomerRequests;

public partial class CustomerRequestDetailsWindow : Window
{
    public CustomerRequestDetailsWindow() { InitializeComponent(); }

    public void Initialize(CustomerRequestDto request)
    {
        SummaryText.Text = $"{request.RequestNumber}  •  {request.StatusText}  •  {request.ReceivingStatusText}  •  {request.SalesStatusText}";
        CustomerText.Text = request.CustomerName;
        PhoneText.Text = string.IsNullOrWhiteSpace(request.CustomerPhone) ? "—" : request.CustomerPhone;
        PaymentText.Text = $"{request.PaymentStatus} - {request.PaidAmount:N3} / {request.TotalAmount:N3}";
        NotesText.Text = string.IsNullOrWhiteSpace(request.Notes) ? "—" : request.Notes;
        TotalText.Text = $"الإجمالي: {request.TotalAmount:N3}";
        LinesGrid.ItemsSource = request.Lines.Select(x => new DetailLine { ItemName = x.ItemName, UnitName = x.UnitName, Quantity = x.Quantity, UnitPrice = x.UnitPrice }).ToList();
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Controls.PrintDialog();
        if (dialog.ShowDialog() == true) dialog.PrintVisual(PrintContent(), Title);
    }

    private UIElement PrintContent() => (UIElement)Content;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    public sealed class DetailLine
    {
        public string ItemName { get; init; } = string.Empty;
        public string? UnitName { get; init; }
        public decimal Quantity { get; init; }
        public decimal UnitPrice { get; init; }
        public decimal LineTotal => Quantity * UnitPrice;
    }
}

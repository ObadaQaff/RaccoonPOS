using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using RaccoonWarehouse.Application.Service.Purchases;
using RaccoonWarehouse.Domain.Cashiers.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Helpers.Localization;

namespace RaccoonWarehouse.Invoices;

public sealed class PurchaseBonusReturnWindow : Window
{
    private readonly DataGrid _grid;
    private readonly Button _save;
    private readonly ComboBox _payment;
    private readonly PurchaseBonusReturnService _service;
    private readonly int _invoiceId;
    private readonly CashierSessionReadDto _session;
    private List<PurchaseBonusReturnRow> _rows = new();
    private bool _busy;
    public PurchaseBonusReturnWindow(PurchaseBonusReturnService service, int invoiceId, CashierSessionReadDto session, bool allowSupplierCredit = false)
    {
        
        (_service, _invoiceId, _session) = (service, invoiceId, session);
        Title = UiText.T("مرتجع مشتريات البونص", "Bonus purchase return");
        Width = 1000; Height = 620; MinWidth = 760; MinHeight = 450;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FlowDirection = UiText.IsEnglish ? FlowDirection.LeftToRight : FlowDirection.RightToLeft;
        Background = new SolidColorBrush(Color.FromRgb(237, 243, 246));
        var root = new DockPanel { Margin = new Thickness(22) };
        var header = new Border { Background = new SolidColorBrush(Color.FromRgb(20, 85, 77)), CornerRadius = new CornerRadius(16), Padding = new Thickness(20), Margin = new Thickness(0, 0, 0, 16) };
        header.Child = new TextBlock { Text = Title, FontSize = 26, FontWeight = FontWeights.Bold, Foreground = Brushes.White };
        DockPanel.SetDock(header, Dock.Top); root.Children.Add(header);
        var hint = new TextBlock { Text = UiText.T("أدخل المدفوع والمجاني المراد إرجاعهما. المجاني لا يولّد رصيداً للمورد.", "Enter paid and free units to return. Free units generate no supplier credit."), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 14) };
        DockPanel.SetDock(hint, Dock.Top); root.Children.Add(hint);
        var footer = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 14, 0, 0) };
        footer.Children.Add(new TextBlock { Text = UiText.T("طريقة الاسترداد", "Refund method"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8) });
        _payment = new ComboBox { Width = 190, Margin = new Thickness(8), DisplayMemberPath = "Name", SelectedValuePath = "Value" };
        var methods = new[] {
            new {Value=PaymentType.Cash,Name=UiText.T("نقد", "Cash")}, new {Value=PaymentType.Visa,Name=UiText.T("فيزا", "Visa")},
            new {Value=PaymentType.Master,Name=UiText.T("ماستر", "Mastercard")}, new {Value=PaymentType.Debit,Name=UiText.T("تحويل بنكي", "Bank transfer")},
            new {Value=PaymentType.MobilePayment,Name=UiText.T("دفع إلكتروني", "Mobile payment")}, new {Value=PaymentType.Credit,Name=UiText.T("رصيد المورد", "Supplier credit")}};
        _payment.ItemsSource = methods.Where(method => allowSupplierCredit || method.Value != PaymentType.Credit);
        _payment.SelectedIndex = 0; footer.Children.Add(_payment);
        _save = new Button { Content = UiText.T("حفظ المرتجع", "Save return"), Padding = new Thickness(18, 10, 18, 10), Margin = new Thickness(8), Background = new SolidColorBrush(Color.FromRgb(20, 85, 77)), Foreground = Brushes.White, IsEnabled = false };
        _save.Click += Save_Click; footer.Children.Add(_save);
        var close = new Button { Content = UiText.T("إغلاق", "Close"), Padding = new Thickness(18, 10, 18, 10), Margin = new Thickness(8) };
        close.Click += (_, _) => { if (!_busy) Close(); }; footer.Children.Add(close);
        DockPanel.SetDock(footer, Dock.Bottom); root.Children.Add(footer);
        _grid = new DataGrid { AutoGenerateColumns = false, CanUserAddRows = false, Background = Brushes.White, IsEnabled = false };
        AddColumn(UiText.T("المنتج", "Product"), "ProductName", true, 2);
        AddColumn(UiText.T("المدفوع المتبقي", "Paid remaining"), "PaidAvailable", true);
        AddColumn(UiText.T("المجاني المتبقي", "Free remaining"), "FreeAvailable", true);
        AddColumn(UiText.T("إرجاع مدفوع", "Return paid"), "PaidToReturn", false);
        AddColumn(UiText.T("إرجاع مجاني", "Return free"), "FreeToReturn", false);
        root.Children.Add(_grid); Content = root;
        Loaded += async (_, _) =>
        {
            try { _rows = await _service.GetRowsAsync(_invoiceId); _grid.ItemsSource = _rows; _grid.IsEnabled = true; _save.IsEnabled = true; }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error); }
        };
        Closing += (_, e) => { if (_busy) e.Cancel = true; };
    }
    private void AddColumn(string name, string path, bool readOnly, int stars = 1)
        => _grid.Columns.Add(new DataGridTextColumn { Header = name, Binding = new Binding(path) { Mode = readOnly ? BindingMode.OneWay : BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.LostFocus, ValidatesOnExceptions = true }, IsReadOnly = readOnly, Width = new DataGridLength(stars, DataGridLengthUnitType.Star) });
    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_busy || !_grid.CommitEdit(DataGridEditingUnit.Cell, true) || !_grid.CommitEdit(DataGridEditingUnit.Row, true)) return;
        _busy = true; _save.IsEnabled = false; _grid.IsEnabled = false;
        try
        {
            var result = await _service.CompleteAsync(_invoiceId, _rows, (PaymentType)_payment.SelectedValue, _session);
            if (!result.Success) { MessageBox.Show(this, result.Message, Title, MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            _busy = false;
            MessageBox.Show(this, UiText.T("تم حفظ المرتجع", "Return saved"), Title);
            DialogResult = true;
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error); }
        finally { _busy = false; _save.IsEnabled = true; _grid.IsEnabled = true; }
    }
}

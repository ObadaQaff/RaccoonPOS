using RaccoonWarehouse.Application.Service.CustomerRequests;
using RaccoonWarehouse.Domain.CustomerRequests;
using System.Windows;

namespace RaccoonWarehouse.CustomerRequests;

public partial class CustomerRequestReceivingWindow : Window
{
    private readonly ICustomerRequestService _service;
    private CustomerRequestDto? _request;
    public List<ReceivingLine> Lines { get; } = new();

    public CustomerRequestReceivingWindow(ICustomerRequestService service)
    {
        _service = service;
        InitializeComponent();
        DataContext = this;
    }

    public void Initialize(CustomerRequestDto request)
    {
        _request = request;
        TitleText.Text = $"استلام {request.RequestNumber}";
        Lines.Clear();
        Lines.AddRange(request.Lines.Select(x => new ReceivingLine(x.Id, x.ItemName, x.UnitName, x.Quantity, x.ReceivedQuantity)));
        StatusCombo.SelectedIndex = (int)request.ReceivingStatus;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_request == null) return;
        if (StatusCombo.SelectedItem is not System.Windows.Controls.ComboBoxItem item || !int.TryParse(item.Tag?.ToString(), out var status)) return;
        var quantities = Lines.ToDictionary(x => x.Id, x => x.ReceivedQuantity);
        var result = await _service.UpdateReceivingAsync(_request.Id, (CustomerRequestReceivingStatus)status, quantities);
        MessageBox.Show(result.Success ? "تم تحديث حالة الاستلام." : result.Message);
        if (result.Success) DialogResult = true;
    }

    public sealed class ReceivingLine
    {
        public int Id { get; }
        public string ItemName { get; }
        public string? UnitName { get; }
        public decimal Quantity { get; }
        public decimal ReceivedQuantity { get; set; }
        public ReceivingLine(int id, string itemName, string? unitName, decimal quantity, decimal receivedQuantity) => (Id, ItemName, UnitName, Quantity, ReceivedQuantity) = (id, itemName, unitName, quantity, receivedQuantity);
    }
}

using RaccoonWarehouse.Application.Service.CustomerRequests;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.CustomerRequests;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Invoices;
using RaccoonWarehouse.Navigation;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
namespace RaccoonWarehouse.CustomerRequests;
public partial class CustomerRequestCreateWindow : Window
{
 public sealed class Line : System.ComponentModel.INotifyPropertyChanged { private decimal _quantity = 1; private decimal _unitPrice; public int? ProductId { get; init; } public int? ProductUnitId { get; init; } public string ItemName { get; set; } = ""; public string? UnitName { get; init; } public decimal Quantity { get => _quantity; set { if (_quantity == value) return; _quantity = value; OnChanged(nameof(Quantity)); OnChanged(nameof(LineTotal)); } } public decimal UnitPrice { get => _unitPrice; set { if (_unitPrice == value) return; _unitPrice = value; OnChanged(nameof(UnitPrice)); OnChanged(nameof(LineTotal)); } } public decimal LineTotal => Quantity * UnitPrice; public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged; private void OnChanged(string name) => PropertyChanged?.Invoke(this, new(name)); }
 private readonly ICustomerRequestService _service; private readonly IProductService _products; private readonly IStockService _stock; private readonly IUserService _users; private List<UserReadDto> _allCustomers = new(); public ObservableCollection<Line> Lines { get; } = new();
 public CustomerRequestCreateWindow(ICustomerRequestService service, IProductService products, IStockService stock, IUserService users) { _service = service; _products = products; _stock = stock; _users = users; InitializeComponent(); DataContext = this; Loaded += async (_, _) => await LoadCustomersAsync(); Lines.CollectionChanged += Lines_CollectionChanged; }
 private void Lines_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) { if (e.OldItems != null) foreach (Line line in e.OldItems) line.PropertyChanged -= Line_PropertyChanged; if (e.NewItems != null) foreach (Line line in e.NewItems) line.PropertyChanged += Line_PropertyChanged; RefreshTotal(); }
 private void Line_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) { if (e.PropertyName == nameof(Line.LineTotal) || e.PropertyName == nameof(Line.Quantity) || e.PropertyName == nameof(Line.UnitPrice)) RefreshTotal(); }
 private async Task LoadCustomersAsync() { _allCustomers = (await _users.GetAllAsync()).Data?.Where(x => x.Role == Domain.Enums.UserRole.Customer).OrderBy(x => x.Name).ToList() ?? new(); CustomerCombo.ItemsSource = _allCustomers; }
 private void CustomerCombo_TextChanged(object sender, TextChangedEventArgs e) { var text = CustomerCombo.Text ?? ""; CustomerCombo.ItemsSource = _allCustomers.Where(x => x.Name.Contains(text, StringComparison.CurrentCultureIgnoreCase)).ToList(); CustomerCombo.IsDropDownOpen = true; }
 private void CustomerCombo_KeyUp(object sender, System.Windows.Input.KeyEventArgs e) { if (e.Key == System.Windows.Input.Key.Enter && CustomerCombo.Items.Count > 0) CustomerCombo.SelectedIndex = 0; }
 private void AddCustomer_Click(object sender, RoutedEventArgs e) { int? id = null; WindowManager.ShowDialog<CreateUser>(WindowSizeType.MediumRectangle, w => { w.InitializeForCustomerQuickCreate(CustomerCombo.Text, null); w.Closed += (_, _) => id = w.CreatedUserId; }); if (id.HasValue) _ = LoadCustomersAndSelectAsync(id.Value); }
 private async Task LoadCustomersAndSelectAsync(int id) { await LoadCustomersAsync(); CustomerCombo.SelectedValue = id; }
 private async void AddProduct_Click(object sender, RoutedEventArgs e) { var w = new PurchaseProductSearchWindow(_products, _stock, async row => { var unit = row.SelectedUnit; if (unit == null) return false; Lines.Add(new Line { ProductId = row.Product.Id, ProductUnitId = unit.Id, ItemName = row.Product.Name ?? "", UnitName = unit.Unit?.Name, Quantity = row.Quantity, UnitPrice = row.SalePrice }); return true; }); w.Owner = this; w.ShowDialog(); }
 private void AddManual_Click(object sender, RoutedEventArgs e) { Lines.Add(new Line { ItemName = "مادة يدوية", Quantity = 1, UnitPrice = 0 }); }
 private void DeleteLine_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.DataContext is Line line) Lines.Remove(line); }
 private void RefreshTotal() { TotalText.Text = $"الإجمالي: {Lines.Sum(x => x.LineTotal):N3}"; }
 private async void Save_Click(object sender, RoutedEventArgs e) { if (CustomerCombo.SelectedItem is not UserReadDto customer) { MessageBox.Show("يرجى اختيار العميل."); return; } if (!decimal.TryParse(PaidText.Text, out var paid)) paid = 0; var result = await _service.CreateAsync(new CustomerRequestWriteDto { CustomerId = customer.Id, CustomerName = customer.Name, CustomerPhone = customer.PhoneNumber, PaidAmount = paid, PaymentNote = PaymentNoteText.Text, Lines = Lines.Select(x => new CustomerRequestLineDto { ProductId = x.ProductId, ProductUnitId = x.ProductUnitId, ItemName = x.ItemName, UnitName = x.UnitName, Quantity = x.Quantity, UnitPrice = x.UnitPrice }).ToList() }); MessageBox.Show(result.Success ? "تم حفظ طلب العميل." : result.Message ?? "تعذر حفظ الطلب."); if (result.Success) DialogResult = true; }
}

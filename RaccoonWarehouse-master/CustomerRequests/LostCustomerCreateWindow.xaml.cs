using RaccoonWarehouse.Application.Service.LostCustomers;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Domain.LostCustomers;
using RaccoonWarehouse.Invoices;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.LostCustomers;

public partial class LostCustomerCreateWindow : Window
{
    private readonly ILostCustomerService _service;
    private readonly IProductService _productService;
    private readonly ObservableCollection<PendingItem> _items = new();
    private int? _editingId;
    private int? _productId;
    private int? _productUnitId;

    public LostCustomerCreateWindow(ILostCustomerService service, IProductService productService)
    {
        _service = service; _productService = productService;
        InitializeComponent(); ItemsGrid.ItemsSource = _items;
        TypeCombo.SelectedIndex = 0; ReasonCombo.SelectedIndex = 0;
    }

    public void Initialize(LostCustomerDto dto)
    {
        _editingId = dto.Id; TypeCombo.SelectedIndex = (int)dto.Type;
        ReasonCombo.SelectedIndex = dto.Reason == LostCustomerReason.ProductNotRegistered ? 1 : 0;
        MessageTextBox.Text = dto.Message ?? string.Empty; NotesBox.Text = dto.Notes ?? string.Empty;
        if (dto.Type == LostCustomerType.MissingProduct)
            _items.Add(new PendingItem(dto.Id, dto.ProductId, dto.ProductUnitId, dto.ProductName, dto.Quantity ?? 1m));
        RefreshItems();
    }

    private void TypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var general = TypeCombo.SelectedIndex == 1;
        ProductPanel.Visibility = general ? Visibility.Collapsed : Visibility.Visible;
        ItemsGrid.Visibility = general ? Visibility.Collapsed : Visibility.Visible;
        GeneralPanel.Visibility = general ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SearchProduct_Click(object sender, RoutedEventArgs e)
    {
        var window = new PurchaseProductSearchWindow(_productService, async row =>
        { _productId = row.Product.Id; _productUnitId = row.ProductUnitId; ProductNameBox.Text = row.ProductName; return true; }) { Owner = this };
        window.ShowDialog();
    }

    private void AddItem_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ProductNameBox.Text) || !decimal.TryParse(QuantityBox.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var quantity) || quantity <= 0)
        { MessageBox.Show("أدخل اسم المنتج وكمية صحيحة.", "تنبيه"); return; }
        _items.Add(new PendingItem(null, _productId, _productUnitId, ProductNameBox.Text.Trim(), quantity));
        ProductNameBox.Clear(); QuantityBox.Text = "1"; _productId = null; _productUnitId = null; RefreshItems();
    }

    private void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is PendingItem item && item.ExistingId == null) { _items.Remove(item); RefreshItems(); }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        var type = TypeCombo.SelectedIndex == 1 ? LostCustomerType.GeneralReason : LostCustomerType.MissingProduct;
        var reason = type == LostCustomerType.GeneralReason ? LostCustomerReason.Other : (ReasonCombo.SelectedIndex == 1 ? LostCustomerReason.ProductNotRegistered : LostCustomerReason.OutOfStock);
        if (type == LostCustomerType.MissingProduct && _items.Count == 0) { MessageBox.Show("أضف منتجاً واحداً على الأقل.", "تنبيه"); return; }
        if (type == LostCustomerType.GeneralReason && string.IsNullOrWhiteSpace(MessageTextBox.Text)) { MessageBox.Show("أدخل سبب فقدان العميل.", "تنبيه"); return; }
        if (type == LostCustomerType.GeneralReason)
        { if (!await SaveItemAsync(null, null, null, string.Empty, null, reason)) return; }
        else foreach (var item in _items.ToList()) if (!await SaveItemAsync(item.ExistingId, item.ProductId, item.ProductUnitId, item.ProductName, item.Quantity, reason)) return;
        DialogResult = true; Close();
    }

    private async Task<bool> SaveItemAsync(int? id, int? productId, int? productUnitId, string productName, decimal? quantity, LostCustomerReason reason)
    {
        var dto = new LostCustomerWriteDto { Type = TypeCombo.SelectedIndex == 1 ? LostCustomerType.GeneralReason : LostCustomerType.MissingProduct, Reason = reason, ProductId = productId, ProductUnitId = productUnitId, ProductName = productName, Quantity = quantity, Message = MessageTextBox.Text, Notes = NotesBox.Text };
        var result = id.HasValue ? await _service.UpdateAsync(id.Value, dto) : await _service.CreateAsync(dto);
        if (result.Success) return true; MessageBox.Show(result.Message ?? "تعذر حفظ السجل.", "خطأ"); return false;
    }

    private void RefreshItems() => ItemsGrid.Items.Refresh();
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private sealed class PendingItem
    {
        public int? ExistingId { get; } public int? ProductId { get; } public int? ProductUnitId { get; } public string ProductName { get; } public decimal Quantity { get; set; }
        public PendingItem(int? existingId, int? productId, int? productUnitId, string productName, decimal quantity) { ExistingId = existingId; ProductId = productId; ProductUnitId = productUnitId; ProductName = productName; Quantity = quantity; }
    }
}

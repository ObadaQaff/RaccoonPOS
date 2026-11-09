using RaccoonWarehouse.Application.Service.PurchaseOrders;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.PurchaseOrders.DTOs;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Helpers.Localization;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace RaccoonWarehouse.PurchaseOrders;

public partial class PurchaseOrdersWindow : Window
{
    private readonly IPurchaseOrderService _service;
    private readonly ILoadingService _loadingService;
    private readonly ObservableCollection<PurchaseOrderReadDto> _orders = new();
    private readonly ICollectionView _ordersView;
    private List<string> _supplierNames = new();
    private bool _filteringSupplierChoices;
    private bool _loaded;
    private bool _loadingOrders;
    private bool _orderOperationActive;
    private bool _canApprove;
    private bool _canReject;
    private bool _canMarkPaid;

    public PurchaseOrdersWindow(
        IPurchaseOrderService service,
        ILoadingService loadingService)
    {
        _service = service;
        _loadingService = loadingService;
        
        InitializeComponent();
        OrdersGrid.ItemsSource = _orders;
        _ordersView = CollectionViewSource.GetDefaultView(_orders);
        _ordersView.Filter = IsOrderVisible;
        SupplierFilter.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(SupplierFilter_TextChanged), true);
        UiText.ApplyWindow(this);
        UpdateStatusCounters();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadAsync();
        _loaded = true;
    }

    private async void Window_Activated(object? sender, EventArgs e)
    {
        if (_loaded && !_loadingOrders && !_orderOperationActive)
            await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_loadingOrders)
            return;

        _loadingOrders = true;
        _loadingService.Show();
        try
        {
            
            var result = await _service.GetAllAsync();
            if (!result.Success)
            {
                MessageBox.Show(result.Message, UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _orders.Clear();
            foreach (var order in result.Data ?? new List<PurchaseOrderReadDto>())
                _orders.Add(order);

            _canApprove = true;
            _canReject = true;
            _canMarkPaid = true;

            ReloadSupplierSuggestions();
            _ordersView?.Refresh();
            UpdateStatusCounters();
            UpdateSelectedOrder();
            UpdateActionButtons();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiText.T("تعذر تحميل القائمة.", "Could not load the list.")
                + Environment.NewLine + ex.Message, UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _loadingService.Hide();
            _loadingOrders = false;
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        if (_orderOperationActive)
            return;

        await LoadAsync();
    }

    private void DateFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        _ordersView?.Refresh();
        UpdateStatusCounters();
    }

    private void ClearDateFilter_Click(object sender, RoutedEventArgs e)
    {
        FromDatePicker.SelectedDate = null;
        ToDatePicker.SelectedDate = null;
        SupplierFilter.SelectedItem = null;
        SupplierFilter.Text = string.Empty;
        ProductNameFilter.Clear();
        BarcodeFilter.Clear();
        ReloadSupplierSuggestions();
        _ordersView?.Refresh();
        UpdateStatusCounters();
    }

    private void RefreshSearchFilters()
    {
        if (_ordersView == null) return;
        _ordersView.Refresh();
        UpdateStatusCounters();
    }

    private void SearchFilter_Changed(object sender, TextChangedEventArgs e) => RefreshSearchFilters();

    private static bool MatchesProductName(string? name, string search)
    {
        var terms = search.Split(new[] { ' ', '\t', '\r', '\n', '%' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var start = 0;
        foreach (var term in terms)
        {
            var index = (name ?? string.Empty).IndexOf(term, start, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return false;
            start = index + term.Length;
        }
        return true;
    }

    private bool MatchesSupplier(string? name)
    {
        var search = SupplierFilter?.Text.Trim() ?? string.Empty;
        if (search.Length == 0) return true;
        if (SupplierFilter?.SelectedItem is string selected && string.Equals(selected, search, StringComparison.OrdinalIgnoreCase))
            return string.Equals(name, selected, StringComparison.OrdinalIgnoreCase);
        return (name ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private void ReloadSupplierSuggestions()
    {
        _supplierNames = _orders.Select(order => order.SupplierName)
            .Where(name => !string.IsNullOrWhiteSpace(name)).Select(name => name!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(name => name).ToList();
        var text = SupplierFilter.Text;
        _filteringSupplierChoices = true;
        try
        {
            SupplierFilter.ItemsSource = _supplierNames;
            SupplierFilter.SelectedItem = _supplierNames.FirstOrDefault(name => string.Equals(name, text, StringComparison.OrdinalIgnoreCase));
            SupplierFilter.Text = text;
            SupplierFilter.IsDropDownOpen = false;
        }
        finally { _filteringSupplierChoices = false; }
    }

    private void SupplierFilter_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_filteringSupplierChoices) return;
        var text = SupplierFilter.Text;
        if (SupplierFilter.IsKeyboardFocusWithin &&
            !(SupplierFilter.SelectedItem is string selected && string.Equals(selected, text, StringComparison.Ordinal)))
        {
            _filteringSupplierChoices = true;
            try
            {
                var editor = SupplierFilter.Template.FindName("PART_EditableTextBox", SupplierFilter) as TextBox;
                var caret = editor?.CaretIndex ?? text.Length;
                var names = _supplierNames.Where(name => name.Contains(text, StringComparison.OrdinalIgnoreCase)).Take(50).ToList();
                SupplierFilter.SelectedItem = null;
                SupplierFilter.ItemsSource = names;
                SupplierFilter.Text = text;
                SupplierFilter.IsDropDownOpen = text.Length > 0 && names.Count > 0;
                if (editor != null) editor.CaretIndex = Math.Min(caret, text.Length);
            }
            finally { _filteringSupplierChoices = false; }
        }
        RefreshSearchFilters();
    }

    private void UpdateStatusCounters()
    {
        if (SubmittedHeadingText == null || ApprovedHeadingText == null || RejectedHeadingText == null || PaidHeadingText == null ||
            SubmittedAmountText == null || ApprovedAmountText == null || RejectedAmountText == null || PaidAmountText == null)
            return;

        var visibleOrders = _orders.Where(IsOrderVisible).ToList();
        var submittedOrders = visibleOrders.Where(x => x.Status == PurchaseOrderStatus.Submitted).ToList();
        var approvedOrders = visibleOrders.Where(x => x.Status == PurchaseOrderStatus.Approved).ToList();
        var rejectedOrders = visibleOrders.Where(x => x.Status == PurchaseOrderStatus.Rejected).ToList();
        var paidOrders = visibleOrders.Where(x => x.PaymentStatus == PurchaseOrderPaymentStatus.Paid).ToList();

        SubmittedHeadingText.Text = $"Submitted ({submittedOrders.Count})";
        ApprovedHeadingText.Text = $"Approved ({approvedOrders.Count})";
        RejectedHeadingText.Text = $"Rejected ({rejectedOrders.Count})";
        PaidHeadingText.Text = $"Paid ({paidOrders.Count})";

        SubmittedAmountText.Text = $"Amount: {submittedOrders.Sum(x => x.TotalAmount):N3}";
        ApprovedAmountText.Text = $"Amount: {approvedOrders.Sum(x => x.TotalAmount):N3}";
        RejectedAmountText.Text = $"Amount: {rejectedOrders.Sum(x => x.TotalAmount):N3}";
        PaidAmountText.Text = $"Amount: {paidOrders.Sum(x => x.TotalAmount):N3}";
    }

    private bool IsOrderVisible(object item)
    {
        if (item is not PurchaseOrderReadDto order)
            return false;

        var orderDate = (order.DocumentDate ?? DateTime.MinValue).Date;
        var from = FromDatePicker?.SelectedDate?.Date;
        var to = ToDatePicker?.SelectedDate?.Date;
        if (from.HasValue && orderDate < from.Value)
            return false;
        if (to.HasValue && orderDate > to.Value)
            return false;
        if (!MatchesSupplier(order.SupplierName)) return false;
        var productName = ProductNameFilter?.Text.Trim() ?? string.Empty;
        var barcode = BarcodeLookup.Normalize(BarcodeFilter?.Text);
        if (productName.Length == 0 && barcode.Length == 0) return true;
        return order.FilterLines.Any(line => MatchesProductName(line.ProductName, productName)
            && (barcode.Length == 0 ||
                BarcodeLookup.AreEquivalent(line.Barcode, barcode) ||
                (line.Barcode?.Contains(barcode, StringComparison.OrdinalIgnoreCase) ?? false)));
    }

    private void OrdersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateSelectedOrder();
        UpdateActionButtons();
    }

    private void UpdateSelectedOrder()
    {
        if (OrdersGrid.SelectedItem is not PurchaseOrderReadDto selected)
        {
            SelectedStatusText.Text = UiText.T("لم يتم اختيار أمر شراء", "No order selected");
            return;
        }

        SelectedStatusText.Text = UiText.T(
            $"{selected.OrderNumber} | {GetStatusText(selected.Status)} | الدفع: {GetPaymentStatusText(selected.PaymentStatus)}",
            $"{selected.OrderNumber} | {GetStatusText(selected.Status)} | Payment: {GetPaymentStatusText(selected.PaymentStatus)}");
    }

    private void UpdateActionButtons()
    {
        var selected = OrdersGrid.SelectedItem as PurchaseOrderReadDto;
        var canPay = selected != null &&
                     selected.Status != PurchaseOrderStatus.Rejected &&
                     selected.Status != PurchaseOrderStatus.Cancelled &&
                     selected.PaymentStatus != PurchaseOrderPaymentStatus.Paid;

        PaidButton.Visibility = canPay && _canMarkPaid ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OrdersGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (OrdersGrid.SelectedItem is PurchaseOrderReadDto)
            _ = ShowDetailsAsync();
    }

    private async void ViewDetails_Click(object sender, RoutedEventArgs e) => await ShowDetailsAsync();

    private void OrdersGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        var row = FindParent<DataGridRow>(e.OriginalSource as DependencyObject);
        if (row?.Item is not PurchaseOrderReadDto selected)
        {
            e.Handled = true;
            return;
        }

        OrdersGrid.SelectedItem = selected;
        var menu = new ContextMenu();
        AddMenuItem(menu, UiText.T("التفاصيل", "Details"), (_, _) => _ = ShowDetailsAsync());

        if (selected.Status == PurchaseOrderStatus.Submitted && _canApprove)
            AddMenuItem(menu, UiText.T("اعتماد", "Approve"), async (_, _) => await ApproveSelectedAsync());

        if ((selected.Status == PurchaseOrderStatus.Submitted || selected.Status == PurchaseOrderStatus.Draft) && _canReject)
            AddMenuItem(menu, UiText.T("رفض", "Reject"), async (_, _) => await RejectSelectedAsync());

        if (selected.Status != PurchaseOrderStatus.Rejected && selected.Status != PurchaseOrderStatus.Cancelled &&
            selected.PaymentStatus != PurchaseOrderPaymentStatus.Paid && _canMarkPaid)
            AddMenuItem(menu, UiText.T("تسجيل كمدفوع", "Mark as paid"), async (_, _) => await MarkSelectedPaidAsync());

        row.ContextMenu = menu;
    }

    private static void AddMenuItem(ContextMenu menu, string header, RoutedEventHandler handler)
    {
        var item = new MenuItem { Header = header };
        item.Click += handler;
        menu.Items.Add(item);
    }

    private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child != null)
        {
            if (child is T parent)
                return parent;
            child = VisualTreeHelper.GetParent(child);
        }
        return null;
    }

    private async Task ShowDetailsAsync()
    {
        if (OrdersGrid.SelectedItem is not PurchaseOrderReadDto selected)
            return;

        _loadingService.Show();
        Result<PurchaseOrderWriteDto>? result = null;
        try
        {
            result = await _service.GetByIdAsync(selected.Id);
        }
        finally
        {
            _loadingService.Hide();
        }

        if (result == null || !result.Success || result.Data == null)
        {
            MessageBox.Show(
                result?.Message ?? UiText.T("تعذر تحميل تفاصيل أمر الشراء.", "Purchase order details could not be loaded."),
                UiText.T("خطأ", "Error"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        var detailsWindow = new PurchaseOrderDetailsWindow(result.Data) { Owner = this };
        detailsWindow.ShowDialog();
    }

    private async void Approve_Click(object sender, RoutedEventArgs e) => await ApproveSelectedAsync();

    private async Task ApproveSelectedAsync()
    {
        if (OrdersGrid.SelectedItem is not PurchaseOrderReadDto selected)
        {
            ShowNotice("Select a purchase order first.");
            return;
        }

        if (selected.Status != PurchaseOrderStatus.Submitted)
        {
            ShowNotice("Only submitted purchase orders can be approved.");
            return;
        }

        if (MessageBox.Show(
                UiText.T("سيتم اعتماد أمر الشراء دون تسجيل مخزون أو محاسبة. هل تريد المتابعة؟", "This will approve the purchase order without posting inventory or accounting. Continue?"),
                UiText.T("تأكيد الاعتماد", "Confirm approval"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        _orderOperationActive = true;
        _loadingService.Show();
        try
        {
            var result = await _service.ApproveAsync(selected.Id);
            _loadingService.Hide();
            MessageBox.Show(result.Message);
            if (result.Success)
                await LoadAsync();
        }
        finally
        {
            _loadingService.Hide();
            _orderOperationActive = false;
        }
    }

    private async void Reject_Click(object sender, RoutedEventArgs e) => await RejectSelectedAsync();

    private async Task RejectSelectedAsync()
    {
        if (OrdersGrid.SelectedItem is not PurchaseOrderReadDto selected)
        {
            ShowNotice("Select a purchase order first.");
            return;
        }

        _orderOperationActive = true;
        _loadingService.Show();
        try
        {
            var result = await _service.RejectAsync(selected.Id, "Rejected by approver.");
            _loadingService.Hide();
            MessageBox.Show(result.Message);
            if (result.Success)
                await LoadAsync();
        }
        finally
        {
            _loadingService.Hide();
            _orderOperationActive = false;
        }
    }

    private async void Paid_Click(object sender, RoutedEventArgs e) => await MarkSelectedPaidAsync();

    private async Task MarkSelectedPaidAsync()
    {
        if (OrdersGrid.SelectedItem is not PurchaseOrderReadDto selected)
        {
            ShowNotice("Select a purchase order first.");
            return;
        }

        if (selected.PaymentStatus == PurchaseOrderPaymentStatus.Paid)
        {
            ShowNotice("This purchase order is already marked as paid.");
            return;
        }

        var note = PromptForPaymentNote(selected.OrderNumber);
        if (note == null)
            return;

        _orderOperationActive = true;
        _loadingService.Show();
        try
        {
            var result = await _service.MarkPaidAsync(selected.Id, note);
            _loadingService.Hide();
            MessageBox.Show(result.Message);
            if (result.Success)
                await LoadAsync();
        }
        finally
        {
            _loadingService.Hide();
            _orderOperationActive = false;
        }
    }

    private void ShowNotice(string message) => MessageBox.Show(
        UiText.T(message switch
        {
            "Select a purchase order first." => "اختر أمر شراء أولاً.",
            "Only submitted purchase orders can be approved." => "يمكن اعتماد أوامر الشراء المرسلة فقط.",
            "This purchase order is already marked as paid." => "أمر الشراء مسجل كمدفوع مسبقاً.",
            _ => message
        }, message),
        UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Warning);

    private string? PromptForPaymentNote(string orderNumber)
    {
        var dialog = new Window
        {
            Owner = this,
            Title = UiText.T("تسجيل دفع أمر الشراء", "Mark purchase order as paid"),
            Width = 560,
            Height = 300,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.FromRgb(238, 244, 248))
        };

        var grid = new Grid { Margin = new Thickness(18) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var prompt = new TextBlock
        {
            Text = $"{UiText.T("اكتب ملاحظة الدفع لأمر الشراء", "Enter the payment note for")} {orderNumber}:",
            Foreground = new SolidColorBrush(Color.FromRgb(23, 56, 95)),
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(prompt, 0);
        grid.Children.Add(prompt);

        var noteTextBox = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(8),
            MinHeight = 90
        };
        Grid.SetRow(noteTextBox, 1);
        grid.Children.Add(noteTextBox);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0)
        };
        var cancelButton = new Button
        {
            Content = UiText.T("إلغاء", "Cancel"),
            Padding = new Thickness(18, 8, 18, 8),
            Margin = new Thickness(0, 0, 8, 0)
        };
        cancelButton.Click += (_, _) => dialog.DialogResult = false;
        var paidButton = new Button
        {
            Content = UiText.T("تسجيل كمدفوع", "Mark as paid"),
            Padding = new Thickness(18, 8, 18, 8),
            Background = new SolidColorBrush(Color.FromRgb(183, 130, 27)),
            Foreground = Brushes.White
        };
        paidButton.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(noteTextBox.Text))
            {
                MessageBox.Show(dialog, UiText.T("اكتب ملاحظة الدفع أولاً.", "Enter a payment note first."), UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Warning);
                noteTextBox.Focus();
                return;
            }

            dialog.DialogResult = true;
        };
        buttons.Children.Add(cancelButton);
        buttons.Children.Add(paidButton);
        Grid.SetRow(buttons, 2);
        grid.Children.Add(buttons);

        dialog.Content = grid;
        dialog.Loaded += (_, _) => noteTextBox.Focus();
        return dialog.ShowDialog() == true ? noteTextBox.Text.Trim() : null;
    }

    private static string GetStatusText(PurchaseOrderStatus status) => status.ToString();

    private static string GetPaymentStatusText(PurchaseOrderPaymentStatus status) => status.ToString();
}

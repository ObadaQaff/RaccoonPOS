using RaccoonWarehouse.Application.Service.PurchaseOrders;
using RaccoonWarehouse.Application.Service.PurchaseRequests;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.PurchaseRequests.DTOs;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace RaccoonWarehouse.PurchaseRequests;

public partial class PurchaseRequestsWindow : Window
{
    private readonly IPurchaseRequestService _service;
    private readonly ILoadingService _loading;
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly IUserSession _userSession;
    private readonly ObservableCollection<PurchaseRequestReadDto> _requests = new();
    private readonly ICollectionView _requestsView;
    private List<string> _supplierNames = new();
    private bool _filteringSupplierChoices;
    private bool _loaded;
    private bool _loadingRequests;
    private bool _canEdit;
    private bool _canSubmit;
    private bool _canApprove;
    private bool _canReject;
    private bool _canCreatePo;
    private bool _isAdmin;

    public PurchaseRequestsWindow(
        IPurchaseRequestService service,
        ILoadingService loading,
        IPurchaseOrderService purchaseOrderService,
        IUserSession userSession)
    {
        _service = service;
        _loading = loading;
        _purchaseOrderService = purchaseOrderService;
        _userSession = userSession;
        
        InitializeComponent();
        RequestsGrid.ItemsSource = _requests;
        _requestsView = CollectionViewSource.GetDefaultView(_requests);
        _requestsView.Filter = IsRequestVisible;
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
        if (_loaded && !_loadingRequests)
            await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_loadingRequests)
            return;

        _loadingRequests = true;
        _loading.Show();
        try
        {
            var result = await _service.GetAllAsync();
            if (!result.Success)
            {
                MessageBox.Show(result.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _requests.Clear();
            foreach (var request in result.Data ?? new())
                _requests.Add(request);

            _canEdit = true;
            _canSubmit = true;
            _canApprove = true;
            _canReject = true;
            _canCreatePo = true;
            _isAdmin = _userSession.CurrentRole == UserRole.Admin;
            ReloadSupplierSuggestions();
            _requestsView?.Refresh();
            UpdateStatusCounters();
            UpdateActionButtons();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiText.T("تعذر تحميل القائمة.", "Could not load the list.")
                + Environment.NewLine + ex.Message, UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _loading.Hide();
            _loadingRequests = false;
        }
    }

    private void NewRequest_Click(object sender, RoutedEventArgs e)
    {
        WindowManager.Show<PurchaseRequestEditorWindow>(WindowSizeType.LargeRectangle);
    }

    private void EditSelected_Click(object sender, RoutedEventArgs e)
    {
        if (RequestsGrid.SelectedItem is not PurchaseRequestReadDto request)
        {
            MessageBox.Show("Select a purchase request first.", "Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        OpenEditor(request);
    }

    private void RequestsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (RequestsGrid.SelectedItem is PurchaseRequestReadDto request)
            OpenEditor(request);
    }

    private static void OpenEditor(PurchaseRequestReadDto request)
    {
        if (request.Status != PurchaseRequestStatus.Draft && request.Status != PurchaseRequestStatus.Rejected)
        {
            MessageBox.Show("Only draft or rejected requests can be edited.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        WindowManager.Show<PurchaseRequestEditorWindow>(
            WindowSizeType.LargeRectangle,
            window => window.InitializeForEdit(request.Id));
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private void DateFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        _requestsView?.Refresh();
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
        _requestsView?.Refresh();
        UpdateStatusCounters();
    }

    private void RefreshSearchFilters()
    {
        if (_requestsView == null) return;
        _requestsView.Refresh();
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
        _supplierNames = _requests.SelectMany(request => request.Lines).Select(line => line.SupplierName)
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
        if (SubmittedHeadingText == null || ApprovedHeadingText == null || RejectedHeadingText == null ||
            SubmittedAmountText == null || ApprovedAmountText == null || RejectedAmountText == null)
            return;

        var visibleRequests = _requests.Where(IsRequestVisible).ToList();
        var submittedRequests = visibleRequests.Where(x => x.Status == PurchaseRequestStatus.Submitted).ToList();
        var approvedRequests = visibleRequests.Where(x => x.Status == PurchaseRequestStatus.Approved).ToList();
        var rejectedRequests = visibleRequests.Where(x => x.Status == PurchaseRequestStatus.Rejected).ToList();

        SubmittedHeadingText.Text = $"Submitted ({submittedRequests.Count})";
        ApprovedHeadingText.Text = $"Approved ({approvedRequests.Count})";
        RejectedHeadingText.Text = $"Rejected ({rejectedRequests.Count})";

        SubmittedAmountText.Text = $"Amount: {submittedRequests.Sum(x => x.TotalAmount):N3}";
        ApprovedAmountText.Text = $"Amount: {approvedRequests.Sum(x => x.ApprovedTotalAmount):N3}";
        RejectedAmountText.Text = $"Amount: {rejectedRequests.Sum(x => x.TotalAmount):N3}";
    }

    private bool IsRequestVisible(object item)
    {
        if (item is not PurchaseRequestReadDto request)
            return false;

        var from = FromDatePicker?.SelectedDate?.Date;
        var to = ToDatePicker?.SelectedDate?.Date;
        if (from.HasValue && request.RequestDate.Date < from.Value)
            return false;
        if (to.HasValue && request.RequestDate.Date > to.Value)
            return false;
        var supplier = SupplierFilter?.Text.Trim() ?? string.Empty;
        var productName = ProductNameFilter?.Text.Trim() ?? string.Empty;
        var barcode = BarcodeLookup.Normalize(BarcodeFilter?.Text);
        if (supplier.Length == 0 && productName.Length == 0 && barcode.Length == 0) return true;
        return request.Lines.Any(line => MatchesSupplier(line.SupplierName)
            && MatchesProductName(line.ItemName, productName)
            && (barcode.Length == 0 ||
                BarcodeLookup.AreEquivalent(line.Barcode, barcode) ||
                (line.Barcode?.Contains(barcode, StringComparison.OrdinalIgnoreCase) ?? false)));
    }

    private async void RequestsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RequestsGrid.SelectedItem is PurchaseRequestReadDto request)
            SelectedStatusText.Text = $"{request.RequestNumber} | {request.Status} | {request.Category}";
        else
            SelectedStatusText.Text = "No request selected";

        UpdateActionButtons();
        await Task.CompletedTask;
    }

    private void UpdateActionButtons()
    {
        var request = RequestsGrid.SelectedItem as PurchaseRequestReadDto;
        var isDraft = request?.Status == PurchaseRequestStatus.Draft;
        var isSubmitted = request?.Status == PurchaseRequestStatus.Submitted;
        var isApproved = request?.Status == PurchaseRequestStatus.Approved;

        SubmitButton.Visibility = isDraft && _canSubmit ? Visibility.Visible : Visibility.Collapsed;
        RejectButton.Visibility = (isDraft || isSubmitted) && _canReject && _isAdmin ? Visibility.Visible : Visibility.Collapsed;
        ApproveButton.Visibility = isSubmitted && _canApprove ? Visibility.Visible : Visibility.Collapsed;
        CreatePoButton.Visibility = isApproved && _canCreatePo ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RequestsGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        var row = FindParent<DataGridRow>(e.OriginalSource as DependencyObject);
        if (row?.Item is not PurchaseRequestReadDto request)
        {
            e.Handled = true;
            return;
        }

        RequestsGrid.SelectedItem = request;
        var menu = new ContextMenu();
        AddMenuItem(menu, "Details", (_, _) => OpenDetails(request));
        if (request.Status == PurchaseRequestStatus.Draft)
        {
            if (_canEdit)
            {
                AddMenuItem(menu, "Update", (_, _) => OpenEditor(request));
                AddMenuItem(menu, "Delete", async (_, _) => await DeleteDraftAsync(request));
            }
            if (_canReject && _isAdmin)
                AddMenuItem(menu, "Reject", async (_, _) => await RejectSelectedAsync());
        }
        else if (request.Status == PurchaseRequestStatus.Submitted)
        {
            if (_canApprove)
                AddMenuItem(menu, "Review / Approve", (_, _) => OpenApproval(request));
            if (_canReject && _isAdmin)
                AddMenuItem(menu, "Reject", async (_, _) => await RejectSelectedAsync());
        }
        else if (request.Status == PurchaseRequestStatus.Approved && _canCreatePo)
        {
            AddMenuItem(menu, "Create PO", async (_, _) => await CreatePurchaseOrderAsync(request));
        }

        if (menu.Items.Count == 0)
        {
            e.Handled = true;
            return;
        }

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

    private async void Submit_Click(object sender, RoutedEventArgs e) => await ExecuteSelectedAsync(request => _service.SubmitAsync(request.Id));
    private void Approve_Click(object sender, RoutedEventArgs e)
    {
        if (RequestsGrid.SelectedItem is not PurchaseRequestReadDto request)
        {
            MessageBox.Show("Select a purchase request first.", "Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (request.Status != PurchaseRequestStatus.Submitted)
        {
            MessageBox.Show("Only submitted purchase requests can be reviewed.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        OpenApproval(request);
    }
    private async void Reject_Click(object sender, RoutedEventArgs e) => await RejectSelectedAsync();

    private async Task RejectSelectedAsync()
    {
        if (RequestsGrid.SelectedItem is not PurchaseRequestReadDto request)
        {
            MessageBox.Show("Select a purchase request first.", "Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var reason = PromptForRejectionReason(request.RequestNumber);
        if (reason == null)
            return;

        await ExecuteSelectedAsync(selected => _service.RejectAsync(selected.Id, reason));
    }

    private string? PromptForRejectionReason(string requestNumber)
    {
        var dialog = new Window
        {
            Owner = this,
            Title = UiText.T("رفض طلب الشراء", "Reject purchase request"),
            Width = 520,
            Height = 285,
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
            Text = $"{UiText.T("اكتب سبب رفض الطلب", "Enter the rejection note for")} {requestNumber}:",
            Foreground = new SolidColorBrush(Color.FromRgb(23, 56, 95)),
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(prompt, 0);
        grid.Children.Add(prompt);

        var reasonTextBox = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(8),
            MinHeight = 90
        };
        Grid.SetRow(reasonTextBox, 1);
        grid.Children.Add(reasonTextBox);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
        var cancelButton = new Button { Content = UiText.T("إلغاء", "Cancel"), Padding = new Thickness(18, 8, 18, 8), Margin = new Thickness(0, 0, 8, 0) };
        cancelButton.Click += (_, _) => dialog.DialogResult = false;
        var rejectButton = new Button { Content = UiText.T("رفض", "Reject"), Padding = new Thickness(18, 8, 18, 8), Background = new SolidColorBrush(Color.FromRgb(182, 67, 75)), Foreground = Brushes.White };
        rejectButton.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(reasonTextBox.Text))
            {
                MessageBox.Show(dialog, UiText.T("اكتب سبب الرفض أولاً.", "Enter a rejection note first."), UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Warning);
                reasonTextBox.Focus();
                return;
            }

            dialog.DialogResult = true;
        };
        buttons.Children.Add(cancelButton);
        buttons.Children.Add(rejectButton);
        Grid.SetRow(buttons, 2);
        grid.Children.Add(buttons);

        dialog.Content = grid;
        dialog.Loaded += (_, _) => reasonTextBox.Focus();
        return dialog.ShowDialog() == true ? reasonTextBox.Text.Trim() : null;
    }

    private async void CreatePo_Click(object sender, RoutedEventArgs e)
    {
        if (RequestsGrid.SelectedItem is not PurchaseRequestReadDto request)
        {
            MessageBox.Show("Select a purchase request first.", "Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (request.Status != PurchaseRequestStatus.Approved)
        {
            MessageBox.Show("Only approved purchase requests can generate a purchase order.", "Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await CreatePurchaseOrderAsync(request);
    }

    private static void OpenApproval(PurchaseRequestReadDto request)
    {
        WindowManager.Show<PurchaseRequestApprovalWindow>(
            WindowSizeType.LargeRectangle,
            window => window.InitializeForApproval(request.Id));
    }

    private static void OpenDetails(PurchaseRequestReadDto request)
    {
        WindowManager.Show<PurchaseRequestDetailsWindow>(
            WindowSizeType.LargeRectangle,
            window => window.Initialize(request.Id));
    }

    private async Task CreatePurchaseOrderAsync(PurchaseRequestReadDto request)
    {
        _loading.Show();
        try
        {
            var result = await _purchaseOrderService.CreateFromPurchaseRequestAsync(request.Id);
            _loading.Hide();
            MessageBox.Show(result.Message, result.Success ? "Created" : "Error", MessageBoxButton.OK,
                result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);
            if (result.Success)
                await LoadAsync();
        }
        finally
        {
            _loading.Hide();
        }
    }

    private async Task DeleteDraftAsync(PurchaseRequestReadDto request)
    {
        if (MessageBox.Show($"Delete draft {request.RequestNumber}?", "Confirm delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        _loading.Show();
        try
        {
            var result = await _service.DeleteAsync(request.Id);
            _loading.Hide();
            MessageBox.Show(result.Message, result.Success ? "Deleted" : "Error", MessageBoxButton.OK,
                result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);
            if (result.Success)
                await LoadAsync();
        }
        finally
        {
            _loading.Hide();
        }
    }

    private async Task ExecuteSelectedAsync(Func<PurchaseRequestReadDto, Task<Result<PurchaseRequestWriteDto>>> action)
    {
        if (RequestsGrid.SelectedItem is not PurchaseRequestReadDto request)
        {
            MessageBox.Show("Select a purchase request first.", "Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _loading.Show();
        try
        {
            var result = await action(request);
            _loading.Hide();
            MessageBox.Show(result.Message, result.Success ? "Completed" : "Error", MessageBoxButton.OK,
                result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);
            if (result.Success)
                await LoadAsync();
        }
        finally
        {
            _loading.Hide();
        }
    }
}

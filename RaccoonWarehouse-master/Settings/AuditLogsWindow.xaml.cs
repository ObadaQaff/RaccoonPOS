using RaccoonWarehouse.Application.Service.Audit;
using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Helpers.Localization;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.Settings;

public partial class AuditLogsWindow : Window
{
    private readonly IAuditLogQueryService _queryService;
    private readonly IPermissionService _permissionService;
    private readonly IUserSession _userSession;
    private readonly ILoadingService _loadingService;
    private int _pageNumber = 1;
    private int? _selectedUserId;
    private bool _loaded;

    public AuditLogsWindow(
        IAuditLogQueryService queryService,
        IPermissionService permissionService,
        IUserSession userSession,
        ILoadingService loadingService)
    {
        _queryService = queryService;
        _permissionService = permissionService;
        _userSession = userSession;
        _loadingService = loadingService;
        InitializeComponent();
        UiText.ApplyWindow(this);
        Loaded += AuditLogsWindow_Loaded;
    }

    public void InitializeForUser(int userId, string userName)
    {
        _selectedUserId = userId;
        UserIdBox.Text = userId.ToString();
        SearchBox.ToolTip = UiText.T($"عرض نشاط {userName}", $"Showing activity for {userName}");
    }

    private async void AuditLogsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_userSession.CurrentRole != UserRole.Admin
            || !await _permissionService.HasPermissionAsync(UserRole.Admin, "AuditLogs.View"))
        {
            MessageBox.Show(UiText.T("ليس لديك صلاحية عرض سجل المراجعة.", "You do not have permission to view audit logs."));
            Close();
            return;
        }

        _loaded = true;
        await LoadPageAsync();
    }

    private async Task LoadPageAsync()
    {
        if (!_loaded)
            return;

        _loadingService.Show();
        try
        {
            var userId = ParseUserId();
            var resultFilter = (ResultFilterBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            bool? succeeded = bool.TryParse(resultFilter, out var parsed) ? parsed : null;
            var result = await _queryService.GetPagedAsync(new AuditLogFilter(
                PageNumber: _pageNumber,
                UserId: userId,
                Search: SearchBox.Text,
                Succeeded: succeeded));

            AuditGrid.ItemsSource = result.Items;
            PageText.Text = UiText.T(
                $"الصفحة {result.PageNumber} من {Math.Max(1, result.TotalPages)} — الإجمالي {result.TotalCount}",
                $"Page {result.PageNumber} of {Math.Max(1, result.TotalPages)} — {result.TotalCount} total");
            StatusText.Text = UiText.T("يتم تحميل السجل على صفحات لتقليل استهلاك الذاكرة.", "Logs are loaded in pages to keep the screen responsive.");
        }
        catch (Exception ex)
        {
            StatusText.Text = UiText.T("تعذر تحميل سجل المراجعة. تأكد من إنشاء جدول AuditLogs.", "Could not load audit logs. Make sure the AuditLogs table exists.");
            MessageBox.Show(ex.Message, UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            _loadingService.Hide();
        }
    }

    private int? ParseUserId()
    {
        if (_selectedUserId.HasValue)
            return _selectedUserId;
        return int.TryParse(UserIdBox.Text?.Trim(), out var id) && id > 0 ? id : null;
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        _pageNumber = 1;
        await LoadPageAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await LoadPageAsync();

    private async void PreviousButton_Click(object sender, RoutedEventArgs e)
    {
        if (_pageNumber <= 1) return;
        _pageNumber--;
        await LoadPageAsync();
    }

    private async void NextButton_Click(object sender, RoutedEventArgs e)
    {
        if (AuditGrid.Items.Count == 0) return;
        _pageNumber++;
        await LoadPageAsync();
    }
}

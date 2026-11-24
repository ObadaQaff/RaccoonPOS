using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Core.Permissions;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Permissions;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace RaccoonWarehouse.Permissions;

public partial class PermissionGroupsWindow : Window
{
    private readonly IPermissionManagementService _service;
    private readonly ObservableCollection<PermissionGroupRowDto> _groups = new();
    private readonly ObservableCollection<PermissionRow> _permissionRows = new();
    private readonly ObservableCollection<PermissionUserDto> _users = new();
    private readonly ICollectionView _filteredUsersView;
    private readonly HashSet<int> _selectedUserIds = new();
    private bool _isRefreshingUserOrder;
    private int _id;

    public PermissionGroupsWindow(IPermissionManagementService service)
    {
        _service = service;
        InitializeComponent();
        GroupsList.ItemsSource = _groups;
        PermissionsList.ItemsSource = _permissionRows;
        UsersList.ItemsSource = _users;
        _filteredUsersView = CollectionViewSource.GetDefaultView(_users);
        _filteredUsersView.Filter = FilterUser;
        if (_filteredUsersView is ListCollectionView listView)
            listView.CustomSort = Comparer<object>.Create((left, right) =>
        {
            var leftSelected = left is PermissionUserDto leftUser && _selectedUserIds.Contains(leftUser.Id);
            var rightSelected = right is PermissionUserDto rightUser && _selectedUserIds.Contains(rightUser.Id);
            if (leftSelected != rightSelected) return leftSelected ? -1 : 1;
            return string.Compare((left as PermissionUserDto)?.Name, (right as PermissionUserDto)?.Name, StringComparison.CurrentCultureIgnoreCase);
        });
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try { await ReloadAsync(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Error / خطأ"); Close(); }
    }

    private async Task ReloadAsync(int? selectedGroupId = null)
    {
        _groups.Clear();
        foreach (var group in await _service.GetGroupsAsync()) _groups.Add(group);
        _users.Clear();
        foreach (var user in await _service.GetUsersAsync()) _users.Add(user);
        GroupsSummary.Text = $"{_groups.Count} groups / مجموعة";
        UsersSummary.Text = $"{_users.Count} users / مستخدم";
        _filteredUsersView.Refresh();

        if (_groups.Count == 0)
        {
            New_Click(this, new RoutedEventArgs());
            return;
        }

        var selectedIndex = selectedGroupId.HasValue
            ? _groups.ToList().FindIndex(x => x.Id == selectedGroupId.Value)
            : 0;
        GroupsList.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
    }

    private async void Group_Selected(object sender, SelectionChangedEventArgs e)
    {
        if (GroupsList.SelectedItem is not PermissionGroupRowDto row) return;
        var group = await _service.GetGroupAsync(row.Id);
        if (group == null) return;

        _id = group.Id;
        GroupName.Text = group.Name;
        GroupDescription.Text = group.Description;
        GroupActive.IsChecked = group.IsActive;
        var selected = group.Permissions.ToDictionary(x => x.AreaKey, StringComparer.OrdinalIgnoreCase);
        _permissionRows.Clear();
        foreach (var area in PermissionAreaRegistry.DisplayAreas)
            _permissionRows.Add(new PermissionRow(area, selected.TryGetValue(area.Key, out var value) ? value.Level : PermissionLevel.None));

        UsersList.SelectedItems.Clear();
        foreach (var user in _users.Where(x => group.UserIds.Contains(x.Id))) UsersList.SelectedItems.Add(user);
        _selectedUserIds.Clear();
        foreach (var userId in group.UserIds) _selectedUserIds.Add(userId);
        _filteredUsersView.Refresh();
        UpdateUsersSummary();
    }

    private void New_Click(object? sender, RoutedEventArgs e)
    {
        _id = 0;
        GroupsList.SelectedIndex = -1;
        GroupName.Clear();
        GroupDescription.Clear();
        GroupActive.IsChecked = true;
        _permissionRows.Clear();
        foreach (var area in PermissionAreaRegistry.DisplayAreas)
            _permissionRows.Add(new PermissionRow(area, PermissionLevel.None));
        UsersList.UnselectAll();
        _selectedUserIds.Clear();
        _filteredUsersView.Refresh();
        UserSearchBox.Clear();
        UpdateUsersSummary();
        GroupName.Focus();
    }

    private bool FilterUser(object item)
    {
        if (item is not PermissionUserDto user) return false;
        var query = UserSearchBox?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(query)) return true;
        return user.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
            || user.Id.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)
            || (user.PhoneNumber?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private void UserSearchBox_TextChanged(object sender, TextChangedEventArgs e) => _filteredUsersView.Refresh();

    private void UsersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshingUserOrder) return;
        _selectedUserIds.Clear();
        foreach (var user in UsersList.SelectedItems.OfType<PermissionUserDto>()) _selectedUserIds.Add(user.Id);
        _isRefreshingUserOrder = true;
        try { _filteredUsersView.Refresh(); }
        finally { _isRefreshingUserOrder = false; }
        UpdateUsersSummary();
    }

    private void SelectFilteredUsers_Click(object sender, RoutedEventArgs e)
    {
        foreach (var user in _users.Where(FilterUser).ToList()) UsersList.SelectedItems.Add(user);
        _selectedUserIds.Clear();
        foreach (var user in UsersList.SelectedItems.OfType<PermissionUserDto>()) _selectedUserIds.Add(user.Id);
        _filteredUsersView.Refresh();
        UpdateUsersSummary();
    }

    private void ClearUsers_Click(object sender, RoutedEventArgs e)
    {
        UsersList.UnselectAll();
        _selectedUserIds.Clear();
        _filteredUsersView.Refresh();
        UpdateUsersSummary();
    }

    private void UpdateUsersSummary() => UsersSummary.Text = $"{UsersList.SelectedItems.Count} selected / محدد من {_users.Count}";

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = await _service.SaveGroupAsync(new PermissionGroupEditDto
            {
                Id = _id,
                Name = GroupName.Text,
                Description = GroupDescription.Text,
                IsActive = GroupActive.IsChecked == true,
                Permissions = _permissionRows.Where(x => x.Level != PermissionLevel.None).Select(x => new PermissionValue(x.Key, x.Level)).ToList(),
                UserIds = UsersList.SelectedItems.Cast<PermissionUserDto>().Select(x => x.Id).ToList()
            });
            if (!result.Success)
            {
                MessageBox.Show(string.Join(Environment.NewLine, result.Errors), "Notice / تنبيه");
                return;
            }
            await ReloadAsync(result.Data);
            MessageBox.Show("Permission group saved successfully. / تم حفظ مجموعة الصلاحيات بنجاح.");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Error / خطأ"); }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    public sealed class PermissionRow : INotifyPropertyChanged
    {
        public string Key { get; }
        public string ArabicName { get; }
        public bool IsCategory { get; }
        public string? ParentKey { get; }
        public IReadOnlyList<PermissionLevel> Levels { get; } = new[] { PermissionLevel.None, PermissionLevel.View, PermissionLevel.Manage, PermissionLevel.Admin };
        private PermissionLevel _level;
        public PermissionLevel Level { get => _level; set { _level = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Level))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
        public PermissionRow(PermissionArea area, PermissionLevel level) { Key = area.Key; ArabicName = area.ArabicName; IsCategory = area.ParentKey == null; ParentKey = area.ParentKey; _level = level; }
    }
}

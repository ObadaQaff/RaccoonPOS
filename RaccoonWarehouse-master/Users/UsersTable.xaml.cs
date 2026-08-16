using AutoMapper;
using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace RaccoonWarehouse
{
    public partial class UsersTable : Window
    {
        private readonly IUserService _userService;
        private readonly IUserSession _userSession;
        private readonly IPermissionService _permissionService;
        private bool _isLoaded;
        private readonly List<UserReadDto> _users = new();
        private ICollectionView? _usersView;

        public UsersTable(IUserService userService, IMapper mapper, IUserSession userSession, IPermissionService permissionService)
        {
            _userService = userService;
            _userSession = userSession;
            _permissionService = permissionService;
            InitializeComponent();
            UiText.ApplyWindow(this);

            Loaded += async (_, _) =>
            {
                if (_isLoaded)
                    return;

                _isLoaded = true;
                await ConfigurePermissionsUiAsync();
                LoadUsers();
            };
        }

        private async Task ConfigurePermissionsUiAsync()
        {
            var role = _userSession.CurrentUser?.Role;
            var canCreate = role.HasValue && await _permissionService.HasPermissionAsync(role.Value, "Users.Create");

            CreateUserBtn.Visibility = canCreate ? Visibility.Visible : Visibility.Collapsed;
            CreatePermissionText.Text = canCreate ? UiText.T("مفعل", "Enabled") : UiText.T("مغلق", "Locked");
            AdminHintText.Text = canCreate
                ? UiText.T("يمكنك إنشاء الحسابات وإدارتها من هذا القسم.", "You can create and manage user accounts from this section.")
                : UiText.T("إنشاء الحسابات أو حذفها يتطلب صلاحية مخصصة.", "Creating or deleting user accounts requires a dedicated permission.");
        }

        private async void LoadUsers()
        {
            var role = _userSession.CurrentUser?.Role;
            if (!role.HasValue || !await _permissionService.HasPermissionAsync(role.Value, "Users.View"))
            {
                MessageBox.Show(UiText.T("ليس لديك صلاحية عرض المستخدمين.", "You do not have permission to view users."));
                Close();
                return;
            }

            var users = await _userService.GetAllAsync();
            _users.Clear();

            if (users.Data != null)
                _users.AddRange(users.Data);

            _usersView = CollectionViewSource.GetDefaultView(_users);
            _usersView.Filter = FilterUsers;
            UsersTable1.ItemsSource = _usersView;
            UpdateCounters();
        }

        private async void CreateUserBtn_Click(object sender, RoutedEventArgs e)
        {
            var role = _userSession.CurrentUser?.Role;
            if (!role.HasValue || !await _permissionService.HasPermissionAsync(role.Value, "Users.Create"))
            {
                MessageBox.Show(UiText.T("ليس لديك صلاحية إنشاء مستخدم جديد.", "You do not have permission to create a new user."));
                return;
            }

            WindowManager.Show<CreateUser>();
        }

        private async void ViewStatement_Click(object sender, RoutedEventArgs e)
        {
            var role = _userSession.CurrentUser?.Role;
            if (!role.HasValue || !await _permissionService.HasPermissionAsync(role.Value, "Users.View"))
            {
                MessageBox.Show(UiText.T("Ù„ÙŠØ³ Ù„Ø¯ÙŠÙƒ ØµÙ„Ø§Ø­ÙŠØ© Ø¹Ø±Ø¶ Ø§Ù„Ø­Ø³Ø§Ø¨Ø§Øª.", "You do not have permission to view accounts."));
                return;
            }

            if (UsersTable1.SelectedItem is not UserReadDto selectedUser)
            {
                MessageBox.Show(UiText.T("ÙŠØ¬Ø¨ ØªØ­Ø¯ÙŠØ¯ Ù…Ø³ØªØ®Ø¯Ù… Ù‚Ø¨Ù„ Ø¹Ø±Ø¶ Ø§Ù„ÙƒØ´Ù.", "You must select a user before viewing the statement."));
                return;
            }

            WindowManager.ShowDialog<UserStatementWindow>(WindowSizeType.LargeRectangle, window => window.Initialize(selectedUser.Id));
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void Update_User(object sender, RoutedEventArgs e)
        {
            var role = _userSession.CurrentUser?.Role;
            if (!role.HasValue || !await _permissionService.HasPermissionAsync(role.Value, "Users.Edit"))
            {
                MessageBox.Show(UiText.T("ليس لديك صلاحية تعديل الحسابات.", "You do not have permission to edit accounts."));
                return;
            }

            if (UsersTable1.SelectedItem is not UserReadDto selectedUser)
            {
                MessageBox.Show(UiText.T("يجب تحديد مستخدم قبل التعديل.", "You must select a user before editing."));
                return;
            }

            WindowManager.ShowDialog<UpdateUser>(
                WindowSizeType.SmallSquare,
                async w => await w.Initialize(selectedUser.Id));
        }

        private async void Delete_User(object sender, RoutedEventArgs e)
        {
            var role = _userSession.CurrentUser?.Role;
            if (!role.HasValue || !await _permissionService.HasPermissionAsync(role.Value, "Users.Delete"))
            {
                MessageBox.Show(UiText.T("ليس لديك صلاحية حذف المستخدمين.", "You do not have permission to delete users."));
                return;
            }

            if (UsersTable1.SelectedItem is not UserReadDto selectedUser)
            {
                MessageBox.Show(UiText.T("يجب تحديد مستخدم قبل الحذف.", "You must select a user before deleting."));
                return;
            }

            var messageResult = MessageBox.Show(
                UiText.IsEnglish
                    ? $"Are you sure you want to delete user {selectedUser.Name}?"
                    : $"هل أنت متأكد من حذف المستخدم {selectedUser.Name}؟",
                UiText.T("تأكيد الحذف", "Confirm Deletion"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (messageResult != MessageBoxResult.Yes)
                return;

            await _userService.DeleteAsync(selectedUser.Id);
            LoadUsers();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _usersView?.Refresh();
            UpdateCounters();
        }

        private bool FilterUsers(object item)
        {
            if (item is not UserReadDto user)
                return false;

            var search = SearchBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(search))
                return true;

            return (user.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                || (user.PhoneNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                || user.Role.ToString().Contains(search, StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateCounters()
        {
            TotalUsersText.Text = _users.Count.ToString();
            VisibleUsersText.Text = _usersView?.Cast<object>().Count().ToString() ?? "0";
        }

        private void UsersTable1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (UsersTable1.SelectedItem is UserReadDto)
                Update_User(sender, e);
        }
    }
}

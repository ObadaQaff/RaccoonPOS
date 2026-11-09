using AutoMapper;
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
        private bool _isLoaded;
        private readonly List<UserReadDto> _users = new();
        private ICollectionView? _usersView;

        public UsersTable(IUserService userService, IMapper mapper, IUserSession userSession)
        {
            _userService = userService;
            _userSession = userSession;
            InitializeComponent();
            UiText.ApplyWindow(this);

            Loaded += async (_, _) =>
            {
                if (_isLoaded)
                    return;

                _isLoaded = true;
                LoadUsers();
            };
        }
        private async void LoadUsers()
        {
            var role = _userSession.CurrentUser?.Role;var users = await _userService.GetAllAsync();
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
            var role = _userSession.CurrentUser?.Role;WindowManager.Show<CreateUser>();
        }

        private async void ViewStatement_Click(object sender, RoutedEventArgs e)
        {
            var role = _userSession.CurrentUser?.Role;if (UsersTable1.SelectedItem is not UserReadDto selectedUser)
            {
                MessageBox.Show(UiText.T("يجب تحديد مستخدم قبل عرض الكشف.", "You must select a user before viewing the statement."));
                return;
            }

            WindowManager.ShowDialog<UserStatementWindow>(WindowSizeType.LargeRectangle, window => window.Initialize(selectedUser.Id, selectedUser.Role));
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void Update_User(object sender, RoutedEventArgs e)
        {
            var role = _userSession.CurrentUser?.Role;if (UsersTable1.SelectedItem is not UserReadDto selectedUser)
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
            var role = _userSession.CurrentUser?.Role;if (UsersTable1.SelectedItem is not UserReadDto selectedUser)
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

        private void RoleFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded)
                return;

            _usersView?.Refresh();
            UpdateCounters();
        }

        private bool FilterUsers(object item)
        {
            if (item is not UserReadDto user)
                return false;

            var search = SearchBox.Text?.Trim();
            var selectedRole = RoleFilterComboBox.SelectedValue?.ToString();
            if (!string.IsNullOrWhiteSpace(selectedRole)
                && !string.Equals(selectedRole, "all", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(user.Role.ToString(), selectedRole, StringComparison.OrdinalIgnoreCase))
                return false;

            if (string.IsNullOrWhiteSpace(search))
                return true;

            return (user.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                || (user.PhoneNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                || user.Role.ToString().Contains(search, StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateCounters()
        {
            if (TotalUsersText == null || VisibleUsersText == null)
                return;

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

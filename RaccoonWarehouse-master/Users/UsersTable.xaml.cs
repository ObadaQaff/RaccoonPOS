using AutoMapper;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Navigation;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

            Loaded += (_, _) =>
            {
                if (_isLoaded)
                    return;

                _isLoaded = true;
                ConfigurePermissionsUi();
                LoadUsers();
            };
        }

        private void ConfigurePermissionsUi()
        {
            var isAdmin = _userSession.CurrentUser?.Role == UserRole.Admin;
            CreateUserBtn.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            CreatePermissionText.Text = isAdmin ? "مفعل" : "مغلق";
            AdminHintText.Text = isAdmin
                ? "يمكنك إنشاء الحسابات وحذفها من هذا القسم."
                : "إنشاء المستخدمين وحذفهم متاحان للمدير فقط.";
        }

        private async void LoadUsers()
        {
            var users = await _userService.GetAllAsync();
            _users.Clear();

            if (users.Data != null)
                _users.AddRange(users.Data);

            _usersView = CollectionViewSource.GetDefaultView(_users);
            _usersView.Filter = FilterUsers;
            UsersTable1.ItemsSource = _usersView;
            UpdateCounters();
        }

        private void CreateUserBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_userSession.CurrentUser?.Role != UserRole.Admin)
            {
                MessageBox.Show("فقط المدير يمكنه إنشاء مستخدم جديد.");
                return;
            }

            WindowManager.Show<CreateUser>();
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Update_User(object sender, RoutedEventArgs e)
        {
            if (_userSession.CurrentUser?.Role != UserRole.Admin
                && _userSession.CurrentUser?.Role != UserRole.Casher)
            {
                MessageBox.Show("ليس لديك صلاحية لتعديل الحسابات.");
                return;
            }

            if (UsersTable1.SelectedItem is not UserReadDto selectedUser)
            {
                MessageBox.Show("يجب تحديد مستخدم قبل التعديل.");
                return;
            }

            WindowManager.ShowDialog<UpdateUser>(
                WindowSizeType.SmallSquare,
                async w => await w.Initialize(selectedUser.Id));
        }

        private async void Delete_User(object sender, RoutedEventArgs e)
        {
            if (_userSession.CurrentUser?.Role != UserRole.Admin)
            {
                MessageBox.Show("فقط المدير يمكنه حذف المستخدمين.");
                return;
            }

            if (UsersTable1.SelectedItem is not UserReadDto selectedUser)
            {
                MessageBox.Show("يجب تحديد مستخدم قبل الحذف.");
                return;
            }

            var messageResult = MessageBox.Show(
                $"هل أنت متأكد من حذف المستخدم {selectedUser.Name}؟",
                "تأكيد الحذف",
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

            return (user.Name?.Contains(search, System.StringComparison.OrdinalIgnoreCase) ?? false)
                || (user.PhoneNumber?.Contains(search, System.StringComparison.OrdinalIgnoreCase) ?? false)
                || user.Role.ToString().Contains(search, System.StringComparison.OrdinalIgnoreCase);
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

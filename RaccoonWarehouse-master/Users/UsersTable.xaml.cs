using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Brands;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Users;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Orders;
using RaccoonWarehouse.Units;
using System.Linq;
using System.Net.WebSockets;
using System.Windows;

namespace RaccoonWarehouse
{
    public partial class UsersTable : Window
    {
        private readonly IUserService _userService;
        private readonly IMapper _mapper;
        private bool _isLoaded = false;

        public UsersTable(IUserService userService,IMapper mapper)
        {
            _userService = userService;
            _mapper = mapper;
            InitializeComponent();

            Loaded += async (_, _) =>
            {
                if (!_isLoaded)
                {
                    _isLoaded = true;
                     LoadUsers();
                }
            };
        }

        private async void LoadUsers()
        {
            var users = await _userService.GetAllAsync();
            UsersTable1.ItemsSource = users.Data;
        }

        private void CreateUserBtn_Click(object sender, RoutedEventArgs e)
        {
            WindowManager.Show<CreateUser>();
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
           
            this.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
         private void Update_User(object sender, RoutedEventArgs e)
        {
            if (UsersTable1.SelectedItem is not UserReadDto selectedUser)
            {
                MessageBox.Show(" يجب عليك تحديد مستخرم لتتمكن من تعديله ");
                return;
            }
            WindowManager.ShowDialog<UpdateUser>(
                    WindowSizeType.SmallSquare,
                    async w =>  w.Initialize(selectedUser.Id)
                );
            }
         private async void Delete_User(object sender, RoutedEventArgs e)
        {
            var selectedUser = UsersTable1.SelectedItem as UserReadDto;

            var messageResult = MessageBox.Show(
            $"Are you sure you want to delete {selectedUser.Name}?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

            if (messageResult == MessageBoxResult.Yes)
            {
               await _userService.DeleteAsync(selectedUser.Id);
                LoadUsers();
            }
        }

        
    }
}

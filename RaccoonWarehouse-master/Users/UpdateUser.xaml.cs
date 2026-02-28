using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users;
using RaccoonWarehouse.Domain.Users.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RaccoonWarehouse
{
    /// <summary>
    /// Interaction logic for UpdateUser.xaml
    /// </summary>
    public partial class UpdateUser : Window
    {
            private UserWriteDto _user; 
            private readonly IUserService _userService;
            private readonly IMapper _mapper;
            private int _userId;
            public int UserId { get; private set; }

        public UpdateUser(IUserService userService,IMapper mapper)
        {
            _mapper = mapper;
            _userService = userService;
            InitializeComponent();
            _user = new UserWriteDto();

        }
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {           
        }
        public async void Initialize(int userId)
        {
            _userId = UserId = userId;
            await Load_User(_userId);
        }
        private async Task  Load_User(int userId) 
        {
            var result = await _userService.GetWriteDtoByIdAsync(userId);

            if (result.Success && result.Data != null)
            {
                _user = result.Data;

                FullName.Text = _user.Name;
                PhoneNumber.Text = _user.PhoneNumber;
                Password.Text = _user.Password;
                Role.ItemsSource = Enum.GetValues(typeof(UserRole));
                Role.SelectedItem = _user.Role;
            }
            else
            {
                MessageBox.Show("User not found.");
                Close();
            }
        }


        private async void Update_User(object sender, RoutedEventArgs e)
        {
            if (_user == null) return;

            if (string.IsNullOrWhiteSpace(FullName.Text) ||
                string.IsNullOrWhiteSpace(Password.Text))
            {
                MessageBox.Show("الرجاء ملء كلمة المرور والاسم على الاقل .");
                return;
            }

            // Update the write DTO
            _user.Name = FullName.Text;
          
            _user.PhoneNumber = PhoneNumber.Text;
            _user.Password = Password.Text;
            _user.Role = (UserRole)Role.SelectedItem;

            var result = await _userService.UpdateAsync(_user);
            if (result.Success)
            {
                MessageBox.Show("تم تحديث البيانات بنجاح");
                return;
            }
            

            this.Close();
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

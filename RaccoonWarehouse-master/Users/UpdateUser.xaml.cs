using AutoMapper;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users.DTOs;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace RaccoonWarehouse
{
    public partial class UpdateUser : Window
    {
        private UserWriteDto _user;
        private readonly IUserService _userService;
        public int UserId { get; private set; }

        public UpdateUser(IUserService userService, IMapper mapper)
        {
            _userService = userService;
            InitializeComponent();
            _user = new UserWriteDto();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.CompletedTask;
        }

        public async Task Initialize(int userId)
        {
            UserId = userId;
            UserIdText.Text = $"#{userId}";
            await LoadUserAsync(userId);
        }

        private async Task LoadUserAsync(int userId)
        {
            var result = await _userService.GetWriteDtoByIdAsync(userId);

            if (!result.Success || result.Data == null)
            {
                MessageBox.Show("المستخدم غير موجود.");
                Close();
                return;
            }

            _user = result.Data;
            FullName.Text = _user.Name;
            PhoneNumber.Text = _user.PhoneNumber;
            Password.Text = _user.Password;
            ConfirmPassword.Text = _user.Password;
            Role.ItemsSource = Enum.GetValues(typeof(UserRole));
            Role.SelectedItem = _user.Role;
        }

        private async void Update_User(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FullName.Text) || string.IsNullOrWhiteSpace(Password.Text))
            {
                MessageBox.Show("الرجاء تعبئة الاسم وكلمة المرور على الأقل.");
                return;
            }

            if (Password.Text != ConfirmPassword.Text)
            {
                MessageBox.Show("تأكيد كلمة المرور غير مطابق.");
                ConfirmPassword.Focus();
                return;
            }

            if (Role.SelectedItem is not UserRole selectedRole)
            {
                MessageBox.Show("الرجاء اختيار نوع الحساب.");
                return;
            }

            _user.Name = FullName.Text.Trim();
            _user.PhoneNumber = PhoneNumber.Text.Trim();
            _user.Password = Password.Text;
            _user.Role = selectedRole;

            var result = await _userService.UpdateAsync(_user);
            if (!result.Success)
            {
                MessageBox.Show(result.Message);
                return;
            }

            MessageBox.Show("تم تحديث البيانات بنجاح");
            Close();
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

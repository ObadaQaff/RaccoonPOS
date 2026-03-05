using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users.DTOs;
using System;
using System.Windows;

namespace RaccoonWarehouse
{
    public partial class CreateUser : Window
    {
        private readonly IUserService _userService;
        private readonly IUserSession _userSession;

        public CreateUser(IUserService userService, IUserSession userSession)
        {
            _userService = userService;
            _userSession = userSession;
            InitializeComponent();

            Role.ItemsSource = Enum.GetValues(typeof(UserRole));
            Role.SelectedIndex = 0;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_userSession.CurrentUser?.Role != UserRole.Admin)
                {
                    MessageBox.Show("فقط المدير يمكنه إنشاء مستخدم جديد.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(FullName.Text) || string.IsNullOrWhiteSpace(Password.Text))
                {
                    MessageBox.Show("الرجاء تعبئة الحقول المطلوبة.");
                    return;
                }

                if (Password.Text != ConfirmPassword.Text)
                {
                    MessageBox.Show("تأكيد كلمة المرور غير مطابق.");
                    return;
                }

                CreateStatusText.Text = "جارٍ الحفظ";

                var user = new UserWriteDto
                {
                    Name = FullName.Text.Trim(),
                    PhoneNumber = PhoneNumber.Text.Trim(),
                    Password = Password.Text,
                    Role = (UserRole)Role.SelectedItem
                };

                var result = await _userService.CreateAsync(user);
                if (!result.Success)
                {
                    CreateStatusText.Text = "فشل";
                    MessageBox.Show(result.Message);
                    return;
                }

                CreateStatusText.Text = "تم";
                MessageBox.Show("تمت إضافة المستخدم بنجاح.");
                FullName.Text = "";
                PhoneNumber.Text = "";
                Password.Text = "";
                ConfirmPassword.Text = "";
                Role.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                CreateStatusText.Text = "فشل";
                MessageBox.Show($"حدث خطأ غير متوقع:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

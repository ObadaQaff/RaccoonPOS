using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System;
using System.Windows;

namespace RaccoonWarehouse
{
    public partial class CreateUser : Window
    {
        private readonly IUserService _userService;
        private readonly IUserSession _userSession;
        private readonly IPermissionService _permissionService;

        public CreateUser(IUserService userService, IUserSession userSession, IPermissionService permissionService)
        {
            _userService = userService;
            _userSession = userSession;
            _permissionService = permissionService;
            InitializeComponent();
            UiText.ApplyWindow(this);

            Role.ItemsSource = Enum.GetValues(typeof(UserRole));
            Role.SelectedIndex = 0;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var role = _userSession.CurrentUser?.Role;
                if (!role.HasValue || !await _permissionService.HasPermissionAsync(role.Value, "Users.Create"))
                {
                    MessageBox.Show(UiText.T("ليس لديك صلاحية إنشاء مستخدم جديد.", "You do not have permission to create a new user."));
                    return;
                }

                if (string.IsNullOrWhiteSpace(FullName.Text) || string.IsNullOrWhiteSpace(Password.Text))
                {
                    MessageBox.Show(UiText.T("الرجاء تعبئة الحقول المطلوبة.", "Please fill in the required fields."));
                    return;
                }

                if (Password.Text != ConfirmPassword.Text)
                {
                    MessageBox.Show(UiText.T("تأكيد كلمة المرور غير مطابق.", "Password confirmation does not match."));
                    return;
                }

                CreateStatusText.Text = UiText.T("جارٍ الحفظ", "Saving");

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
                    CreateStatusText.Text = UiText.T("فشل", "Failed");
                    MessageBox.Show(result.Message);
                    return;
                }

                CreateStatusText.Text = UiText.T("تم", "Done");
                MessageBox.Show(UiText.T("تمت إضافة المستخدم بنجاح.", "User added successfully."));
                FullName.Text = "";
                PhoneNumber.Text = "";
                Password.Text = "";
                ConfirmPassword.Text = "";
                Role.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                CreateStatusText.Text = UiText.T("فشل", "Failed");
                MessageBox.Show(
                    $"{UiText.T("حدث خطأ غير متوقع", "An unexpected error occurred")}:\n{ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

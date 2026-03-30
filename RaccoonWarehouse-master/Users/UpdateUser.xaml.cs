using AutoMapper;
using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System.Threading.Tasks;
using System.Windows;

namespace RaccoonWarehouse
{
    public partial class UpdateUser : Window
    {
        private UserWriteDto _user;
        private readonly IUserService _userService;
        private readonly IUserSession _userSession;
        private readonly IPermissionService _permissionService;
        public int UserId { get; private set; }

        public UpdateUser(IUserService userService, IMapper mapper, IUserSession userSession, IPermissionService permissionService)
        {
            _userService = userService;
            _userSession = userSession;
            _permissionService = permissionService;
            InitializeComponent();
            UiText.ApplyWindow(this);
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
                MessageBox.Show(UiText.T("المستخدم غير موجود.", "The user was not found."));
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
            var currentRole = _userSession.CurrentUser?.Role;
            if (!currentRole.HasValue || !await _permissionService.HasPermissionAsync(currentRole.Value, "Users.Edit"))
            {
                MessageBox.Show(UiText.T("ليس لديك صلاحية تعديل المستخدمين.", "You do not have permission to edit users."));
                return;
            }

            if (string.IsNullOrWhiteSpace(FullName.Text) || string.IsNullOrWhiteSpace(Password.Text))
            {
                MessageBox.Show(UiText.T("الرجاء تعبئة الاسم وكلمة المرور على الأقل.", "Please fill in at least the name and password."));
                return;
            }

            if (Password.Text != ConfirmPassword.Text)
            {
                MessageBox.Show(UiText.T("تأكيد كلمة المرور غير مطابق.", "Password confirmation does not match."));
                ConfirmPassword.Focus();
                return;
            }

            if (Role.SelectedItem is not UserRole selectedRole)
            {
                MessageBox.Show(UiText.T("الرجاء اختيار نوع الحساب.", "Please choose an account type."));
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

            MessageBox.Show(UiText.T("تم تحديث البيانات بنجاح", "The user was updated successfully."));
            Close();
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

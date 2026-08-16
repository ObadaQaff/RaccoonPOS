using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System;
using System.Globalization;
using System.Windows;

namespace RaccoonWarehouse
{
    public partial class CreateUser : Window
    {
        private readonly IUserService _userService;
        private readonly IUserSession _userSession;
        private readonly IPermissionService _permissionService;
        private bool _customerQuickCreateMode;

        public int? CreatedUserId { get; private set; }

        public CreateUser(IUserService userService, IUserSession userSession, IPermissionService permissionService)
        {
            _userService = userService;
            _userSession = userSession;
            _permissionService = permissionService;
            InitializeComponent();
            UiText.ApplyWindow(this);

            Role.ItemsSource = Enum.GetValues(typeof(UserRole));
            Role.SelectedIndex = 0;
            CreditStatus.ItemsSource = Enum.GetValues(typeof(RaccoonWarehouse.Domain.Enums.CreditStatus));
            CreditStatus.SelectedItem = RaccoonWarehouse.Domain.Enums.CreditStatus.Active;
            CurrentBalance.Text = "0.00";
        }

        public void InitializeForCustomerQuickCreate(string? initialName = null, string? initialPhone = null)
        {
            _customerQuickCreateMode = true;
            Role.SelectedItem = UserRole.Customer;
            Role.IsEnabled = false;
            PasswordPanel.Visibility = Visibility.Collapsed;
            ConfirmPasswordPanel.Visibility = Visibility.Collapsed;
            RolePanel.Visibility = Visibility.Collapsed;
            Title = UiText.T("Ø¥Ø¶Ø§ÙØ© Ø¹Ù…ÙŠÙ„ Ø¬Ø¯ÙŠØ¯", "Add New Customer");
            CreateBtn.Content = UiText.T("Ø¥Ø¶Ø§ÙØ© Ø§Ù„Ø¹Ù…ÙŠÙ„", "Add Customer");
            FormHintText.Text = UiText.T(
                "Ø£Ø¯Ø®Ù„ Ø§Ø³Ù… Ø§Ù„Ø¹Ù…ÙŠÙ„ ÙˆØ¨ÙŠØ§Ù†Ø§ØªÙ‡ Ø§Ù„Ø¨Ù†ÙƒÙŠØ© Ø«Ù… Ø§Ø­ÙØ¸Ù‡ ÙƒØ¹Ù…ÙŠÙ„ Ø¬Ø¯ÙŠØ¯.",
                "Enter the customer name and bank details, then save the new customer.");

            if (!string.IsNullOrWhiteSpace(initialName))
                FullName.Text = initialName.Trim();

            if (!string.IsNullOrWhiteSpace(initialPhone))
                PhoneNumber.Text = initialPhone.Trim();

            FullName.Focus();
        }

        public void InitializeForEmployeeCreate()
        {
            _customerQuickCreateMode = false;
            Role.SelectedItem = UserRole.Casher;
            Role.IsEnabled = true;
            PasswordPanel.Visibility = Visibility.Visible;
            ConfirmPasswordPanel.Visibility = Visibility.Visible;
            RolePanel.Visibility = Visibility.Visible;
            Title = UiText.T("Ø¥Ø¶Ø§ÙØ© Ù…ÙˆØ¸Ù Ø¬Ø¯ÙŠØ¯", "Add New Staff User");
            CreateBtn.Content = UiText.T("Ø¥Ø¶Ø§ÙØ© Ø§Ù„Ù…ÙˆØ¸Ù", "Add Staff User");
            FormHintText.Text = UiText.T(
                "Ø£Ø¯Ø®Ù„ Ø¨ÙŠØ§Ù†Ø§Øª Ø§Ù„Ù…ÙˆØ¸Ù Ø«Ù… Ø§Ø®ØªØ± Ø§Ù„Ø¯ÙˆØ± Ø§Ù„Ù…Ù†Ø§Ø³Ø¨ ÙˆØ­ÙØ¸ Ø§Ù„Ø­Ø³Ø§Ø¨.",
                "Enter the staff details, choose the appropriate role, and save the account.");
            FullName.Focus();
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

                if (string.IsNullOrWhiteSpace(FullName.Text))
                {
                    MessageBox.Show(UiText.T("الرجاء تعبئة الحقول المطلوبة.", "Please fill in the required fields."));
                    return;
                }

                var selectedRole = Role.SelectedItem is UserRole roleValue ? roleValue : UserRole.Customer;
                var isCustomer = _customerQuickCreateMode || selectedRole == UserRole.Customer;

                if (!isCustomer && string.IsNullOrWhiteSpace(Password.Text))
                {
                    MessageBox.Show(UiText.T("الرجاء تعبئة كلمة المرور.", "Please enter the password."));
                    return;
                }

                if (!isCustomer && Password.Text != ConfirmPassword.Text)
                {
                    MessageBox.Show(UiText.T("تأكيد كلمة المرور غير مطابق.", "Password confirmation does not match."));
                    return;
                }

                var password = Password.Text;
                if (isCustomer && string.IsNullOrWhiteSpace(password))
                {
                    password = $"cust-{Guid.NewGuid():N}";
                }

                CreateStatusText.Text = UiText.T("جارٍ الحفظ", "Saving");

                var user = new UserWriteDto
                {
                    Name = FullName.Text.Trim(),
                    PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber.Text) ? null : PhoneNumber.Text.Trim(),
                    Password = password,
                    Role = isCustomer ? UserRole.Customer : selectedRole,
                    BankName = string.IsNullOrWhiteSpace(BankName.Text) ? null : BankName.Text.Trim(),
                    BankAccountNumber = string.IsNullOrWhiteSpace(BankAccountNumber.Text) ? null : BankAccountNumber.Text.Trim(),
                    BankIban = string.IsNullOrWhiteSpace(BankIban.Text) ? null : BankIban.Text.Trim(),
                    BankSwiftCode = string.IsNullOrWhiteSpace(BankSwiftCode.Text) ? null : BankSwiftCode.Text.Trim(),
                    CreditLimit = decimal.TryParse(CreditLimit.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var creditLimit)
                        ? creditLimit
                        : 0m,
                    CreditDays = int.TryParse(CreditDays.Text, out var creditDays) ? creditDays : 0,
                    OpeningBalance = 0m,
                    CurrentBalance = 0m,
                    LastPaymentDate = null,
                    CreditStatus = CreditStatus.SelectedItem is RaccoonWarehouse.Domain.Enums.CreditStatus selectedCreditStatus
                        ? selectedCreditStatus
                        : RaccoonWarehouse.Domain.Enums.CreditStatus.Active
                };

                var result = await _userService.CreateAsync(user);
                if (!result.Success)
                {
                    CreateStatusText.Text = UiText.T("فشل", "Failed");
                    MessageBox.Show(result.Message);
                    return;
                }

                CreatedUserId = result.Data?.Id;
                CreateStatusText.Text = UiText.T("تم", "Done");
                MessageBox.Show(UiText.T("تمت إضافة المستخدم بنجاح.", "User added successfully."));

                if (_customerQuickCreateMode)
                {
                    DialogResult = true;
                    Close();
                    return;
                }

                FullName.Text = "";
                PhoneNumber.Text = "";
                Password.Text = "";
                ConfirmPassword.Text = "";
                Role.SelectedIndex = 0;
                BankName.Text = "";
                BankAccountNumber.Text = "";
                BankIban.Text = "";
                BankSwiftCode.Text = "";
                CreditLimit.Text = "";
                CreditDays.Text = "";
                CurrentBalance.Text = "0.00";
                CreditStatus.SelectedItem = RaccoonWarehouse.Domain.Enums.CreditStatus.Active;
                CreatedUserId = null;
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

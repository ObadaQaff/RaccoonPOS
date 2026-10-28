using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse
{
    public partial class CreateUser : Window
    {
        private readonly IUserService _userService;
        private readonly IUserSession _userSession;
        private readonly ILoadingService _loadingService;
        private bool _customerQuickCreateMode;
        private UserRole? _quickCreateRole;

        private static readonly UserRole[] QuickCreateRoles =
        {
            UserRole.Customer,
            UserRole.Supplier,
            UserRole.Doctor,
            UserRole.Pharmacy,
            UserRole.PharmaceuticalWarehouse
        };

        public int? CreatedUserId { get; private set; }

        public CreateUser(IUserService userService, IUserSession userSession, ILoadingService loadingService)
        {
            _userService = userService;
            _userSession = userSession;
            _loadingService = loadingService;

            InitializeComponent();
            UiText.ApplyWindow(this);
            CreateBtn.Content = UiText.T("إنشاء الحساب", "Create Account");

            Role.ItemsSource = Enum.GetValues(typeof(UserRole));
            Role.SelectedIndex = 0;
            CreditStatus.ItemsSource = Enum.GetValues(typeof(RaccoonWarehouse.Domain.Enums.CreditStatus));
            CreditStatus.SelectedItem = RaccoonWarehouse.Domain.Enums.CreditStatus.Active;
            CurrentBalance.Text = "0.00";
        }

        public void InitializeForCustomerQuickCreate(string? initialName = null, string? initialPhone = null)
        {
            InitializeForAllowedQuickCreate(UserRole.Customer, initialName, initialPhone);
        }

        public void InitializeForAllowedQuickCreate(UserRole initialRole = UserRole.Customer, string? initialName = null, string? initialPhone = null)
        {
            _customerQuickCreateMode = true;
            _quickCreateRole = QuickCreateRoles.Contains(initialRole) ? initialRole : UserRole.Customer;
            Role.SelectedItem = _quickCreateRole.Value;
            Role.IsEnabled = false;
            Role.Visibility = Visibility.Collapsed;
            QuickRolePanel.Visibility = Visibility.Visible;
            SetQuickRoleSelection(_quickCreateRole.Value);
            PasswordPanel.Visibility = Visibility.Collapsed;
            ConfirmPasswordPanel.Visibility = Visibility.Collapsed;
            BankDetailsPanel.Visibility = Visibility.Collapsed;
            CreditDetailsPanel.Visibility = Visibility.Collapsed;
            RolePanel.Visibility = Visibility.Visible;
            Title = UiText.T("إضافة مستخدم جديد", "Add New User");
            CreateBtn.Content = UiText.T("إضافة المستخدم", "Add User");
            FormHintText.Text = UiText.T(
                "أدخل اسم المستخدم ورقم الهاتف واختر نوع الحساب ثم احفظه.",
                "Enter the user name and phone number, choose the account type, and save it.");

            if (!string.IsNullOrWhiteSpace(initialName))
                FullName.Text = initialName.Trim();

            if (!string.IsNullOrWhiteSpace(initialPhone))
                PhoneNumber.Text = initialPhone.Trim();

            FullName.Focus();
        }

        public void InitializeForSupplierQuickCreate(string? initialName = null, string? initialPhone = null)
        {
            InitializeForAllowedQuickCreate(UserRole.Supplier, initialName, initialPhone);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var loadingShown = false;
            try
            {if (string.IsNullOrWhiteSpace(FullName.Text))
                {
                    MessageBox.Show(UiText.T("الرجاء تعبئة الحقول المطلوبة.", "Please fill in the required fields."));
                    return;
                }

                var selectedRole = _customerQuickCreateMode
                    ? GetSelectedQuickRole()
                    : Role.SelectedItem is UserRole roleValue ? roleValue : UserRole.Customer;

                var isCustomer = selectedRole == UserRole.Customer;
                var isQuickCreate = _quickCreateRole.HasValue;

                if (isQuickCreate && !QuickCreateRoles.Contains(selectedRole))
                {
                    MessageBox.Show(
                        UiText.T("نوع الحساب المختار غير مسموح به من هذه الشاشة.", "The selected account type is not allowed from this screen."),
                        UiText.T("تنبيه", "Notice"));
                    return;
                }

                if (!isQuickCreate && !isCustomer && string.IsNullOrWhiteSpace(Password.Text))
                {
                    MessageBox.Show(UiText.T("الرجاء تعبئة كلمة المرور.", "Please enter the password."));
                    return;
                }

                if (!isQuickCreate && !isCustomer && Password.Text != ConfirmPassword.Text)
                {
                    MessageBox.Show(UiText.T("تأكيد كلمة المرور غير مطابق.", "Password confirmation does not match."));
                    return;
                }

                var password = Password.Text;
                if (isQuickCreate && string.IsNullOrWhiteSpace(password))
                {
                    password = $"cust-{Guid.NewGuid():N}";
                }

                CreateStatusText.Text = UiText.T("جارٍ الحفظ", "Saving");

                CreateBtn.IsEnabled = false;
                _loadingService.Show();
                loadingShown = true;

                var user = new UserWriteDto
                {
                    Name = FullName.Text.Trim(),
                    PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber.Text) ? null : PhoneNumber.Text.Trim(),
                    Password = password,
                    Role = selectedRole,
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
                    if (loadingShown)
                    {
                        _loadingService.Hide();
                        loadingShown = false;
                    }
                    MessageBox.Show(result.Message);
                    return;
                }

                CreatedUserId = result.Data?.Id;
                if (loadingShown)
                {
                    _loadingService.Hide();
                    loadingShown = false;
                }
                CreateStatusText.Text = UiText.T("تم", "Done");
                MessageBox.Show(UiText.T("تمت إضافة المستخدم بنجاح.", "User added successfully."));
                if (_quickCreateRole.HasValue)
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
            finally
            {
                if (loadingShown)
                    _loadingService.Hide();
                CreateBtn.IsEnabled = true;
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private UserRole GetSelectedQuickRole()
        {
            var selected = new[]
            {
                CustomerRoleRadio,
                SupplierRoleRadio,
                DoctorRoleRadio,
                PharmacyRoleRadio,
                WarehouseRoleRadio
            }.FirstOrDefault(radio => radio.IsChecked == true);

            return selected?.Tag is string roleText &&
                   Enum.TryParse<UserRole>(roleText, out var role)
                ? role
                : UserRole.Customer;
        }

        private void SetQuickRoleSelection(UserRole role)
        {
            CustomerRoleRadio.IsChecked = role == UserRole.Customer;
            SupplierRoleRadio.IsChecked = role == UserRole.Supplier;
            DoctorRoleRadio.IsChecked = role == UserRole.Doctor;
            PharmacyRoleRadio.IsChecked = role == UserRole.Pharmacy;
            WarehouseRoleRadio.IsChecked = role == UserRole.PharmaceuticalWarehouse;
        }
    }
}

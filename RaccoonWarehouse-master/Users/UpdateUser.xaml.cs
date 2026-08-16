using AutoMapper;
using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System.Globalization;
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
        private readonly ILoadingService _loadingService;

        public int UserId { get; private set; }

        public UpdateUser(
            IUserService userService,
            IMapper mapper,
            IUserSession userSession,
            IPermissionService permissionService,
            ILoadingService loadingService)
        {
            _userService = userService;
            _userSession = userSession;
            _permissionService = permissionService;
            _loadingService = loadingService;

            InitializeComponent();
            UiText.ApplyWindow(this);
            _user = new UserWriteDto();
            CreditStatus.ItemsSource = Enum.GetValues(typeof(RaccoonWarehouse.Domain.Enums.CreditStatus));
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
            _loadingService.Show();
            try
            {
                var result = await _userService.GetWriteDtoByIdAsync(userId);
                if (!result.Success || result.Data == null)
                {
                    MessageBox.Show(UiText.T("The user was not found.", "The user was not found."));
                    Close();
                    return;
                }

                _user = result.Data;
                FullName.Text = _user.Name;
                PhoneNumber.Text = _user.PhoneNumber;
                Password.Text = _user.Password;
                ConfirmPassword.Text = _user.Password;
                BankName.Text = _user.BankName;
                BankAccountNumber.Text = _user.BankAccountNumber;
                BankIban.Text = _user.BankIban;
                BankSwiftCode.Text = _user.BankSwiftCode;
                CreditLimit.Text = _user.CreditLimit.ToString("0.00");
                CreditDays.Text = _user.CreditDays.ToString();
                CurrentBalance.Text = _user.CurrentBalance.ToString("0.00");
                CreditStatus.SelectedItem = _user.CreditStatus;
                Role.ItemsSource = Enum.GetValues(typeof(UserRole));
                Role.SelectedItem = _user.Role;
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private async void Update_User(object sender, RoutedEventArgs e)
        {
            _loadingService.Show();
            try
            {
                var currentRole = _userSession.CurrentUser?.Role;
                if (!currentRole.HasValue || !await _permissionService.HasPermissionAsync(currentRole.Value, "Users.Edit"))
                {
                    MessageBox.Show(UiText.T("You do not have permission to edit users.", "You do not have permission to edit users."));
                    return;
                }

                if (string.IsNullOrWhiteSpace(FullName.Text) || string.IsNullOrWhiteSpace(Password.Text))
                {
                    MessageBox.Show(UiText.T("Please fill in at least the name and password.", "Please fill in at least the name and password."));
                    return;
                }

                if (Password.Text != ConfirmPassword.Text)
                {
                    MessageBox.Show(UiText.T("Password confirmation does not match.", "Password confirmation does not match."));
                    ConfirmPassword.Focus();
                    return;
                }

                if (Role.SelectedItem is not UserRole selectedRole)
                {
                    MessageBox.Show(UiText.T("Please choose an account type.", "Please choose an account type."));
                    return;
                }

                _user.Name = FullName.Text.Trim();
                _user.PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber.Text) ? null : PhoneNumber.Text.Trim();
                _user.Password = Password.Text;
                _user.Role = selectedRole;
                _user.BankName = string.IsNullOrWhiteSpace(BankName.Text) ? null : BankName.Text.Trim();
                _user.BankAccountNumber = string.IsNullOrWhiteSpace(BankAccountNumber.Text) ? null : BankAccountNumber.Text.Trim();
                _user.BankIban = string.IsNullOrWhiteSpace(BankIban.Text) ? null : BankIban.Text.Trim();
                _user.BankSwiftCode = string.IsNullOrWhiteSpace(BankSwiftCode.Text) ? null : BankSwiftCode.Text.Trim();
                _user.CreditLimit = decimal.TryParse(CreditLimit.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var creditLimit)
                    ? creditLimit
                    : 0m;
                _user.CreditDays = int.TryParse(CreditDays.Text, out var creditDays) ? creditDays : 0;
                if (CreditStatus.SelectedItem is RaccoonWarehouse.Domain.Enums.CreditStatus selectedCreditStatus)
                {
                    _user.CreditStatus = selectedCreditStatus;
                }

                var result = await _userService.UpdateAsync(_user);
                if (!result.Success)
                {
                    _loadingService.Hide();
                    MessageBox.Show(result.Message);
                    return;
                }

                _loadingService.Hide();
                MessageBox.Show(UiText.T("The user was updated successfully.", "The user was updated successfully."));
                Close();
            }
            catch (System.Exception ex)
            {
                _loadingService.Hide();
                MessageBox.Show(ex.Message);
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            _loadingService.Show();
            _loadingService.Hide();
            Close();
        }
    }
}

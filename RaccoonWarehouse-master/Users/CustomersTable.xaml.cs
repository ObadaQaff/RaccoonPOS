using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace RaccoonWarehouse
{
    public partial class CustomersTable : Window
    {
        private sealed class FilterOption
        {
            public string Key { get; init; } = string.Empty;
            public string Display { get; init; } = string.Empty;
        }

        private readonly IUserService _userService;
        private readonly IUserSession _userSession;
        private readonly IPermissionService _permissionService;
        private bool _isLoaded;
        private readonly List<UserReadDto> _customers = new();
        private ICollectionView? _customersView;
        private UserReadDto? _selectedCustomer;

        public CustomersTable(IUserService userService, IUserSession userSession, IPermissionService permissionService)
        {
            _userService = userService;
            _userSession = userSession;
            _permissionService = permissionService;

            InitializeComponent();
            UiText.ApplyWindow(this);
            InitializeFilters();

            Loaded += async (_, _) =>
            {
                if (_isLoaded)
                    return;

                _isLoaded = true;
                await ConfigurePermissionsUiAsync();
                LoadCustomers();
            };
        }

        private void InitializeFilters()
        {
            CreditStatusFilter.ItemsSource = new[]
            {
                new FilterOption { Key = "all", Display = UiText.T("الكل", "All") },
                new FilterOption { Key = "active", Display = UiText.T("نشط", "Active") },
                new FilterOption { Key = "warning", Display = UiText.T("تنبيه", "Warning") },
                new FilterOption { Key = "blocked", Display = UiText.T("موقوف", "Blocked") },
                new FilterOption { Key = "suspended", Display = UiText.T("معلق", "Suspended") }
            };
            CreditStatusFilter.DisplayMemberPath = nameof(FilterOption.Display);
            CreditStatusFilter.SelectedIndex = 0;

            BalanceFilter.ItemsSource = new[]
            {
                new FilterOption { Key = "all", Display = UiText.T("الكل", "All") },
                new FilterOption { Key = "debt", Display = UiText.T("مدين", "In debt") },
                new FilterOption { Key = "credit", Display = UiText.T("له رصيد", "In credit") },
                new FilterOption { Key = "settled", Display = UiText.T("مسدد", "Settled") }
            };
            BalanceFilter.DisplayMemberPath = nameof(FilterOption.Display);
            BalanceFilter.SelectedIndex = 0;

            HintText.Text = UiText.T("يمكنك البحث أو التصفية حسب حالة الائتمان أو الرصيد.", "You can search or filter by credit status and balance.");
        }

        private async Task ConfigurePermissionsUiAsync()
        {
            var role = _userSession.CurrentUser?.Role;
            var canCreate = role.HasValue && await _permissionService.HasPermissionAsync(role.Value, "Users.Create");

            CreateCustomerBtn.Visibility = canCreate ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void LoadCustomers()
        {
            var role = _userSession.CurrentUser?.Role;
            if (!role.HasValue || !await _permissionService.HasPermissionAsync(role.Value, "Users.View"))
            {
                MessageBox.Show(UiText.T("ليس لديك صلاحية عرض الزبائن.", "You do not have permission to view customers."));
                Close();
                return;
            }

            var customers = await _userService.GetAllAsync();
            _customers.Clear();

            if (customers.Data != null)
                _customers.AddRange(customers.Data.Where(x => x.Role == UserRole.Customer));

            _customersView = CollectionViewSource.GetDefaultView(_customers);
            _customersView.Filter = FilterCustomers;
            CustomersGrid.ItemsSource = _customersView;
            if (CustomersGrid.SelectedItem is not UserReadDto && _customers.Count > 0)
                CustomersGrid.SelectedIndex = 0;
            UpdateSelectedCustomerDetails(CustomersGrid.SelectedItem as UserReadDto);
            UpdateCounters();
        }

        private async void CreateCustomerBtn_Click(object sender, RoutedEventArgs e)
        {
            var role = _userSession.CurrentUser?.Role;
            if (!role.HasValue || !await _permissionService.HasPermissionAsync(role.Value, "Users.Create"))
            {
                MessageBox.Show(UiText.T("ليس لديك صلاحية إنشاء زبون جديد.", "You do not have permission to create a new customer."));
                return;
            }

            WindowManager.ShowDialog<CreateUser>(WindowSizeType.SmallSquare, window => window.InitializeForCustomerQuickCreate());
            LoadCustomers();
        }

        private async void ViewStatement_Click(object sender, RoutedEventArgs e)
        {
            var role = _userSession.CurrentUser?.Role;
            if (!role.HasValue || !await _permissionService.HasPermissionAsync(role.Value, "Users.View"))
            {
                MessageBox.Show(UiText.T("ليس لديك صلاحية عرض الحسابات.", "You do not have permission to view statements."));
                return;
            }

            if (CustomersGrid.SelectedItem is not UserReadDto selectedCustomer)
            {
                MessageBox.Show(UiText.T("يجب تحديد زبون قبل عرض كشف الحساب.", "You must select a customer before viewing the statement."));
                return;
            }

            WindowManager.ShowDialog<UserStatementWindow>(WindowSizeType.LargeRectangle, window => window.Initialize(selectedCustomer.Id));
        }

        private async void UpdateCustomer_Click(object sender, RoutedEventArgs e)
        {
            var role = _userSession.CurrentUser?.Role;
            if (!role.HasValue || !await _permissionService.HasPermissionAsync(role.Value, "Users.Edit"))
            {
                MessageBox.Show(UiText.T("ليس لديك صلاحية تعديل الزبائن.", "You do not have permission to edit customers."));
                return;
            }

            if (CustomersGrid.SelectedItem is not UserReadDto selectedCustomer)
            {
                MessageBox.Show(UiText.T("يجب تحديد زبون قبل التعديل.", "You must select a customer before editing."));
                return;
            }

            WindowManager.ShowDialog<UpdateUser>(WindowSizeType.SmallSquare, async window => await window.Initialize(selectedCustomer.Id));
            LoadCustomers();
        }

        private async void DeleteCustomer_Click(object sender, RoutedEventArgs e)
        {
            var role = _userSession.CurrentUser?.Role;
            if (!role.HasValue || !await _permissionService.HasPermissionAsync(role.Value, "Users.Delete"))
            {
                MessageBox.Show(UiText.T("ليس لديك صلاحية حذف الزبائن.", "You do not have permission to delete customers."));
                return;
            }

            if (CustomersGrid.SelectedItem is not UserReadDto selectedCustomer)
            {
                MessageBox.Show(UiText.T("يجب تحديد زبون قبل الحذف.", "You must select a customer before deleting."));
                return;
            }

            var messageResult = MessageBox.Show(
                UiText.IsEnglish
                    ? $"Are you sure you want to delete customer {selectedCustomer.Name}?"
                    : $"هل أنت متأكد من حذف الزبون {selectedCustomer.Name}؟",
                UiText.T("تأكيد الحذف", "Confirm Deletion"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (messageResult != MessageBoxResult.Yes)
                return;

            await _userService.DeleteAsync(selectedCustomer.Id);
            LoadCustomers();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterChanged(sender, e);
        }

        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            _customersView?.Refresh();
            UpdateCounters();
        }

        private bool FilterCustomers(object item)
        {
            if (item is not UserReadDto customer || customer.Role != UserRole.Customer)
                return false;

            var search = SearchBox.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var matchesSearch =
                    (customer.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (customer.PhoneNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (customer.BankName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (customer.BankAccountNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (customer.BankIban?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                    || customer.Id.ToString().Contains(search, StringComparison.OrdinalIgnoreCase);

                if (!matchesSearch)
                    return false;
            }

            if (CreditStatusFilter.SelectedItem is FilterOption creditStatusFilter)
            {
                if (creditStatusFilter.Key == "active" && customer.CreditStatus != CreditStatus.Active)
                    return false;
                if (creditStatusFilter.Key == "warning" && customer.CreditStatus != CreditStatus.Warning)
                    return false;
                if (creditStatusFilter.Key == "blocked" && customer.CreditStatus != CreditStatus.Blocked)
                    return false;
                if (creditStatusFilter.Key == "suspended" && customer.CreditStatus != CreditStatus.Suspended)
                    return false;
            }

            if (BalanceFilter.SelectedItem is FilterOption balanceFilter)
            {
                if (balanceFilter.Key == "debt" && customer.CurrentBalance <= 0m)
                    return false;
                if (balanceFilter.Key == "credit" && customer.CurrentBalance >= 0m)
                    return false;
                if (balanceFilter.Key == "settled" && customer.CurrentBalance != 0m)
                    return false;
            }

            return true;
        }

        private void UpdateCounters()
        {
            var visibleCount = _customersView?.Cast<object>().Count() ?? 0;
            TotalCustomersText.Text = _customers.Count.ToString();
            VisibleCustomersText.Text = visibleCount.ToString();
            TotalCustomersCardText.Text = _customers.Count.ToString();
            ActiveCustomersCardText.Text = _customers.Count(x => x.CreditStatus == CreditStatus.Active).ToString();
            DebtCustomersCardText.Text = _customers.Count(x => x.CurrentBalance > 0m).ToString();
            CreditCustomersCardText.Text = _customers.Count(x => x.CurrentBalance < 0m).ToString();

            if (_selectedCustomer == null)
                UpdateSelectedCustomerDetails(CustomersGrid.SelectedItem as UserReadDto);
        }

        private void CustomersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedCustomer = CustomersGrid.SelectedItem as UserReadDto;
            UpdateSelectedCustomerDetails(_selectedCustomer);
        }

        private void UpdateSelectedCustomerDetails(UserReadDto? customer)
        {
            if (customer == null)
            {
                SelectedNameText.Text = UiText.T("لا يوجد عميل محدد", "No customer selected");
                SelectedStatusText.Text = "—";
                SelectedBalanceText.Text = "—";
                SelectedLimitText.Text = "—";
                SelectedPaymentText.Text = "—";
                SelectedBankText.Text = "—";
                return;
            }

            SelectedNameText.Text = customer.Name;
            SelectedStatusText.Text = customer.CreditStatus.ToString();
            SelectedBalanceText.Text = customer.CurrentBalance.ToString("N2");
            SelectedLimitText.Text = customer.CreditLimit.ToString("N2");
            SelectedPaymentText.Text = customer.LastPaymentDate?.ToString("yyyy-MM-dd hh:mm tt") ?? "—";
            SelectedBankText.Text = string.IsNullOrWhiteSpace(customer.BankName) ? "—" : customer.BankName!;
        }

        private void CustomersGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CustomersGrid.SelectedItem is UserReadDto)
                UpdateCustomer_Click(sender, e);
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

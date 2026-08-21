using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using System.Windows;

namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class CustomersDashboardActionHandler : IDashboardActionHandler
    {
        private readonly IUserSession _userSession;
        private readonly IPermissionService _permissionService;

        public CustomersDashboardActionHandler(
            IUserSession userSession,
            IPermissionService permissionService)
        {
            _userSession = userSession;
            _permissionService = permissionService;
        }

        public bool CanHandle(string actionKey)
        {
            return actionKey is "Customers.Create" or "Customers.CreateSupplier" or "Customers.List";
        }

        public async Task ExecuteAsync(string actionKey, DashboardActionContext context)
        {
            switch (actionKey)
            {
                case "Customers.Create":
                    if (!await HasPermissionAsync("Users.Create"))
                    {
                        MessageBox.Show(UiText.T("ليس لديك صلاحية إنشاء عميل جديد.", "You do not have permission to create a new customer."));
                        return;
                    }

                    WindowManager.ShowDialog<CreateUser>(WindowSizeType.SmallSquare, window => window.InitializeForCustomerQuickCreate());
                    return;

                case "Customers.CreateSupplier":
                    if (!await HasPermissionAsync("Users.Create"))
                    {
                        MessageBox.Show(UiText.T("ليس لديك صلاحية إنشاء مورد جديد.", "You do not have permission to create a new supplier."));
                        return;
                    }

                    WindowManager.ShowDialog<CreateUser>(WindowSizeType.SmallSquare, window => window.InitializeForSupplierQuickCreate());
                    return;

                case "Customers.List":
                    if (!await HasPermissionAsync("Users.View"))
                    {
                        MessageBox.Show(UiText.T("ليس لديك صلاحية عرض العملاء والموردين.", "You do not have permission to view customers and suppliers."));
                        return;
                    }

                    WindowManager.Show<CustomersTable>();
                    return;
            }
        }

        private async Task<bool> HasPermissionAsync(string permissionKey)
        {
            var role = _userSession.CurrentUser?.Role;
            return role.HasValue && await _permissionService.HasPermissionAsync(role.Value, permissionKey);
        }
    }
}

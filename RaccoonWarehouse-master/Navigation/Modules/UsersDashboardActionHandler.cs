using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Delegates;
using RaccoonWarehouse.Employees;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using System.Windows;

namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class UsersDashboardActionHandler : IDashboardActionHandler
    {
        private readonly IUserSession _userSession;
        private readonly IPermissionService _permissionService;
        private readonly IDelegateFeatureService _delegateFeatureService;
        private readonly IEmployeeFeatureService _employeeFeatureService;

        public UsersDashboardActionHandler(
            IUserSession userSession,
            IPermissionService permissionService,
            IDelegateFeatureService delegateFeatureService,
            IEmployeeFeatureService employeeFeatureService)
        {
            _userSession = userSession;
            _permissionService = permissionService;
            _delegateFeatureService = delegateFeatureService;
            _employeeFeatureService = employeeFeatureService;
        }

        public bool CanHandle(string actionKey)
        {
            return actionKey is "Users.Create" or "Users.List" or "Delegates.List" or "Employees.List";
        }

        public async Task ExecuteAsync(string actionKey, DashboardActionContext context)
        {
            switch (actionKey)
            {
                case "Users.Create":
                    if (!await HasPermissionAsync("Users.Create"))
                    {
                        MessageBox.Show(UiText.T("ليس لديك صلاحية إنشاء مستخدم جديد.", "You do not have permission to create a new user."));
                        return;
                    }

                    WindowManager.Show<CreateUser>();
                    return;
                case "Users.List":
                    if (!await HasPermissionAsync("Users.View"))
                    {
                        MessageBox.Show(UiText.T("ليس لديك صلاحية عرض المستخدمين.", "You do not have permission to view users."));
                        return;
                    }

                    WindowManager.Show<UsersTable>();
                    return;
                case "Delegates.List":
                    if (!await _delegateFeatureService.IsEnabledAsync())
                    {
                        MessageBox.Show(UiText.T("نظام المندوبين غير مفعل حالياً.", "The delegates module is currently disabled."));
                        return;
                    }

                    WindowManager.Show<DelegatesTable>();
                    return;
                case "Employees.List":
                    if (!await _employeeFeatureService.IsEnabledAsync())
                    {
                        MessageBox.Show(UiText.T("نظام الموظفين غير مفعل حالياً.", "The employees module is currently disabled."));
                        return;
                    }

                    WindowManager.Show<EmployeesTable>();
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

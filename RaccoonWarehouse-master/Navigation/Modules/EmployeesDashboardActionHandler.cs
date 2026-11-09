using RaccoonWarehouse.Application.Service.Modules;
using RaccoonWarehouse.Core.Permissions;
using RaccoonWarehouse.Core.Modules;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Employees;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using System.Windows;

namespace RaccoonWarehouse.Navigation.Modules;

public sealed class EmployeesDashboardActionHandler : IDashboardActionHandler
{
    private readonly IModuleStateStore _state;
    private readonly IPermissionService _permissions;
    public EmployeesDashboardActionHandler(IModuleStateStore state, IPermissionService permissions) { _state = state; _permissions = permissions; }
    public bool CanHandle(string actionKey) => actionKey is "Employees.List" or "Employees.Shifts";
    public Task ExecuteAsync(string actionKey, DashboardActionContext context)
    {
        if (!_state.IsEnabled(EmployeesDashboardModule.Key))
        {
            MessageBox.Show(UiText.T("وحدة الموظفين غير مفعلة حالياً.", "Employees module is currently disabled."));
            return Task.CompletedTask;
        }

        var hasAccess = actionKey == "Employees.Shifts"
            ? _permissions.HasPermission("Employees.ShiftManagement", PermissionLevel.View)
                || _permissions.HasPermission("Shifts", PermissionLevel.View)
            : _permissions.HasPermission("Employees.EmployeeProfiles", PermissionLevel.View)
                || _permissions.HasPermission("Employees", PermissionLevel.View);

        if (!hasAccess)
        {
            MessageBox.Show(UiText.T("لا تملك صلاحية الوصول إلى هذه المنطقة.", "You do not have permission to access this area."));
            return Task.CompletedTask;
        }

        if (actionKey == "Employees.List") WindowManager.Show<EmployeesTable>();
        else WindowManager.Show<ShiftManagementWindow>();
        return Task.CompletedTask;
    }
}

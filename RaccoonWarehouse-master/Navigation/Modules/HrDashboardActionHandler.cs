using RaccoonWarehouse.Application.Service.Modules;
using RaccoonWarehouse.Core.Modules;
using RaccoonWarehouse.Core.Permissions;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Employees;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using System.Windows;

namespace RaccoonWarehouse.Navigation.Modules;

public sealed class HrDashboardActionHandler : IDashboardActionHandler
{
    private readonly IModuleStateStore _state;
    private readonly IPermissionService _permissions;

    public HrDashboardActionHandler(IModuleStateStore state, IPermissionService permissions)
    {
        _state = state;
        _permissions = permissions;
    }

    public bool CanHandle(string actionKey) => actionKey == "HR.Dashboard";

    public Task ExecuteAsync(string actionKey, DashboardActionContext context)
    {
        if (!_state.IsEnabled(ModuleStateStore.HrModuleKey) || !_state.IsEnabled(ModuleStateStore.EmployeesModuleKey))
        {
            MessageBox.Show(UiText.T("HR module is disabled.", "HR module is disabled."));
            return Task.CompletedTask;
        }

        if (!_permissions.HasPermission("HR", PermissionLevel.View))
        {
            MessageBox.Show(UiText.T("You do not have permission to access this area.", "You do not have permission to access this area."));
            return Task.CompletedTask;
        }

        WindowManager.Show<HrDashboardWindow>(WindowSizeType.LargeRectangle);
        return Task.CompletedTask;
    }
}

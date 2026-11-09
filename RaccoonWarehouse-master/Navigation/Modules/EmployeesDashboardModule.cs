using RaccoonWarehouse.Application.Service.Modules;
using RaccoonWarehouse.Core.Modules;
using RaccoonWarehouse.Core.Permissions;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Navigation.Modules;

public sealed class EmployeesDashboardModule : IModuleDefinitionProvider
{
    public const string Key = ModuleStateStore.EmployeesModuleKey;
    private readonly IModuleStateStore _state;
    private readonly IPermissionService _permissions;
    public EmployeesDashboardModule(IModuleStateStore state, IPermissionService permissions) { _state = state; _permissions = permissions; }
    public string ModuleKey => Key;

    public Task<ModuleDefinition> GetDefinitionAsync()
    {
        if (!_state.IsEnabled(Key) || !_permissions.HasPermission("Employees", PermissionLevel.View))
            return Task.FromResult(new ModuleDefinition(Key, UiText.T("الموظفون", "Employees"), Array.Empty<ModuleGroupDefinition>()));

        return Task.FromResult(new ModuleDefinition(
            Key,
            UiText.T("الموظفون", "Employees"),
            new[]
            {
                new ModuleGroupDefinition(UiText.T("إدارة الموظفين", "Employee Management"), new[]
                {
                    new ModuleActionDefinition("Employees.List", UiText.T("قائمة الموظفين", "Employees")),
                    new ModuleActionDefinition("Employees.Shifts", UiText.T("إدارة الورديات", "Shift Management"))
                })
            }));
    }
}

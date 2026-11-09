using RaccoonWarehouse.Application.Service.Modules;
using RaccoonWarehouse.Core.Modules;
using RaccoonWarehouse.Core.Permissions;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Helpers.Localization;

namespace RaccoonWarehouse.Navigation.Modules;

public sealed class HrDashboardModule : IModuleDefinitionProvider
{
    public const string Key = ModuleStateStore.HrModuleKey;
    private readonly IModuleStateStore _state;
    private readonly IPermissionService _permissions;
    public HrDashboardModule(IModuleStateStore state, IPermissionService permissions) { _state = state; _permissions = permissions; }
    public string ModuleKey => Key;
    public Task<ModuleDefinition> GetDefinitionAsync() => Task.FromResult(new ModuleDefinition(Key, UiText.T("الموارد البشرية", "HR"), _state.IsEnabled(Key) && _permissions.HasPermission("HR", PermissionLevel.View) ? new[] { new ModuleGroupDefinition(UiText.T("إدارة الموارد البشرية", "HR Management"), new[] { new ModuleActionDefinition("HR.Dashboard", UiText.T("لوحة الموارد البشرية", "HR Dashboard")) }) } : Array.Empty<ModuleGroupDefinition>()));
}

using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Core.Modules;
using RaccoonWarehouse.Helpers.Localization;

namespace RaccoonWarehouse.Navigation.Modules;

public sealed class InsuranceDashboardModule : IModuleDefinitionProvider
{
    private readonly IUserSession _userSession;
    private readonly IPermissionService _permissions;

    public InsuranceDashboardModule(IUserSession userSession, IPermissionService permissions)
    {
        _userSession = userSession;
        _permissions = permissions;
    }

    public string ModuleKey => "Insurance";

    public async Task<ModuleDefinition> GetDefinitionAsync()
    {
        var actions = new List<ModuleActionDefinition>();
        var role = _userSession.CurrentUser?.Role;
        if (role.HasValue && await _permissions.HasPermissionAsync(role.Value, "InsuranceCompanies.View"))
            actions.Add(new ModuleActionDefinition("Insurance.Companies", UiText.T("شركات التأمين", "Insurance Companies"), "InsuranceCompanies.View"));
        if (role.HasValue && await _permissions.HasPermissionAsync(role.Value, "InsuranceClaims.View"))
            actions.Add(new ModuleActionDefinition("Insurance.Claims", UiText.T("مطالبات التأمين", "Insurance Claims"), "InsuranceClaims.View"));
        if (role.HasValue && (await _permissions.HasPermissionAsync(role.Value, "InsurancePayments.View")
            || await _permissions.HasPermissionAsync(role.Value, "InsuranceReports.View")))
            actions.Add(new ModuleActionDefinition("Insurance.Settlements", UiText.T("تسويات التأمين", "Insurance Settlements"), "InsurancePayments.View"));
        if (role.HasValue && await _permissions.HasPermissionAsync(role.Value, "InsuranceContracts.View"))
            actions.Add(new ModuleActionDefinition("Insurance.Setup", UiText.T("إعدادات التأمين", "Insurance Setup"), "InsuranceContracts.View"));

        return new ModuleDefinition(
            ModuleKey,
            UiText.T("التأمين", "Insurance"),
            new[] { new ModuleGroupDefinition(UiText.T("إدارة التأمين", "Insurance Management"), actions) });
    }
}

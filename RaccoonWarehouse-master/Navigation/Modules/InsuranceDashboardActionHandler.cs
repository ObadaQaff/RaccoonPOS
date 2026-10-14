using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Insurance;
using System.Windows;

namespace RaccoonWarehouse.Navigation.Modules;

public sealed class InsuranceDashboardActionHandler : IDashboardActionHandler
{
    private readonly IUserSession _userSession;
    private readonly IPermissionService _permissions;

    public InsuranceDashboardActionHandler(IUserSession userSession, IPermissionService permissions)
    {
        _userSession = userSession;
        _permissions = permissions;
    }

    public bool CanHandle(string actionKey) => actionKey is "Insurance.Companies" or "Insurance.Claims" or "Insurance.Settlements" or "Insurance.Setup";

    public async Task ExecuteAsync(string actionKey, DashboardActionContext context)
    {
        var permission = actionKey switch
        {
            "Insurance.Companies" => "InsuranceCompanies.View",
            "Insurance.Claims" => "InsuranceClaims.View",
            "Insurance.Settlements" => "InsurancePayments.View",
            _ => "InsuranceContracts.View"
        };
        var role = _userSession.CurrentUser?.Role;
        var allowed = role.HasValue && await _permissions.HasPermissionAsync(role.Value, permission);
        if (actionKey == "Insurance.Settlements" && role.HasValue)
            allowed = allowed || await _permissions.HasPermissionAsync(role.Value, "InsuranceReports.View");
        if (!allowed)
        {
            MessageBox.Show(UiText.T("لا تملك صلاحية لهذه العملية.", "You do not have permission for this operation."));
            return;
        }

        if (actionKey == "Insurance.Companies")
            WindowManager.Show<InsuranceCompaniesWindow>();
        else if (actionKey == "Insurance.Claims")
            WindowManager.Show<InsuranceClaimsWindow>();
        else if (actionKey == "Insurance.Settlements")
            WindowManager.Show<InsuranceSettlementWindow>();
        else
            WindowManager.Show<InsuranceSetupWindow>();
    }
}

using RaccoonWarehouse.Insurance;

namespace RaccoonWarehouse.Navigation.Modules;

public sealed class InsuranceDashboardActionHandler : IDashboardActionHandler
{
public InsuranceDashboardActionHandler() { }

    public bool CanHandle(string actionKey) => actionKey is "Insurance.Companies" or "Insurance.Claims" or "Insurance.Settlements" or "Insurance.Setup";

    public async Task ExecuteAsync(string actionKey, DashboardActionContext context)
    {if (actionKey == "Insurance.Companies")
            WindowManager.Show<InsuranceCompaniesWindow>();
        else if (actionKey == "Insurance.Claims")
            WindowManager.Show<InsuranceClaimsWindow>();
        else if (actionKey == "Insurance.Settlements")
            WindowManager.Show<InsuranceSettlementWindow>();
        else
            WindowManager.Show<InsuranceSetupWindow>();
    }
}

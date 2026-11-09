using RaccoonWarehouse.Core.Modules;
using RaccoonWarehouse.Helpers.Localization;

namespace RaccoonWarehouse.Navigation.Modules;

public sealed class InsuranceDashboardModule : IModuleDefinitionProvider
{
    public string ModuleKey => "Insurance";
    public Task<ModuleDefinition> GetDefinitionAsync()
    {
        var actions = new[]
        {
            new ModuleActionDefinition("Insurance.Companies", UiText.T("شركات التأمين", "Insurance Companies")),
            new ModuleActionDefinition("Insurance.Claims", UiText.T("مطالبات التأمين", "Insurance Claims")),
            new ModuleActionDefinition("Insurance.Settlements", UiText.T("تسويات التأمين", "Insurance Settlements")),
            new ModuleActionDefinition("Insurance.Setup", UiText.T("إعدادات التأمين", "Insurance Setup"))
        };
        return Task.FromResult(new ModuleDefinition(ModuleKey, UiText.T("التأمين", "Insurance"), new[] { new ModuleGroupDefinition(UiText.T("إدارة التأمين", "Insurance Management"), actions) }));
    }
}

using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Core.Modules;
using RaccoonWarehouse.Helpers.Localization;

namespace RaccoonWarehouse.Navigation.Modules;

public sealed class UsersDashboardModule : IModuleDefinitionProvider
{
    public const string Key = "Users";
    private readonly IDelegateFeatureService _delegateFeatureService;
    public UsersDashboardModule(IDelegateFeatureService delegateFeatureService)
    {
        _delegateFeatureService = delegateFeatureService;
    }
    public string ModuleKey => Key;
    public async Task<ModuleDefinition> GetDefinitionAsync()
    {
        var actions = new List<ModuleActionDefinition>
        {
            new("Users.Create", UiText.T("إضافة مستخدم جديد", "Create User")),
            new("Users.List", UiText.T("إستعلام او تعديل مستخدم", "Manage Users"))
        };
        if (await _delegateFeatureService.IsEnabledAsync()) actions.Add(new("Delegates.List", UiText.T("إدارة المندوبين", "Manage Delegates")));
        return new ModuleDefinition(Key, UiText.T("المستخدمون", "Users"), new[] { new ModuleGroupDefinition(UiText.T("إدارة المستخدمين", "User Management"), actions) });
    }
}

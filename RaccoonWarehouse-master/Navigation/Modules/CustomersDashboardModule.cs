using RaccoonWarehouse.Core.Modules;
using RaccoonWarehouse.Helpers.Localization;

namespace RaccoonWarehouse.Navigation.Modules;

public sealed class CustomersDashboardModule : IModuleDefinitionProvider
{
    public const string Key = "Customers";
    public string ModuleKey => Key;
    public Task<ModuleDefinition> GetDefinitionAsync()
    {
        var actions = new[]
        {
            new ModuleActionDefinition("Customers.Create", UiText.T("إضافة عميل جديد", "Add New Customer")),
            new ModuleActionDefinition("Customers.CreateSupplier", UiText.T("إضافة مورد جديد", "Add New Supplier")),
            new ModuleActionDefinition("Customers.List", UiText.T("إدارة حسابات المستخدمين", "Manage User Accounts"))
        };
        return Task.FromResult(new ModuleDefinition(Key, UiText.T("حسابات المستخدمين", "User Accounts"), new[] { new ModuleGroupDefinition(UiText.T("إدارة حسابات المستخدمين", "User Account Management"), actions) }));
    }
}

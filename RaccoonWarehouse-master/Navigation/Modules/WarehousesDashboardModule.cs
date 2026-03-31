using RaccoonWarehouse.Core.Modules;
using RaccoonWarehouse.Helpers.Localization;

namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class WarehousesDashboardModule : IModuleDefinitionProvider
    {
        public const string Key = "Warehouses";

        public string ModuleKey => Key;

        public Task<ModuleDefinition> GetDefinitionAsync()
        {
            return Task.FromResult(
                new ModuleDefinition(
                    Key,
                    UiText.T("المستودعات", "Warehouses"),
                    new[]
                    {
                        new ModuleGroupDefinition(
                            UiText.T("إدارة المستودعات", "Warehouse Management"),
                            new[]
                            {
                                new ModuleActionDefinition("Warehouses.Create", "إضافة مستودع جديد"),
                                new ModuleActionDefinition("Warehouses.List", "إستعلام او تعديل مستودع")
                            })
                    }));
        }
    }
}

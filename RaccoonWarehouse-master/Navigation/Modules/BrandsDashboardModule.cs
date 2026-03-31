using RaccoonWarehouse.Core.Modules;
using RaccoonWarehouse.Helpers.Localization;

namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class BrandsDashboardModule : IModuleDefinitionProvider
    {
        public const string Key = "Brands";

        public string ModuleKey => Key;

        public Task<ModuleDefinition> GetDefinitionAsync()
        {
            return Task.FromResult(
                new ModuleDefinition(
                    Key,
                    UiText.T("العلامات التجارية", "Brands"),
                    new[]
                    {
                        new ModuleGroupDefinition(
                            UiText.T("إدارة العلامات التجارية", "Brand Management"),
                            new[]
                            {
                                new ModuleActionDefinition("Brands.Create", "إضافة علامة تجارية جديدة"),
                                new ModuleActionDefinition("Brands.List", "إستعلام او تعديل العلامة التجارية")
                            })
                    }));
        }
    }
}

using RaccoonWarehouse.Core.Modules;
using RaccoonWarehouse.Helpers.Localization;

namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class CategoriesDashboardModule : IModuleDefinitionProvider
    {
        public const string Key = "Categories";

        public string ModuleKey => Key;

        public Task<ModuleDefinition> GetDefinitionAsync()
        {
            return Task.FromResult(
                new ModuleDefinition(
                    Key,
                    UiText.T("الفئات", "Categories"),
                    new[]
                    {
                        new ModuleGroupDefinition(
                            UiText.T("الفئات", "Categories"),
                            new[]
                            {
                                new ModuleActionDefinition("Categories.List", "إستعلام او نتعديل فئة"),
                                new ModuleActionDefinition("Categories.Create", "إضافة فئة")
                            }),
                        new ModuleGroupDefinition(
                            UiText.T("الفئات الفرعية", "Subcategories"),
                            new[]
                            {
                                new ModuleActionDefinition("SubCategories.List", "إستعلام او تعديل فئة فرعية"),
                                new ModuleActionDefinition("SubCategories.Create", "إضافة فئة فرعية")
                            })
                    }));
        }
    }
}

using RaccoonWarehouse.Core.Modules;
using RaccoonWarehouse.Helpers.Localization;

namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class ProductsDashboardModule : IModuleDefinitionProvider
    {
        public const string Key = "Products";

        public string ModuleKey => Key;

        public Task<ModuleDefinition> GetDefinitionAsync()
        {
            return Task.FromResult(
                new ModuleDefinition(
                    Key,
                    UiText.T("الأصناف", "Products"),
                    new[]
                    {
                        new ModuleGroupDefinition(
                            UiText.T("الأصناف", "Products"),
                            new[]
                            {
                                new ModuleActionDefinition("Products.Create", "بطاقةإدخال صنف"),
                                new ModuleActionDefinition("Products.List", "بحث عن صنف")
                            }),
                        new ModuleGroupDefinition(
                            UiText.T("الفئات والفئات الفرعية", "Categories and Subcategories"),
                            new[]
                            {
                                new ModuleActionDefinition("Categories.Create", UiText.T("إضافة فئة", "Add Category")),
                                new ModuleActionDefinition("Categories.List", UiText.T("استعلام أو تعديل فئة", "Search or Edit Category")),
                                new ModuleActionDefinition("SubCategories.Create", UiText.T("إضافة فئة فرعية", "Add Subcategory")),
                                new ModuleActionDefinition("SubCategories.List", UiText.T("استعلام أو تعديل فئة فرعية", "Search or Edit Subcategory"))
                            }),
                        new ModuleGroupDefinition(
                            UiText.T("العلامات التجارية", "Brands"),
                            new[]
                            {
                                new ModuleActionDefinition("Brands.Create", UiText.T("إضافة علامة تجارية", "Add Brand")),
                                new ModuleActionDefinition("Brands.List", UiText.T("استعلام أو تعديل علامة تجارية", "Search or Edit Brand"))
                            }),
                        new ModuleGroupDefinition(
                            UiText.T("التقارير", "Reports"),
                            new[]
                            {
                                new ModuleActionDefinition("Products.PriceList", "قائمة الأسعار"),
                                new ModuleActionDefinition("Products.ItemCostDetails", UiText.T("تفاصيل تكلفة الأصناف", "Item Cost Details")),
                                new ModuleActionDefinition("Products.ProfitReport", "أرباح الأصناف"),
                                new ModuleActionDefinition("Products.InactiveReport", "اصناف لم تتحرك منذ مدة"),
                                new ModuleActionDefinition("Stocks.BalancesReport", "الجرد والفرق"),
                                new ModuleActionDefinition("Stocks.LowStockReport", "بضائع تحت الحد الأدنى"),
                                new ModuleActionDefinition("Stocks.MaterialMovementsReport", "التسويات المخزنية")
                            })
                    }));
        }
    }
}

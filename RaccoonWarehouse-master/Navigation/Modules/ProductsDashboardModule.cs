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
                            UiText.T("بطاقات وأصناف", "Cards and Items"),
                            new[]
                            {
                                new ModuleActionDefinition("Products.Create", "بطاقةإدخال صنف"),
                                new ModuleActionDefinition("Products.List", "بحث عن صنف")
                            }),
                        new ModuleGroupDefinition(
                            UiText.T("الأسعار والتحليل", "Pricing and Analysis"),
                            new[]
                            {
                                new ModuleActionDefinition("Products.PriceList", "قائمة الأسعار"),
                                new ModuleActionDefinition("Products.ProfitReport", "أرباح الأصناف"),
                                new ModuleActionDefinition("Products.InactiveReport", "اصناف لم تتحرك منذ مدة")
                            }),
                        new ModuleGroupDefinition(
                            UiText.T("الرقابة المخزنية", "Inventory Control"),
                            new[]
                            {
                                new ModuleActionDefinition("Stocks.BalancesReport", "الجرد والفرق"),
                                new ModuleActionDefinition("Stocks.LowStockReport", "بضائع تحت الحد الأدنى"),
                                new ModuleActionDefinition("Stocks.MaterialMovementsReport", "التسويات المخزنية")
                            })
                    }));
        }
    }
}

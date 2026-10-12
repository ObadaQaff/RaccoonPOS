using RaccoonWarehouse.Core.Modules;
using RaccoonWarehouse.Helpers.Localization;

namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class PurchaseOrdersDashboardModule : IModuleDefinitionProvider
    {
        public string ModuleKey => "PurchaseOrders";

        public Task<ModuleDefinition> GetDefinitionAsync()
        {
            return Task.FromResult(new ModuleDefinition(
                ModuleKey,
                UiText.T("أوامر الشراء", "Purchase Orders"),
                new[]
                {
                    new ModuleGroupDefinition(
                        UiText.T("المشتريات", "Purchasing"),
                        new[]
                        {
                            new ModuleActionDefinition("PurchaseOrders.Create", UiText.T("إنشاء أمر شراء", "Create Purchase Order"), "PurchaseOrder.Create"),
                            new ModuleActionDefinition("PurchaseOrders.List", UiText.T("أوامر الشراء والاعتماد", "Purchase Orders and Approval"), "PurchaseOrder.View")
                        })
                }));
        }
    }
}

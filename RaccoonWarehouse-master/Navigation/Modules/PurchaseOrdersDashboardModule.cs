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
                            new ModuleActionDefinition("PurchaseRequests.List", UiText.T("طلبات الشراء — PR List", "PR List"), "PurchaseRequest.View"),
                            new ModuleActionDefinition("PurchaseOrders.Create", UiText.T("إنشاء أمر شراء — Create PO", "Create PO"), "PurchaseOrder.Create"),
                            new ModuleActionDefinition("PurchaseOrders.List", UiText.T("أوامر الشراء والاعتماد — PO List", "PO List"), "PurchaseOrder.View"),
                            new ModuleActionDefinition("Invoices.Purchase", UiText.T("إنشاء فاتورة مشتريات — Create PI", "Create PI"), "Invoices.Purchase")
                        }),
                    new ModuleGroupDefinition(
                        UiText.T("التقارير", "Reports"),
                        new[]
                        {
                            new ModuleActionDefinition("product-sales-average", UiText.T("متوسط مبيعات الأصناف", "Product Sales Average"), "product-sales-average"),
                            new ModuleActionDefinition("below-min-stock", UiText.T("بضائع تحت الحد الأدنى", "Below minimum stock"), "below-min-stock")
                        })
                }));
        }
    }
}

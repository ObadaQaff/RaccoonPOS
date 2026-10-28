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
                            new ModuleActionDefinition("PurchaseRequests.List", UiText.T("طلبات الشراء — PR List", "PR List")),
                            new ModuleActionDefinition("PurchaseOrders.Create", UiText.T("إنشاء أمر شراء — Create PO", "Create PO")),
                            new ModuleActionDefinition("PurchaseOrders.List", UiText.T("أوامر الشراء والاعتماد — PO List", "PO List")),
                            new ModuleActionDefinition("Invoices.Purchase", UiText.T("إنشاء فاتورة مشتريات — Create PI", "Create PI")),
                            new ModuleActionDefinition("CustomerRequests.List", UiText.T("إدارة طلبات العملاء", "Customer Requests")),
                            new ModuleActionDefinition("LostCustomers.List", UiText.T("إدارة العملاء المفقودين", "Lost Customers"))
                        }),
                    new ModuleGroupDefinition(
                        UiText.T("التقارير", "Reports"),
                        new[]
                        {
                            new ModuleActionDefinition("product-sales-average", UiText.T("متوسط مبيعات الأصناف", "Product Sales Average")),
                            new ModuleActionDefinition("below-min-stock", UiText.T("بضائع تحت الحد الأدنى", "Below minimum stock")),
                            new ModuleActionDefinition("customer-requests-report", UiText.T("تقرير طلبات العملاء", "Customer Requests"))
                            ,new ModuleActionDefinition("lost-customers-report", UiText.T("تقرير العملاء المفقودين", "Lost Customers Report"))
                        })
                }));
        }
    }
}

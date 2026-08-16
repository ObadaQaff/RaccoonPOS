using RaccoonWarehouse.Core.Modules;
using RaccoonWarehouse.Helpers.Localization;

namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class SalesDashboardModule : IModuleDefinitionProvider
    {
        public const string Key = "Sales";

        public string ModuleKey => Key;

        public Task<ModuleDefinition> GetDefinitionAsync()
        {
            return Task.FromResult(
                new ModuleDefinition(
                    Key,
                    UiText.T("الفواتير", "Invoices"),
                    new[]
                    {
                        new ModuleGroupDefinition(
                            UiText.T("الفواتير والمبيعات", "Invoices and Sales"),
                            new[]
                            {
                                new ModuleActionDefinition("Invoices.Sales", "فاتورة مبيعات"),
                                new ModuleActionDefinition("Invoices.SalesReturn", "مردودات المبيعات"),
                                new ModuleActionDefinition("Invoices.Purchase", "فاتورة مشتريات")
                            }),
                        new ModuleGroupDefinition(
                            UiText.T("التحصيل والدفع", "Receipts and Payments"),
                            new[]
                            {
                                new ModuleActionDefinition("Vouchers.Receipt", "سند قبض"),
                                new ModuleActionDefinition("Vouchers.Payment", "سند دفع")
                            }),
                        new ModuleGroupDefinition(
                            UiText.T("المخزون", "Inventory"),
                            new[]
                            {
                                new ModuleActionDefinition("Stocks.In", "سند ادخال بضاعة"),
                                new ModuleActionDefinition("Stocks.Out", "سند اخراج بضاعة"),
                                new ModuleActionDefinition("Stocks.Adjustment", "تسوية المخزون")
                            }),
                        new ModuleGroupDefinition(
                            UiText.T("التحليلات", "Analysis"),
                            new[]
                            {
                                new ModuleActionDefinition("Reports.Sales", "تقرير المبيعات"),
                                new ModuleActionDefinition("Reports.CreditSales", "تقرير مبيعات الآجل"),
                                new ModuleActionDefinition("Reports.InvoiceProfit", "تحليل ربحية الفواتير"),
                                new ModuleActionDefinition("Reports.ShiftSummary", "لوحة الكاشيرات")
                            }),
                        new ModuleGroupDefinition(
                            UiText.T("الطلبيات", "Orders"),
                            new[]
                            {
                                new ModuleActionDefinition("Orders.Import", "طلبية استيراد")
                            })
                    }));
        }
    }
}

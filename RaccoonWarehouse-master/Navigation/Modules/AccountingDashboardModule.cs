using RaccoonWarehouse.Core.Modules;
using RaccoonWarehouse.Helpers.Localization;

namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class AccountingDashboardModule : IModuleDefinitionProvider
    {
        public const string Key = "Accounting";

        public string ModuleKey => Key;

        public Task<ModuleDefinition> GetDefinitionAsync()
        {
            return Task.FromResult(
                new ModuleDefinition(
                    Key,
                    UiText.T("المحاسبة", "Accounting"),
                    new[]
                    {
                        new ModuleGroupDefinition(
                            UiText.T("المحاسبة", "Accounting"),
                            new[]
                            {
                                new ModuleActionDefinition("Accounting.Checks", UiText.T("الشيكات", "Checks")),
                                new ModuleActionDefinition("Accounting.Accounts", "دليل الحسابات"),
                                new ModuleActionDefinition("Accounting.JournalEntry.Create", "قيد يومية يدوي"),
                                new ModuleActionDefinition("Accounting.JournalEntries", "سجل القيود"),
                                new ModuleActionDefinition("Accounting.Operations", UiText.T("عمليات المحاسبة", "Accounting Operations")),
                                new ModuleActionDefinition("Accounting.TrialBalance", "ميزان المراجعة"),
                                new ModuleActionDefinition("Accounting.GeneralLedger", "دفتر الأستاذ"),
                                new ModuleActionDefinition("Accounting.BalanceSheet", "الميزانية العمومية")
                            }),
                        new ModuleGroupDefinition(
                            UiText.T("التقارير", "Reports"),
                            new[]
                            {
                                new ModuleActionDefinition("Accounting.CustomerDebts", UiText.T("ذمم العملاء", "Customer Debts")),
                                new ModuleActionDefinition("Accounting.SupplierPayables", UiText.T("ذمم الموردين", "Supplier Payables")),
                                new ModuleActionDefinition("Accounting.PartyBalances", UiText.T("أرصدة العملاء والموردين", "Customer and Supplier Balances"))
                            })
                    }));
        }
    }
}

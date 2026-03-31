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
                                new ModuleActionDefinition("Accounting.Accounts", "دليل الحسابات"),
                                new ModuleActionDefinition("Accounting.JournalEntry.Create", "قيد يومية يدوي"),
                                new ModuleActionDefinition("Accounting.JournalEntries", "سجل القيود"),
                                new ModuleActionDefinition("Accounting.TrialBalance", "ميزان المراجعة"),
                                new ModuleActionDefinition("Accounting.GeneralLedger", "دفتر الأستاذ"),
                                new ModuleActionDefinition("Accounting.BalanceSheet", "الميزانية العمومية")
                            })
                    }));
        }
    }
}

using RaccoonWarehouse.Core.Modules;
using RaccoonWarehouse.Helpers.Localization;

namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class SettingsDashboardModule : IModuleDefinitionProvider
    {
        public const string Key = "Settings";

        public string ModuleKey => Key;

        public Task<ModuleDefinition> GetDefinitionAsync()
        {
            return Task.FromResult(
                new ModuleDefinition(
                    Key,
                    UiText.T("الإعدادات", "Settings"),
                    new[]
                    {
                        new ModuleGroupDefinition(
                            UiText.T("إعدادات الوحدات", "Unit Settings"),
                            new[]
                            {
                                new ModuleActionDefinition("Units.Create", "إضافة وحدة جديدة"),
                                new ModuleActionDefinition("Units.List", "إستعلام او تعديل وحدة")
                            }),
                        new ModuleGroupDefinition(
                            UiText.T("إعدادات الوحدات الوظيفية", "Module Settings"),
                            new[]
                            {
                                new ModuleActionDefinition("Settings.Delegates", "إعدادات نظام المندوبين"),
                                new ModuleActionDefinition("Settings.Employees", "إعدادات نظام الموظفين"),
                                new ModuleActionDefinition("Settings.Accounting", "إعدادات نظام المحاسبة"),
                                new ModuleActionDefinition("Settings.InventoryCosting", UiText.T("طريقة تقييم المخزون", "Inventory Costing")),
                                new ModuleActionDefinition("Settings.RefundCode", UiText.T("إعدادات رمز المرتجع", "Refund Code Settings"), "Settings.ManageSettings"),
                                new ModuleActionDefinition("Settings.DocumentBranding", UiText.T("إعدادات الطباعة وهوية الصيدلية", "Printing and business identity"), "Settings.ManageSettings"),
                                new ModuleActionDefinition("Settings.JoFotara", UiText.T("إعدادات جوافوترة", "JoFotara Settings"), "Settings.ManageSettings"),
                                new ModuleActionDefinition("Settings.PosRowColors", UiText.T("ألوان صفوف نقطة البيع", "POS Row Colors")),
                                new ModuleActionDefinition("Settings.Language", UiText.T("إعدادات اللغة", "Language Settings"))
                            }),
                        new ModuleGroupDefinition(
                            UiText.T("الصلاحيات", "Permissions"),
                            new[]
                            {
                                new ModuleActionDefinition("Settings.Permissions", "مدير صلاحيات النظام"),
                                new ModuleActionDefinition("Settings.AuditLogs", UiText.T("سجل مراجعة النظام", "System audit log"), "AuditLogs.View")
                            }),
                        new ModuleGroupDefinition(
                            UiText.T("تبديل قاعدة البيانات", "Database Switching"),
                            new[]
                            {
                                new ModuleActionDefinition("Settings.SwitchDatabase", UiText.T("تبديل قاعدة البيانات", "Switch Database"))
                            })
                    }));
        }
    }
}

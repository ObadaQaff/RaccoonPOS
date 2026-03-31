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
                                new ModuleActionDefinition("Settings.Language", UiText.T("إعدادات اللغة", "Language Settings"))
                            }),
                        new ModuleGroupDefinition(
                            UiText.T("الصلاحيات", "Permissions"),
                            new[]
                            {
                                new ModuleActionDefinition("Settings.Permissions", "مدير صلاحيات النظام")
                            })
                    }));
        }
    }
}

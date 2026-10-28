using RaccoonWarehouse.Core.Localization;
using RaccoonWarehouse.Core.Modules;
using RaccoonWarehouse.Settings;

namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class ReportsDashboardModule : IModuleDefinitionProvider
    {
        public const string Key = "Reports";
        private readonly IUiTextLocalizer _textLocalizer;

        public ReportsDashboardModule(IUiTextLocalizer textLocalizer)
        {
            _textLocalizer = textLocalizer;
        }

        public string ModuleKey => Key;

        public async Task<ModuleDefinition> GetDefinitionAsync()
        {
            var groups = ReportCatalog.All
                .GroupBy(x => _textLocalizer.T(x.ArabicCategory, x.EnglishCategory))
                .Select(group => new ModuleGroupDefinition(
                    group.Key,
                    group.Select(item => new ModuleActionDefinition(
                        item.Key,
                        _textLocalizer.T(item.ArabicDisplayName, item.EnglishDisplayName))).ToArray()))
                .ToArray();

            return new ModuleDefinition(Key, "Reports", groups);
        }
    }
}

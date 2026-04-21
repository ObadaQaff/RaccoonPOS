using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Core.Localization;
using RaccoonWarehouse.Core.Modules;
using RaccoonWarehouse.Settings;

namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class ReportsDashboardModule : IModuleDefinitionProvider
    {
        public const string Key = "Reports";

        private readonly IUserSession _userSession;
        private readonly IReportPermissionService _reportPermissionService;
        private readonly IUiTextLocalizer _textLocalizer;

        public ReportsDashboardModule(
            IUserSession userSession,
            IReportPermissionService reportPermissionService,
            IUiTextLocalizer textLocalizer)
        {
            _userSession = userSession;
            _reportPermissionService = reportPermissionService;
            _textLocalizer = textLocalizer;
        }

        public string ModuleKey => Key;

        public async Task<ModuleDefinition> GetDefinitionAsync()
        {
            HashSet<string>? deniedReportKeys = null;
            var role = _userSession.CurrentUser?.Role;

            if (role != null)
            {
                deniedReportKeys = await _reportPermissionService.GetDeniedReportKeysAsync(role.Value);
            }

            var groups = ReportCatalog.All
                .Where(x => deniedReportKeys == null || !deniedReportKeys.Contains(x.Key))
                .GroupBy(x => _textLocalizer.T(x.ArabicCategory, x.EnglishCategory))
                .Select(group => new ModuleGroupDefinition(
                    group.Key,
                    group.Select(item => new ModuleActionDefinition(
                        item.Key,
                        _textLocalizer.T(item.ArabicDisplayName, item.EnglishDisplayName),
                        item.Key)).ToArray()))
                .ToArray();

            return new ModuleDefinition(Key, "Reports", groups);
        }
    }
}

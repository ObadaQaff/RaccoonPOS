using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Core.Localization;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Settings;
using System.Windows;

namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class ReportsDashboardActionHandler : IDashboardActionHandler
    {
        private readonly IUserSession _userSession;
        private readonly IReportPermissionService _reportPermissionService;
        private readonly IWindowNavigationService _windowNavigationService;
        private readonly IUiTextLocalizer _textLocalizer;

        public ReportsDashboardActionHandler(
            IUserSession userSession,
            IReportPermissionService reportPermissionService,
            IWindowNavigationService windowNavigationService,
            IUiTextLocalizer textLocalizer)
        {
            _userSession = userSession;
            _reportPermissionService = reportPermissionService;
            _windowNavigationService = windowNavigationService;
            _textLocalizer = textLocalizer;
        }

        public bool CanHandle(string actionKey)
        {
            return ReportCatalog.All.Any(x => string.Equals(x.Key, actionKey, StringComparison.Ordinal));
        }

        public async Task ExecuteAsync(string actionKey, DashboardActionContext context)
        {
            var report = ReportCatalog.All.FirstOrDefault(x => string.Equals(x.Key, actionKey, StringComparison.Ordinal));
            if (report == null)
            {
                return;
            }

            var role = _userSession.CurrentUser?.Role;
            if (role != null)
            {
                var canView = await _reportPermissionService.CanViewAsync(role.Value, report.Key);
                if (!canView)
                {
                    MessageBox.Show(_textLocalizer.T(
                        "ليس لديك صلاحية لعرض هذا التقرير.",
                        "You do not have permission to view this report."));
                    return;
                }
            }

            Action openWindow = () => _windowNavigationService.Show(report.Key, report.WindowSize);
            if (report.UseLoadingWrapper)
            {
                context.OpenReportWindow(openWindow);
                return;
            }

            openWindow();
        }
    }
}

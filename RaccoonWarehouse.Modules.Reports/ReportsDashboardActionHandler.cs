
using RaccoonWarehouse.Core.Localization;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Settings;
using System.Windows;

namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class ReportsDashboardActionHandler : IDashboardActionHandler
    {
        private readonly IWindowNavigationService _windowNavigationService;
        private readonly IUiTextLocalizer _textLocalizer;

        public ReportsDashboardActionHandler(IWindowNavigationService windowNavigationService,
            IUiTextLocalizer textLocalizer)
        {
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

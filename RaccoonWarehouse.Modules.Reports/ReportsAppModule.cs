using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Core.Modules;
using RaccoonWarehouse.Navigation.Modules;

namespace RaccoonWarehouse.Modules.Reports
{
    public sealed class ReportsAppModule : IAppModule
    {
        public string ModuleKey => ReportsDashboardModule.Key;

        public void RegisterServices(IServiceCollection services)
        {
            services.AddTransient<IModuleDefinitionProvider, ReportsDashboardModule>();
            services.AddTransient<IDashboardActionHandler, ReportsDashboardActionHandler>();
        }
    }
}

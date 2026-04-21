using Microsoft.Extensions.DependencyInjection;

namespace RaccoonWarehouse.Core.Modules
{
    public static class AppModuleServiceCollectionExtensions
    {
        public static IServiceCollection AddAppModule(this IServiceCollection services, IAppModule module)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(module);

            module.RegisterServices(services);
            services.AddSingleton(module);

            return services;
        }
    }
}

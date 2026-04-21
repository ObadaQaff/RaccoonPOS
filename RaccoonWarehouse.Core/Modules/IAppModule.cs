using Microsoft.Extensions.DependencyInjection;

namespace RaccoonWarehouse.Core.Modules
{
    public interface IAppModule
    {
        string ModuleKey { get; }

        void RegisterServices(IServiceCollection services);
    }
}

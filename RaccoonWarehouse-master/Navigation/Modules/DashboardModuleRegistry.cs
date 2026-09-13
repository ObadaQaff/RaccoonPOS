using RaccoonWarehouse.Core.Modules;

namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class DashboardModuleRegistry
    {
        private readonly IReadOnlyDictionary<string, IModuleDefinitionProvider> _providers;
        private readonly IModuleConfiguration _configuration;

        public DashboardModuleRegistry(
            IEnumerable<IModuleDefinitionProvider> providers,
            IModuleConfiguration configuration)
        {
            _providers = providers.ToDictionary(x => x.ModuleKey, StringComparer.Ordinal);
            _configuration = configuration;
        }

        public Task<ModuleDefinition> GetDefinitionAsync(string moduleKey)
        {
            if (!_providers.TryGetValue(moduleKey, out var provider))
            {
                throw new InvalidOperationException($"Dashboard module '{moduleKey}' is not registered.");
            }

            if (!_configuration.IsEnabled(moduleKey))
            {
                return Task.FromResult(new ModuleDefinition(
                    moduleKey,
                    moduleKey,
                    Array.Empty<ModuleGroupDefinition>()));
            }

            return provider.GetDefinitionAsync();
        }
    }
}

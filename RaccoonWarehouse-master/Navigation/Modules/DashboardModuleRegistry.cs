using RaccoonWarehouse.Core.Modules;

namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class DashboardModuleRegistry
    {
        private readonly IReadOnlyDictionary<string, IModuleDefinitionProvider> _providers;

        public DashboardModuleRegistry(IEnumerable<IModuleDefinitionProvider> providers)
        {
            _providers = providers.ToDictionary(x => x.ModuleKey, StringComparer.Ordinal);
        }

        public Task<ModuleDefinition> GetDefinitionAsync(string moduleKey)
        {
            if (!_providers.TryGetValue(moduleKey, out var provider))
            {
                throw new InvalidOperationException($"Dashboard module '{moduleKey}' is not registered.");
            }

            return provider.GetDefinitionAsync();
        }
    }
}

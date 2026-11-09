using RaccoonWarehouse.Core.Modules;
using RaccoonWarehouse.Core.Permissions;
using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class DashboardModuleRegistry
    {
        private readonly IReadOnlyDictionary<string, IModuleDefinitionProvider> _providers;
        private readonly IModuleConfiguration _configuration;
        private readonly IPermissionService _permissions;
        private readonly IPermissionActionResolver _actionResolver;

        public DashboardModuleRegistry(
            IEnumerable<IModuleDefinitionProvider> providers,
            IModuleConfiguration configuration,
            IPermissionService permissions,
            IPermissionActionResolver actionResolver)
        {
            _providers = providers.ToDictionary(x => x.ModuleKey, StringComparer.Ordinal);
            _configuration = configuration;
            _permissions = permissions;
            _actionResolver = actionResolver;
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

            var area = PermissionAreaRegistry.AreaForModule(moduleKey);
            if (area != null && !_permissions.HasPermission(area, PermissionLevel.View))
                return Task.FromResult(new ModuleDefinition(moduleKey, moduleKey, Array.Empty<ModuleGroupDefinition>()));

            return FilterDefinitionAsync(provider, moduleKey);
        }

        private async Task<ModuleDefinition> FilterDefinitionAsync(IModuleDefinitionProvider provider, string moduleKey)
        {
            var definition = await provider.GetDefinitionAsync();
            var groups = definition.Groups
                .Select(group => new ModuleGroupDefinition(
                    group.Title,
                    group.Actions
                        .Where(action => IsActionAllowed(action.Key))
                        .ToArray()))
                .Where(group => group.Actions.Count > 0)
                .ToArray();
            return definition with { Groups = groups };
        }

        private bool IsActionAllowed(string actionKey)
        {
            var area = _actionResolver.GetArea(actionKey);
            return area != null && _permissions.HasPermission(area, _actionResolver.GetRequiredLevel(actionKey));
        }
    }
}

using RaccoonWarehouse.Core.Permissions;

namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class DashboardActionRegistry
    {
        private readonly IReadOnlyList<IDashboardActionHandler> _handlers;
        private readonly IPermissionService _permissions;
        private readonly IPermissionActionResolver _resolver;

        public DashboardActionRegistry(
            IEnumerable<IDashboardActionHandler> handlers,
            IPermissionService permissions,
            IPermissionActionResolver resolver)
        {
            _handlers = handlers.ToList();
            _permissions = permissions;
            _resolver = resolver;
        }

        public Task ExecuteAsync(string actionKey, DashboardActionContext context)
        {
            var handler = _handlers.FirstOrDefault(x => x.CanHandle(actionKey));
            if (handler == null)
            {
                throw new InvalidOperationException($"No dashboard action handler is registered for '{actionKey}'.");
            }

            var area = _resolver.GetArea(actionKey);
            if (area != null && !_permissions.HasPermission(area, _resolver.GetRequiredLevel(actionKey)))
                throw new UnauthorizedAccessException($"Permission denied for dashboard action '{actionKey}'.");

            return handler.ExecuteAsync(actionKey, context);
        }

        public bool CanHandle(string actionKey) => _handlers.Any(x => x.CanHandle(actionKey));
    }
}

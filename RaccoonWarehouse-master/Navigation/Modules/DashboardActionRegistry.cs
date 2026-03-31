namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class DashboardActionRegistry
    {
        private readonly IReadOnlyList<IDashboardActionHandler> _handlers;

        public DashboardActionRegistry(IEnumerable<IDashboardActionHandler> handlers)
        {
            _handlers = handlers.ToList();
        }

        public Task ExecuteAsync(string actionKey, DashboardActionContext context)
        {
            var handler = _handlers.FirstOrDefault(x => x.CanHandle(actionKey));
            if (handler == null)
            {
                throw new InvalidOperationException($"No dashboard action handler is registered for '{actionKey}'.");
            }

            return handler.ExecuteAsync(actionKey, context);
        }
    }
}

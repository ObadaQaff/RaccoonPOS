namespace RaccoonWarehouse.Navigation.Modules
{
    public interface IDashboardActionHandler
    {
        bool CanHandle(string actionKey);

        Task ExecuteAsync(string actionKey, DashboardActionContext context);
    }
}

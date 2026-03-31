namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class DashboardActionContext
    {
        public required Action<Action> OpenReportWindow { get; init; }

        public required Func<Task> RefreshAccountingNavigationAsync { get; init; }
    }
}

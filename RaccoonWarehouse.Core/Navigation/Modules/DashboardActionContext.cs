namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class DashboardActionContext
    {
        public required Action<Action> OpenReportWindow { get; init; }

        public required Func<Task> RefreshAccountingNavigationAsync { get; init; }
        public Func<Task> RefreshEmployeesNavigationAsync { get; init; } = () => Task.CompletedTask;
        public Func<Task> RefreshHrNavigationAsync { get; init; } = () => Task.CompletedTask;
    }
}

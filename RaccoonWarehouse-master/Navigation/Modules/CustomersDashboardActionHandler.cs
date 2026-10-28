using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using System.Windows;

namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class CustomersDashboardActionHandler : IDashboardActionHandler
    {
        public CustomersDashboardActionHandler(
            IUserSession userSession)
        {
            }

        public bool CanHandle(string actionKey)
        {
            return actionKey is "Customers.Create" or "Customers.CreateSupplier" or "Customers.List";
        }

        public async Task ExecuteAsync(string actionKey, DashboardActionContext context)
        {
            switch (actionKey)
            {
                case "Customers.Create":WindowManager.ShowDialog<CreateUser>(WindowSizeType.MediumRectangle, window => window.InitializeForCustomerQuickCreate());
                    return;

                case "Customers.CreateSupplier":WindowManager.ShowDialog<CreateUser>(WindowSizeType.MediumRectangle, window => window.InitializeForSupplierQuickCreate());
                    return;

                case "Customers.List":WindowManager.Show<UsersTable>();
                    return;
            }
        }}
}

using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Delegates;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using System.Windows;

namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class UsersDashboardActionHandler : IDashboardActionHandler
    {
        private readonly IDelegateFeatureService _delegateFeatureService;

        public UsersDashboardActionHandler(
            IUserSession userSession,
            IDelegateFeatureService delegateFeatureService)
        {
            _delegateFeatureService = delegateFeatureService;
        }

        public bool CanHandle(string actionKey)
            => actionKey is "Users.Create" or "Users.List" or "Delegates.List";

        public async Task ExecuteAsync(string actionKey, DashboardActionContext context)
        {
            switch (actionKey)
            {
                case "Users.Create":
                    WindowManager.Show<CreateUser>();
                    return;
                case "Users.List":
                    WindowManager.Show<UsersTable>();
                    return;
                case "Delegates.List":
                    if (!await _delegateFeatureService.IsEnabledAsync())
                    {
                        MessageBox.Show(UiText.T("نظام المندوبين غير مفعل حالياً.", "The module is currently disabled."));
                        return;
                    }

                    WindowManager.Show<DelegatesTable>();
                    return;
            }
        }
    }
}

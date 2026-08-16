using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Core.Modules;
using RaccoonWarehouse.Helpers.Localization;

namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class CustomersDashboardModule : IModuleDefinitionProvider
    {
        public const string Key = "Customers";

        private readonly IUserSession _userSession;
        private readonly IPermissionService _permissionService;

        public CustomersDashboardModule(
            IUserSession userSession,
            IPermissionService permissionService)
        {
            _userSession = userSession;
            _permissionService = permissionService;
        }

        public string ModuleKey => Key;

        public async Task<ModuleDefinition> GetDefinitionAsync()
        {
            var actions = new List<ModuleActionDefinition>();
            var role = _userSession.CurrentUser?.Role;

            if (role.HasValue && await _permissionService.HasPermissionAsync(role.Value, "Users.Create"))
            {
                actions.Add(new ModuleActionDefinition("Customers.Create", UiText.T("إضافة زبون جديد", "Add New Customer"), "Users.Create"));
            }

            if (role.HasValue && await _permissionService.HasPermissionAsync(role.Value, "Users.View"))
            {
                actions.Add(new ModuleActionDefinition("Customers.List", UiText.T("إستعلام أو تعديل زبون", "Search or edit customer"), "Users.View"));
            }

            return new ModuleDefinition(
                Key,
                UiText.T("الزبائن", "Customers"),
                new[]
                {
                    new ModuleGroupDefinition(UiText.T("إدارة الزبائن", "Customer Management"), actions)
                });
        }
    }
}

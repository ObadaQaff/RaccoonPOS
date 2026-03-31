using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Core.Modules;
using RaccoonWarehouse.Helpers.Localization;

namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class UsersDashboardModule : IModuleDefinitionProvider
    {
        public const string Key = "Users";

        private readonly IUserSession _userSession;
        private readonly IPermissionService _permissionService;
        private readonly IDelegateFeatureService _delegateFeatureService;
        private readonly IEmployeeFeatureService _employeeFeatureService;

        public UsersDashboardModule(
            IUserSession userSession,
            IPermissionService permissionService,
            IDelegateFeatureService delegateFeatureService,
            IEmployeeFeatureService employeeFeatureService)
        {
            _userSession = userSession;
            _permissionService = permissionService;
            _delegateFeatureService = delegateFeatureService;
            _employeeFeatureService = employeeFeatureService;
        }

        public string ModuleKey => Key;

        public async Task<ModuleDefinition> GetDefinitionAsync()
        {
            var actions = new List<ModuleActionDefinition>();
            var role = _userSession.CurrentUser?.Role;

            if (role.HasValue && await _permissionService.HasPermissionAsync(role.Value, "Users.Create"))
            {
                actions.Add(new ModuleActionDefinition("Users.Create", "إضافة مستخدم جديد", "Users.Create"));
            }

            if (role.HasValue && await _permissionService.HasPermissionAsync(role.Value, "Users.View"))
            {
                actions.Add(new ModuleActionDefinition("Users.List", "إستعلام او تعديل مستخدم", "Users.View"));
            }

            if (await _delegateFeatureService.IsEnabledAsync())
            {
                actions.Add(new ModuleActionDefinition("Delegates.List", "إدارة المندوبين"));
            }

            if (await _employeeFeatureService.IsEnabledAsync())
            {
                actions.Add(new ModuleActionDefinition("Employees.List", "إدارة الموظفين"));
            }

            return new ModuleDefinition(
                Key,
                UiText.T("المستخدمون", "Users"),
                new[]
                {
                    new ModuleGroupDefinition(UiText.T("إدارة المستخدمين", "User Management"), actions)
                });
        }
    }
}

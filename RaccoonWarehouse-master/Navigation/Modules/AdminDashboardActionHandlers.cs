using RaccoonWarehouse.Accounting;
using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Brands;
using RaccoonWarehouse.Delegates;
using RaccoonWarehouse.Employees;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Settings;
using RaccoonWarehouse.Units;
using RaccoonWarehouse.Warehouses;
using System.Windows;

namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class WarehousesDashboardActionHandler : IDashboardActionHandler
    {
        public bool CanHandle(string actionKey) => actionKey is "Warehouses.Create" or "Warehouses.List";

        public Task ExecuteAsync(string actionKey, DashboardActionContext context)
        {
            switch (actionKey)
            {
                case "Warehouses.Create":
                    WindowManager.Show<CreateWarehouse>();
                    break;
                case "Warehouses.List":
                    WindowManager.Show<WarehousesTable>();
                    break;
            }

            return Task.CompletedTask;
        }
    }

    public sealed class BrandsDashboardActionHandler : IDashboardActionHandler
    {
        public bool CanHandle(string actionKey) => actionKey is "Brands.Create" or "Brands.List";

        public Task ExecuteAsync(string actionKey, DashboardActionContext context)
        {
            switch (actionKey)
            {
                case "Brands.Create":
                    WindowManager.Show<CreateBrand>(WindowSizeType.MediumRectangle);
                    break;
                case "Brands.List":
                    WindowManager.Show<BrandsTable>(WindowSizeType.MediumRectangle);
                    break;
            }

            return Task.CompletedTask;
        }
    }

    public sealed class SettingsDashboardActionHandler : IDashboardActionHandler
    {
        private readonly IPermissionService _permissionService;
        private readonly IUserSession _userSession;

        public SettingsDashboardActionHandler(
            IPermissionService permissionService,
            IUserSession userSession)
        {
            _permissionService = permissionService;
            _userSession = userSession;
        }

        public bool CanHandle(string actionKey)
        {
            return actionKey is
                "Units.Create" or
                "Units.List" or
                "Settings.Permissions" or
                "Settings.Delegates" or
                "Settings.Employees" or
                "Settings.Accounting" or
                "Settings.Language";
        }

        public async Task ExecuteAsync(string actionKey, DashboardActionContext context)
        {
            switch (actionKey)
            {
                case "Units.Create":
                    WindowManager.Show<CreateUnit>();
                    return;
                case "Units.List":
                    WindowManager.Show<UnitsTable>();
                    return;
                case "Settings.Permissions":
                    if (!await HasPermissionAsync("Permissions.ManageRoles"))
                    {
                        MessageBox.Show(UiText.T("ليس لديك صلاحية إدارة صلاحيات النظام.", "You do not have permission to manage system permissions."));
                        return;
                    }

                    WindowManager.Show<ReportPermissionsManager>(WindowSizeType.LargeRectangle);
                    return;
                case "Settings.Delegates":
                    if (!await HasPermissionAsync("Settings.ManageSettings"))
                    {
                        MessageBox.Show(UiText.T("ليس لديك صلاحية تعديل الإعدادات.", "You do not have permission to edit settings."));
                        return;
                    }

                    WindowManager.ShowDialog<DelegateFeatureSettingsWindow>(WindowSizeType.SmallSquare);
                    return;
                case "Settings.Employees":
                    if (!await HasPermissionAsync("Settings.ManageSettings"))
                    {
                        MessageBox.Show(UiText.T("ليس لديك صلاحية تعديل الإعدادات.", "You do not have permission to edit settings."));
                        return;
                    }

                    WindowManager.ShowDialog<EmployeeFeatureSettingsWindow>(WindowSizeType.SmallSquare);
                    return;
                case "Settings.Accounting":
                    if (!await HasPermissionAsync("Settings.ManageSettings"))
                    {
                        MessageBox.Show(UiText.T("ليس لديك صلاحية تعديل الإعدادات.", "You do not have permission to edit settings."));
                        return;
                    }

                    WindowManager.ShowDialog<AccountingFeatureSettingsWindow>(WindowSizeType.SmallSquare);
                    await context.RefreshAccountingNavigationAsync();
                    return;
                case "Settings.Language":
                    if (!await HasPermissionAsync("Settings.ManageSettings"))
                    {
                        MessageBox.Show(UiText.T("ليس لديك صلاحية تعديل الإعدادات.", "You do not have permission to edit settings."));
                        return;
                    }

                    WindowManager.ShowDialog<LanguageSettingsWindow>(WindowSizeType.SmallSquare);
                    return;
            }
        }

        private async Task<bool> HasPermissionAsync(string permissionKey)
        {
            var role = _userSession.CurrentUser?.Role;
            return role.HasValue && await _permissionService.HasPermissionAsync(role.Value, permissionKey);
        }
    }

    public sealed class AccountingDashboardActionHandler : IDashboardActionHandler
    {
        public bool CanHandle(string actionKey)
        {
            return actionKey is
                "Accounting.Checks" or
                "Accounting.Accounts" or
                "Accounting.JournalEntry.Create" or
                "Accounting.JournalEntries" or
                "Accounting.Operations" or
                "Accounting.TrialBalance" or
                "Accounting.GeneralLedger" or
                "Accounting.BalanceSheet" or
                "Accounting.CustomerDebts" or
                "Accounting.SupplierPayables";
        }

        public Task ExecuteAsync(string actionKey, DashboardActionContext context)
        {
            switch (actionKey)
            {
                case "Accounting.Checks":
                    WindowManager.Show<ChecksDashboard>(WindowSizeType.LargeRectangle);
                    break;
                case "Accounting.Accounts":
                    WindowManager.Show<AccountsTable>(WindowSizeType.LargeRectangle);
                    break;
                case "Accounting.JournalEntry.Create":
                    WindowManager.Show<CreateJournalEntry>(WindowSizeType.LargeRectangle);
                    break;
                case "Accounting.JournalEntries":
                    WindowManager.Show<JournalEntriesBrowser>(WindowSizeType.LargeRectangle);
                    break;
                case "Accounting.Operations":
                    WindowManager.Show<AccountingOperationsBrowser>(WindowSizeType.LargeRectangle);
                    break;
                case "Accounting.TrialBalance":
                    context.OpenReportWindow(() => WindowManager.Show<TrialBalanceReport>(WindowSizeType.LargeRectangle));
                    break;
                case "Accounting.GeneralLedger":
                    context.OpenReportWindow(() => WindowManager.Show<GeneralLedgerReport>(WindowSizeType.LargeRectangle));
                    break;
                case "Accounting.BalanceSheet":
                    context.OpenReportWindow(() => WindowManager.Show<BalanceSheetReport>(WindowSizeType.LargeRectangle));
                    break;
                case "Accounting.CustomerDebts":
                    context.OpenReportWindow(() => WindowManager.Show<PartyBalanceReport>(WindowSizeType.LargeRectangle, window => window.Initialize(RaccoonWarehouse.Domain.Enums.UserRole.Customer)));
                    break;
                case "Accounting.SupplierPayables":
                    context.OpenReportWindow(() => WindowManager.Show<PartyBalanceReport>(WindowSizeType.LargeRectangle, window => window.Initialize(RaccoonWarehouse.Domain.Enums.UserRole.Supplier)));
                    break;
            }

            return Task.CompletedTask;
        }
    }
}

using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Stocks;
using System.Windows;

namespace RaccoonWarehouse.Navigation.Modules;

public sealed class LotsDashboardActionHandler : IDashboardActionHandler
{
    private readonly IPermissionService _permissionService;
    private readonly IUserSession _userSession;

    public LotsDashboardActionHandler(IPermissionService permissionService, IUserSession userSession)
    {
        _permissionService = permissionService;
        _userSession = userSession;
    }

    public bool CanHandle(string actionKey) => actionKey == "Stocks.Lots";

    public async Task ExecuteAsync(string actionKey, DashboardActionContext context)
    {
        if (actionKey != "Stocks.Lots")
            return;

        var role = _userSession.CurrentUser?.Role;
        if (!role.HasValue || !await _permissionService.HasPermissionAsync(role.Value, "StockLots.View"))
        {
            MessageBox.Show(
                UiText.T("ليس لديك صلاحية عرض دفعات المخزون.", "You do not have permission to view stock lots."),
                UiText.T("الصلاحيات", "Permissions"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        WindowManager.Show<StockLotsWindow>(WindowSizeType.LargeRectangle);
    }
}

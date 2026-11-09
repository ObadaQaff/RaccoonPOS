using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Stocks;

namespace RaccoonWarehouse.Navigation.Modules;

public sealed class LotsDashboardActionHandler : IDashboardActionHandler
{
    public LotsDashboardActionHandler() { }

    public bool CanHandle(string actionKey) => actionKey == "Stocks.Lots";

    public async Task ExecuteAsync(string actionKey, DashboardActionContext context)
    {
        if (actionKey != "Stocks.Lots")
            return;WindowManager.Show<StockLotsWindow>(WindowSizeType.LargeRectangle);
    }
}

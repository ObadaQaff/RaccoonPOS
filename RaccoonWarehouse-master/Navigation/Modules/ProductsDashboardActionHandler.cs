using RaccoonWarehouse.Products;
using RaccoonWarehouse.Products.Reports;
using RaccoonWarehouse.Reports;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Stocks.Reports;

namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class ProductsDashboardActionHandler : IDashboardActionHandler
    {
        public bool CanHandle(string actionKey)
        {
            return actionKey is
                "Products.Create" or
                "Products.List" or
                "Products.PriceList" or
                "Products.ProfitReport" or
                "Products.InactiveReport" or
                "Stocks.BalancesReport" or
                "Stocks.LowStockReport" or
                "Stocks.MaterialMovementsReport";
        }

        public Task ExecuteAsync(string actionKey, DashboardActionContext context)
        {
            switch (actionKey)
            {
                case "Products.Create":
                    WindowManager.Show<CreateProduct>();
                    break;
                case "Products.List":
                    WindowManager.Show<ProductsTable>();
                    break;
                case "Products.PriceList":
                    context.OpenReportWindow(() => WindowManager.Show<PriceListReport>(WindowSizeType.LargeRectangle));
                    break;
                case "Products.ProfitReport":
                    context.OpenReportWindow(() => WindowManager.Show<ProductProfitReport>(WindowSizeType.LargeRectangle));
                    break;
                case "Products.InactiveReport":
                    context.OpenReportWindow(() => WindowManager.Show<InactiveProductsReport>(WindowSizeType.LargeRectangle));
                    break;
                case "Stocks.BalancesReport":
                    context.OpenReportWindow(() => WindowManager.Show<StockBalancesReport>(WindowSizeType.LargeRectangle));
                    break;
                case "Stocks.LowStockReport":
                    context.OpenReportWindow(() => WindowManager.Show<LowStockReport>(WindowSizeType.LargeRectangle));
                    break;
                case "Stocks.MaterialMovementsReport":
                    context.OpenReportWindow(() => WindowManager.Show<MaterialMovementsReport>(WindowSizeType.LargeRectangle));
                    break;
            }

            return Task.CompletedTask;
        }
    }
}

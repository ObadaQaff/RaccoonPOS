using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.FinancialTransactions;
using RaccoonWarehouse.FinancialTransactions.Reports;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Invoices.Reports;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Products.Reports;
using RaccoonWarehouse.Reports;
using RaccoonWarehouse.Settings;
using RaccoonWarehouse.Stocks;
using RaccoonWarehouse.Stocks.Reports;
using System.Windows;

namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class ReportsDashboardActionHandler : IDashboardActionHandler
    {
        private readonly IUserSession _userSession;
        private readonly IReportPermissionService _reportPermissionService;

        public ReportsDashboardActionHandler(
            IUserSession userSession,
            IReportPermissionService reportPermissionService)
        {
            _userSession = userSession;
            _reportPermissionService = reportPermissionService;
        }

        public bool CanHandle(string actionKey)
        {
            return ReportCatalog.All.Any(x => string.Equals(x.Key, actionKey, StringComparison.Ordinal));
        }

        public async Task ExecuteAsync(string actionKey, DashboardActionContext context)
        {
            var report = ReportCatalog.All.FirstOrDefault(x => string.Equals(x.Key, actionKey, StringComparison.Ordinal));
            var role = _userSession.CurrentUser?.Role;

            if (report != null && role != null)
            {
                var canView = await _reportPermissionService.CanViewAsync(role.Value, report.Key);
                if (!canView)
                {
                    MessageBox.Show(UiText.T("ليس لديك صلاحية لعرض هذا التقرير.", "You do not have permission to view this report."));
                    return;
                }
            }

            switch (actionKey)
            {
                case "current-stock":
                    WindowManager.Show<CurrentStock>();
                    break;
                case "stock-movements":
                    context.OpenReportWindow(() => WindowManager.Show<StockMovementsReport>(WindowSizeType.LargeRectangle));
                    break;
                case "sales-report":
                    context.OpenReportWindow(() => WindowManager.Show<SalesReport>(WindowSizeType.LargeRectangle));
                    break;
                case "credit-sales":
                    context.OpenReportWindow(() => WindowManager.Show<CreditSalesReport>(WindowSizeType.LargeRectangle));
                    break;
                case "inactive-products":
                    context.OpenReportWindow(() => WindowManager.Show<InactiveProductsReport>(WindowSizeType.LargeRectangle));
                    break;
                case "discount-summary":
                    context.OpenReportWindow(() => WindowManager.Show<DiscountSummaryReport>(WindowSizeType.LargeRectangle));
                    break;
                case "item-cost-detail":
                    context.OpenReportWindow(() => WindowManager.Show<ItemCostDetailReport>(WindowSizeType.LargeRectangle));
                    break;
                case "price-list":
                    context.OpenReportWindow(() => WindowManager.Show<PriceListReport>(WindowSizeType.LargeRectangle));
                    break;
                case "below-min-stock":
                    context.OpenReportWindow(() => WindowManager.Show<LowStockReport>(WindowSizeType.LargeRectangle));
                    break;
                case "stock-balance-by-date":
                    context.OpenReportWindow(() => WindowManager.Show<StockBalanceByDateReport>(WindowSizeType.LargeRectangle));
                    break;
                case "invoices-profit":
                    context.OpenReportWindow(() => WindowManager.Show<InvoicesProfitBrowser>(WindowSizeType.LargeRectangle));
                    break;
                case "inventory-movement-summary":
                    WindowManager.Show<InventoryMovementSummary>(WindowSizeType.LargeRectangle);
                    break;
                case "stock-valuation":
                    context.OpenReportWindow(() => WindowManager.Show<StockValuationReport>(WindowSizeType.LargeRectangle));
                    break;
                case "product-profit":
                    context.OpenReportWindow(() => WindowManager.Show<ProductProfitReport>(WindowSizeType.LargeRectangle));
                    break;
                case "cash-flow":
                    context.OpenReportWindow(() => WindowManager.Show<CashFlowReport>(WindowSizeType.LargeRectangle));
                    break;
                case "profit-loss":
                    context.OpenReportWindow(() => WindowManager.Show<ProfitLossReport>(WindowSizeType.LargeRectangle));
                    break;
                case "stock-balances":
                    context.OpenReportWindow(() => WindowManager.Show<StockBalancesReport>(WindowSizeType.LargeRectangle));
                    break;
                case "material-movements":
                    context.OpenReportWindow(() => WindowManager.Show<MaterialMovementsReport>(WindowSizeType.LargeRectangle));
                    break;
                case "inactive-items":
                    context.OpenReportWindow(() => WindowManager.Show<InactiveItemsReport>(WindowSizeType.LargeRectangle));
                    break;
            }
        }
    }
}

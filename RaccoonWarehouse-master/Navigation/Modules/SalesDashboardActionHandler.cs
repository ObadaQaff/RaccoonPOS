using RaccoonWarehouse.Invoices;
using RaccoonWarehouse.Invoices.Reports;
using RaccoonWarehouse.Orders;
using RaccoonWarehouse.POS;
using RaccoonWarehouse.Reports;
using RaccoonWarehouse.Stocks;
using RaccoonWarehouse.Vouchers;

namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class SalesDashboardActionHandler : IDashboardActionHandler
    {
        public bool CanHandle(string actionKey)
        {
            return actionKey is
                "Vouchers.Receipt" or
                "Vouchers.Payment" or
                "Invoices.Sales" or
                "Invoices.Purchase" or
                "Invoices.SalesReturn" or
                "Stocks.In" or
                "Stocks.Out" or
                "Stocks.Adjustment" or
                "Orders.Import" or
                "Reports.Sales" or
                "Reports.CreditSales" or
                "Reports.InvoiceProfit" or
                "Reports.ShiftSummary";
        }

        public Task ExecuteAsync(string actionKey, DashboardActionContext context)
        {
            switch (actionKey)
            {
                case "Vouchers.Receipt":
                    WindowManager.Show<CreateVoucher>();
                    break;
                case "Vouchers.Payment":
                    WindowManager.Show<PaymentVoucher>();
                    break;
                case "Invoices.Sales":
                    WindowManager.Show<CreateSalesInvoice>();
                    break;
                case "Invoices.Purchase":
                    WindowManager.Show<PayInvoice>();
                    break;
                case "Invoices.SalesReturn":
                    WindowManager.Show<SalesReturn>();
                    break;
                case "Stocks.In":
                    WindowManager.Show<StockIn>();
                    break;
                case "Stocks.Out":
                    WindowManager.Show<StockOut>();
                    break;
                case "Stocks.Adjustment":
                    WindowManager.Show<StockAdjustmentWindow>();
                    break;
                case "Orders.Import":
                    WindowManager.Show<ImportOrder>();
                    break;
                case "Reports.Sales":
                    context.OpenReportWindow(() => WindowManager.Show<SalesReport>(WindowSizeType.LargeRectangle));
                    break;
                case "Reports.CreditSales":
                    context.OpenReportWindow(() => WindowManager.Show<CreditSalesReport>(WindowSizeType.LargeRectangle));
                    break;
                case "Reports.InvoiceProfit":
                    context.OpenReportWindow(() => WindowManager.Show<InvoicesProfitBrowser>(WindowSizeType.LargeRectangle));
                    break;
                case "Reports.ShiftSummary":
                    context.OpenReportWindow(() => WindowManager.Show<DailySalesReport>(WindowSizeType.LargeRectangle));
                    break;
            }

            return Task.CompletedTask;
        }
    }
}

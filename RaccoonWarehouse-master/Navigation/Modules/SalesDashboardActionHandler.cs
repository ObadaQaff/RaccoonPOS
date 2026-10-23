using RaccoonWarehouse.Invoices;
using RaccoonWarehouse.Invoices.Reports;
using RaccoonWarehouse.Orders;
using RaccoonWarehouse.POS;
using RaccoonWarehouse.Reports;
using RaccoonWarehouse.Stocks;
using RaccoonWarehouse.Vouchers;
using RaccoonWarehouse.FinancialTransactions.Reports;
using RaccoonWarehouse.PurchaseOrders;
using RaccoonWarehouse.PurchaseRequests;
using RaccoonWarehouse.PurchaseControl;
using RaccoonWarehouse.GoodsReceipts;

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
                "PurchaseOrders.Create" or
                "PurchaseOrders.List" or
                "PurchaseRequests.List" or
                "PurchaseControl.Dashboard" or
                "PurchaseControl.List" or
                "GoodsReceipts.List" or
                "Invoices.SalesReturn" or
                "Stocks.In" or
                "Stocks.Out" or
                "Stocks.Adjustment" or
                "Orders.Import" or
                "Reports.Sales" or
                "Reports.CreditSales" or
                "Reports.InvoiceProfit" or
                "Reports.ShiftSummary" or
                "Reports.PaymentsReceipts" or
                "Reports.InvoicePaymentMethods";
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
                    WindowManager.ShowDialog<PayInvoice>();
                    break;
                case "PurchaseOrders.Create":
                    WindowManager.Show<PayInvoice>(WindowSizeType.FullScreen, window => window.ConfigureAsPurchaseOrder());
                    break;
                case "PurchaseOrders.List":
                    WindowManager.Show<PurchaseOrdersWindow>(WindowSizeType.LargeRectangle);
                    break;
                case "PurchaseRequests.List":
                    WindowManager.Show<PurchaseRequestsWindow>(WindowSizeType.LargeRectangle);
                    break;
                case "PurchaseControl.Dashboard":
                    WindowManager.Show<PurchaseControlDashboardWindow>(WindowSizeType.LargeRectangle);
                    break;
                case "PurchaseControl.List":
                    WindowManager.Show<PurchaseControlWindow>(WindowSizeType.FullScreen);
                    break;
                case "GoodsReceipts.List":
                    WindowManager.Show<GoodsReceiptsWindow>(WindowSizeType.LargeRectangle);
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
                case "Reports.PaymentsReceipts":
                    context.OpenReportWindow(() => WindowManager.Show<CashFlowReport>(WindowSizeType.LargeRectangle));
                    break;
                case "Reports.InvoicePaymentMethods":
                    context.OpenReportWindow(() => WindowManager.Show<InvoicePaymentMethodsReport>(WindowSizeType.LargeRectangle));
                    break;
            }

            return Task.CompletedTask;
        }
    }
}

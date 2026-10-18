using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Navigation;

namespace RaccoonWarehouse.Reports;

public sealed class PurchaseReportWindow : SalesReport
{
    public PurchaseReportWindow(
        IInvoiceService invoiceService,
        IUserService userService,
        ILoadingService loadingService,
        SourceDocumentNavigationService sourceDocumentNavigationService,
        IFinancialTransactionService financialTransactionService)
        : base(
            invoiceService,
            userService,
            loadingService,
            sourceDocumentNavigationService,
            financialTransactionService)
    {
        ConfigureAsPurchaseReport();
    }
}

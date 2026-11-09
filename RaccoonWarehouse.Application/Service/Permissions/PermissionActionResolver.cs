using RaccoonWarehouse.Core.Permissions;
using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Application.Service.Permissions;

public sealed class PermissionActionResolver : IPermissionActionResolver
{
    public string? GetArea(string actionKey) => PermissionAreaRegistry.AreaForAction(actionKey);

    public PermissionLevel GetRequiredLevel(string actionKey) => actionKey switch
    {
        "Users.Create" or "Users.List" or "Settings.Permissions" or "Settings.SwitchDatabase" => PermissionLevel.Admin,
        "Vouchers.Receipt" or "Vouchers.Payment" or "Invoices.SalesReturn" or "Stocks.Adjustment" or "Accounting.JournalEntry.Create" or "Accounting.Operations" => PermissionLevel.Admin,
        "Products.List" or "Categories.List" or "SubCategories.List" or "Brands.List" or "Warehouses.List" or "PurchaseOrders.List" or "PurchaseRequests.List" or "Customers.List" or "Employees.List" or "Employees.Shifts" or "HR.Dashboard" => PermissionLevel.View,
        "Products.PriceList" or "Products.ItemCostDetails" or "Products.ProfitReport" or "Products.InactiveReport" or "Stocks.BalancesReport" or "Stocks.StockMovementsReport" or "Stocks.StockBalanceByDateReport" or "Stocks.InventoryMovementSummary" or "Stocks.LowStockReport" or "Stocks.ExpiringStockReport" or "Stocks.MaterialMovementsReport" or "below-min-stock" or "product-sales-average" or "customer-requests-report" or "lost-customers-report" or "purchase-report" or "Reports.Sales" or "Reports.CreditSales" or "Reports.InvoiceProfit" or "Reports.ShiftSummary" or "Reports.PaymentsReceipts" or "Reports.InvoicePaymentMethods" or "Accounting.TrialBalance" or "Accounting.GeneralLedger" or "Accounting.BalanceSheet" or "Accounting.CustomerDebts" or "Accounting.SupplierPayables" or "Accounting.PartyBalances" => PermissionLevel.View,
        _ => PermissionLevel.Manage
    };
}

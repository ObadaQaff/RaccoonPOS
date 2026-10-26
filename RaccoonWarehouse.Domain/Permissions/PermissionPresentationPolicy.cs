namespace RaccoonWarehouse.Domain.Permissions;

public enum PermissionKind { View, CreateEdit, Delete, Sensitive, Internal }

/// <summary>Presentation only. Original keys and individual grants remain authoritative.</summary>
public static class PermissionPresentationPolicy
{
    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Customers.List"] = "Customers.View",
        ["Categories.List"] = "Categories.View", ["SubCategories.List"] = "SubCategories.View",
        ["Brands.List"] = "Brands.View", ["Products.List"] = "Products.View",
        ["Units.List"] = "Units.View", ["Warehouses.List"] = "Warehouses.View",
        ["Employees.List"] = "Employees.View", ["Delegates.List"] = "Delegates.View",
        ["Settings.Permissions"] = "Permissions.ManageRoles", ["Invoices.Sales"] = "SalesInvoice.View",
        ["Invoices.Purchase"] = "PurchaseInvoice.View", ["Stocks.In"] = "StockInVoucher.Create",
        ["Stocks.Out"] = "StockOutVoucher.Create", ["Stocks.Adjustment"] = "StockAdjustment.Create",
        ["Stocks.Lots"] = "StockLots.View", ["Products.PriceList"] = "PriceListReport.View",
        ["Products.ItemCostDetails"] = "ItemCostDetailReport.View", ["Products.ProfitReport"] = "ProductProfitReport.View",
        ["Products.InactiveReport"] = "InactiveProductsReport.View", ["Stocks.BalancesReport"] = "StockBalancesReport.View",
        ["Stocks.LowStockReport"] = "BelowMinimumStockReport.View", ["Stocks.MaterialMovementsReport"] = "MaterialMovementsReport.View",
        ["Reports.Sales"] = "SalesReport.View", ["Reports.CreditSales"] = "CreditSalesReport.View",
        ["Reports.InvoiceProfit"] = "InvoicesProfitReport.View", ["Reports.ShiftSummary"] = "CashierReport.View",
        ["Reports.PaymentsReceipts"] = "CashFlowReport.View", ["Reports.InvoicePaymentMethods"] = "InvoicePaymentMethodsReport.View"
    };

    public static string CanonicalKey(string key) => Aliases.GetValueOrDefault(key, PermissionCatalog.FindByKey(key)?.Key ?? PermissionCatalog.FindByLegacyReportKey(key)?.Key ?? key);
    public static string Resource(string key) => CanonicalKey(key)[..CanonicalKey(key).LastIndexOf('.')];
    public static string Action(string key) => CanonicalKey(key)[(CanonicalKey(key).LastIndexOf('.') + 1)..];
    public static PermissionKind Classify(string key) => Action(key) switch
    {
        "View" or "List" or "Read" or "Details" or "Search" => PermissionKind.View,
        // These exact actions have ordinary CRUD semantics. Other verbs stay advanced.
        "Create" or "Add" or "Edit" or "Update" => PermissionKind.CreateEdit,
        "Delete" or "Remove" => PermissionKind.Delete,
        _ => PermissionKind.Sensitive
    };

    private static readonly Lazy<Dictionary<string, string[]>> Views = new(() => PermissionCatalog.All
        .Where(item => Classify(item.Key) == PermissionKind.View)
        .GroupBy(item => Resource(item.Key), StringComparer.OrdinalIgnoreCase)
        .ToDictionary(group => group.Key, group => group.SelectMany(item => item.LegacyReportKey == null ? new[] { item.Key } : new[] { item.Key, item.LegacyReportKey })
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), StringComparer.OrdinalIgnoreCase));

    public static IReadOnlyList<string> ViewDependencies(string key)
    {
        var required = Views.Value.GetValueOrDefault(Resource(key), Array.Empty<string>()).AsEnumerable();
        if (key is "Settings.Employees" or "Settings.Delegates" or "Settings.Accounting" or "Settings.Language")
            required = required.Append("Settings.ManageSettings");
        if (CanonicalKey(key) == "Permissions.ManageRoles")
            required = required.Concat(new[] { "Permissions.ManageRoles", "Settings.Permissions" });
        return required.Where(view => !view.Equals(key, StringComparison.OrdinalIgnoreCase)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static bool? Aggregate(IEnumerable<bool> values)
    {
        var distinct = values.Distinct().ToArray();
        return distinct.Length == 1 ? distinct[0] : null;
    }
}

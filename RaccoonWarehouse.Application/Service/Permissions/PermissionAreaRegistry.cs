using RaccoonWarehouse.Domain.Permissions;

namespace RaccoonWarehouse.Application.Service.Permissions;

public static class PermissionAreaRegistry
{
    public static IReadOnlyList<PermissionArea> All { get; } = new[]
    {
        Category("Sales", "المبيعات", "Sales"),
        Child("Sales.PointOfSale", "نقطة البيع", "Point of Sale", "Sales"),
        Child("Sales.SalesInvoices", "فواتير المبيعات", "Sales Invoices", "Sales"),
        Child("Sales.SalesReports", "تقارير المبيعات", "Sales Reports", "Sales"),

        Category("Inventory", "المخزون", "Inventory"),
        Child("Inventory.Products", "الأصناف", "Products", "Inventory"),
        Child("Inventory.StockOperations", "عمليات المخزون", "Stock Operations", "Inventory"),
        Child("Inventory.StockReports", "تقارير المخزون", "Stock Reports", "Inventory"),
        Child("Inventory.Warehouses", "المستودعات", "Warehouses", "Inventory"),
        Child("Inventory.Categories", "الفئات والعلامات التجارية", "Categories and Brands", "Inventory"),
        Child("Inventory.StockAdjustments", "تسويات المخزون", "Stock Adjustments", "Inventory"),

        Category("Purchasing", "المشتريات", "Purchasing"),
        Child("Purchasing.PurchaseInvoices", "فواتير المشتريات", "Purchase Invoices", "Purchasing"),
        Child("Purchasing.PurchaseOrders", "أوامر الشراء", "Purchase Orders", "Purchasing"),
        Child("Purchasing.PurchaseRequests", "طلبات الشراء", "Purchase Requests", "Purchasing"),
        Child("Purchasing.Suppliers", "الموردون", "Suppliers", "Purchasing"),
        Child("Purchasing.PurchaseReports", "تقارير المشتريات", "Purchase Reports", "Purchasing"),

        Category("Accounting", "المحاسبة", "Accounting"),
        Child("Accounting.ChartOfAccounts", "شجرة الحسابات", "Chart of Accounts", "Accounting"),
        Child("Accounting.FinancialTransactions", "العمليات المالية", "Financial Transactions", "Accounting"),
        Child("Accounting.Receipts", "سندات القبض", "Receipts", "Accounting"),
        Child("Accounting.Payments", "سندات الصرف", "Payments", "Accounting"),
        Child("Accounting.FinancialReports", "التقارير المالية", "Financial Reports", "Accounting"),

        Category("Customers", "العملاء", "Customers"),
        Child("Customers.CustomerProfiles", "بيانات العملاء", "Customer Profiles", "Customers"),
        Child("Customers.CustomerRequests", "طلبات العملاء", "Customer Requests", "Customers"),
        Child("Customers.LostCustomers", "العملاء المفقودون", "Lost Customers", "Customers"),

        Category("Insurance", "التأمين", "Insurance"),
        Child("Insurance.Management", "إدارة التأمين", "Insurance Management", "Insurance"),

        Category("Employees", "الموظفون", "Employees", true, "Employees"),
        Child("Employees.EmployeeProfiles", "ملفات الموظفين", "Employee Profiles", "Employees", true, "Employees"),
        Child("Employees.ShiftManagement", "إدارة الورديات", "Shift Management", "Employees", true, "Employees"),

        Category("HR", "الموارد البشرية", "Human Resources", true, "HR"),
        Child("HR.Attendance", "الحضور والانصراف", "Attendance", "HR", true, "HR"),
        Child("HR.Leaves", "الإجازات والغياب", "Leaves and Absences", "HR", true, "HR"),
        Child("HR.Payroll", "الرواتب", "Payroll", "HR", true, "HR"),
        Child("HR.HRReports", "تقارير الموارد البشرية", "HR Reports", "HR", true, "HR"),

        Category("Users", "المستخدمون", "Users"),
        Child("Users.UserManagement", "إدارة المستخدمين", "User Management", "Users"),
        Category("Settings", "إعدادات النظام", "System Settings"),
        Child("Settings.ModuleManagement", "إدارة الوحدات", "Module Management", "Settings"),
        Child("Settings.SystemConfiguration", "إعدادات النظام", "System Configuration", "Settings"),
        Child("Settings.AuditLogs", "سجل التدقيق", "Audit Logs", "Settings"),
        Child("Settings.PermissionManagement", "إدارة الصلاحيات", "Permission Management", "Settings"),

        // Legacy keys remain recognized so existing database rows are not invalidated.
        Legacy("POS", "نقطة البيع", "Point of Sale"),
        Legacy("Products", "الأصناف", "Products"),
        Legacy("Inventory", "المخزون", "Inventory"),
        Legacy("Purchasing", "المشتريات", "Purchasing"),
        Legacy("Customers", "العملاء", "Customers"),
        Legacy("Suppliers", "الموردون", "Suppliers"),
        Legacy("Accounting", "المحاسبة", "Accounting"),
        Legacy("Reports", "التقارير", "Reports"),
        Legacy("Employees", "الموظفون", "Employees", true, "Employees"),
        Legacy("Shifts", "الورديات", "Shift Management", true, "Employees"),
        Legacy("HR", "الموارد البشرية", "Human Resources", true, "HR"),
        Legacy("Settings", "إعدادات النظام", "System Settings"),
        Legacy("Modules", "إدارة الوحدات", "Module Management"),
        Legacy("Users", "المستخدمون", "Users"),
        Legacy("Permissions", "مجموعات الصلاحيات", "Permission Groups")
    };

    public static IReadOnlyList<PermissionArea> DisplayAreas { get; } = All.Where(x => !x.IsLegacy).ToArray();

    public static bool IsKnown(string key) => All.Any(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
    public static PermissionArea? Find(string key) => All.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));

    public static string? AreaForModule(string moduleKey) => moduleKey switch
    {
        "Sales" => "Sales",
        "PurchaseOrders" => "Purchasing",
        "Warehouses" or "Categories" or "Brands" or "Products" => "Inventory",
        "Users" => "Users",
        "Settings" => "Settings",
        "Accounting" => "Accounting",
        "Customers" => "Customers",
        "Insurance" => "Insurance",
        "Employees" => "Employees",
        "HR" => "HR",
        _ => null
    };

    public static string? AreaForAction(string actionKey) => actionKey switch
    {
        "Products.Create" or "Products.List" or "Units.Create" or "Units.List" => "Inventory.Products",
        "Products.PriceList" or "Products.ItemCostDetails" or "Products.ProfitReport" or "Products.InactiveReport" or "Stocks.BalancesReport" or "Stocks.StockMovementsReport" or "Stocks.StockBalanceByDateReport" or "Stocks.InventoryMovementSummary" or "Stocks.LowStockReport" or "Stocks.ExpiringStockReport" or "Stocks.MaterialMovementsReport" or "below-min-stock" or "product-sales-average" => "Inventory.StockReports",
        "Categories.Create" or "Categories.List" or "SubCategories.Create" or "SubCategories.List" or "Brands.Create" or "Brands.List" => "Inventory.Categories",
        "Warehouses.Create" or "Warehouses.List" or "Stocks.Lots" => "Inventory.Warehouses",
        "Stocks.In" or "Stocks.Out" or "Stocks.Adjustment" => "Inventory.StockOperations",
        "Invoices.Purchase" or "Invoices.PurchaseReturn" => "Purchasing.PurchaseInvoices",
        "PurchaseOrders.Create" or "PurchaseOrders.List" => "Purchasing.PurchaseOrders",
        "PurchaseRequests.List" or "PurchaseControl.Dashboard" or "PurchaseControl.List" or "GoodsReceipts.List" => "Purchasing.PurchaseRequests",
        "Suppliers.Create" or "Suppliers.List" => "Purchasing.Suppliers",
        "Reports.Purchase" or "purchase-report" => "Purchasing.PurchaseReports",
        "Vouchers.Receipt" => "Accounting.Receipts",
        "Vouchers.Payment" => "Accounting.Payments",
        "Accounting.Accounts" or "Accounting.JournalEntries" => "Accounting.ChartOfAccounts",
        "Accounting.JournalEntry.Create" or "Accounting.Operations" or "Accounting.Checks" => "Accounting.FinancialTransactions",
        "Accounting.TrialBalance" or "Accounting.GeneralLedger" or "Accounting.BalanceSheet" or "Accounting.CustomerDebts" or "Accounting.SupplierPayables" or "Accounting.PartyBalances" => "Accounting.FinancialReports",
        "Invoices.Sales" or "Invoices.SalesReturn" => "Sales.SalesInvoices",
        "Reports.Sales" or "Reports.CreditSales" or "Reports.InvoiceProfit" or "Reports.ShiftSummary" or "Reports.PaymentsReceipts" or "Reports.InvoicePaymentMethods" => "Sales.SalesReports",
        "Customers.Create" or "Customers.CreateSupplier" or "Customers.List" => "Customers.CustomerProfiles",
        "CustomerRequests.List" or "customer-requests-report" => "Customers.CustomerRequests",
        "LostCustomers.List" or "lost-customers-report" => "Customers.LostCustomers",
        "Insurance.Claims" or "Insurance.Companies" or "Insurance.Settlements" or "Insurance.Setup" => "Insurance.Management",
        "Orders.Import" => "Sales.SalesInvoices",
        "Users.Create" or "Users.List" => "Users.UserManagement",
        "Settings.Delegates" => "Settings.SystemConfiguration",
        "Settings.Permissions" => "Settings.PermissionManagement",
        "Settings.Employees" or "Settings.Hr" or "Settings.Accounting" or "Settings.InventoryCosting" or "Settings.RefundCode" or "Settings.DocumentBranding" or "Settings.JoFotara" or "Settings.PosRowColors" or "Settings.Language" => "Settings.SystemConfiguration",
        "Settings.AuditLogs" => "Settings.AuditLogs",
        "Settings.SwitchDatabase" => "Settings.ModuleManagement",
        "Employees.List" => "Employees.EmployeeProfiles",
        "Employees.Shifts" => "Employees.ShiftManagement",
        "HR.Dashboard" => "HR.HRReports",
        _ => null
    };

    private static PermissionArea Category(string key, string ar, string en, bool moduleBound = false, string? moduleKey = null)
        => new(key, ar, en, moduleBound, moduleKey);

    private static PermissionArea Child(string key, string ar, string en, string parent, bool moduleBound = false, string? moduleKey = null)
        => new(key, ar, en, moduleBound, moduleKey, parent);

    private static PermissionArea Legacy(string key, string ar, string en, bool moduleBound = false, string? moduleKey = null)
        => new(key, ar, en, moduleBound, moduleKey, null, true);
}

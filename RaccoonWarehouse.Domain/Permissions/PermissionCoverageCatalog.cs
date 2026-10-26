namespace RaccoonWarehouse.Domain.Permissions;

public sealed record ScreenPermission(string TypeName, string? Resource, string EntryAction, string Path);

public static class PermissionCoverageCatalog
{
    public static IReadOnlyList<ScreenPermission> Screens { get; } = new ScreenPermission[]
    {
        new("RaccoonWarehouse.Accounting.AccountingFeatureSettingsWindow", "Settings", "ManageSettings", "RaccoonWarehouse-master/Accounting/AccountingFeatureSettingsWindow.xaml"),
        new("RaccoonWarehouse.Accounting.AccountingOperationsBrowser", "Accounting.Operations", "View", "RaccoonWarehouse-master/Accounting/AccountingOperationsBrowser.xaml"),
        new("RaccoonWarehouse.Accounting.AccountsTable", "Accounting.Accounts", "View", "RaccoonWarehouse-master/Accounting/AccountsTable.xaml"),
        new("RaccoonWarehouse.Accounting.AddAccountDialog", "Accounting.Accounts", "Create", "RaccoonWarehouse-master/Accounting/AddAccountDialog.xaml"),
        new("RaccoonWarehouse.Accounting.BalanceSheetReport", "Accounting.BalanceSheet", "View", "RaccoonWarehouse-master/Accounting/BalanceSheetReport.xaml"),
        new("RaccoonWarehouse.Accounting.CreateJournalEntry", "Accounting.JournalEntry", "Create", "RaccoonWarehouse-master/Accounting/CreateJournalEntry.xaml"),
        new("RaccoonWarehouse.Accounting.GeneralLedgerReport", "Accounting.GeneralLedger", "View", "RaccoonWarehouse-master/Accounting/GeneralLedgerReport.xaml"),
        new("RaccoonWarehouse.Accounting.JournalEntriesBrowser", "Accounting.JournalEntries", "View", "RaccoonWarehouse-master/Accounting/JournalEntriesBrowser.xaml"),
        new("RaccoonWarehouse.Accounting.PartyBalanceReport", "Accounting.PartyBalances", "View", "RaccoonWarehouse-master/Accounting/PartyBalanceReport.xaml"),
        new("RaccoonWarehouse.Accounting.TrialBalanceReport", "Accounting.TrialBalance", "View", "RaccoonWarehouse-master/Accounting/TrialBalanceReport.xaml"),
        new("RaccoonWarehouse.Auth.CloseCashierSessionWindow", "CashDrawer", "CloseShift", "RaccoonWarehouse-master/Auth/CloseCashierSessionWindow.xaml"),
        new("RaccoonWarehouse.Auth.LoginWindow", null, "View", "RaccoonWarehouse-master/Auth/LoginWindow.xaml"),
        new("RaccoonWarehouse.Auth.StartCashierSessionWindow", "CashDrawer", "View", "RaccoonWarehouse-master/Auth/StartCashierSessionWindow.xaml"),
        new("RaccoonWarehouse.Brands.BrandsTable", "Brands", "View", "RaccoonWarehouse-master/Brands/BrandsTable.xaml"),
        new("RaccoonWarehouse.Brands.CreateBrand", "Brands", "Create", "RaccoonWarehouse-master/Brands/CreateBrand.xaml"),
        new("RaccoonWarehouse.Brands.UpdateBrand", "Brands", "Edit", "RaccoonWarehouse-master/Brands/UpdateBrand.xaml"),
        new("RaccoonWarehouse.Categories.CategoriesTable", "Categories", "View", "RaccoonWarehouse-master/Categories/CategoriesTable.xaml"),
        new("RaccoonWarehouse.Categories.CreateCategory", "Categories", "Create", "RaccoonWarehouse-master/Categories/CreateCategory.xaml"),
        new("RaccoonWarehouse.Categories.UpdateCategory", "Categories", "Edit", "RaccoonWarehouse-master/Categories/UpdateCategory.xaml"),
        new("RaccoonWarehouse.ChatAssistant.ChatAssistantSettingsWindow", null, "View", "RaccoonWarehouse-master/ChatAssistant/ChatAssistantSettingsWindow.xaml"),
        new("RaccoonWarehouse.ChatAssistant.ChatAssistantWindow", null, "View", "RaccoonWarehouse-master/ChatAssistant/ChatAssistantWindow.xaml"),
        new("RaccoonWarehouse.Invoices.CheckEditWindow", "Accounting.Checks", "View", "RaccoonWarehouse-master/Checks/CheckEditWindow.xaml"),
        new("RaccoonWarehouse.ChecksDashboard", "Accounting.Checks", "View", "RaccoonWarehouse-master/Checks/ChecksDashboard.xaml"),
        new("RaccoonWarehouse.Common.Loading.LoadingWindow", null, "View", "RaccoonWarehouse-master/Common/Loading/LoadingWindow.xaml"),
        new("RaccoonWarehouse.Common.Loading.LoadingWindow_v2", null, "View", "RaccoonWarehouse-master/Common/Loading/LoadingWindow_v2.xaml"),
        new("RaccoonWarehouse.Delegates.CreateDelegate", "Delegates", "Create", "RaccoonWarehouse-master/Delegates/CreateDelegate.xaml"),
        new("RaccoonWarehouse.Delegates.DelegateDetails", "Delegates", "View", "RaccoonWarehouse-master/Delegates/DelegateDetails.xaml"),
        new("RaccoonWarehouse.Delegates.DelegateFeatureSettingsWindow", "Settings", "ManageSettings", "RaccoonWarehouse-master/Delegates/DelegateFeatureSettingsWindow.xaml"),
        new("RaccoonWarehouse.Delegates.DelegatesTable", "Delegates", "View", "RaccoonWarehouse-master/Delegates/DelegatesTable.xaml"),
        new("RaccoonWarehouse.Delegates.UpdateDelegate", "Delegates", "Edit", "RaccoonWarehouse-master/Delegates/UpdateDelegate.xaml"),
        new("RaccoonWarehouse.Employees.CreateEmployee", "Employees", "Create", "RaccoonWarehouse-master/Employees/CreateEmployee.xaml"),
        new("RaccoonWarehouse.Employees.EmployeeDetails", "Employees", "View", "RaccoonWarehouse-master/Employees/EmployeeDetails.xaml"),
        new("RaccoonWarehouse.Employees.EmployeeFeatureSettingsWindow", "Settings", "ManageSettings", "RaccoonWarehouse-master/Employees/EmployeeFeatureSettingsWindow.xaml"),
        new("RaccoonWarehouse.Employees.EmployeesTable", "Employees", "View", "RaccoonWarehouse-master/Employees/EmployeesTable.xaml"),
        new("RaccoonWarehouse.Employees.UpdateEmployee", "Employees", "Edit", "RaccoonWarehouse-master/Employees/UpdateEmployee.xaml"),
        new("RaccoonWarehouse.FinancialTransactions.PaymentWindow", "Vouchers", "View", "RaccoonWarehouse-master/FinancialTransactions/PaymentWindow.xaml"),
        new("RaccoonWarehouse.FinancialTransactions.ProfitLossReport", "ProfitLossReport", "View", "RaccoonWarehouse-master/FinancialTransactions/ProfitLossReport.xaml"),
        new("RaccoonWarehouse.FinancialTransactions.ReceiptWindow", "Vouchers", "View", "RaccoonWarehouse-master/FinancialTransactions/ReceiptWindow.xaml"),
        new("RaccoonWarehouse.FinancialTransactions.Reports.CashFlowReport", "CashFlowReport", "View", "RaccoonWarehouse-master/FinancialTransactions/Reports/CashFlowReport.xaml"),
        new("RaccoonWarehouse.FinancialTransactions.Reports.InvoicePaymentMethodsReport", "InvoicePaymentMethodsReport", "View", "RaccoonWarehouse-master/FinancialTransactions/Reports/InvoicePaymentMethodsReport.xaml"),
        new("RaccoonWarehouse.GoodsReceipts.GoodsReceiptsWindow", "GoodsReceipt", "View", "RaccoonWarehouse-master/GoodsReceipts/GoodsReceiptsWindow.xaml"),
        new("RaccoonWarehouse.Dashboard", "Dashboard", "View", "RaccoonWarehouse-master/Home/Dashboard.xaml"),
        new("RaccoonWarehouse.Insurance.CreateInsuranceCompany", "InsuranceCompanies", "Create", "RaccoonWarehouse-master/Insurance/CreateInsuranceCompany.xaml"),
        new("RaccoonWarehouse.Insurance.InsuranceClaimsWindow", "InsuranceClaims", "View", "RaccoonWarehouse-master/Insurance/InsuranceClaimsWindow.xaml"),
        new("RaccoonWarehouse.Insurance.InsuranceCompaniesWindow", "InsuranceCompanies", "View", "RaccoonWarehouse-master/Insurance/InsuranceCompaniesWindow.xaml"),
        new("RaccoonWarehouse.Insurance.InsuranceInvoiceWindow", "InsuranceClaims", "View", "RaccoonWarehouse-master/Insurance/InsuranceInvoiceWindow.xaml"),
        new("RaccoonWarehouse.Insurance.InsuranceSettlementWindow", "InsurancePayments", "View", "RaccoonWarehouse-master/Insurance/InsuranceSettlementWindow.xaml"),
        new("RaccoonWarehouse.Insurance.InsuranceSetupWindow", "InsuranceContracts", "View", "RaccoonWarehouse-master/Insurance/InsuranceSetupWindow.xaml"),
        new("RaccoonWarehouse.Invoices.ChangeInvoicePaymentWindow", "SalesInvoice", "ChangePayment", "RaccoonWarehouse-master/Invoices/ChangeInvoicePaymentWindow.xaml"),
        new("RaccoonWarehouse.Invoices.CheckDetailsWindow", "SalesInvoice", "View", "RaccoonWarehouse-master/Invoices/CheckDetailsWindow.xaml"),
        new("RaccoonWarehouse.Invoices.CreateSalesInvoice", "SalesInvoice", "Create", "RaccoonWarehouse-master/Invoices/CreateSalesInvoice.xaml"),
        new("RaccoonWarehouse.Invoices.FractionalSaleConfigurationWindow", "Products", "View", "RaccoonWarehouse-master/Invoices/FractionalSaleConfigurationWindow.xaml"),
        new("RaccoonWarehouse.Invoices.POS", "POS", "View", "RaccoonWarehouse-master/Invoices/POS.xaml"),
        new("RaccoonWarehouse.Invoices.PayInvoice", "PurchaseInvoice", "View", "RaccoonWarehouse-master/Invoices/PayInvoice.xaml"),
        new("RaccoonWarehouse.Invoices.ProductSearchWindow", "Lookup", "View", "RaccoonWarehouse-master/Invoices/ProductSearchWindow.xaml"),
        new("RaccoonWarehouse.Invoices.ProductUnitSaleConfigurationWindow", "Products", "View", "RaccoonWarehouse-master/Invoices/ProductUnitSaleConfigurationWindow.xaml"),
        new("RaccoonWarehouse.Invoices.PurchaseProductSearchWindow", "Lookup", "View", "RaccoonWarehouse-master/Invoices/PurchaseProductSearchWindow.xaml"),
        new("RaccoonWarehouse.Invoices.Reports.InvoicesProfitBrowser", "InvoicesProfitReport", "View", "RaccoonWarehouse-master/Invoices/Reports/InvoicesProfitBrowser.xaml"),
        new("RaccoonWarehouse.Invoices.SalesReturn", "SalesInvoice", "View", "RaccoonWarehouse-master/Invoices/SalesReturn.xaml"),
        new("RaccoonWarehouse.Invoices.SearchSalesInvoiceWindow", "SalesInvoice", "View", "RaccoonWarehouse-master/Invoices/SearchSalesInvoiceWindow.xaml"),
        new("RaccoonWarehouse.Licensing.LicenseExpiredWindow", null, "View", "RaccoonWarehouse-master/Licensing/LicenseExpiredWindow.xaml"),
        new("RaccoonWarehouse.MainWindow", null, "View", "RaccoonWarehouse-master/MainWindow.xaml"),
        new("RaccoonWarehouse.Notifications.NotificationToastWindow", null, "View", "RaccoonWarehouse-master/Notifications/NotificationToastWindow.xaml"),
        new("RaccoonWarehouse.Orders.ImportOrder", "Orders", "View", "RaccoonWarehouse-master/Orders/ImportOrder.xaml"),
        new("RaccoonWarehouse.Orders.OrderInvoiceDetails", "Orders", "View", "RaccoonWarehouse-master/Orders/OrderInvoiceDetails.xaml"),
        new("RaccoonWarehouse.Orders.OrdersTable", "Orders", "View", "RaccoonWarehouse-master/Orders/OrdersTable.xaml"),
        new("RaccoonWarehouse.POS.DailySalesReport", "CashierReport", "View", "RaccoonWarehouse-master/POS/DailySalesReport.xaml"),
        new("RaccoonWarehouse.POS.ExchangeInvoiceWindow", "SalesInvoice", "View", "RaccoonWarehouse-master/POS/ExchangeInvoiceWindow.xaml"),
        new("RaccoonWarehouse.POS.HoldInvoiceColorWindow", "POS", "View", "RaccoonWarehouse-master/POS/HoldInvoiceColorWindow.xaml"),
        new("RaccoonWarehouse.POS.ResumeHeldInvoiceWindow", "POS", "View", "RaccoonWarehouse-master/POS/ResumeHeldInvoiceWindow.xaml"),
        new("RaccoonWarehouse.POS.ReturnInvoiceWindow", "SalesInvoice", "View", "RaccoonWarehouse-master/POS/ReturnInvoiceWindow.xaml"),
        new("RaccoonWarehouse.Products.CreateProduct", "Products", "Create", "RaccoonWarehouse-master/Products/CreateProduct.xaml"),
        new("RaccoonWarehouse.Products.ProductsTable", "Products", "View", "RaccoonWarehouse-master/Products/ProductsTable.xaml"),
        new("RaccoonWarehouse.Products.Reports.InactiveProductsReport", "InactiveProductsReport", "View", "RaccoonWarehouse-master/Products/Reports/InactiveProductsReport.xaml"),
        new("RaccoonWarehouse.Products.Reports.ProductProfitReport", "ProductProfitReport", "View", "RaccoonWarehouse-master/Products/Reports/ProductProfitReport.xaml"),
        new("RaccoonWarehouse.Products.UpdateProduct", "Products", "Edit", "RaccoonWarehouse-master/Products/UpdateProduct.xaml"),
        new("RaccoonWarehouse.PurchaseControl.PurchaseControlDashboardWindow", "PurchaseControl", "View", "RaccoonWarehouse-master/PurchaseControl/PurchaseControlDashboardWindow.xaml"),
        new("RaccoonWarehouse.PurchaseControl.PurchaseControlWindow", "PurchaseControl", "View", "RaccoonWarehouse-master/PurchaseControl/PurchaseControlWindow.xaml"),
        new("RaccoonWarehouse.PurchaseOrders.PurchaseOrderDetailsWindow", "PurchaseOrder", "View", "RaccoonWarehouse-master/PurchaseOrders/PurchaseOrderDetailsWindow.xaml"),
        new("RaccoonWarehouse.PurchaseOrders.PurchaseOrdersWindow", "PurchaseOrder", "View", "RaccoonWarehouse-master/PurchaseOrders/PurchaseOrdersWindow.xaml"),
        new("RaccoonWarehouse.PurchaseRequests.PurchaseRequestApprovalWindow", "PurchaseRequest", "Approve", "RaccoonWarehouse-master/PurchaseRequests/PurchaseRequestApprovalWindow.xaml"),
        new("RaccoonWarehouse.PurchaseRequests.PurchaseRequestDetailsWindow", "PurchaseRequest", "View", "RaccoonWarehouse-master/PurchaseRequests/PurchaseRequestDetailsWindow.xaml"),
        new("RaccoonWarehouse.PurchaseRequests.PurchaseRequestEditorWindow", "PurchaseRequest", "View", "RaccoonWarehouse-master/PurchaseRequests/PurchaseRequestEditorWindow.xaml"),
        new("RaccoonWarehouse.PurchaseRequests.PurchaseRequestsWindow", "PurchaseRequest", "View", "RaccoonWarehouse-master/PurchaseRequests/PurchaseRequestsWindow.xaml"),
        new("RaccoonWarehouse.Reports.BelowMinimumStockReport", "BelowMinimumStockReport", "View", "RaccoonWarehouse-master/Reports/BelowMinimumStockReport.xaml"),
        new("RaccoonWarehouse.Reports.CreditSalesReport", "CreditSalesReport", "View", "RaccoonWarehouse-master/Reports/CreditSalesReport.xaml"),
        new("RaccoonWarehouse.Reports.DiscountSummaryReport", "DiscountSummaryReport", "View", "RaccoonWarehouse-master/Reports/DiscountSummaryReport.xaml"),
        new("RaccoonWarehouse.Reports.InactiveItemsReport", "InactiveItemsReport", "View", "RaccoonWarehouse-master/Reports/InactiveItemsReport.xaml"),
        new("RaccoonWarehouse.Reports.ItemCostDetailReport", "ItemCostDetailReport", "View", "RaccoonWarehouse-master/Reports/ItemCostDetailReport.xaml"),
        new("RaccoonWarehouse.Reports.MaterialMovementsReport", "MaterialMovementsReport", "View", "RaccoonWarehouse-master/Reports/MaterialMovementsReport.xaml"),
        new("RaccoonWarehouse.Reports.PriceListReport", "PriceListReport", "View", "RaccoonWarehouse-master/Reports/PriceListReport.xaml"),
        new("RaccoonWarehouse.Reports.SalesReport", "SalesReport", "View", "RaccoonWarehouse-master/Reports/SalesReport.xaml"),
        new("RaccoonWarehouse.Reports.StockBalancesReport", "StockBalancesReport", "View", "RaccoonWarehouse-master/Reports/StockBalancesReport.xaml"),
        new("RaccoonWarehouse.Settings.AuditLogsWindow", "AuditLogs", "View", "RaccoonWarehouse-master/Settings/AuditLogsWindow.xaml"),
        new("RaccoonWarehouse.Settings.InventoryCostingSettingsWindow", "Settings", "ManageSettings", "RaccoonWarehouse-master/Settings/InventoryCostingSettingsWindow.xaml"),
        new("RaccoonWarehouse.Settings.LanguageSettingsWindow", "Settings", "ManageSettings", "RaccoonWarehouse-master/Settings/LanguageSettingsWindow.xaml"),
        new("RaccoonWarehouse.Settings.PosRowColorSettingsWindow", "Settings", "ManageSettings", "RaccoonWarehouse-master/Settings/PosRowColorSettingsWindow.xaml"),
        new("RaccoonWarehouse.Settings.ReportPermissionsManager", "Permissions", "View", "RaccoonWarehouse-master/Settings/ReportPermissionsManager.xaml"),
        new("RaccoonWarehouse.Stocks.CreateStock", "StockAdjustment", "Create", "RaccoonWarehouse-master/Stocks/CreateStock.xaml"),
        new("RaccoonWarehouse.Stocks.CurrentStock", "CurrentStockReport", "View", "RaccoonWarehouse-master/Stocks/CurrentStock.xaml"),
        new("RaccoonWarehouse.Stocks.Reports.ExpiringStockReport", "ExpiringStockReport", "View", "RaccoonWarehouse-master/Stocks/Reports/ExpiringStockReport.xaml"),
        new("RaccoonWarehouse.Stocks.Reports.InventoryMovementSummary", "InventoryMovementSummaryReport", "View", "RaccoonWarehouse-master/Stocks/Reports/InventoryMovementSummary.xaml"),
        new("RaccoonWarehouse.Stocks.Reports.LowStockReport", "BelowMinimumStockReport", "View", "RaccoonWarehouse-master/Stocks/Reports/LowStockReport.xaml"),
        new("RaccoonWarehouse.Stocks.Reports.StockBalanceByDateReport", "StockBalanceByDateReport", "View", "RaccoonWarehouse-master/Stocks/Reports/StockBalanceByDateReport.xaml"),
        new("RaccoonWarehouse.Stocks.Reports.StockMovementsReport", "StockMovementsReport", "View", "RaccoonWarehouse-master/Stocks/Reports/StockMovementsReport.xaml"),
        new("RaccoonWarehouse.Stocks.Reports.StockValuationReport", "StockValuationReport", "View", "RaccoonWarehouse-master/Stocks/Reports/StockValuationReport.xaml"),
        new("RaccoonWarehouse.Stocks.SearchStockInWindow", "StockInVoucher", "View", "RaccoonWarehouse-master/Stocks/SearchStockInWindow.xaml"),
        new("RaccoonWarehouse.Stocks.StockAdjustmentWindow", "StockAdjustment", "View", "RaccoonWarehouse-master/Stocks/StockAdjustmentWindow.xaml"),
        new("RaccoonWarehouse.Stocks.StockIn", "StockInVoucher", "View", "RaccoonWarehouse-master/Stocks/StockIn.xaml"),
        new("RaccoonWarehouse.Stocks.StockLotsWindow", "StockLots", "View", "RaccoonWarehouse-master/Stocks/StockLotsWindow.xaml"),
        new("RaccoonWarehouse.Stocks.StockOut", "StockOutVoucher", "View", "RaccoonWarehouse-master/Stocks/StockOut.xaml"),
        new("RaccoonWarehouse.SubCategories.CreateSubCategory", "SubCategories", "Create", "RaccoonWarehouse-master/SubCategories/CreateSubCategory.xaml"),
        new("RaccoonWarehouse.SubCategories.SubCategoryTable", "SubCategories", "View", "RaccoonWarehouse-master/SubCategories/SubCategoryTable.xaml"),
        new("RaccoonWarehouse.SubCategories.UpdateSubCategory", "SubCategories", "Edit", "RaccoonWarehouse-master/SubCategories/UpdateSubCategory.xaml"),
        new("RaccoonWarehouse.Units.CreateUnit", "Units", "Create", "RaccoonWarehouse-master/Units/CreateUnit.xaml"),
        new("RaccoonWarehouse.Units.UnitsTable", "Units", "View", "RaccoonWarehouse-master/Units/UnitsTable.xaml"),
        new("RaccoonWarehouse.Units.UpdateUnit", "Units", "Edit", "RaccoonWarehouse-master/Units/UpdateUnit.xaml"),
        new("RaccoonWarehouse.CreateUser", "Users", "Create", "RaccoonWarehouse-master/Users/CreateUser.xaml"),
        new("RaccoonWarehouse.CustomersTable", "Customers", "View", "RaccoonWarehouse-master/Users/CustomersTable.xaml"),
        new("RaccoonWarehouse.UpdateUser", "Users", "Edit", "RaccoonWarehouse-master/Users/UpdateUser.xaml"),
        new("RaccoonWarehouse.UserStatementWindow", "PartyStatement", "View", "RaccoonWarehouse-master/Users/UserStatementWindow.xaml"),
        new("RaccoonWarehouse.UsersTable", "Users", "View", "RaccoonWarehouse-master/Users/UsersTable.xaml"),
        new("RaccoonWarehouse.Vouchers.CreateVoucher", "Vouchers", "Create", "RaccoonWarehouse-master/Vouchers/CreateVoucher.xaml"),
        new("RaccoonWarehouse.Vouchers.PaymentVoucher", "Vouchers", "View", "RaccoonWarehouse-master/Vouchers/PaymentVoucher.xaml"),
        new("RaccoonWarehouse.Vouchers.SearchVoucherWindow", "Vouchers", "View", "RaccoonWarehouse-master/Vouchers/SearchVoucherWindow.xaml"),
        new("RaccoonWarehouse.Warehouses.CreateWarehouse", "Warehouses", "Create", "RaccoonWarehouse-master/Warehouses/CreateWarehouse.xaml"),
        new("RaccoonWarehouse.Warehouses.WarehousesTable", "Warehouses", "View", "RaccoonWarehouse-master/Warehouses/WarehousesTable.xaml"),
        new("RaccoonWarehouse.Employees.Payroll.EmployeeEditorControl", "Employees", "View", "RaccoonWarehouse-master/Employees/Payroll/EmployeeEditorControl.cs"),
        new("RaccoonWarehouse.Employees.Payroll.EmployeePayrollWorkspaceWindow", "Employees", "View", "RaccoonWarehouse-master/Employees/Payroll/EmployeePayrollWorkspaceWindow.cs"),
        new("RaccoonWarehouse.Employees.Payroll.PayrollMonthWindow", "Payroll", "View", "RaccoonWarehouse-master/Employees/Payroll/PayrollMonthWindow.cs"),
        new("RaccoonWarehouse.Employees.Payroll.PayrollPolicyWindow", "Payroll", "Configure", "RaccoonWarehouse-master/Employees/Payroll/PayrollPolicyWindow.cs"),
        new("RaccoonWarehouse.Insurance.InsuranceCompanyStatementWindow", "InsuranceReports", "View", "RaccoonWarehouse-master/Insurance/InsuranceCompanyStatementWindow.cs"),
        new("RaccoonWarehouse.Insurance.PosInsuranceDetailsWindow", "POS", "Create", "RaccoonWarehouse-master/Insurance/PosInsuranceDetailsWindow.cs"),
        new("RaccoonWarehouse.Invoices.PurchaseBonusReturnWindow", "PurchaseReturn", "View", "RaccoonWarehouse-master/Invoices/PurchaseBonusReturnWindow.cs"),
        new("RaccoonWarehouse.Reports.ProductSalesAverageReport", "SalesReport", "View", "RaccoonWarehouse-master/Reports/ProductSalesAverageReport.cs"),
        new("RaccoonWarehouse.Settings.DocumentBrandingWindow", "Settings", "ManageSettings", "RaccoonWarehouse-master/Settings/DocumentBrandingWindow.cs"),
        new("RaccoonWarehouse.Settings.RefundCodeSettingsWindow", "Settings", "ManageSettings", "RaccoonWarehouse-master/Settings/RefundCodeSettingsWindow.xaml"),
    };

    public static IEnumerable<PermissionCatalogItem> AdditionalPermissions()
    {
        var sort = 10000;
        yield return new() { Key = "Accounting.Accounts.Create", Resource = "Accounting.Accounts", Module = "Accounting", DisplayName = "شجرة الحسابات", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.Accounts.Delete", Resource = "Accounting.Accounts", Module = "Accounting", DisplayName = "شجرة الحسابات", Action = "Delete", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.Accounts.Edit", Resource = "Accounting.Accounts", Module = "Accounting", DisplayName = "شجرة الحسابات", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.Accounts.View", Resource = "Accounting.Accounts", Module = "Accounting", DisplayName = "شجرة الحسابات", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.BalanceSheet.View", Resource = "Accounting.BalanceSheet", Module = "Accounting", DisplayName = "الميزانية العمومية", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.Checks.UpdateStatus", Resource = "Accounting.Checks", Module = "Accounting", DisplayName = "تعديل الشيك", Action = "UpdateStatus", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.Checks.View", Resource = "Accounting.Checks", Module = "Accounting", DisplayName = "تعديل الشيك", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.CostCenters.Create", Resource = "Accounting.CostCenters", Module = "Accounting", DisplayName = "Accounting.CostCenters", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.CostCenters.Delete", Resource = "Accounting.CostCenters", Module = "Accounting", DisplayName = "Accounting.CostCenters", Action = "Delete", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.CostCenters.Edit", Resource = "Accounting.CostCenters", Module = "Accounting", DisplayName = "Accounting.CostCenters", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.CostCenters.View", Resource = "Accounting.CostCenters", Module = "Accounting", DisplayName = "Accounting.CostCenters", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.Currency.View", Resource = "Accounting.Currency", Module = "Accounting", DisplayName = "Accounting.Currency", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.GeneralLedger.View", Resource = "Accounting.GeneralLedger", Module = "Accounting", DisplayName = "دفتر الأستاذ العام", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.JournalEntries.View", Resource = "Accounting.JournalEntries", Module = "Accounting", DisplayName = "سجل القيود", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.JournalEntry.Create", Resource = "Accounting.JournalEntry", Module = "Accounting", DisplayName = "قيد يومية يدوي", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.JournalEntry.Post", Resource = "Accounting.JournalEntry", Module = "Accounting", DisplayName = "قيد يومية يدوي", Action = "Post", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.JournalEntry.Reverse", Resource = "Accounting.JournalEntry", Module = "Accounting", DisplayName = "قيد يومية يدوي", Action = "Reverse", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.JournalEntry.SetPostingLockDate", Resource = "Accounting.JournalEntry", Module = "Accounting", DisplayName = "قيد يومية يدوي", Action = "SetPostingLockDate", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.JournalEntry.View", Resource = "Accounting.JournalEntry", Module = "Accounting", DisplayName = "قيد يومية يدوي", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.OpeningBalance.Post", Resource = "Accounting.OpeningBalance", Module = "Accounting", DisplayName = "Accounting.OpeningBalance", Action = "Post", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.OpeningBalance.View", Resource = "Accounting.OpeningBalance", Module = "Accounting", DisplayName = "Accounting.OpeningBalance", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.Operations.Delete", Resource = "Accounting.Operations", Module = "Accounting", DisplayName = "Accounting Operations", Action = "Delete", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.Operations.Post", Resource = "Accounting.Operations", Module = "Accounting", DisplayName = "Accounting Operations", Action = "Post", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.Operations.View", Resource = "Accounting.Operations", Module = "Accounting", DisplayName = "Accounting Operations", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.PartyBalances.View", Resource = "Accounting.PartyBalances", Module = "Accounting", DisplayName = "Accounts Balances", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.Periods.Close", Resource = "Accounting.Periods", Module = "Accounting", DisplayName = "Accounting.Periods", Action = "Close", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.Periods.Configure", Resource = "Accounting.Periods", Module = "Accounting", DisplayName = "Accounting.Periods", Action = "Configure", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.Periods.View", Resource = "Accounting.Periods", Module = "Accounting", DisplayName = "Accounting.Periods", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.Reconciliation.Reconcile", Resource = "Accounting.Reconciliation", Module = "Accounting", DisplayName = "Accounting.Reconciliation", Action = "Reconcile", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.Reconciliation.View", Resource = "Accounting.Reconciliation", Module = "Accounting", DisplayName = "Accounting.Reconciliation", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.RecurringJournal.Create", Resource = "Accounting.RecurringJournal", Module = "Accounting", DisplayName = "Accounting.RecurringJournal", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.RecurringJournal.Delete", Resource = "Accounting.RecurringJournal", Module = "Accounting", DisplayName = "Accounting.RecurringJournal", Action = "Delete", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.RecurringJournal.Edit", Resource = "Accounting.RecurringJournal", Module = "Accounting", DisplayName = "Accounting.RecurringJournal", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.RecurringJournal.Post", Resource = "Accounting.RecurringJournal", Module = "Accounting", DisplayName = "Accounting.RecurringJournal", Action = "Post", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.RecurringJournal.View", Resource = "Accounting.RecurringJournal", Module = "Accounting", DisplayName = "Accounting.RecurringJournal", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.Tax.Create", Resource = "Accounting.Tax", Module = "Accounting", DisplayName = "Accounting.Tax", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.Tax.Delete", Resource = "Accounting.Tax", Module = "Accounting", DisplayName = "Accounting.Tax", Action = "Delete", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.Tax.Edit", Resource = "Accounting.Tax", Module = "Accounting", DisplayName = "Accounting.Tax", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.Tax.View", Resource = "Accounting.Tax", Module = "Accounting", DisplayName = "Accounting.Tax", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.TrialBalance.View", Resource = "Accounting.TrialBalance", Module = "Accounting", DisplayName = "ميزان المراجعة", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "AuditLogs.View", Resource = "AuditLogs", Module = "Administration", DisplayName = "Audit Logs", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "BelowMinimumStockReport.View", Resource = "BelowMinimumStockReport", Module = "Reports", DisplayName = "بضائع تحت الحد الأدنى", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Brands.Create", Resource = "Brands", Module = "MasterData", DisplayName = "العلامات التجارية", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Brands.Delete", Resource = "Brands", Module = "MasterData", DisplayName = "العلامات التجارية", Action = "Delete", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Brands.Edit", Resource = "Brands", Module = "MasterData", DisplayName = "العلامات التجارية", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Brands.View", Resource = "Brands", Module = "MasterData", DisplayName = "العلامات التجارية", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "CashDrawer.AccessCashDrawer", Resource = "CashDrawer", Module = "Finance", DisplayName = "إنهاء جلسة الكاشير", Action = "AccessCashDrawer", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "CashDrawer.CloseShift", Resource = "CashDrawer", Module = "Finance", DisplayName = "إنهاء جلسة الكاشير", Action = "CloseShift", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "CashDrawer.View", Resource = "CashDrawer", Module = "Finance", DisplayName = "إنهاء جلسة الكاشير", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "CashFlowReport.View", Resource = "CashFlowReport", Module = "Reports", DisplayName = "المدفوعات والمقبوضات", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "CashierReport.View", Resource = "CashierReport", Module = "Reports", DisplayName = "لوحة الكاشيرات", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Categories.Create", Resource = "Categories", Module = "MasterData", DisplayName = "جدول الفئات", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Categories.Delete", Resource = "Categories", Module = "MasterData", DisplayName = "جدول الفئات", Action = "Delete", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Categories.Edit", Resource = "Categories", Module = "MasterData", DisplayName = "جدول الفئات", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Categories.View", Resource = "Categories", Module = "MasterData", DisplayName = "جدول الفئات", Action = "View", SortOrder = sort++, IsLegacy = false };

        yield return new() { Key = "CreditSalesReport.View", Resource = "CreditSalesReport", Module = "Reports", DisplayName = "تقرير مبيعات الذمم", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "CurrentStockReport.View", Resource = "CurrentStockReport", Module = "Reports", DisplayName = "تقرير الجرد", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Customers.Create", Resource = "Customers", Module = "MasterData", DisplayName = "إدارة العملاء والموردين", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Customers.Delete", Resource = "Customers", Module = "MasterData", DisplayName = "إدارة العملاء والموردين", Action = "Delete", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Customers.Edit", Resource = "Customers", Module = "MasterData", DisplayName = "إدارة العملاء والموردين", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Customers.View", Resource = "Customers", Module = "MasterData", DisplayName = "إدارة العملاء والموردين", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Dashboard.View", Resource = "Dashboard", Module = "Reports", DisplayName = "نظام المستودع", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Delegates.Create", Resource = "Delegates", Module = "MasterData", DisplayName = "إنشاء مندوب", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Delegates.Delete", Resource = "Delegates", Module = "MasterData", DisplayName = "إنشاء مندوب", Action = "Delete", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Delegates.Edit", Resource = "Delegates", Module = "MasterData", DisplayName = "إنشاء مندوب", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Delegates.View", Resource = "Delegates", Module = "MasterData", DisplayName = "إنشاء مندوب", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "DiscountSummaryReport.View", Resource = "DiscountSummaryReport", Module = "Reports", DisplayName = "ملخص الخصومات", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Employees.Create", Resource = "Employees", Module = "Employees", DisplayName = "إنشاء موظف", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Employees.Delete", Resource = "Employees", Module = "Employees", DisplayName = "إنشاء موظف", Action = "Delete", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Employees.Edit", Resource = "Employees", Module = "Employees", DisplayName = "إنشاء موظف", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Employees.SensitiveEdit", Resource = "Employees", Module = "Employees", DisplayName = "إنشاء موظف", Action = "SensitiveEdit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Employees.SensitiveView", Resource = "Employees", Module = "Employees", DisplayName = "إنشاء موظف", Action = "SensitiveView", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Employees.View", Resource = "Employees", Module = "Employees", DisplayName = "إنشاء موظف", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "ExpiringStockReport.View", Resource = "ExpiringStockReport", Module = "Reports", DisplayName = "Expiry and Near-Expiry Stock", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "FinancialTransactions.Post", Resource = "FinancialTransactions", Module = "Purchases", DisplayName = "FinancialTransactions", Action = "Post", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "FinancialTransactions.Reverse", Resource = "FinancialTransactions", Module = "Purchases", DisplayName = "FinancialTransactions", Action = "Reverse", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "FinancialTransactions.View", Resource = "FinancialTransactions", Module = "Purchases", DisplayName = "FinancialTransactions", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "GoodsReceipt.Confirm", Resource = "GoodsReceipt", Module = "Purchases", DisplayName = "Goods Receipts", Action = "Confirm", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "GoodsReceipt.Create", Resource = "GoodsReceipt", Module = "Purchases", DisplayName = "Goods Receipts", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "GoodsReceipt.View", Resource = "GoodsReceipt", Module = "Purchases", DisplayName = "Goods Receipts", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "InactiveItemsReport.View", Resource = "InactiveItemsReport", Module = "Reports", DisplayName = "الأصناف لم تتحرك منذ مدة", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "InactiveProductsReport.View", Resource = "InactiveProductsReport", Module = "Reports", DisplayName = "تقرير الأصناف غير المتحركة", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "InsuranceClaims.Approve", Resource = "InsuranceClaims", Module = "Insurance", DisplayName = "مطالبات التأمين / Insurance Claims", Action = "Approve", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "InsuranceClaims.Create", Resource = "InsuranceClaims", Module = "Insurance", DisplayName = "مطالبات التأمين / Insurance Claims", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "InsuranceClaims.Submit", Resource = "InsuranceClaims", Module = "Insurance", DisplayName = "مطالبات التأمين / Insurance Claims", Action = "Submit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "InsuranceClaims.View", Resource = "InsuranceClaims", Module = "Insurance", DisplayName = "مطالبات التأمين / Insurance Claims", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "InsuranceCompanies.Create", Resource = "InsuranceCompanies", Module = "Insurance", DisplayName = "إنشاء شركة تأمين", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "InsuranceCompanies.Delete", Resource = "InsuranceCompanies", Module = "Insurance", DisplayName = "إنشاء شركة تأمين", Action = "Delete", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "InsuranceCompanies.Edit", Resource = "InsuranceCompanies", Module = "Insurance", DisplayName = "إنشاء شركة تأمين", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "InsuranceCompanies.View", Resource = "InsuranceCompanies", Module = "Insurance", DisplayName = "إنشاء شركة تأمين", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "InsuranceContracts.Edit", Resource = "InsuranceContracts", Module = "Insurance", DisplayName = "إعدادات التأمين / Insurance Setup", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "InsuranceContracts.View", Resource = "InsuranceContracts", Module = "Insurance", DisplayName = "إعدادات التأمين / Insurance Setup", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "InsuranceNetworks.Edit", Resource = "InsuranceNetworks", Module = "Operations", DisplayName = "InsuranceNetworks", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "InsuranceNetworks.View", Resource = "InsuranceNetworks", Module = "Operations", DisplayName = "InsuranceNetworks", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "InsurancePayments.Create", Resource = "InsurancePayments", Module = "Insurance", DisplayName = "تسويات التأمين / Insurance Settlements", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "InsurancePayments.Reconcile", Resource = "InsurancePayments", Module = "Insurance", DisplayName = "تسويات التأمين / Insurance Settlements", Action = "Reconcile", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "InsurancePayments.View", Resource = "InsurancePayments", Module = "Insurance", DisplayName = "تسويات التأمين / Insurance Settlements", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "InsurancePolicies.Edit", Resource = "InsurancePolicies", Module = "Operations", DisplayName = "InsurancePolicies", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "InsurancePolicies.View", Resource = "InsurancePolicies", Module = "Operations", DisplayName = "InsurancePolicies", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "InsuranceReports.View", Resource = "InsuranceReports", Module = "Operations", DisplayName = "InsuranceReports", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "InventoryMovementSummaryReport.View", Resource = "InventoryMovementSummaryReport", Module = "Reports", DisplayName = "ملخص حركة الأصناف", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "InvoicePaymentMethodsReport.View", Resource = "InvoicePaymentMethodsReport", Module = "Reports", DisplayName = "Invoice Payment Methods", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "InvoicesProfitReport.View", Resource = "InvoicesProfitReport", Module = "Reports", DisplayName = "عرض الفواتير - التكلفة والضريبة والربح", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "ItemCostDetailReport.View", Resource = "ItemCostDetailReport", Module = "Reports", DisplayName = "تفاصيل تكلفة الأصناف", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "MaterialMovementsReport.View", Resource = "MaterialMovementsReport", Module = "Reports", DisplayName = "التسويات المخزنية", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Orders.Create", Resource = "Orders", Module = "Purchases", DisplayName = "طلبات الاستيراد", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Orders.Edit", Resource = "Orders", Module = "Purchases", DisplayName = "طلبات الاستيراد", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Orders.Import", Resource = "Orders", Module = "Purchases", DisplayName = "طلبات الاستيراد", Action = "Import", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Orders.UpdateCartItems", Resource = "Orders", Module = "Purchases", DisplayName = "طلبات الاستيراد", Action = "UpdateCartItems", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Orders.UpdateCartStatus", Resource = "Orders", Module = "Purchases", DisplayName = "طلبات الاستيراد", Action = "UpdateCartStatus", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Orders.View", Resource = "Orders", Module = "Purchases", DisplayName = "طلبات الاستيراد", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "POS.Create", Resource = "POS", Module = "Sales", DisplayName = "نقطة البيع", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "POS.Post", Resource = "POS", Module = "Sales", DisplayName = "نقطة البيع", Action = "Post", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "POS.View", Resource = "POS", Module = "Sales", DisplayName = "نقطة البيع", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PartyStatement.View", Resource = "PartyStatement", Module = "Finance", DisplayName = "كشف حساب المستخدم", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Payroll.Approve", Resource = "Payroll", Module = "Purchases", DisplayName = "Payroll", Action = "Approve", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Payroll.Calculate", Resource = "Payroll", Module = "Purchases", DisplayName = "Payroll", Action = "Calculate", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Payroll.Configure", Resource = "Payroll", Module = "Purchases", DisplayName = "Payroll", Action = "Configure", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Payroll.Correct", Resource = "Payroll", Module = "Purchases", DisplayName = "Payroll", Action = "Correct", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Payroll.View", Resource = "Payroll", Module = "Purchases", DisplayName = "Payroll", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Permissions.ManageRoles", Resource = "Permissions", Module = "Administration", DisplayName = "مدير صلاحيات النظام", Action = "ManageRoles", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Permissions.View", Resource = "Permissions", Module = "Administration", DisplayName = "مدير صلاحيات النظام", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PriceListReport.View", Resource = "PriceListReport", Module = "Reports", DisplayName = "قائمة الأسعار", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "ProductProfitReport.View", Resource = "ProductProfitReport", Module = "Reports", DisplayName = "تقرير أرباح الأصناف", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Products.Create", Resource = "Products", Module = "MasterData", DisplayName = "إعداد البيع الجزئي", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Products.Delete", Resource = "Products", Module = "MasterData", DisplayName = "إعداد البيع الجزئي", Action = "Delete", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Products.Edit", Resource = "Products", Module = "MasterData", DisplayName = "إعداد البيع الجزئي", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Products.View", Resource = "Products", Module = "MasterData", DisplayName = "إعداد البيع الجزئي", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "ProfitLossReport.View", Resource = "ProfitLossReport", Module = "Reports", DisplayName = "تقرير الأرباح والخسائر", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PurchaseControl.Create", Resource = "PurchaseControl", Module = "Purchases", DisplayName = "Spider Pharmacy | Purchase Control Dashboard", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PurchaseControl.View", Resource = "PurchaseControl", Module = "Purchases", DisplayName = "Spider Pharmacy | Purchase Control Dashboard", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PurchaseInvoice.Post", Resource = "PurchaseInvoice", Module = "Purchases", DisplayName = "فاتورة مشتريات", Action = "Post", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PurchaseInvoice.View", Resource = "PurchaseInvoice", Module = "Purchases", DisplayName = "فاتورة مشتريات", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PurchaseOrder.Approve", Resource = "PurchaseOrder", Module = "Purchases", DisplayName = "تفاصيل أمر الشراء", Action = "Approve", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PurchaseOrder.Create", Resource = "PurchaseOrder", Module = "Purchases", DisplayName = "تفاصيل أمر الشراء", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PurchaseOrder.MarkPaid", Resource = "PurchaseOrder", Module = "Purchases", DisplayName = "تفاصيل أمر الشراء", Action = "MarkPaid", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PurchaseOrder.Reject", Resource = "PurchaseOrder", Module = "Purchases", DisplayName = "تفاصيل أمر الشراء", Action = "Reject", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PurchaseOrder.View", Resource = "PurchaseOrder", Module = "Purchases", DisplayName = "تفاصيل أمر الشراء", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PurchaseRequest.Approve", Resource = "PurchaseRequest", Module = "Purchases", DisplayName = "Approve Purchase Request", Action = "Approve", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PurchaseRequest.Create", Resource = "PurchaseRequest", Module = "Purchases", DisplayName = "Approve Purchase Request", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PurchaseRequest.Delete", Resource = "PurchaseRequest", Module = "Purchases", DisplayName = "Approve Purchase Request", Action = "Delete", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PurchaseRequest.Edit", Resource = "PurchaseRequest", Module = "Purchases", DisplayName = "Approve Purchase Request", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PurchaseRequest.Reject", Resource = "PurchaseRequest", Module = "Purchases", DisplayName = "Approve Purchase Request", Action = "Reject", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PurchaseRequest.Review", Resource = "PurchaseRequest", Module = "Purchases", DisplayName = "Approve Purchase Request", Action = "Review", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PurchaseRequest.Submit", Resource = "PurchaseRequest", Module = "Purchases", DisplayName = "Approve Purchase Request", Action = "Submit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PurchaseRequest.View", Resource = "PurchaseRequest", Module = "Purchases", DisplayName = "Approve Purchase Request", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PurchaseReturn.Confirm", Resource = "PurchaseReturn", Module = "Purchases", DisplayName = "PurchaseReturn", Action = "Confirm", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PurchaseReturn.Create", Resource = "PurchaseReturn", Module = "Purchases", DisplayName = "PurchaseReturn", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PurchaseReturn.Post", Resource = "PurchaseReturn", Module = "Purchases", DisplayName = "PurchaseReturn", Action = "Post", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PurchaseReturn.View", Resource = "PurchaseReturn", Module = "Purchases", DisplayName = "PurchaseReturn", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "SalesInvoice.ChangePayment", Resource = "SalesInvoice", Module = "Sales", DisplayName = "Change payment method", Action = "ChangePayment", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "SalesInvoice.Create", Resource = "SalesInvoice", Module = "Sales", DisplayName = "Change payment method", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "SalesInvoice.CreatePOSInvoice", Resource = "SalesInvoice", Module = "Sales", DisplayName = "Change payment method", Action = "CreatePOSInvoice", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "SalesInvoice.Edit", Resource = "SalesInvoice", Module = "Sales", DisplayName = "Change payment method", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "SalesInvoice.View", Resource = "SalesInvoice", Module = "Sales", DisplayName = "Change payment method", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "SalesReport.View", Resource = "SalesReport", Module = "Reports", DisplayName = "تقرير المبيعات", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Settings.ManageSettings", Resource = "Settings", Module = "Administration", DisplayName = "إعدادات نظام المحاسبة", Action = "ManageSettings", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Settings.View", Resource = "Settings", Module = "Administration", DisplayName = "إعدادات نظام المحاسبة", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "StockAdjustment.Create", Resource = "StockAdjustment", Module = "Inventory", DisplayName = "بطاقة ادخال صنف ", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "StockAdjustment.Import", Resource = "StockAdjustment", Module = "Inventory", DisplayName = "بطاقة ادخال صنف ", Action = "Import", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "StockAdjustment.Post", Resource = "StockAdjustment", Module = "Inventory", DisplayName = "بطاقة ادخال صنف ", Action = "Post", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "StockAdjustment.TryParsePositiveQuantity", Resource = "StockAdjustment", Module = "Inventory", DisplayName = "بطاقة ادخال صنف ", Action = "TryParsePositiveQuantity", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "StockAdjustment.View", Resource = "StockAdjustment", Module = "Inventory", DisplayName = "بطاقة ادخال صنف ", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "StockBalanceByDateReport.View", Resource = "StockBalanceByDateReport", Module = "Inventory", DisplayName = "أرصدة المخزون بتاريخ", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "StockBalancesReport.View", Resource = "StockBalancesReport", Module = "Inventory", DisplayName = "الجرد والفرق", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "StockInVoucher.Create", Resource = "StockInVoucher", Module = "Inventory", DisplayName = "استعلام عن سند إدخال", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "StockInVoucher.Edit", Resource = "StockInVoucher", Module = "Inventory", DisplayName = "استعلام عن سند إدخال", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "StockInVoucher.Post", Resource = "StockInVoucher", Module = "Inventory", DisplayName = "استعلام عن سند إدخال", Action = "Post", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "StockInVoucher.View", Resource = "StockInVoucher", Module = "Inventory", DisplayName = "استعلام عن سند إدخال", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "StockLots.Adjust", Resource = "StockLots", Module = "Inventory", DisplayName = "Lot Management / إدارة دفعات المخزون", Action = "Adjust", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "StockLots.Edit", Resource = "StockLots", Module = "Inventory", DisplayName = "Lot Management / إدارة دفعات المخزون", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "StockLots.Post", Resource = "StockLots", Module = "Inventory", DisplayName = "Lot Management / إدارة دفعات المخزون", Action = "Post", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "StockLots.View", Resource = "StockLots", Module = "Inventory", DisplayName = "Lot Management / إدارة دفعات المخزون", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "StockMovementsReport.View", Resource = "StockMovementsReport", Module = "Inventory", DisplayName = "تفصيل حركة المخزون", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "StockOutVoucher.View", Resource = "StockOutVoucher", Module = "Inventory", DisplayName = "سند إخراج بضاعة", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "StockTransactions.Post", Resource = "StockTransactions", Module = "Purchases", DisplayName = "StockTransactions", Action = "Post", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "StockTransactions.PostRange", Resource = "StockTransactions", Module = "Purchases", DisplayName = "StockTransactions", Action = "PostRange", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "StockValuationReport.View", Resource = "StockValuationReport", Module = "Inventory", DisplayName = "تقييم المخزون", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "SubCategories.Create", Resource = "SubCategories", Module = "MasterData", DisplayName = "إنشاء فئة فرعية", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "SubCategories.Delete", Resource = "SubCategories", Module = "MasterData", DisplayName = "إنشاء فئة فرعية", Action = "Delete", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "SubCategories.Edit", Resource = "SubCategories", Module = "MasterData", DisplayName = "إنشاء فئة فرعية", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "SubCategories.View", Resource = "SubCategories", Module = "MasterData", DisplayName = "إنشاء فئة فرعية", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "SupplierInvoice.Match", Resource = "SupplierInvoice", Module = "Purchases", DisplayName = "SupplierInvoice", Action = "Match", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "SupplierPayment.Allocate", Resource = "SupplierPayment", Module = "Purchases", DisplayName = "SupplierPayment", Action = "Allocate", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Units.Create", Resource = "Units", Module = "MasterData", DisplayName = "إنشاء وحدة", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Units.Delete", Resource = "Units", Module = "MasterData", DisplayName = "إنشاء وحدة", Action = "Delete", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Units.Edit", Resource = "Units", Module = "MasterData", DisplayName = "إنشاء وحدة", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Units.View", Resource = "Units", Module = "MasterData", DisplayName = "إنشاء وحدة", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Users.Create", Resource = "Users", Module = "Administration", DisplayName = "إنشاء مستخدم", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Users.Delete", Resource = "Users", Module = "Administration", DisplayName = "إنشاء مستخدم", Action = "Delete", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Users.Edit", Resource = "Users", Module = "Administration", DisplayName = "إنشاء مستخدم", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Users.View", Resource = "Users", Module = "Administration", DisplayName = "إنشاء مستخدم", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Vouchers.Create", Resource = "Vouchers", Module = "Finance", DisplayName = "سند دفع", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Vouchers.Edit", Resource = "Vouchers", Module = "Finance", DisplayName = "سند دفع", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Vouchers.Post", Resource = "Vouchers", Module = "Finance", DisplayName = "سند دفع", Action = "Post", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Vouchers.Print", Resource = "Vouchers", Module = "Finance", DisplayName = "سند دفع", Action = "Print", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Vouchers.View", Resource = "Vouchers", Module = "Finance", DisplayName = "سند دفع", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Warehouses.Create", Resource = "Warehouses", Module = "MasterData", DisplayName = "إضافة مستودع", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Warehouses.Delete", Resource = "Warehouses", Module = "MasterData", DisplayName = "إضافة مستودع", Action = "Delete", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Warehouses.Edit", Resource = "Warehouses", Module = "MasterData", DisplayName = "إضافة مستودع", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Warehouses.View", Resource = "Warehouses", Module = "MasterData", DisplayName = "إضافة مستودع", Action = "View", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.Checks.Create", Resource = "Accounting.Checks", Module = "Accounting", DisplayName = "تعديل الشيك", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.Checks.Edit", Resource = "Accounting.Checks", Module = "Accounting", DisplayName = "تعديل الشيك", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.JournalEntries.Reverse", Resource = "Accounting.JournalEntries", Module = "Accounting", DisplayName = "سجل القيود", Action = "Reverse", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.JournalEntry.Delete", Resource = "Accounting.JournalEntry", Module = "Accounting", DisplayName = "قيد يومية يدوي", Action = "Delete", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.PartyBalances.Export", Resource = "Accounting.PartyBalances", Module = "Accounting", DisplayName = "Accounts Balances", Action = "Export", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Accounting.PartyBalances.Print", Resource = "Accounting.PartyBalances", Module = "Accounting", DisplayName = "Accounts Balances", Action = "Print", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "BelowMinimumStockReport.Export", Resource = "BelowMinimumStockReport", Module = "Reports", DisplayName = "بضائع تحت الحد الأدنى", Action = "Export", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "CashFlowReport.Export", Resource = "CashFlowReport", Module = "Reports", DisplayName = "المدفوعات والمقبوضات", Action = "Export", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "CashierReport.Print", Resource = "CashierReport", Module = "Reports", DisplayName = "لوحة الكاشيرات", Action = "Print", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "CurrentStockReport.Export", Resource = "CurrentStockReport", Module = "Reports", DisplayName = "تقرير الجرد", Action = "Export", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "InsuranceClaims.Edit", Resource = "InsuranceClaims", Module = "Insurance", DisplayName = "مطالبات التأمين / Insurance Claims", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "InsuranceContracts.Create", Resource = "InsuranceContracts", Module = "Insurance", DisplayName = "إعدادات التأمين / Insurance Setup", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "InvoicePaymentMethodsReport.Export", Resource = "InvoicePaymentMethodsReport", Module = "Reports", DisplayName = "Invoice Payment Methods", Action = "Export", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Orders.Delete", Resource = "Orders", Module = "Purchases", DisplayName = "طلبات الاستيراد", Action = "Delete", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Orders.Export", Resource = "Orders", Module = "Purchases", DisplayName = "طلبات الاستيراد", Action = "Export", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Orders.Print", Resource = "Orders", Module = "Purchases", DisplayName = "طلبات الاستيراد", Action = "Print", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "POS.ApplyDiscount", Resource = "POS", Module = "Sales", DisplayName = "نقطة البيع", Action = "ApplyDiscount", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "POS.Cancel", Resource = "POS", Module = "Sales", DisplayName = "نقطة البيع", Action = "Cancel", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "POS.Delete", Resource = "POS", Module = "Sales", DisplayName = "نقطة البيع", Action = "Delete", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "POS.Edit", Resource = "POS", Module = "Sales", DisplayName = "نقطة البيع", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "POS.Print", Resource = "POS", Module = "Sales", DisplayName = "نقطة البيع", Action = "Print", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PartyStatement.Export", Resource = "PartyStatement", Module = "Finance", DisplayName = "كشف حساب المستخدم", Action = "Export", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PartyStatement.Print", Resource = "PartyStatement", Module = "Finance", DisplayName = "كشف حساب المستخدم", Action = "Print", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PurchaseControl.Edit", Resource = "PurchaseControl", Module = "Purchases", DisplayName = "Spider Pharmacy | Purchase Control Dashboard", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PurchaseInvoice.Create", Resource = "PurchaseInvoice", Module = "Purchases", DisplayName = "فاتورة مشتريات", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PurchaseInvoice.Edit", Resource = "PurchaseInvoice", Module = "Purchases", DisplayName = "فاتورة مشتريات", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PurchaseInvoice.Print", Resource = "PurchaseInvoice", Module = "Purchases", DisplayName = "فاتورة مشتريات", Action = "Print", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PurchaseOrder.Print", Resource = "PurchaseOrder", Module = "Purchases", DisplayName = "تفاصيل أمر الشراء", Action = "Print", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "PurchaseRequest.Print", Resource = "PurchaseRequest", Module = "Purchases", DisplayName = "Approve Purchase Request", Action = "Print", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "SalesInvoice.Confirm", Resource = "SalesInvoice", Module = "Sales", DisplayName = "Change payment method", Action = "Confirm", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "SalesInvoice.Post", Resource = "SalesInvoice", Module = "Sales", DisplayName = "Change payment method", Action = "Post", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "SalesInvoice.Print", Resource = "SalesInvoice", Module = "Sales", DisplayName = "Change payment method", Action = "Print", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "SalesReport.Export", Resource = "SalesReport", Module = "Reports", DisplayName = "تقرير المبيعات", Action = "Export", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "SalesReport.Print", Resource = "SalesReport", Module = "Reports", DisplayName = "تقرير المبيعات", Action = "Print", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "StockAdjustment.Edit", Resource = "StockAdjustment", Module = "Inventory", DisplayName = "بطاقة ادخال صنف ", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "StockInVoucher.Print", Resource = "StockInVoucher", Module = "Inventory", DisplayName = "استعلام عن سند إدخال", Action = "Print", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "StockMovementsReport.Export", Resource = "StockMovementsReport", Module = "Inventory", DisplayName = "تفصيل حركة المخزون", Action = "Export", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "StockMovementsReport.Print", Resource = "StockMovementsReport", Module = "Inventory", DisplayName = "تفصيل حركة المخزون", Action = "Print", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "StockOutVoucher.Create", Resource = "StockOutVoucher", Module = "Inventory", DisplayName = "سند إخراج بضاعة", Action = "Create", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "StockOutVoucher.Edit", Resource = "StockOutVoucher", Module = "Inventory", DisplayName = "سند إخراج بضاعة", Action = "Edit", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "StockOutVoucher.Post", Resource = "StockOutVoucher", Module = "Inventory", DisplayName = "سند إخراج بضاعة", Action = "Post", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "StockOutVoucher.Print", Resource = "StockOutVoucher", Module = "Inventory", DisplayName = "سند إخراج بضاعة", Action = "Print", SortOrder = sort++, IsLegacy = false };
        yield return new() { Key = "Customers.List", Resource = "Customers", Module = "MasterData", DisplayName = "العملاء", Action = "List", SortOrder = sort++, IsLegacy = true };
    }
}

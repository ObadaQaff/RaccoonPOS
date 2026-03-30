namespace RaccoonWarehouse.Domain.Permissions
{
    public sealed class PermissionCatalogItem
    {
        public string Key { get; init; } = string.Empty;
        public string Module { get; init; } = string.Empty;
        public string Resource { get; init; } = string.Empty;
        public string Action { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string? LegacyReportKey { get; init; }
        public int SortOrder { get; init; }
    }

    public static class PermissionCatalog
    {
        public static IReadOnlyList<PermissionCatalogItem> All { get; } = BuildPermissions();

        public static PermissionCatalogItem? FindByKey(string key)
        {
            return All.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
        }

        public static PermissionCatalogItem? FindByLegacyReportKey(string reportKey)
        {
            return All.FirstOrDefault(x => string.Equals(x.LegacyReportKey, reportKey, StringComparison.OrdinalIgnoreCase));
        }

        private static IReadOnlyList<PermissionCatalogItem> BuildPermissions()
        {
            var items = new List<PermissionCatalogItem>();
            var sort = 1;

            AddResource(items, ref sort, "Dashboard", "Dashboard", "لوحة التحكم", "View");
            AddResource(items, ref sort, "Sales", "SalesInvoice", "فاتورة المبيعات", "View", "Create", "Edit", "Delete", "Print", "Cancel", "Return", "Post", "ApplyDiscount", "ChangePrice", "Reopen");
            AddResource(items, ref sort, "Sales", "POS", "نقطة البيع", "View", "Create", "Print", "ApplyDiscount", "ChangePrice", "CloseShift", "ReopenShift");
            AddResource(items, ref sort, "Purchases", "PurchaseInvoice", "فاتورة المشتريات", "View", "Create", "Edit", "Delete", "Print", "Approve", "Post");
            AddResource(items, ref sort, "Inventory", "StockInVoucher", "سند إدخال بضاعة", "View", "Create", "Edit", "Delete", "Print", "Approve", "Post");
            AddResource(items, ref sort, "Inventory", "StockOutVoucher", "سند إخراج بضاعة", "View", "Create", "Edit", "Delete", "Print", "Approve", "Post");
            AddResource(items, ref sort, "Inventory", "StockTransferVoucher", "سند نقل مخزني", "View", "Create", "Edit", "Delete", "Print", "Approve", "Post");
            AddResource(items, ref sort, "Inventory", "StockAdjustment", "التسويات المخزنية", "View", "Create", "Edit", "Delete", "Print", "Approve", "Post");
            AddResource(items, ref sort, "MasterData", "Customers", "العملاء", "View", "Create", "Edit", "Delete");
            AddResource(items, ref sort, "MasterData", "Suppliers", "الموردون", "View", "Create", "Edit", "Delete");
            AddResource(items, ref sort, "MasterData", "Products", "الأصناف", "View", "Create", "Edit", "Delete", "ChangePrice", "ViewCost");
            AddResource(items, ref sort, "MasterData", "Warehouses", "المستودعات", "View", "Create", "Edit", "Delete");
            AddResource(items, ref sort, "MasterData", "Branches", "الفروع", "View", "Create", "Edit", "Delete");
            AddResource(items, ref sort, "MasterData", "Employees", "الموظفون", "View", "Create", "Edit", "Delete");
            AddResource(items, ref sort, "MasterData", "Delegates", "المندوبون", "View", "Create", "Edit", "Delete");
            AddResource(items, ref sort, "Finance", "Vouchers", "السندات", "View", "Create", "Edit", "Delete", "Print", "Approve", "Post");
            AddResource(items, ref sort, "Finance", "CashDrawer", "الصندوق", "View", "AccessCashDrawer", "CloseShift", "ReopenShift");
            AddResource(items, ref sort, "Finance", "SensitiveFinancial", "العمليات المالية الحساسة", "ViewCost", "ViewProfit", "ApplyDiscount", "ApproveHighDiscount", "CancelPaidInvoice", "ChangePrice");
            AddResource(items, ref sort, "Administration", "Users", "المستخدمون", "View", "Create", "Edit", "Delete", "ManageUsers");
            AddResource(items, ref sort, "Administration", "Permissions", "الصلاحيات", "View", "ManageRoles", "ManageSettings");
            AddResource(items, ref sort, "Administration", "Settings", "الإعدادات", "View", "ManageSettings");
            AddResource(items, ref sort, "Administration", "AuditLogs", "سجل المراجعة", "View", "Export");

            AddReportPermissions(items, ref sort);
            return items;
        }

        private static void AddResource(List<PermissionCatalogItem> items, ref int sort, string module, string resource, string displayName, params string[] actions)
        {
            foreach (var action in actions)
            {
                items.Add(new PermissionCatalogItem
                {
                    Key = $"{resource}.{action}",
                    Module = module,
                    Resource = resource,
                    Action = action,
                    DisplayName = displayName,
                    SortOrder = sort++
                });
            }
        }

        private static void AddReportPermissions(List<PermissionCatalogItem> items, ref int sort)
        {
            var reportMap = new (string LegacyKey, string Resource, string DisplayName)[]
            {
                ("current-stock", "CurrentStockReport", "المخزون الحالي"),
                ("stock-movements", "StockMovementsReport", "تفصيل حركة المخزون"),
                ("stock-balance-by-date", "StockBalanceByDateReport", "أرصدة المخزون بتاريخ معين"),
                ("below-min-stock", "BelowMinimumStockReport", "بضائع تحت الحد الأدنى"),
                ("inventory-movement-summary", "InventoryMovementSummaryReport", "ملخص حركة الأصناف"),
                ("stock-valuation", "StockValuationReport", "تقييم المخزون"),
                ("inactive-products", "InactiveProductsReport", "أصناف لم تتحرك منذ مدة"),
                ("sales-report", "SalesReport", "تقرير المبيعات"),
                ("invoices-profit", "InvoicesProfitReport", "تحليل ربحية الفواتير"),
                ("product-profit", "ProductProfitReport", "أرباح الأصناف"),
                ("cash-flow", "CashFlowReport", "التحصيل والدفع"),
                ("profit-loss", "ProfitLossReport", "تقرير الأرباح والخسائر"),
                ("credit-sales", "CreditSalesReport", "تقرير مبيعات الآجل"),
                ("discount-summary", "DiscountSummaryReport", "ملخص الخصومات"),
                ("item-cost-detail", "ItemCostDetailReport", "تفاصيل تكلفة الأصناف"),
                ("price-list", "PriceListReport", "قائمة الأسعار"),
                ("stock-balances", "StockBalancesReport", "الجرد والفرق"),
                ("material-movements", "MaterialMovementsReport", "التسويات المخزنية"),
                ("inactive-items", "InactiveItemsReport", "الأصناف الراكدة")
            };

            foreach (var report in reportMap)
            {
                items.Add(new PermissionCatalogItem
                {
                    Key = $"{report.Resource}.View",
                    Module = "Reports",
                    Resource = report.Resource,
                    Action = "View",
                    DisplayName = report.DisplayName,
                    LegacyReportKey = report.LegacyKey,
                    SortOrder = sort++
                });

                items.Add(new PermissionCatalogItem
                {
                    Key = $"{report.Resource}.Export",
                    Module = "Reports",
                    Resource = report.Resource,
                    Action = "Export",
                    DisplayName = report.DisplayName,
                    LegacyReportKey = report.LegacyKey,
                    SortOrder = sort++
                });
            }
        }
    }
}

namespace RaccoonWarehouse.Settings
{
    public sealed class ReportCatalogItem
    {
        public string Key { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
    }

    public static class ReportCatalog
    {
        public static IReadOnlyList<ReportCatalogItem> All { get; } = new List<ReportCatalogItem>
        {
            new() { Key = "current-stock", DisplayName = "المخزون الحالي", Category = " الاصناف والمخزون" },
            new() { Key = "stock-movements", DisplayName = "حركات الاصناف", Category = " الاصناف والمخزون" },
            new() { Key = "stock-balance-by-date", DisplayName = "أرصدة المخزون بتاريخ معين", Category = " الاصناف والمخزون" },
            new() { Key = "below-min-stock", DisplayName = "بضائع تحت الحد الأدنى", Category = " الاصناف والمخزون" },
            new() { Key = "inventory-movement-summary", DisplayName = "ملخص حركات المخزون", Category = " الاصناف والمخزون" },
            new() { Key = "inactive-products", DisplayName = "اصناف لم تتحرك منذ مدة", Category = " الاصناف والمخزون" },
            new() { Key = "sales-report", DisplayName = "تقرير المبيعات", Category = "التقارير المالية" },
            new() { Key = "invoices-profit", DisplayName = "تحليل ربحية الفواتير", Category = "التقارير المالية" },
            new() { Key = "product-profit", DisplayName = "أرباح الأصناف", Category = "التقارير المالية" },
            new() { Key = "cash-flow", DisplayName = "التحصيل والدفع", Category = "التقارير المالية" },
            new() { Key = "profit-loss", DisplayName = "تقرير الأرباح والخسائر", Category = "التقارير المالية" },
            new() { Key = "credit-sales", DisplayName = "تقرير مبيعات الآجل", Category = "متنوعة" },
            new() { Key = "discount-summary", DisplayName = "ملخص الخصومات", Category = "متنوعة" },
            new() { Key = "item-cost-detail", DisplayName = "تفاصيل تكلفة الأصناف", Category = "متنوعة" },
            new() { Key = "price-list", DisplayName = "قائمة الأسعار", Category = "متنوعة" },
            new() { Key = "stock-balances", DisplayName = "أرصدة المخزون", Category = "متنوعة" },
            new() { Key = "material-movements", DisplayName = "حركة المواد", Category = "متنوعة" },
            new() { Key = "inactive-items", DisplayName = "الأصناف الراكدة", Category = "متنوعة" }
        };

        public static ReportCatalogItem? FindByDisplayName(string displayName)
        {
            return All.FirstOrDefault(x => string.Equals(x.DisplayName, displayName, StringComparison.Ordinal));
        }
    }
}

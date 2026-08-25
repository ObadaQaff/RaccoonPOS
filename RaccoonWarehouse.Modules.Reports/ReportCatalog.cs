using RaccoonWarehouse.Navigation;

namespace RaccoonWarehouse.Settings
{
    public sealed record ReportCatalogItem(
        string Key,
        string ArabicDisplayName,
        string EnglishDisplayName,
        string ArabicCategory,
        string EnglishCategory,
        WindowSizeType WindowSize = WindowSizeType.LargeRectangle,
        bool UseLoadingWrapper = true);

    public static class ReportCatalog
    {
        public static IReadOnlyList<ReportCatalogItem> All { get; } = new List<ReportCatalogItem>
        {
            new("current-stock", "تقرير الجرد", "Inventory report", "الأصناف والمخزون", "Inventory", WindowSizeType.MediumRectangle, false),
            new("stock-movements", "تفصيل حركة المخزون", "Stock movements", "الأصناف والمخزون", "Inventory"),
            new("stock-balance-by-date", "أرصدة المخزون بتاريخ معين", "Stock balance by date", "الأصناف والمخزون", "Inventory"),
            new("below-min-stock", "بضائع تحت الحد الأدنى", "Below minimum stock", "الأصناف والمخزون", "Inventory"),
            new("inventory-movement-summary", "ملخص حركة الأصناف", "Inventory movement summary", "الأصناف والمخزون", "Inventory", WindowSizeType.LargeRectangle, false),
            new("stock-valuation", "تقييم المخزون", "Stock valuation", "الأصناف والمخزون", "Inventory"),
            new("inactive-products", "أصناف لم تتحرك منذ مدة", "Inactive products", "الأصناف والمخزون", "Inventory"),
            new("sales-report", "تقرير المبيعات", "Sales report", "التقارير المالية", "Financial reports"),
            new("invoices-profit", "تحليل ربحية الفواتير", "Invoice profitability", "التقارير المالية", "Financial reports"),
            new("product-profit", "أرباح الأصناف", "Product profit", "التقارير المالية", "Financial reports"),
            new("cash-flow", "التحصيل والدفع", "Cash flow", "التقارير المالية", "Financial reports"),
            new("profit-loss", "تقرير الأرباح والخسائر", "Profit and loss", "التقارير المالية", "Financial reports"),
            new("credit-sales", "تقرير مبيعات الآجل", "Credit sales", "متنوعة", "Miscellaneous"),
            new("discount-summary", "ملخص الخصومات", "Discount summary", "متنوعة", "Miscellaneous"),
            new("item-cost-detail", "تفاصيل تكلفة الأصناف", "Item cost detail", "متنوعة", "Miscellaneous"),
            new("price-list", "قائمة الأسعار", "Price list", "متنوعة", "Miscellaneous"),
            new("stock-balances", "الجرد والفرق", "Stock balances", "الأصناف والمخزون", "Inventory"),
            new("material-movements", "التسويات المخزنية", "Material movements", "الأصناف والمخزون", "Inventory"),
            new("inactive-items", "الأصناف الراكدة", "Inactive items", "متنوعة", "Miscellaneous")
        };
    }
}

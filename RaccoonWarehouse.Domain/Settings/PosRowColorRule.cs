using System.Text.Json.Serialization;

namespace RaccoonWarehouse.Domain.Settings;

public sealed class PosRowColorRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; }
    public string Scope { get; set; } = "Both";
    public string ConditionType { get; set; } = "None";
    public decimal? NumericValue { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public List<int> ProductIds { get; set; } = new();
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public string ForegroundColor { get; set; } = "#263238";
    public string AccentColor { get; set; } = "#90A4AE";
    public string? Label { get; set; }
    // Presentation and sales-policy behavior are configured together, but the
    // policy is evaluated in memory and is never stored in the product catalog.
    public string SaleAction { get; set; } = PosRowColorActions.None;
}

public static class PosRowColorActions
{
    public const string None = "None";
    public const string Warn = "Warn";
    public const string RequireConfirmation = "RequireConfirmation";
    public const string BlockSale = "BlockSale";
    public const string BlockDiscount = "BlockDiscount";

    public static readonly IReadOnlyList<string> All =
        new[] { None, Warn, RequireConfirmation, BlockSale, BlockDiscount };
}

public sealed record PosRowColorDecision(
    string BackgroundColor,
    string ForegroundColor,
    string AccentColor,
    string? Label = null,
    string RuleId = "",
    string RuleName = "",
    string SaleAction = PosRowColorActions.None,
    decimal? DiscountLimitPercent = null)
{
    public bool BlocksSale => string.Equals(SaleAction, PosRowColorActions.BlockSale, StringComparison.OrdinalIgnoreCase);
    public bool RequiresConfirmation => string.Equals(SaleAction, PosRowColorActions.RequireConfirmation, StringComparison.OrdinalIgnoreCase);
    public bool BlocksDiscount => string.Equals(SaleAction, PosRowColorActions.BlockDiscount, StringComparison.OrdinalIgnoreCase);
}

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
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public string ForegroundColor { get; set; } = "#263238";
    public string AccentColor { get; set; } = "#90A4AE";
    public string? Label { get; set; }
}

public sealed record PosRowColorDecision(
    string BackgroundColor,
    string ForegroundColor,
    string AccentColor,
    string? Label = null);

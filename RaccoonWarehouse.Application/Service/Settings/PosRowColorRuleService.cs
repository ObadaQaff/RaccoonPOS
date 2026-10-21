using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Settings;

namespace RaccoonWarehouse.Application.Service.Settings;

public interface IPosRowColorRuleService
{
    Task<IReadOnlyList<PosRowColorRule>> GetRulesAsync(CancellationToken cancellationToken = default);
    Task SaveRulesAsync(IReadOnlyList<PosRowColorRule> rules, CancellationToken cancellationToken = default);
    void InvalidateCache();
    PosRowColorDecision Classify(
        DateTime? expiryDate,
        decimal stockQuantity,
        decimal discountPercent,
        int? categoryId = null,
        string? categoryName = null,
        bool discountBlocked = false);
}

public sealed class PosRowColorRuleService : IPosRowColorRuleService
{
    public const string SettingKey = "Pos.RowColorRules";
    private readonly ApplicationDbContext _dbContext;
    private IReadOnlyList<PosRowColorRule>? _cachedRules;

    public PosRowColorRuleService(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<PosRowColorRule>> GetRulesAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedRules != null)
            return _cachedRules;

        var value = await _dbContext.AppSettings.AsNoTracking()
            .Where(x => x.Key == SettingKey)
            .Select(x => x.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(value))
            return _cachedRules = DefaultRules();

        try
        {
            var rules = JsonSerializer.Deserialize<List<PosRowColorRule>>(value,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return _cachedRules = rules?.Where(x => !string.IsNullOrWhiteSpace(x.Id)).ToList()
                ?? DefaultRules();
        }
        catch (JsonException)
        {
            return _cachedRules = DefaultRules();
        }
    }

    public async Task SaveRulesAsync(IReadOnlyList<PosRowColorRule> rules, CancellationToken cancellationToken = default)
    {
        var normalized = rules.Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .Select(x => { x.Id = string.IsNullOrWhiteSpace(x.Id) ? Guid.NewGuid().ToString("N") : x.Id; return x; })
            .ToList();
        var value = JsonSerializer.Serialize(normalized, new JsonSerializerOptions { WriteIndented = true });
        var setting = await _dbContext.AppSettings.FirstOrDefaultAsync(x => x.Key == SettingKey, cancellationToken);
        if (setting == null)
        {
            _dbContext.AppSettings.Add(new Domain.Settings.AppSetting
            {
                Key = SettingKey, Value = value,
                Description = "POS and product-search row color rules.",
                CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now
            });
        }
        else
        {
            setting.Value = value;
            setting.UpdatedDate = DateTime.Now;
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
        _cachedRules = normalized;
    }

    public void InvalidateCache() => _cachedRules = null;

    public PosRowColorDecision Classify(DateTime? expiryDate, decimal stockQuantity, decimal discountPercent,
        int? categoryId = null, string? categoryName = null, bool discountBlocked = false)
    {
        var today = DateTime.Today;
        var daysToExpiry = expiryDate.HasValue ? (expiryDate.Value.Date - today).Days : (int?)null;
        var rules = _cachedRules ?? DefaultRules();
        foreach (var rule in rules.Where(x => x.IsActive).OrderByDescending(x => x.Priority))
        {
            var matches = rule.ConditionType switch
            {
                "Expired" => daysToExpiry <= 0,
                "ExpiryWithinDays" => daysToExpiry.HasValue && daysToExpiry > 0 && daysToExpiry <= rule.NumericValue,
                "DiscountAtLeast" => !discountBlocked && discountPercent >= rule.NumericValue,
                "DiscountBlocked" => discountBlocked,
                "LowStock" => stockQuantity <= rule.NumericValue,
                "Category" => (rule.CategoryId.HasValue && categoryId == rule.CategoryId) ||
                               (!string.IsNullOrWhiteSpace(rule.CategoryName) && string.Equals(rule.CategoryName, categoryName, StringComparison.OrdinalIgnoreCase)),
                _ => false
            };
            if (matches)
                return new(rule.BackgroundColor, rule.ForegroundColor, rule.AccentColor, rule.Label);
        }
        return new("#FFFFFF", "#263238", "#90A4AE");
    }

    public static List<PosRowColorRule> DefaultRules() => new()
    {
        Rule("Expired", 100, "Expired", null, "#FDE2E2", "#9B1C1C", "#DC2626", "Expired"),
        Rule("Expiry within 30 days", 90, "ExpiryWithinDays", 30, "#FCE7F3", "#9D174D", "#DB2777", "Near expiry"),
        Rule("Expiry within 90 days", 80, "ExpiryWithinDays", 90, "#FFF1D6", "#92400E", "#F59E0B", "Expiry warning"),
        Rule("Discount 10% or more", 70, "DiscountAtLeast", 10, "#E2F7E9", "#166534", "#16A34A", "10%+ discount"),
        Rule("Discount 5% or more", 60, "DiscountAtLeast", 5, "#F0FDF4", "#166534", "#65A30D", "5%+ discount"),
        Rule("Low stock", 40, "LowStock", 10, "#FEF3C7", "#92400E", "#D97706", "Low stock")
    };

    private static PosRowColorRule Rule(string name, int priority, string type, decimal? value,
        string background, string foreground, string accent, string label) => new()
    {
        Name = name, Priority = priority, ConditionType = type, NumericValue = value,
        BackgroundColor = background, ForegroundColor = foreground, AccentColor = accent, Label = label
    };
}

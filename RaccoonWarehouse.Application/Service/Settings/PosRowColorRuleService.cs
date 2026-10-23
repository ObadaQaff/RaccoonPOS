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
    decimal? GetDefaultDiscountPercent(
        int? categoryId = null,
        string? categoryName = null,
        string scope = "POS",
        int? productId = null);
    PosRowColorDecision Classify(
        DateTime? expiryDate,
        decimal stockQuantity,
        decimal discountPercent,
        int? categoryId = null,
        string? categoryName = null,
        bool discountBlocked = false,
        string scope = "Both",
        int? productId = null);
}

public sealed class PosRowColorRuleService : IPosRowColorRuleService
{
    public const string SettingKey = "Pos.RowColorRules";
    public static event Action? RulesChanged;
    private readonly ApplicationDbContext _dbContext;
    private static readonly object CacheSync = new();
    private static readonly SemaphoreSlim CacheLoadLock = new(1, 1);
    private static IReadOnlyList<PosRowColorRule>? _cachedRules;
    private static long _cacheGeneration;

    public PosRowColorRuleService(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<PosRowColorRule>> GetRulesAsync(CancellationToken cancellationToken = default)
    {
        await CacheLoadLock.WaitAsync(cancellationToken);
        try
        {
            while (true)
            {
                long generation;
                lock (CacheSync)
                {
                    if (_cachedRules != null)
                        return _cachedRules;
                    generation = _cacheGeneration;
                }

                var rules = await LoadRulesAsync(cancellationToken);
                lock (CacheSync)
                {
                    // A POS launch or settings save supersedes an older load.
                    if (generation != _cacheGeneration)
                        continue;
                    return _cachedRules = rules;
                }
            }
        }
        finally
        {
            CacheLoadLock.Release();
        }
    }

    private async Task<IReadOnlyList<PosRowColorRule>> LoadRulesAsync(CancellationToken cancellationToken)
    {
        var value = await _dbContext.AppSettings.AsNoTracking()
            .Where(x => x.Key == SettingKey)
            .Select(x => x.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(value))
            return DefaultRules();

        try
        {
            var rules = JsonSerializer.Deserialize<List<PosRowColorRule>>(value,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return NormalizeRules(rules ?? DefaultRules());
        }
        catch (JsonException)
        {
            return DefaultRules();
        }
    }

    public async Task SaveRulesAsync(IReadOnlyList<PosRowColorRule> rules, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeRules(rules.Where(x => !string.IsNullOrWhiteSpace(x.Name)));
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
        lock (CacheSync)
        {
            _cacheGeneration++;
            _cachedRules = normalized;
        }
        RulesChanged?.Invoke();
    }

    public void InvalidateCache()
    {
        lock (CacheSync)
        {
            _cacheGeneration++;
            _cachedRules = null;
        }
    }

    public decimal? GetDefaultDiscountPercent(
        int? categoryId = null,
        string? categoryName = null,
        string scope = "POS",
        int? productId = null)
    {
        var rules = Volatile.Read(ref _cachedRules) ?? DefaultRules();
        return rules
            .Where(rule => rule.IsActive
                && string.Equals(rule.ConditionType, "DefaultDiscount", StringComparison.OrdinalIgnoreCase)
                && AppliesToScope(rule, scope)
                && MatchesOptionalTarget(rule, categoryId, categoryName, productId)
                && rule.NumericValue.HasValue
                && rule.NumericValue.Value >= 0m
                && rule.NumericValue.Value <= 100m)
            .OrderByDescending(rule => rule.Priority)
            .Select(rule => rule.NumericValue)
            .FirstOrDefault();
    }

    public PosRowColorDecision Classify(DateTime? expiryDate, decimal stockQuantity, decimal discountPercent,
        int? categoryId = null, string? categoryName = null, bool discountBlocked = false, string scope = "Both", int? productId = null)
    {
        var today = DateTime.Today;
        var daysToExpiry = expiryDate.HasValue ? (expiryDate.Value.Date - today).Days : (int?)null;
        var rules = Volatile.Read(ref _cachedRules) ?? DefaultRules();
        foreach (var rule in rules
            .Where(x => x.IsActive && AppliesToScope(x, scope))
            .OrderByDescending(x => x.Priority)
            .ThenByDescending(x => GetTargetSpecificity(x, categoryId, categoryName, productId)))
        {
            var isDiscountLimitRule = string.Equals(rule.ConditionType, "DiscountMaximum", StringComparison.OrdinalIgnoreCase)
                && string.Equals(rule.SaleAction, PosRowColorActions.BlockDiscount, StringComparison.OrdinalIgnoreCase)
                && rule.NumericValue.HasValue;
            var matches = rule.ConditionType switch
            {
                "Expired" => daysToExpiry <= 0,
                "ExpiryWithinDays" => daysToExpiry.HasValue && daysToExpiry > 0 && daysToExpiry <= rule.NumericValue,
                "DiscountMaximum" => MatchesOptionalTarget(rule, categoryId, categoryName, productId)
                    && (string.Equals(scope, "ProductSearch", StringComparison.OrdinalIgnoreCase)
                        || (!discountBlocked
                            && rule.NumericValue.HasValue
                            && discountPercent > rule.NumericValue.Value)),
                "DefaultDiscount" => MatchesOptionalTarget(rule, categoryId, categoryName, productId),
                "DiscountBlocked" => discountBlocked,
                "LowStock" => stockQuantity <= rule.NumericValue,
                "Category" => (rule.CategoryId.HasValue && categoryId == rule.CategoryId) ||
                               (!string.IsNullOrWhiteSpace(rule.CategoryName) && string.Equals(rule.CategoryName, categoryName, StringComparison.OrdinalIgnoreCase)),
                "Product" => productId.HasValue && rule.ProductIds?.Contains(productId.Value) == true,
                _ => false
            };
            if (matches)
            {
                var action = PosRowColorActions.All.Contains(rule.SaleAction, StringComparer.OrdinalIgnoreCase)
                    ? rule.SaleAction
                    : PosRowColorActions.None;
                return new(rule.BackgroundColor, rule.ForegroundColor, rule.AccentColor, rule.Label,
                    rule.Id, rule.Name, action, isDiscountLimitRule ? rule.NumericValue : null);
            }
        }
        return new("#FFFFFF", "#263238", "#90A4AE");
    }

    private static bool MatchesOptionalTarget(PosRowColorRule rule, int? categoryId, string? categoryName, int? productId)
    {
        if (rule.ProductIds?.Count > 0)
            return productId.HasValue && rule.ProductIds.Contains(productId.Value);

        if (rule.CategoryId.HasValue || !string.IsNullOrWhiteSpace(rule.CategoryName))
            return (rule.CategoryId.HasValue && categoryId == rule.CategoryId) ||
                   (!string.IsNullOrWhiteSpace(rule.CategoryName) && string.Equals(rule.CategoryName, categoryName, StringComparison.OrdinalIgnoreCase));

        return true;
    }

    private static int GetTargetSpecificity(PosRowColorRule rule, int? categoryId, string? categoryName, int? productId)
    {
        if (rule.ProductIds?.Count > 0 && productId.HasValue && rule.ProductIds.Contains(productId.Value))
            return 2;

        if ((rule.CategoryId.HasValue && categoryId == rule.CategoryId) ||
            (!string.IsNullOrWhiteSpace(rule.CategoryName) &&
             string.Equals(rule.CategoryName, categoryName, StringComparison.OrdinalIgnoreCase)))
            return 1;

        return 0;
    }

    private static List<PosRowColorRule> NormalizeRules(IEnumerable<PosRowColorRule> rules)
    {
        var normalized = rules.Where(x => !string.IsNullOrWhiteSpace(x.Name)).ToList();
        foreach (var rule in normalized)
        {
            if (string.IsNullOrWhiteSpace(rule.Id))
                rule.Id = Guid.NewGuid().ToString("N");

            // Existing databases used DiscountAtLeast for the old maximum-discount behavior.
            // Interpret and save those records using the new explicit condition name.
            if (string.Equals(rule.ConditionType, "DiscountAtLeast", StringComparison.OrdinalIgnoreCase))
                rule.ConditionType = "DiscountMaximum";
        }

        return normalized.Count > 0 ? normalized : DefaultRules();
    }

    private static bool AppliesToScope(PosRowColorRule rule, string scope)
    {
        var ruleScope = string.IsNullOrWhiteSpace(rule.Scope) ? "Both" : rule.Scope.Trim();
        return string.Equals(ruleScope, "Both", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ruleScope, scope, StringComparison.OrdinalIgnoreCase);
    }

    public static List<PosRowColorRule> DefaultRules() => new()
    {
        Rule("Expired", 100, "Expired", null, "#FDE2E2", "#9B1C1C", "#DC2626", "Expired"),
        Rule("Expiry within 30 days", 90, "ExpiryWithinDays", 30, "#FCE7F3", "#9D174D", "#DB2777", "Near expiry"),
        Rule("Expiry within 90 days", 80, "ExpiryWithinDays", 90, "#FFF1D6", "#92400E", "#F59E0B", "Expiry warning"),
        Rule("Maximum discount 10%", 70, "DiscountMaximum", 10, "#E2F7E9", "#166534", "#16A34A", "Maximum 10%"),
        Rule("Maximum discount 5%", 60, "DiscountMaximum", 5, "#F0FDF4", "#166534", "#65A30D", "Maximum 5%"),
        Rule("Default discount 10%", 30, "DefaultDiscount", 10, "#E7EEF5", "#1F4568", "#7B9AB4", "Default 10%"),
        Rule("Low stock", 40, "LowStock", 10, "#FEF3C7", "#92400E", "#D97706", "Low stock")
    };

    private static PosRowColorRule Rule(string name, int priority, string type, decimal? value,
        string background, string foreground, string accent, string label) => new()
    {
        Name = name, Priority = priority, ConditionType = type, NumericValue = value,
        BackgroundColor = background, ForegroundColor = foreground, AccentColor = accent, Label = label
    };
}

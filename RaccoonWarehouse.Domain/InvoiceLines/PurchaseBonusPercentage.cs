using System.Globalization;

namespace RaccoonWarehouse.Domain.InvoiceLines;

/// <summary>
/// Parses the purchase bonus percentage used by purchase invoices.
/// A value from 0 to 100 is the bonus percentage; a value above 100 is
/// interpreted as total received percentage (110% becomes a 10% bonus).
/// The percentage is applied to the paid quantity for cost allocation. This
/// percentage mode is cost-only and does not create free stock quantity.
/// </summary>
public static class PurchaseBonusPercentage
{
    public static bool TryNormalize(string? input, out decimal normalizedPercentage)
    {
        normalizedPercentage = 0m;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var value = input.Trim();
        if (value.EndsWith('%'))
            value = value[..^1].Trim();

        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            && !decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out parsed))
            return false;

        if (parsed < 0m || parsed > 1000m)
            return false;

        normalizedPercentage = parsed > 100m ? parsed - 100m : parsed;
        normalizedPercentage = Math.Round(normalizedPercentage, 6, MidpointRounding.AwayFromZero);
        return true;
    }

    public static decimal CalculateFreeQuantity(decimal paidQuantity, decimal normalizedPercentage)
        => paidQuantity > 0m
            ? Math.Round(paidQuantity * Math.Max(0m, normalizedPercentage) / 100m, 6, MidpointRounding.AwayFromZero)
            : 0m;

    public static decimal CalculateCostQuantity(decimal paidQuantity, decimal normalizedPercentage)
        => paidQuantity > 0m
            ? paidQuantity + CalculateFreeQuantity(paidQuantity, normalizedPercentage)
            : paidQuantity;
}

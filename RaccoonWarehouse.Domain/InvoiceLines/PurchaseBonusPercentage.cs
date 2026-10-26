using System.Globalization;

namespace RaccoonWarehouse.Domain.InvoiceLines;

/// <summary>
/// Parses the purchase bonus percentage used by purchase invoices.
/// A value from 0 to 100 is the bonus percentage; a value above 100 is
/// interpreted as total received percentage (110% becomes a 10% bonus).
/// The bonus is one fractional unit per invoice line, matching the legacy
/// purchase-bonus workflow requested by the application.
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

    public static decimal CalculateFreeQuantity(decimal normalizedPercentage)
        => Math.Round(Math.Max(0m, normalizedPercentage) / 100m, 6, MidpointRounding.AwayFromZero);

    public static decimal CalculateCostQuantity(decimal paidQuantity, decimal normalizedPercentage)
        => paidQuantity > 0m
            ? paidQuantity + CalculateFreeQuantity(normalizedPercentage)
            : paidQuantity;
}

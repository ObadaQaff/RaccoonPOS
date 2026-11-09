using System.Globalization;

namespace RaccoonWarehouse.Domain.InvoiceLines;

/// <summary>
/// Parses the purchase bonus percentage used by purchase invoices.
/// The percentage is applied to the line amount using the equivalent-bonus
/// formula. It is cost-only and does not create free stock quantity.
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

        return TryNormalize(parsed, out normalizedPercentage);
    }

    /// <summary>
    /// Validates a nonnegative percentage without imposing an arbitrary upper
    /// bound or changing its meaning.
    /// </summary>
    public static bool TryNormalize(decimal input, out decimal normalizedPercentage)
    {
        normalizedPercentage = 0m;
        if (input < 0m)
            return false;

        normalizedPercentage = input;
        return true;
    }

    /// <summary>
    /// Calculates the equivalent amount using:
    /// GrossTotal * 100 / (100 + BonusPercentage).
    /// The algebraically equivalent division form avoids overflowing when
    /// multiplying a valid monetary amount by 100.
    /// </summary>
    public static decimal CalculateEquivalentNetTotal(decimal grossTotal, decimal bonusPercentage)
    {
        if (grossTotal < 0m)
            throw new ArgumentOutOfRangeException(nameof(grossTotal), "Gross total must be nonnegative.");
        if (!TryNormalize(bonusPercentage, out var validatedPercentage))
            throw new ArgumentOutOfRangeException(nameof(bonusPercentage), "Bonus percentage must be nonnegative.");
        if (grossTotal == 0m)
            return 0m;

        var divisor = 1m + validatedPercentage / 100m;
        return grossTotal / divisor;
    }
}

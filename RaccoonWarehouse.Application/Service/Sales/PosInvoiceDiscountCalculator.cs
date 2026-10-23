using RaccoonWarehouse.Domain.InvoiceLines.DTOs;

namespace RaccoonWarehouse.Application.Service.Sales;

public sealed record PosInvoiceDiscountPlan(
    bool Success,
    decimal Percentage,
    decimal MaximumPercentage,
    IReadOnlyList<decimal> Amounts);

/// <summary>Plans a common sale discount without modifying any invoice rows.</summary>
public static class PosInvoiceDiscountCalculator
{
    public static PosInvoiceDiscountPlan Calculate(
        IReadOnlyList<InvoiceLineWriteDto> rows,
        decimal input,
        bool isPercentage,
        decimal policyMaximum = 100m)
    {
        var maximum = Math.Min(GetMaximumPercentage(rows), Math.Clamp(policyMaximum, 0m, 100m));
        var gross = rows.Sum(row => row.Quantity * row.UnitPrice);
        if (input < 0m || rows.Any(row => row.Quantity <= 0m || row.UnitPrice <= 0m))
            return new(false, 0m, maximum, Array.Empty<decimal>());
        if (input == 0m)
            return new(true, 0m, maximum, new decimal[rows.Count]);
        if (rows.Count == 0 || gross <= 0m)
            return new(false, 0m, maximum, Array.Empty<decimal>());
        if ((isPercentage && input > 100m) || (!isPercentage && input > gross))
            return new(false, 100m, maximum, Array.Empty<decimal>());

        var percentage = isPercentage ? input : input / gross * 100m;
        if (percentage > maximum)
            return new(false, percentage, maximum, Array.Empty<decimal>());

        var target = Math.Round(isPercentage ? gross * (percentage / 100m) : input,
            3, MidpointRounding.AwayFromZero);
        var amounts = AllocateAmounts(rows, percentage, target);
        return new(amounts != null, percentage, maximum, amounts ?? Array.Empty<decimal>());
    }

    public static decimal GetMaximumPercentage(IReadOnlyList<InvoiceLineWriteDto> rows)
    {
        if (rows.Count == 0)
            return 0m;

        return rows.Min(row =>
        {
            var gross = row.Quantity * row.UnitPrice;
            return gross <= 0m ? 0m : Math.Min(100m,
                Math.Floor(GetMaximumDiscountAmount(row) / gross * 100_000m) / 1_000m);
        });
    }

    public static decimal GetMaximumDiscountAmount(InvoiceLineWriteDto row)
    {
        var gross = row.Quantity * row.UnitPrice;
        if (row.Quantity <= 0m || gross <= 0m)
            return 0m;

        // POS sale prices include tax; UnitCost is compared with the net sale
        // subtotal by the existing POS/invoice calculators. Round the cost up so
        // a three-decimal subtotal can never be below the actual cost.
        var factor = row.TaxExempt ? 1m : 1m + Math.Max(0m, row.TaxRate) / 100m;
        var netCost = Math.Ceiling(row.Quantity * Math.Max(0m, row.UnitCost) * 1_000m) / 1_000m;
        var margin = Math.Max(0m, gross - netCost * factor);
        return Math.Floor(margin * 1_000m) / 1_000m;
    }

    /// <summary>
    /// Largest-remainder currency rounding keeps the requested total exact.
    /// Also used when one discounted row is split over several stock lots.
    /// </summary>
    public static decimal[]? AllocateAmounts(
        IReadOnlyList<InvoiceLineWriteDto> rows,
        decimal percentage,
        decimal target)
    {
        if (percentage < 0m || percentage > 100m || target < 0m ||
            target != Math.Round(target, 3, MidpointRounding.AwayFromZero))
            return null;

        var raw = rows.Select(row => row.Quantity * row.UnitPrice * (percentage / 100m)).ToArray();
        var amounts = raw.Select(amount => Math.Floor(amount * 1_000m) / 1_000m).ToArray();
        var limits = rows.Select(GetMaximumDiscountAmount).ToArray();
        if (amounts.Where((amount, index) => amount < 0m || amount > limits[index]).Any())
            return null;

        var remainder = target - amounts.Sum();
        if (remainder < 0m || remainder > rows.Count * 0.001m)
            return null;

        foreach (var index in Enumerable.Range(0, rows.Count)
                     .OrderByDescending(index => raw[index] - amounts[index]))
        {
            if (remainder == 0m)
                break;
            if (amounts[index] + 0.001m > limits[index])
                continue;
            amounts[index] += 0.001m;
            remainder -= 0.001m;
        }

        return remainder == 0m ? amounts : null;
    }
}

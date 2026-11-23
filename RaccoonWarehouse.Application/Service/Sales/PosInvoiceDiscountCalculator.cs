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
        decimal policyMaximum = 100m,
        IReadOnlyDictionary<InvoiceLineWriteDto, decimal>? existingDiscounts = null,
        IReadOnlyList<decimal>? rowMaximumAmounts = null,
        bool allowPartial = false)
    {
        var gross = rows.Sum(row => row.Quantity * row.UnitPrice);
        var maximum = Math.Min(GetMaximumPercentage(rows, existingDiscounts), Math.Clamp(policyMaximum, 0m, 100m));
        if (rowMaximumAmounts != null && gross > 0m)
        {
            var rowCapacity = rowMaximumAmounts.Sum(amount => Math.Max(0m, amount));
            maximum = Math.Min(maximum,
                Math.Floor(rowCapacity / gross * 100_000m) / 1_000m);
        }

        if (input < 0m || rows.Any(row => row.Quantity <= 0m || row.UnitPrice <= 0m))
            return new(false, 0m, maximum, Array.Empty<decimal>());
        if (input == 0m)
            return new(true, 0m, maximum, new decimal[rows.Count]);
        if (rows.Count == 0 || gross <= 0m)
            return new(false, 0m, maximum, Array.Empty<decimal>());
        if ((isPercentage && input > 100m) || (!isPercentage && input > gross))
            return new(false, 100m, maximum, Array.Empty<decimal>());

        var percentage = isPercentage ? input : input / gross * 100m;
        if (percentage > maximum && !allowPartial)
            return new(false, percentage, maximum, Array.Empty<decimal>());

        var amounts = isPercentage
            ? rows.Select((row, index) =>
            {
                var automaticLimit = GetMaximumDiscountAmount(row,
                    existingDiscounts?.GetValueOrDefault(row) ?? 0m);
                var limit = rowMaximumAmounts == null
                    ? automaticLimit
                    : Math.Min(automaticLimit, Math.Max(0m, rowMaximumAmounts[index]));
                var requested = Math.Max(0m, row.Quantity * row.UnitPrice) * percentage / 100m;
                return Math.Round(Math.Min(requested, limit), 3, MidpointRounding.AwayFromZero);
            }).ToArray()
            : AllocateAmounts(rows, percentage,
                Math.Min(Math.Round(input, 3, MidpointRounding.AwayFromZero),
                    rowMaximumAmounts?.Sum(amount => Math.Max(0m, amount))
                        ?? rows.Sum(row => GetMaximumDiscountAmount(row,
                            existingDiscounts?.GetValueOrDefault(row) ?? 0m))),
                existingDiscounts, rowMaximumAmounts);
        return new(amounts != null, percentage, maximum, amounts ?? Array.Empty<decimal>());
    }

    public static decimal GetMaximumPercentage(
        IReadOnlyList<InvoiceLineWriteDto> rows,
        IReadOnlyDictionary<InvoiceLineWriteDto, decimal>? existingDiscounts = null)
    {
        if (rows.Count == 0)
            return 0m;

        var gross = rows.Sum(row => Math.Max(0m, row.Quantity * row.UnitPrice));
        if (gross <= 0m)
            return 0m;

        // The POS rule service is the authority for discount limits. Do not
        // apply a second cost-based ceiling here: row-level discounts use the
        // same POS rules, and invoice-level discounts must behave identically.
        // The returned capacity only prevents the invoice discount from
        // exceeding the remaining gross amount after existing row discounts.
        var availableDiscount = rows.Sum(row => GetMaximumDiscountAmount(row,
            existingDiscounts?.GetValueOrDefault(row) ?? 0m));
        return Math.Min(100m,
            Math.Floor(availableDiscount / gross * 100_000m) / 1_000m);
    }

    public static decimal GetMaximumDiscountAmount(InvoiceLineWriteDto row, decimal existingDiscountAmount = 0m)
    {
        var gross = row.Quantity * row.UnitPrice;
        if (row.Quantity <= 0m || gross <= 0m)
            return 0m;

        var existingAmount = Math.Max(0m, existingDiscountAmount);
        var remainingGross = Math.Max(0m, gross - existingAmount);
        return Math.Floor(remainingGross * 1_000m) / 1_000m;
    }

    /// <summary>
    /// Largest-remainder currency rounding keeps the requested total exact.
    /// Also used when one discounted row is split over several stock lots.
    /// </summary>
    public static decimal[]? AllocateAmounts(
        IReadOnlyList<InvoiceLineWriteDto> rows,
        decimal percentage,
        decimal target,
        IReadOnlyDictionary<InvoiceLineWriteDto, decimal>? existingDiscounts = null,
        IReadOnlyList<decimal>? rowMaximumAmounts = null)
    {
        if (percentage < 0m || percentage > 100m || target < 0m ||
            target != Math.Round(target, 3, MidpointRounding.AwayFromZero))
            return null;

        if (rowMaximumAmounts != null && rowMaximumAmounts.Count != rows.Count)
            return null;

        var limits = rows.Select((row, index) =>
        {
            var automaticLimit = GetMaximumDiscountAmount(row,
                existingDiscounts?.GetValueOrDefault(row) ?? 0m);
            return rowMaximumAmounts == null
                ? automaticLimit
                : Math.Min(automaticLimit, Math.Max(0m, rowMaximumAmounts[index]));
        }).ToArray();
        var raw = rows.Select(row => Math.Max(0m, row.Quantity * row.UnitPrice) * (percentage / 100m)).ToArray();
        var amounts = AllocateWithinRowLimits(raw, limits, target);
        if (amounts == null)
            return null;

        return amounts;
    }

    private static decimal[]? AllocateWithinRowLimits(
        IReadOnlyList<decimal> raw,
        IReadOnlyList<decimal> limits,
        decimal target)
    {
        if (raw.Count != limits.Count || raw.Count == 0)
            return null;

        var targetUnits = (long)Math.Round(target * 1_000m, 0, MidpointRounding.AwayFromZero);
        var capacities = limits
            .Select(limit => Math.Max(0L, (long)Math.Floor(limit * 1_000m)))
            .ToArray();
        if (targetUnits < 0 || targetUnits > capacities.Sum())
            return null;

        // Allocate proportionally first, capping rows that cannot carry their
        // proportional share. Remaining value is redistributed to other rows.
        var remainingTarget = target;
        var remainingIndexes = Enumerable.Range(0, raw.Count).ToList();
        var amounts = new decimal[raw.Count];
        while (remainingIndexes.Count > 0)
        {
            var remainingRaw = remainingIndexes.Sum(index => raw[index]);
            if (remainingRaw <= 0m)
                break;

            var capped = new List<int>();
            foreach (var index in remainingIndexes)
            {
                var proportional = remainingTarget * raw[index] / remainingRaw;
                var capacity = capacities[index] / 1_000m;
                if (proportional > capacity)
                {
                    amounts[index] = capacity;
                    remainingTarget -= capacity;
                    capped.Add(index);
                }
            }

            if (capped.Count == 0)
            {
                foreach (var index in remainingIndexes)
                    amounts[index] = remainingTarget * raw[index] / remainingRaw;
                break;
            }

            remainingIndexes.RemoveAll(capped.Contains);
            if (remainingTarget < 0m)
                return null;
        }

        var amountUnits = new long[amounts.Length];
        for (var index = 0; index < amounts.Length; index++)
            amountUnits[index] = Math.Min(capacities[index], Math.Max(0L,
                (long)Math.Floor(amounts[index] * 1_000m)));

        var remainder = targetUnits - amountUnits.Sum();
        if (remainder < 0)
            return null;

        foreach (var index in Enumerable.Range(0, raw.Count)
                     .OrderByDescending(index => amounts[index] * 1_000m - amountUnits[index]))
        {
            if (remainder == 0)
                break;

            var available = capacities[index] - amountUnits[index];
            if (available <= 0)
                continue;

            var add = Math.Min(remainder, available);
            amountUnits[index] += add;
            remainder -= add;
        }

        return remainder == 0
            ? amountUnits.Select(units => units / 1_000m).ToArray()
            : null;
    }
}

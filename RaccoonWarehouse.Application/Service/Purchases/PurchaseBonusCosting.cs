using RaccoonWarehouse.Domain.InvoiceLines;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;

namespace RaccoonWarehouse.Application.Service.Purchases;

/// <summary>Allocation within one purchase line, never averaging different receipts.</summary>
public static class PurchaseBonusCosting
{
    public static void Validate(IEnumerable<InvoiceLineWriteDto> lines)
    {
        foreach (var line in lines)
        {
            if (line.FreeQuantity < 0m)
                throw new ArgumentException("Free quantity must be non-negative.");

            if (!string.IsNullOrWhiteSpace(line.BonusRatio))
            {
                if (!PurchaseBonusRatio.TryCalculate(line.BonusRatio, line.Quantity, out var expectedRatio))
                    throw new ArgumentException("Invalid bonus ratio; use positive whole numbers, e.g. 3+1.");
                if (line.FreeQuantity != expectedRatio)
                    throw new ArgumentException("Apply the bonus again after changing quantity or ratio.");
            }

            if (line.BonusPercentage.HasValue)
            {
                var normalized = line.BonusPercentage.Value;
                if (normalized < 0m || normalized > 1000m)
                    throw new ArgumentException("Bonus percentage must be between 0% and 1000%.");

                // Percentage bonus is cost-only. Do not let a stale legacy free quantity
                // create additional stock for a percentage-based purchase line.
                line.FreeQuantity = 0m;
            }
        }
    }

    public static void Allocate(ICollection<InvoiceLineWriteDto> lines, decimal? invoiceDiscount, bool includesTax)
    {
        Validate(lines);
        var allocateInvoice = lines.Any(line => line.FreeQuantity > 0m
            || !string.IsNullOrWhiteSpace(line.BonusRatio)
            || line.BonusPercentage.HasValue
            || line.PurchaseAcquisitionCost.HasValue);
        var values = lines.Select(line =>
        {
            var gross = Math.Max(0m, line.Quantity * line.UnitPrice);
            var net = gross - Math.Clamp(line.LineDiscountAmount, 0m, gross);
            var divisor = includesTax && !line.TaxExempt && line.TaxRate > 0m ? 1m + line.TaxRate / 100m : 1m;
            var value = Math.Round(net / divisor, 3);
            return line.BonusPercentage.HasValue && line.Quantity > 0m
                ? Math.Round(ApplyPercentageBonusToPurchaseCost(line, value), 6)
                : value;
        }).ToArray();
        var subtotal = values.Sum();
        var discount = Math.Clamp(invoiceDiscount ?? 0m, 0m, subtotal);
        decimal allocated = 0m;
        var list = lines.ToList();
        decimal cumulativeValue = 0m;
        for (var i = 0; i < list.Count; i++)
        {
            cumulativeValue += values[i];
            var targetDiscount = subtotal > 0m ? Math.Round(discount * cumulativeValue / subtotal, 6) : 0m;
            var share = targetDiscount - allocated;
            allocated = targetDiscount;
            var line = list[i];
            // Null preserves compatibility with historical lines and ordinary purchases.
            if (allocateInvoice)
            {
                var acquisitionCost = Math.Max(0m, values[i] - share);
                line.PurchaseAcquisitionCost = Math.Round(acquisitionCost, 6);
            }
        }
    }

    public static decimal EffectiveUnitCost(decimal acquisitionCost, decimal receivedQuantity)
        => receivedQuantity > 0m ? Math.Round(acquisitionCost / receivedQuantity, 6) : 0m;

    private static decimal ApplyPercentageBonusToPurchaseCost(InvoiceLineWriteDto line, decimal amount)
    {
        if (!line.BonusPercentage.HasValue || line.Quantity <= 0m)
            return amount;

        var costQuantity = PurchaseBonusPercentage.CalculateCostQuantity(line.Quantity, line.BonusPercentage.Value);
        return costQuantity > 0m
            ? Math.Round(amount * line.Quantity / costQuantity, 6, MidpointRounding.AwayFromZero)
            : amount;
    }
}

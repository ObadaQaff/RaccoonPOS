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
                throw new ArgumentException("Free quantity must be non-negative / الكمية المجانية يجب ألا تكون سالبة.");
            if (string.IsNullOrWhiteSpace(line.BonusRatio))
                continue;
            if (!PurchaseBonusRatio.TryCalculate(line.BonusRatio, line.Quantity, out var expected))
                throw new ArgumentException("Invalid bonus ratio; use positive whole numbers, e.g. 3+1 / عرض البونص غير صالح؛ استخدم أعداداً صحيحة موجبة مثل 3+1.");
            if (line.FreeQuantity != expected)
                throw new ArgumentException("Apply the bonus again after changing quantity or ratio / أعد تطبيق البونص بعد تغيير الكمية أو العرض.");
        }
    }

    public static void Allocate(ICollection<InvoiceLineWriteDto> lines, decimal? invoiceDiscount, bool includesTax)
    {
        Validate(lines);
        var allocateInvoice = lines.Any(line => line.FreeQuantity > 0m || !string.IsNullOrWhiteSpace(line.BonusRatio) || line.PurchaseAcquisitionCost.HasValue);
        var values = lines.Select(line =>
        {
            var gross = Math.Max(0m, line.Quantity * line.UnitPrice);
            var net = gross - Math.Clamp(line.LineDiscountAmount, 0m, gross);
            var divisor = includesTax && !line.TaxExempt && line.TaxRate > 0m ? 1m + line.TaxRate / 100m : 1m;
            return Math.Round(net / divisor, 3);
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
                line.PurchaseAcquisitionCost = Math.Round(Math.Max(0m, values[i] - share), 6);
        }
    }

    public static decimal EffectiveUnitCost(decimal acquisitionCost, decimal receivedQuantity)
        => receivedQuantity > 0m ? Math.Round(acquisitionCost / receivedQuantity, 6) : 0m;
}

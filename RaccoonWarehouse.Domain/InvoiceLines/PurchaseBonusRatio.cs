using System.Globalization;
using System.Text.RegularExpressions;

namespace RaccoonWarehouse.Domain.InvoiceLines;

public static class PurchaseBonusRatio
{
    public static bool TryCalculate(string? ratio, decimal paidQuantity, out decimal freeQuantity)
    {
        freeQuantity = 0m;
        if (paidQuantity < 0m || ratio == null || ratio.Length > 40)
            return false;
        var match = Regex.Match(ratio, @"^\s*([0-9]+)\s*\+\s*([0-9]+)\s*$", RegexOptions.CultureInvariant);
        if (!match.Success
            || !decimal.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var required)
            || !decimal.TryParse(match.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var reward)
            || required <= 0m || reward <= 0m)
            return false;
        try
        {
            freeQuantity = decimal.Floor(paidQuantity / required) * reward;
            return freeQuantity <= 999999999999m;
        }
        catch (OverflowException) { return false; }
    }
}

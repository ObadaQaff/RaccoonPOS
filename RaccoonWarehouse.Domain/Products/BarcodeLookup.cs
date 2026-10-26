using System;
using System.Globalization;
using System.Linq;

namespace RaccoonWarehouse.Domain.Products
{
    public static class BarcodeLookup
    {
        public static string Normalize(string? barcode)
        {
            var normalized = new string((barcode ?? string.Empty)
                .Where(character => !char.IsControl(character))
                .ToArray())
                .Trim();

            if (normalized.Length >= 3 && normalized[0] == ']'
                && normalized[1] is 'C' or 'd' or 'Q' or 'E' or 'A' or 'X' or 'B' or 'I' or 'P'
                && char.IsDigit(normalized[2]))
            {
                normalized = normalized[3..].Trim();
            }

            // GS1 AI 01 is followed by the 14-digit product GTIN.
            return ExtractGs1ProductBarcode(normalized) ?? normalized;
        }

        public static string? ExtractGs1ProductBarcode(string? barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return null;

            var normalized = barcode.Trim();
            if (normalized.Length <= 16
                || !normalized.StartsWith("01", System.StringComparison.Ordinal)
                || !normalized[2..16].All(char.IsDigit))
            {
                return null;
            }

            return normalized.Substring(2, 14);
        }

        public static bool MatchesItemCode(long? itemCode, string? barcode)
        {
            if (!itemCode.HasValue || string.IsNullOrWhiteSpace(barcode))
                return false;

            var normalized = Normalize(barcode);
            return long.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericBarcode)
                && itemCode.Value == numericBarcode;
        }

        public static bool AreEquivalent(string? first, string? second)
        {
            var normalizedFirst = Normalize(first);
            var normalizedSecond = Normalize(second);
            if (string.IsNullOrWhiteSpace(normalizedFirst) || string.IsNullOrWhiteSpace(normalizedSecond))
                return false;

            if (long.TryParse(normalizedFirst, NumberStyles.Integer, CultureInfo.InvariantCulture, out var firstNumber) &&
                long.TryParse(normalizedSecond, NumberStyles.Integer, CultureInfo.InvariantCulture, out var secondNumber))
                return firstNumber == secondNumber;

            return string.Equals(normalizedFirst, normalizedSecond, StringComparison.OrdinalIgnoreCase);
        }

    }
}

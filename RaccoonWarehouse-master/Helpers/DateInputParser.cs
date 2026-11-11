using System;
using System.Globalization;
using System.Linq;

namespace RaccoonWarehouse.Helpers
{
    public static class DateInputParser
    {
        private static readonly string[] DateFormats =
        {
            "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy",
            "yyyy/MM/dd", "yyyy-MM-dd", "MM/dd/yyyy", "M/d/yyyy",
            "MM-dd-yyyy", "M-d-yyyy"
        };

        public static bool TryParse(string? text, out DateTime date)
        {
            date = default;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var normalized = NormalizeDigits(text.Trim());
            if (normalized.All(char.IsDigit) && normalized.Length == 8 &&
                DateTime.TryParseExact(normalized, "ddMMyyyy", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out date))
            {
                date = date.Date;
                return true;
            }

            if (DateTime.TryParseExact(normalized, DateFormats, CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces, out date) ||
                DateTime.TryParse(normalized, CultureInfo.CurrentCulture,
                    DateTimeStyles.AllowWhiteSpaces, out date))
            {
                date = date.Date;
                return true;
            }

            return false;
        }

        private static string NormalizeDigits(string value)
        {
            return new string(value.Select(character => character switch
            {
                >= '\u0660' and <= '\u0669' => (char)('0' + character - '\u0660'),
                >= '\u06F0' and <= '\u06F9' => (char)('0' + character - '\u06F0'),
                _ => character
            }).ToArray());
        }
    }
}

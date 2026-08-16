using System;
using System.Globalization;
using System.Linq;
using RaccoonWarehouse.Domain.Accounting.Enums;

namespace RaccoonWarehouse.Domain.Accounting.Accounts
{
    public static class AccountCodeHelper
    {
        private const int CodeLength = 10;

        public static string GenerateRootCode(int rootIndex)
        {
            if (rootIndex < 1 || rootIndex > 5)
                throw new ArgumentOutOfRangeException(nameof(rootIndex));

            return rootIndex.ToString(CultureInfo.InvariantCulture).PadRight(CodeLength, '0');
        }

        public static string GetRootCode(AccountType accountType)
        {
            return accountType switch
            {
                AccountType.Asset => "1000000000",
                AccountType.Liability => "2000000000",
                AccountType.Equity => "3000000000",
                AccountType.Revenue => "4000000000",
                AccountType.Expense => "5000000000",
                _ => throw new ArgumentOutOfRangeException(nameof(accountType))
            };
        }

        public static string GenerateCode(Account parent, int childIndex)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (childIndex < 1 || childIndex > 9) throw new ArgumentOutOfRangeException(nameof(childIndex));

            var parentCode = string.IsNullOrWhiteSpace(parent.AccountCode) ? parent.Code : parent.AccountCode;
            if (string.IsNullOrWhiteSpace(parentCode))
                throw new InvalidOperationException("Parent account code is required to generate child code.");

            var numericPrefix = NormalizePrefix(parentCode);
            if (numericPrefix.Length >= CodeLength)
                throw new InvalidOperationException("Parent account code is already at maximum depth.");

            var candidate = $"{numericPrefix}{childIndex.ToString(CultureInfo.InvariantCulture)}";
            return candidate.PadRight(CodeLength, '0');
        }

        public static bool IsFlatAccountCode(string? code)
        {
            return !string.IsNullOrWhiteSpace(code)
                   && code.Length == CodeLength
                   && code.All(char.IsDigit);
        }

        public static string NormalizePrefix(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new InvalidOperationException("Account code is required.");

            if (IsFlatAccountCode(code))
                return code.TrimStart('0').TrimEnd('0');

            if (code.Contains('.'))
            {
                var digits = code
                    .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(segment =>
                    {
                        if (!int.TryParse(segment, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                            throw new InvalidOperationException($"Invalid account code segment '{segment}'.");
                        return value.ToString(CultureInfo.InvariantCulture);
                    });

                var flattened = string.Concat(digits);
                if (flattened.Length == 0)
                    throw new InvalidOperationException("Account code is invalid.");

                return flattened;
            }

            if (!code.All(char.IsDigit))
                throw new InvalidOperationException("Account code must contain only digits or dotted digits.");

            return code.TrimStart('0');
        }
    }
}

using System;

namespace RaccoonWarehouse.Domain.Accounting.Accounts
{
    public static class AccountCodeHelper
    {
        public static string GenerateCode(Account parent, int childIndex)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (childIndex < 1) throw new ArgumentOutOfRangeException(nameof(childIndex));

            var parentCode = string.IsNullOrWhiteSpace(parent.AccountCode) ? parent.Code : parent.AccountCode;
            if (string.IsNullOrWhiteSpace(parentCode))
            {
                throw new InvalidOperationException("Parent account code is required to generate child code.");
            }

            var segment = childIndex.ToString(parent.AccountLevel == 1 ? "00" : "000");
            return $"{parentCode}-{segment}";
        }
    }
}

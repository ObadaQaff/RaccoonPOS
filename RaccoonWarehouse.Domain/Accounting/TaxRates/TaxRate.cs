using RaccoonWarehouse.Domain.Accounting.Accounts;
using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Base;

namespace RaccoonWarehouse.Domain.Accounting.TaxRates
{
    public class TaxRate : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public int TaxAccountId { get; set; }
        public Account TaxAccount { get; set; } = null!;
        public TaxType TaxType { get; set; }
        public bool IsActive { get; set; } = true;
    }
}

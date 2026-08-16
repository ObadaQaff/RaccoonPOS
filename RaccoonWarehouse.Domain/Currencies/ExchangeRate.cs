using RaccoonWarehouse.Domain.Base;

namespace RaccoonWarehouse.Domain.Currencies
{
    public class ExchangeRate : BaseEntity
    {
        public int FromCurrencyId { get; set; }
        public Currency FromCurrency { get; set; } = null!;
        public int ToCurrencyId { get; set; }
        public Currency ToCurrency { get; set; } = null!;
        public decimal Rate { get; set; }
        public DateTime EffectiveDate { get; set; }
    }
}

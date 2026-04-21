using RaccoonWarehouse.Domain.Base;

namespace RaccoonWarehouse.Domain.Currencies
{
    public class Currency : BaseEntity
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ArabicName { get; set; }
        public string? EnglishName { get; set; }
        public string? Symbol { get; set; }
        public decimal ExchangeRate { get; set; } = 1m;
        public bool IsBaseCurrency { get; set; }
        public bool IsActive { get; set; } = true;
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
    }
}

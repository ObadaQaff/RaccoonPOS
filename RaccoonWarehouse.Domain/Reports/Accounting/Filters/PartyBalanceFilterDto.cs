using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.Reports.Accounting.Filters
{
    public class PartyBalanceFilterDto
    {
        public UserRole Role { get; set; }
        public DateTime AsOfDate { get; set; }
        public string? Search { get; set; }
        public bool OutstandingOnly { get; set; } = true;
    }
}

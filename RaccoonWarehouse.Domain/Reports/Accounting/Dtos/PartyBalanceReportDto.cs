using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.Reports.Accounting.Dtos
{
    public class PartyBalanceReportDto
    {
        public UserRole Role { get; set; }
        public DateTime AsOfDate { get; set; }
        public decimal TotalOutstanding { get; set; }
        public int OutstandingCount { get; set; }
        public List<PartyBalanceRowDto> Rows { get; set; } = new();
    }
}

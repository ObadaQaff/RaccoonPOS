using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.Reports.Accounting.Dtos
{
    public class UserStatementReportDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal ClosingBalance { get; set; }
        public List<UserStatementRowDto> Rows { get; set; } = new();
    }
}

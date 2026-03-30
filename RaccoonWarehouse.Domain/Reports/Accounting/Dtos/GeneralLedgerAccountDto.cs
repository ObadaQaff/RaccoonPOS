using RaccoonWarehouse.Domain.Accounting.Enums;

namespace RaccoonWarehouse.Domain.Reports.Accounting.Dtos
{
    public class GeneralLedgerAccountDto
    {
        public int AccountId { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public AccountType AccountType { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal ClosingBalance { get; set; }
        public List<GeneralLedgerRowDto> Rows { get; set; } = new();
    }
}

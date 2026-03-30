namespace RaccoonWarehouse.Domain.Reports.Accounting.Dtos
{
    public class BalanceSheetRowDto
    {
        public int AccountId { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal Balance { get; set; }
    }
}

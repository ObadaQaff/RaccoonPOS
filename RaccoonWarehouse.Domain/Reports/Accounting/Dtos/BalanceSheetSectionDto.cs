namespace RaccoonWarehouse.Domain.Reports.Accounting.Dtos
{
    public class BalanceSheetSectionDto
    {
        public string Title { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public List<BalanceSheetRowDto> Rows { get; set; } = new();
    }
}

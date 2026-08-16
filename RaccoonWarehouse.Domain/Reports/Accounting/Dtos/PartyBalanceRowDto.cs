namespace RaccoonWarehouse.Domain.Reports.Accounting.Dtos
{
    public class PartyBalanceRowDto
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal Balance { get; set; }
        public DateTime? LastMovementDate { get; set; }
    }
}

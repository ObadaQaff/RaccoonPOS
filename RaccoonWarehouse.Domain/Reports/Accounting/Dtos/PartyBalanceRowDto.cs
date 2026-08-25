using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.Reports.Accounting.Dtos
{
    public class PartyBalanceRowDto
    {
        public UserRole Role { get; set; }
        public bool IsCombined { get; set; }
        public string RoleLabel => IsCombined ? "حساب موحد" : Role == UserRole.Customer ? "\u0639\u0645\u064a\u0644" : "\u0645\u0648\u0631\u062f";
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal Balance { get; set; }
        public DateTime? LastMovementDate { get; set; }
    }
}

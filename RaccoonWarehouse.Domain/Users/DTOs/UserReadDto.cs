using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Users.DTOs
{
    public class UserReadDto : IBaseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; }
        public UserRole Role { get; set; } = UserRole.Customer;
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankIban { get; set; }
        public string? BankSwiftCode { get; set; }
        public decimal CreditLimit { get; set; }
        public int CreditDays { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal CurrentBalance { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public CreditStatus CreditStatus { get; set; } = CreditStatus.Active;
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }

    }
}

using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Enums;
using DelegateEntity = RaccoonWarehouse.Domain.Delegates.Delegate;
using EmployeeEntity = RaccoonWarehouse.Domain.Employees.Employee;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Users
{
    public class User:BaseEntity
    {
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
        public DelegateEntity? DelegateProfile { get; set; }
        public EmployeeEntity? EmployeeProfile { get; set; }
    }
}

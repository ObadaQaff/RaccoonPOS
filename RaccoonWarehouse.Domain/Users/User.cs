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
        public DelegateEntity? DelegateProfile { get; set; }
        public EmployeeEntity? EmployeeProfile { get; set; }
    }
}

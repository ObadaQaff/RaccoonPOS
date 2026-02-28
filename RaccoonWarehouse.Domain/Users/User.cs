using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Enums;
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
    }
}

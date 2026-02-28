using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Users.DTOs
{
    public class UserWriteDto : IBaseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; }
        public UserRole Role { get; set; } = UserRole.Customer;
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}

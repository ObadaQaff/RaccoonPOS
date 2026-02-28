using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users;
using RaccoonWarehouse.Domain.Users.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Cashiers.DTOs
{
    public class CashierSessionReadDto : IBaseDto
    {
        public int Id { get; set; }
        public int CashierId { get; set; }
        public UserReadDto Cashier { get; set; } = null!;
        public string? CashierName {get;set;}
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public decimal StatrBalance { get; set; }
        public decimal EndingBalance { get; set; }
        public SessionStatus Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}

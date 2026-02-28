using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Cashiers
{
    public class CashierSession :BaseEntity 
    {
        public int CashierId { get; set; }
        public User Cashier { get; set; } = null!;
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public decimal StatrBalance { get; set; }
        public decimal EndingBalance { get; set; }
        public SessionStatus Status { get; set; }

    }
}

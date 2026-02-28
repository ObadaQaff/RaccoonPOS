using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Stock.DTOs
{
    public class InactiveProductRowDto
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ITEMCODE { get; set; }

        public decimal CurrentStock { get; set; }
        public decimal MinimumQuantity { get; set; }

        public DateTime? LastMovementDate { get; set; }

        public int DaysSinceLastMovement { get; set; }

        public string StatusText =>
            DaysSinceLastMovement >= 90 ? "ميت"
            : DaysSinceLastMovement >= 60 ? "بطيء جداً"
            : "بطيء";
    }
}

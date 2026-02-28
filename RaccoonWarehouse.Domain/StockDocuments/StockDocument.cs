using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.StockItems;
using RaccoonWarehouse.Domain.StockItems.DTOs;
using RaccoonWarehouse.Domain.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.StockDocuments
{
    public class StockDocument : BaseEntity
    {
        public string DocumentNumber { get; set; } = string.Empty;
        public StockVoucherType Type { get; set; } 
        public string? Notes { get; set; }
        public int? SupplierId { get; set; }
        public User? Supplier { get; set; }
        public List<StockItem> Items { get; set; }
    }
}

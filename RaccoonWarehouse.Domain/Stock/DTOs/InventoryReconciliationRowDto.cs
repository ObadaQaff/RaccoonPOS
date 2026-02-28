using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Stock.DTOs
{
    public class InventoryReconciliationRowDto
    {
        public int ProductId { get; set; }
        public int ProductUnitId { get; set; }

        public string? ProductName { get; set; }
        public string? ITEMCODE { get; set; }
        public string? UnitName { get; set; }

        public decimal SystemQty { get; set; }     // من النظام
        public decimal PhysicalQty { get; set; }   // من سند الجرد
        public decimal VarianceQty { get; set; }   // Physical - System

        public decimal CostPrice { get; set; }     // حسب المتوفر عندك
        public decimal VarianceValue { get; set; } // VarianceQty * CostPrice

        public string? StatusText { get; set; }    // زيادة / عجز / مطابق
    }
}

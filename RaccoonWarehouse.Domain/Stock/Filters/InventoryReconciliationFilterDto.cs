using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Stock.Filters
{
    public class InventoryReconciliationFilterDto
    {
        // تاريخ الجرد (رصيد النظام لآخر اليوم)
        public DateTime AsOfDate { get; set; }

        // سند الجرد الفعلي (StockId) - إذا عندك سند جرد واحد واضح
        public int? CountStockId { get; set; }

        // إذا بدك تدخل الفواتير بالحسبة مثل التقارير السابقة
        public bool IncludeInvoices { get; set; } = true;

        // فلترة اختيارية
        public int? ProductId { get; set; }
    }
}

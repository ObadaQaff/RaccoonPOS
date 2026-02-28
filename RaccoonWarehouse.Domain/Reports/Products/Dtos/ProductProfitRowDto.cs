using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Reports.Products.Dtos
{
    public class ProductProfitRowDto
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ITEMCODE { get; set; }
        public string? UnitName { get; set; } // optional

        public decimal SalesQty { get; set; }     // صافي الكمية (مع المرتجعات)
        public decimal SubTotal { get; set; }     // مجموع قبل الضريبة (مع إشارة المرتجع)
        public decimal Discount { get; set; }     // خصم موزّع على السطور
        public decimal NetSales { get; set; }     // SubTotal - Discount (قبل الضريبة)
        public decimal Tax { get; set; }          // مجموع الضريبة
        public decimal COGS { get; set; }         // تكلفة البضاعة المباعة
        public decimal GrossProfit { get; set; }  // NetSales - COGS
        public decimal Margin { get; set; }       // %
    }
}

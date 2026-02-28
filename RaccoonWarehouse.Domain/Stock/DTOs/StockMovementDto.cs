using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Stock.DTOs
{
    public class StockMovementDto
    {
        public DateTime Date { get; set; }
        public string? DocumentNumber { get; set; }
        public string? DocumentType { get; set; }

        public int ProductId { get; set; }
        public string? ProductName { get; set; }

        public string? UnitName { get; set; }

        public decimal Quantity { get; set; }

        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }

        public string? CreatedBy { get; set; }

        // Optional but very useful for filtering:
        public int StockItemId { get; set; }
        public int StockDocumentId { get; set; }
    }
}

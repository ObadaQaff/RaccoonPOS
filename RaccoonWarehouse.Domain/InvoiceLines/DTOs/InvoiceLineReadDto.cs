using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using RaccoonWarehouse.Domain.Vouchers.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.InvoiceLines.DTOs
{
    public class InvoiceLineReadDto : IBaseDto
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public string? OriginalInvoiceId { get; set; }

        public InvoiceReadDto? Invoice { get; set; }

        public int ProductId { get; set; }
        public ProductReadDto? Product { get; set; }
        public int ProductUnitId { get; set; }
        public ProductUnitReadDto? ProductUnit { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string ProductName { get; set; } 

        public decimal LineTotal => Quantity * UnitPrice;
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public decimal UnitCost { get; set; }          // PurchasePrice at time of invoice
        public decimal CostTotal => Quantity * UnitCost; // optional computed


        public bool TaxExempt { get; set; }
        public decimal TaxRate { get; set; }           // snapshot from product وقت البيع
        public decimal TaxAmount { get; set; }         // الضريبة لهذا السطر
        public decimal LineSubTotal { get; set; }      // Quantity * UnitPrice قبل الضريبة
        public decimal Profit { get; set; }        // (LineSubTotal - Tax? عادة لا) - CostTotal
        public decimal ProfitBeforeTax { get; set; } // (LineSubTotal) - CostTotal

    }
}

using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Cashiers;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.Stock;
using RaccoonWarehouse.Domain.Users;
using RaccoonWarehouse.Domain.Vouchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.StockTransactions
{
    public class StockTransaction : BaseEntity
    {
        public Product Product { get; set; }
        public int ProductId { get; set; }
        public ProductUnit ProductUnit { get; set; }
        public int ProductUnitId { get; set; }
        public int? StockId { get; set; }
        public Stock.Stock? Stock { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public TransactionType TransactionType { get; set; }
        public Invoice? Invoice { get; set; }
        public int? InvoiceId { get; set; }
        public Voucher? Voucher { get; set; }
        public int? VoucherId { get; set; }
        public User? Casher { get; set; }
        public int? CasherId { get; set; }
        public CashierSession? CashierSession { get; set; }
        public int? CashierSessionId { get; set; }
        public User? Customer { get; set; }
        public int? CustomerId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? Notes { get; set; }
  
    }
}

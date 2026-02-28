using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Cashiers;
using RaccoonWarehouse.Domain.Cashiers.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using RaccoonWarehouse.Domain.Stock.DTOs;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Domain.Vouchers.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace RaccoonWarehouse.Domain.StockTransactions.DTOs
{
    public class StockTransactionWriteDto:IBaseDto
    {
        public int Id { get; set; }
        public ProductWriteDto Product { get; set; }
        public int ProductId { get; set; }
        public ProductUnitWriteDto ProductUnit { get; set; }
        public int ProductUnitId { get; set; }
        public int? StockId { get; set; }
        public StockWriteDto? Stock { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public TransactionType TransactionType { get; set; }
        public InvoiceWriteDto? Invoice { get; set; }
        public int? InvoiceId { get; set; }
        public VoucherWriteDto? Voucher { get; set; }
        public int? VoucherId { get; set; }
        public UserWriteDto? Casher { get; set; }
        public int? CasherId { get; set; }
        public CashierSessionWriteDto? CashierSession { get; set; }
        public int? CashierSessionId { get; set; }
        public UserWriteDto? Customer { get; set; }
        public int? CustomerId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}

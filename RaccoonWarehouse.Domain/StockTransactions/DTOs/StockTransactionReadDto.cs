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
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.StockTransactions.DTOs
{
    public class StockTransactionReadDto:IBaseDto
    {
        public int Id { get; set; }
        public ProductReadDto Product { get; set; }
        public int ProductId { get; set; }
        public ProductUnitReadDto ProductUnit { get; set; }
        public int ProductUnitId { get; set; }
        public int? StockId { get; set; }
        public StockReadDto? Stock { get; set; }
        public decimal Quantity { get; set; }
        public decimal QuantityPerUnitSnapshot { get; set; }
        public decimal BaseQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public TransactionType TransactionType { get; set; }
        public InvoiceReadDto? Invoice { get; set; }
        public int? InvoiceId { get; set; }
        public VoucherReadDto? Voucher { get; set; }
        public int? VoucherId { get; set; }
        public UserReadDto? Casher { get; set; }
        public int? CasherId { get; set; }
        public CashierSessionReadDto? CashierSession { get; set; }
        public int? CashierSessionId { get; set; }
        public UserReadDto? Customer { get; set; }
        public int? CustomerId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}

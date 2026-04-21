using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Cashiers;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.StockAdjustments;
using RaccoonWarehouse.Domain.StockLots;
using RaccoonWarehouse.Domain.Users;
using RaccoonWarehouse.Domain.Vouchers;

namespace RaccoonWarehouse.Domain.StockTransactions
{
    public class StockTransaction : BaseEntity
    {
        public Product Product { get; set; } = null!;
        public int ProductId { get; set; }
        public ProductUnit ProductUnit { get; set; } = null!;
        public int ProductUnitId { get; set; }
        public int? StockId { get; set; }
        public Stock.Stock? Stock { get; set; }
        public int? WarehouseId { get; set; }
        public int? StockLotId { get; set; }
        public StockLot? StockLot { get; set; }
        public int? StockAdjustmentId { get; set; }
        public StockAdjustment? StockAdjustment { get; set; }
        public int? StockDocumentId { get; set; }
        public decimal Quantity { get; set; }
        public decimal QuantityPerUnitSnapshot { get; set; }
        public decimal BaseQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public DateTime? ExpiryDate { get; set; }
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
        public int? BranchId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? Notes { get; set; }
        public string? ReferenceNumber { get; set; }
        public AccountingSourceType? SourceType { get; set; }
        public int? SourceId { get; set; }
        public int? CreatedBy { get; set; }
    }
}

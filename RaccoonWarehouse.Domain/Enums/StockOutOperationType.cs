namespace RaccoonWarehouse.Domain.Enums;

public enum StockOutOperationType
{
    Damage = 1,
    Expired = 2,
    InternalUse = 3,
    CustomerSaleReturn = 4,
    PurchaseInvoiceReturn = 5,
    StockInReturn = 6,
    Other = 7
}

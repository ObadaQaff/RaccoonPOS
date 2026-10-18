namespace RaccoonWarehouse.Domain.Enums
{
    public enum StockMovementSource
    {
        All = 0,
        Sale = 1,
        Return = 2,
        Purchase = 3,
        PurchaseReturn = 4,
        StockIn = 5,
        StockOut = 6,
        Adjustment = 7,
        Damage = 8,
        Voucher = 9
    }
}

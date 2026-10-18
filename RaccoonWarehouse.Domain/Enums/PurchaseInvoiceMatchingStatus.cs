namespace RaccoonWarehouse.Domain.Enums;

public enum PurchaseInvoiceMatchingStatus
{
    Draft = 0,
    Matching = 1,
    Disputed = 2,
    Matched = 3,
    Posted = 4,
    PartiallyPaid = 5,
    Paid = 6,
    Credited = 7
}

namespace RaccoonWarehouse.Domain.Integration;

public sealed record OrderSubmittedV1(Guid EventId, string EventType, int EventVersion,
    DateTime OccurredAtUtc, PandaOrderSnapshot Order);
public sealed record PandaOrderSnapshot(int OrderId, string? OrderNumber, int CustomerId,
    string? CustomerName, string? CustomerPhone, string? ShopName, string? DeliveryReference,
    string? DeliveryLocationReference, string CurrencyCode, decimal SubTotal, decimal DiscountTotal,
    decimal TaxTotal, decimal GrandTotal, IReadOnlyList<PandaOrderLineSnapshot> Lines, string? Note);
public sealed record PandaOrderLineSnapshot(int OrderLineId, int SourceProductId, int SourceProductUnitId,
    string ItemCode, string ProductName, string? UnitName, decimal Quantity, decimal UnitPrice,
    decimal DiscountAmount, decimal TaxRate, decimal TaxAmount, decimal LineSubTotal, decimal LineTotal);

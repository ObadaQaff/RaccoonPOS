namespace RaccoonWarehouse.CustomerRequests;
public sealed record CustomerRequestSaleLine(int RequestLineId, int ProductId, int ProductUnitId, decimal Quantity, decimal UnitPrice);
public sealed record CustomerRequestSaleDraft(int RequestId, int CustomerId, IReadOnlyList<CustomerRequestSaleLine> Lines);
public static class CustomerRequestSaleContext
{ private static readonly object Sync = new(); private static CustomerRequestSaleDraft? _pending; public static void Set(CustomerRequestSaleDraft draft) { lock (Sync) _pending = draft; } public static CustomerRequestSaleDraft? Take() { lock (Sync) { var d = _pending; _pending = null; return d; } } }

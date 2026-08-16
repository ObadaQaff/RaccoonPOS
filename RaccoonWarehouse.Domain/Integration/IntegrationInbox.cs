using RaccoonWarehouse.Domain.Base;

namespace RaccoonWarehouse.Domain.Integration;

public enum IntegrationInboxStatus { Processing = 0, Completed = 1, Failed = 2 }

public sealed class IntegrationInbox : BaseEntity
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int EventVersion { get; set; }
    public string SourceSystem { get; set; } = "Panda";
    public string ExternalOrderId { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public IntegrationInboxStatus Status { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public int? RaccoonInvoiceId { get; set; }
    public string? LastErrorCode { get; set; }
    public string? LastErrorSummary { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

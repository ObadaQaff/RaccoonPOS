namespace RaccoonWarehouse.Core.Audit
{
    public sealed record AuditEvent(
        string Action,
        string? EntityType = null,
        int? EntityId = null,
        bool Succeeded = true,
        string? ErrorMessage = null);

    public interface IAuditLogService
    {
        Task WriteAsync(AuditEvent auditEvent);
    }
}

using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.Audit
{
    public sealed class AuditLog : BaseEntity
    {
        public int? UserId { get; set; }
        public UserRole? Role { get; set; }
        public Guid SessionId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? PermissionKey { get; set; }
        public string? EntityType { get; set; }
        public int? EntityId { get; set; }
        public bool Succeeded { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

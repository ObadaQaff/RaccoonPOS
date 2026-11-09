using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.Permissions
{
    public class RolePermission : BaseEntity
    {
        public UserRole Role { get; set; }
        public string PermissionKey { get; set; } = string.Empty;
        public bool IsAllowed { get; set; } = true;
    }
}

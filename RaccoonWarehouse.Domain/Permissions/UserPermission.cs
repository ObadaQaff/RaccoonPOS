using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Users;

namespace RaccoonWarehouse.Domain.Permissions
{
    public class UserPermission : BaseEntity
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public string PermissionKey { get; set; } = string.Empty;
        public bool IsAllowed { get; set; }
    }
}

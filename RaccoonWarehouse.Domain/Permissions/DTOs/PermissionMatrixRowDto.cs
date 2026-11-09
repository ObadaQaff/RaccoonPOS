namespace RaccoonWarehouse.Domain.Permissions.DTOs
{
    public class PermissionMatrixRowDto
    {
        public string Module { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public Dictionary<string, bool> Actions { get; set; } = new();
    }
}

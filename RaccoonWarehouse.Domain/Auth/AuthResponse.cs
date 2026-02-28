using RaccoonWarehouse.Domain.Users.DTOs;

namespace RaccoonWarehouse.Domain.Auth
{
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public UserReadDto? User { get; set; }
    }
}

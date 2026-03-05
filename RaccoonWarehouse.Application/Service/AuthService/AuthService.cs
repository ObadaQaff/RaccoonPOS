using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Domain.Auth;
using RaccoonWarehouse.Domain.Users.DTOs;
using System.Linq;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Application.Service.AuthService
{
    public class AuthService : IAuthService
    {
        public IUOW _uOW { get; set; }

        public AuthService(IUOW uOW)
        {
            _uOW = uOW;
        }

        public async Task<AuthResponse> AuthenticateAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return new AuthResponse { Success = false, Message = "Please enter username and password." };

            var users = await _uOW.Users
                .GetAllWithFilteringAndInclude(u => u.PhoneNumber == username || u.Name == username);

            var user = users?.FirstOrDefault();

            if (user == null)
                return new AuthResponse { Success = false, Message = "User not found." };

            if (string.IsNullOrEmpty(user.Password))
                return new AuthResponse { Success = false, Message = "User has no password set." };

            if (user.Password != password)
                return new AuthResponse { Success = false, Message = "Password is wrong." };

            return new AuthResponse
            {
                Success = true,
                Message = "Login successful.",
                User = new UserReadDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Role = user.Role,
                    Password = user.Password,
                    PhoneNumber = user.PhoneNumber,
                    CreatedDate = user.CreatedDate,
                    UpdatedDate = user.UpdatedDate
                }
            };
        }
    }

    public interface IAuthService
    {
        Task<AuthResponse> AuthenticateAsync(string username, string password);
    }
}

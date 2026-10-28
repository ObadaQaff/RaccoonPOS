using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Domain.Auth;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Domain.Users;
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

            var user = await FindUserAsync(username);

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
                User = user
            };
        }

        public async Task<UserReadDto?> FindUserAsync(string identifier)
        {
            var value = identifier?.Trim();
            if (string.IsNullOrWhiteSpace(value)) return null;
            var users = await _uOW.Users.GetAllWithFilteringAndInclude(u => u.PhoneNumber == value || u.Name == value);
            if (int.TryParse(value, out var number))
            {
                var numbered = await _uOW.Users.GetAllWithFilteringAndInclude(u => u.UserNumber == number);
                users = users.Concat(numbered).GroupBy(x => x.Id).Select(x => x.First()).ToList();
            }
            var user = users?.FirstOrDefault();
            return user == null ? null : new UserReadDto { Id = user.Id, UserNumber = user.UserNumber, Name = user.Name, Role = user.Role, PhoneNumber = user.PhoneNumber, Password = user.Password, CreatedDate = user.CreatedDate, UpdatedDate = user.UpdatedDate };
        }
    }

    public interface IAuthService
    {
        Task<AuthResponse> AuthenticateAsync(string username, string password);
        Task<UserReadDto?> FindUserAsync(string identifier);
    }
}

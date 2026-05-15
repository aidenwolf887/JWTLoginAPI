using JWTLoginAPI.DTOs;
using JWTLoginAPI.Entities;

namespace JWTLoginAPI.Interfaces
{
    public interface IAuthService
    {
        Task<User> RegisterAsync(UserRegisterDto request);
        Task<string?> LoginAsync(UserLoginDto request);

        Task<bool> UpdateUserAsync(Guid id, string newUsername);
    }
}

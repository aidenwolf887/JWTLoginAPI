using JWTLoginAPI.DTOs;
using JWTLoginAPI.Entities;

namespace JWTLoginAPI.Interfaces
{
    public interface IAuthService
    {
        Task<UserResponseDto> RegisterAsync(UserRegisterDto request);
        Task<AuthResponseDto> LoginAsync(UserLoginDto request, string ipAddress);
        Task<AuthResponseDto> RefreshTokenAsync(string token, string ipAddress);
        Task<bool> LogoutAsync(string token);

        Task<bool> UpdateUserAsync(Guid id, string newUsername);
    }
}

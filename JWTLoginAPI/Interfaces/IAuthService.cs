using JWTLoginAPI.DTOs;
using JWTLoginAPI.Entities;

namespace JWTLoginAPI.Interfaces
{
    public interface IAuthService
    {
        Task<User> RegisterAsync(UserRegisterDto request);
    }
}

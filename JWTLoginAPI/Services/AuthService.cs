using Microsoft.EntityFrameworkCore;
using JWTLoginAPI.Data;
using JWTLoginAPI.DTOs;
using JWTLoginAPI.Entities;
using JWTLoginAPI.Interfaces;


namespace JWTLoginAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;

        public AuthService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User> RegisterAsync(UserRegisterDto request)
        {
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                throw new Exception("Username already exists.");
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                throw new Exception("Email already exists.");

            string salt = BCrypt.Net.BCrypt.GenerateSalt();
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, salt);
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }
    }
}

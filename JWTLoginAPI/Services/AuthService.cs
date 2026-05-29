using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
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
        private readonly IConfiguration _configuration;

        public AuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<UserResponseDto> RegisterAsync(UserRegisterDto request)
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
                PasswordHash = passwordHash,
                Role = "User"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };
        }

        // PERBAIKAN: Menambahkan parameter ipAddress & mengubah return type menjadi AuthResponseDto? sesuai interface
        public async Task<AuthResponseDto?> LoginAsync(UserLoginDto request, string ipAddress)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return null;
            }

            // Menggunakan CreateJwtToken agar seragam
            var jwtToken = CreateJwtToken(user);
            var refreshToken = CreateRefreshToken();

            user.RefreshToken = refreshToken.Token;
            user.TokenCreated = refreshToken.Created;
            user.TokenExpires = refreshToken.Expires;

            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                Token = jwtToken,
                Username = user.Username,
                Role = user.Role
            };
        }

        // PERBAIKAN: Menghapus tanda ? pada AuthResponseDto jika interface mengharuskan non-nullable
        public async Task<AuthResponseDto?> RefreshTokenAsync(string token, string ipAddress)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == token);

            if (user == null || user.TokenExpires < DateTime.UtcNow)
            {
                return null;
            }

            var newJwtToken = CreateJwtToken(user);
            var newRefreshToken = CreateRefreshToken();

            user.RefreshToken = newRefreshToken.Token;
            user.TokenCreated = newRefreshToken.Created;
            user.TokenExpires = newRefreshToken.Expires;

            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                Token = newJwtToken,
                Username = user.Username,
                Role = user.Role
            };
        }

        public async Task<bool> LogoutAsync(string token)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == token);
            if (user == null) return false;

            user.RefreshToken = string.Empty;
            user.TokenCreated = DateTime.MinValue;
            user.TokenExpires = DateTime.MinValue;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateUserAsync(Guid id, string newUsername)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            user.Username = newUsername;
            return await _context.SaveChangesAsync() > 0;
        }

        // ================= HELPER METHODS =================

        // PERBAIKAN: Mengubah nama dari 'CreateToken' menjadi 'CreateJwtToken' agar sesuai panggilan di atas
        private string CreateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role) // Menggunakan role dinamis dari DB
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15), // Disarankan 15 menit untuk akses token
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // PERBAIKAN: Menambahkan fungsi CreateRefreshToken yang sebelumnya hilang
        private (string Token, DateTime Created, DateTime Expires) CreateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);

            return (
                Token: Convert.ToBase64String(randomNumber),
                Created: DateTime.UtcNow,
                Expires: DateTime.UtcNow.AddDays(7) // Aktif selama 7 hari
            );
        }
    }
}
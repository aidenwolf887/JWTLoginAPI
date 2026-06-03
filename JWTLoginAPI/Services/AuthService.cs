using Microsoft.AspNetCore.Identity;
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
        // Ganti DbContext dengan UserManager dan RoleManager bawaan Identity
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<User> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        public async Task<UserResponseDto> RegisterAsync(UserRegisterDto request)
        {
            // 1. Buat objek entitas User kustom kita
            var user = new User
            {
                UserName = request.Username, // Menggunakan properti bawaan Identity (N besar)
                Email = request.Email
                // Properti 'Role' tidak disimpan di kolom User lagi, melainkan di tabel relasi AspNetUserRoles
            };

            // 2. Gunakan UserManager untuk membuat user sekaligus melakukan hashing password otomatis
            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errorMessages = string.Join(" ", result.Errors.Select(e => e.Description));
                throw new Exception($"Registrasi gagal: {errorMessages}");
            }

            // 3. Pastikan Role "User" sudah terdaftar di sistem AspNetRoles
            if (!await _roleManager.RoleExistsAsync("User"))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>("User"));
            }

            // 4. Hubungkan User baru ini ke Role "User" (Many-to-Many otomatis terisi di DB)
            await _userManager.AddToRoleAsync(user, "User");

            return new UserResponseDto
            {
                Id = user.Id,
                Username = user.UserName!,
                Email = user.Email!,
                Role = "User",
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<AuthResponseDto?> LoginAsync(UserLoginDto request, string ipAddress)
        {
            // Cari user berdasarkan username menggunakan UserManager
            var user = await _userManager.FindByNameAsync(request.Username);

            // Cek password menggunakan internal hashing validator bawaan .NET
            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            {
                return null;
            }

            // Ambil role user dari tabel relasi Identity
            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? "User";

            // Buat token akses dan refresh token
            var jwtToken = await CreateJwtToken(user);
            var refreshToken = CreateRefreshToken();

            user.RefreshToken = refreshToken.Token;
            user.TokenCreated = refreshToken.Created;
            user.TokenExpires = refreshToken.Expires;

            // Simpan perubahan data refresh token ke database lewat UserManager
            await _userManager.UpdateAsync(user);

            return new AuthResponseDto
            {
                Token = jwtToken,
                Username = user.UserName!,
                Role = primaryRole
            };
        }

        public async Task<AuthResponseDto?> RefreshTokenAsync(string token, string ipAddress)
        {
            // Ambil data user langsung dari pool Users Identity berdasarkan properti kustom kita
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.RefreshToken == token);

            if (user == null || user.TokenExpires < DateTime.UtcNow)
            {
                return null;
            }

            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? "User";

            var newJwtToken = await CreateJwtToken(user);
            var newRefreshToken = CreateRefreshToken();

            user.RefreshToken = newRefreshToken.Token;
            user.TokenCreated = newRefreshToken.Created;
            user.TokenExpires = newRefreshToken.Expires;

            await _userManager.UpdateAsync(user);

            return new AuthResponseDto
            {
                Token = newJwtToken,
                Username = user.UserName!,
                Role = primaryRole
            };
        }

        public async Task<bool> LogoutAsync(string token)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.RefreshToken == token);
            if (user == null) return false;

            user.RefreshToken = string.Empty;
            user.TokenCreated = DateTime.MinValue;
            user.TokenExpires = DateTime.MinValue;

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> UpdateUserAsync(Guid id, string newUsername)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return false;

            user.UserName = newUsername;
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        // ================= HELPER METHODS =================

        private async Task<string> CreateJwtToken(User user)
        {
            // Klaim dasar
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            // Ambil seluruh role dinamis milik user dari Identity database dan masukkan ke klaim token
            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private (string Token, DateTime Created, DateTime Expires) CreateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);

            return (
                Token: Convert.ToBase64String(randomNumber),
                Created: DateTime.UtcNow,
                Expires: DateTime.UtcNow.AddDays(7)
            );
        }
    }
}
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JWTLoginAPI.Data;
using JWTLoginAPI.DTOs;
using JWTLoginAPI.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace JWTLoginAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ApplicationDbContext _context; // Kita butuh ini untuk mengambil token dari DB di level Controller

        public AuthController(IAuthService _authService, ApplicationDbContext context)
        {
            this._authService = _authService;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto request, [FromServices] IValidator<UserRegisterDto> validator)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));
            }

            var userResponse = await _authService.RegisterAsync(request);
            return Ok(new { message = "User berhasil didaftarkan!", data = userResponse });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDto request)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var result = await _authService.LoginAsync(request, ipAddress);

            if (result == null)
            {
                return BadRequest("Username atau password salah.");
            }

            // Ambil token dari DB untuk dipasang ke Cookie HTTP-Only
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user != null)
            {
                SetRefreshTokenCookie(user.RefreshToken, user.TokenExpires);
            }

            return Ok(new { message = "Login Berhasil!", token = result.Token });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            // 1. Ambil Refresh Token dari Cookie browser
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                return Unauthorized("Refresh token tidak ditemukan. Silakan login kembali.");
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var result = await _authService.RefreshTokenAsync(refreshToken, ipAddress);

            if (result == null)
            {
                return Unauthorized("Token tidak valid atau sudah kadaluwarsa.");
            }

            // 2. Ambil token baru dari DB untuk update Cookie
            var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken); // Token lama sudah diganti di service, cari berdasarkan user jika perlu, atau ubah arsitektur DTO. Tapi karena service kita langsung mengupdate DB, kita cari user berdasarkan username yang dikembalikan di DTO hasil refresh
            var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == result.Username);

            if (updatedUser != null)
            {
                SetRefreshTokenCookie(updatedUser.RefreshToken, updatedUser.TokenExpires);
            }

            return Ok(new { token = result.Token });
        }

        [HttpPost("logout"), Authorize]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _authService.LogoutAsync(refreshToken);
            }

            // Hapus Cookie di Browser dengan cara membuatnya langsung expired
            Response.Cookies.Delete("refreshToken");

            return Ok(new { message = "Logout berhasil!" });
        }

        // ================= HELPER METHOD COOKIE =================
        private void SetRefreshTokenCookie(string token, DateTime expires)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true, // Mengamankan token dari serangan XSS (JavaScript tidak bisa baca)
                Expires = expires,
                SameSite = SameSiteMode.Strict, // Mencegah serangan CSRF
                Secure = true // Hanya berjalan di HTTPS (saat production)
            };

            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }
    }
}
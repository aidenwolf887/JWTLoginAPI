using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
// // PERUBAHAN: Menghapus 'using Microsoft.EntityFrameworkCore;' dan 'using JWTLoginAPI.Data;' 
// Karena Controller tidak lagi melakukan query database secara langsung menggunakan EF Core.
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

        // // PERUBAHAN: Menghapus 'ApplicationDbContext context' dari parameter Constructor.
        // Sekarang Controller benar-benar bersih dan hanya bergantung pada `IAuthService`.
        public AuthController(IAuthService authService)
        {
            _authService = authService;
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

            // // PERUBAHAN: Menghapus query manual '_context.Users.FirstOrDefaultAsync(...)' yang ada di kode lama.
            // Data 'RefreshToken' dan 'Expires' kini langsung diambil dari objek 'result' bawaan Service.
            SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpires);

            return Ok(new { message = "Login Berhasil!", token = result.Token });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
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

            // // PERUBAHAN: Menghapus total 2 baris query ke '_context.Users' yang mencari user lama & user terupdate.
            // Logika pencarian dan pembaharuan token di DB sudah diselesaikan di dalam '_authService.RefreshTokenAsync'.
            SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpires);

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

            Response.Cookies.Delete("refreshToken");

            return Ok(new { message = "Logout berhasil!" });
        }

        // ================= HELPER METHOD COOKIE =================
        private void SetRefreshTokenCookie(string token, DateTime expires)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = expires,
                SameSite = SameSiteMode.Strict,
                Secure = true
            };

            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }

        [HttpGet("user-data"), Authorize(Roles = "User,Admin")]
        public IActionResult GetUserData()
        {
            return Ok(new { message = "Sukses! Ini adalah data yang bisa dilihat oleh user dan admin" });
        }

        [HttpGet("admin-data"), Authorize(Roles = "Admin")]
        public IActionResult GetAdminData()
        {
            return Ok(new { message = "Sukses! Ini adalah data yang hanya bisa dilihat oleh admin" });
        }
    }
}
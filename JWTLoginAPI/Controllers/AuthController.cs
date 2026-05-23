using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using JWTLoginAPI.DTOs;
using JWTLoginAPI.Interfaces;
using FluentValidation;


namespace JWTLoginAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody]UserRegisterDto request, [FromServices]IValidator<UserRegisterDto> validator)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(e => new {e.PropertyName, e.ErrorMessage})); 
            }

            var userRespone = await _authService.RegisterAsync(request);
            return Ok(new { message = "User registered successfully", data = userRespone });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDto request)
        {
            var token = await _authService.LoginAsync(request);

            if (token == null)
            {
                return BadRequest("Username atau password salah!");
            }

            return Ok(new {
                message = "Login Berhasil!",
                token = token
            });
        }

        [HttpPut("update-username"), Authorize]
        public async Task<IActionResult> UpdateUsername(string newUsername)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (userId == null) return Unauthorized();

            var result = await _authService.UpdateUserAsync(Guid.Parse(userId), newUsername);

            if (!result) return BadRequest("Gagal memperbarui username!");

            return Ok("Username berhasil di perbarui!");
        }
    }
}

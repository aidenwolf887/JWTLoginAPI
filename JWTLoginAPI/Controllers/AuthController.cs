using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using JWTLoginAPI.DTOs;
using JWTLoginAPI.Interfaces;


namespace JWTLoginAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService   _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegisterDto request)
        {
            var user = await _authService.RegisterAsync(request);
            return Ok(new { message= "User registered successfully", data = user.Username });
        }
    }
}

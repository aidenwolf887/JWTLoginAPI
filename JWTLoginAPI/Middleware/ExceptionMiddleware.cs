using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace JWTLoginAPI.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                // Lanjutkan request ke tahapan berikutnya jika tidak ada error
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                // Jika ada error di bagian kodingan manapun, tangkap di sini!
                _logger.LogError($"Terjadi kesalahan sistem: {ex.Message}");
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            // Default error level produksi: 500 Internal Server Error
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            // Kamu bisa kustomisasi status code jika error-nya spesifik
            if (exception is UnauthorizedAccessException)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            }

            // 3. Logika Kondisional: Jika di Development tampilkan error asli, jika di Production set NULL
            string? detailedMessage = _env.IsDevelopment()
                ? exception.Message
                : null; // Otomatis rahasia sistem aman di production!

            // Respon rapi yang akan diterima oleh Frontend / User
            var response = new
            {
                statusCode = context.Response.StatusCode,
                message = "Waduh, terjadi kesalahan pada server internal kami. Tim engineer sedang memperbaikinya!",
                detailed = detailedMessage // Bisa dimatikan saat benar-benar production agar aman
            };

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonSerializer.Serialize(response, options);

            return context.Response.WriteAsync(json);
        }
    }
}
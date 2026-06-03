using Microsoft.AspNetCore.Identity;
using System;

namespace JWTLoginAPI.Entities
{
    // UBAH BARIS INI: Warisi dari IdentityUser<Guid>
    public class User : IdentityUser<Guid>
    {
        // Kolom standar seperti Id, UserName, Email, PasswordHash TIDAK PERLU ditulis lagi 
        // karena sudah otomatis diwarisi dari IdentityUser.

        // Kita cukup pertahankan kolom kustom kita saja:
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime TokenCreated { get; set; }
        public DateTime TokenExpires { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
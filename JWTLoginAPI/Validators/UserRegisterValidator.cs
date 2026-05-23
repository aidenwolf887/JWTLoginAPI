using FluentValidation;
using JWTLoginAPI.DTOs;

namespace JWTLoginAPI.Validators
{
    public class UserRegisterValidator : AbstractValidator<UserRegisterDto>
    {
        public UserRegisterValidator() 
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username tidak boleh kosong.")
                .MinimumLength(3).WithMessage("Username minimal 3 karakter.")
                .MaximumLength(20).WithMessage("Username maksimal 20 karakter.")
                .Matches(@"^[a-zA-Z0-9]+$").WithMessage("Username hanya boleh mengandung huruf dan angka.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email tidak boleh kosong.")
                .EmailAddress().WithMessage("Format email tidak valid.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password tidak boleh kosong.")
                .MinimumLength(8).WithMessage("Password minimal harus 8 karakter.")
                .Matches(@"[A-Z]").WithMessage("Password harus mengandung setidaknya satu huruf besar.")
                .Matches(@"[a-z]").WithMessage("Password harus mengandung setidaknya satu huruf kecil.")
                .Matches(@"[0-9]").WithMessage("Password harus mengandung setidaknya satu angka.")
                .Matches(@"[\W]").WithMessage("Password harus mengandung setidaknya satu karakter khusus.");
        }
    }
}

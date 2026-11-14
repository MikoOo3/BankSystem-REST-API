using System.ComponentModel.DataAnnotations;

namespace BankSystem.DataAccess.DTOs.Auth
{
    public class LoginDTO
    {
        [Required(ErrorMessage = "Email is required.")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            ErrorMessage = "Invalid email format. Expected format: example@domain.com")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;
    }
}

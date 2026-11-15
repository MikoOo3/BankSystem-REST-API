using System.ComponentModel.DataAnnotations;

namespace BankDataAccessLayer.DTOs
{
    public class UserProfileUpdateDTO
    {
        [Required]
        public Guid Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
    }
}

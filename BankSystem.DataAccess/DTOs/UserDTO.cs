using System;
using System.ComponentModel.DataAnnotations;

namespace BankDataAccessLayer
{
    public class UserDTO
    {
        public UserDTO(Guid id, string fullName, string email, string passwordHash, string role, int failedLoginAttempts, 
            DateTimeOffset? LockoutEnd, bool isActive, DateTime createdAt)
        {
            this.Id = id;
            this.FullName = fullName;
            this.Email = email;
            this.PasswordHash = passwordHash;
            this.Role = role;
            this.FailedLoginAttempts = failedLoginAttempts;
            this.LockoutEnd = LockoutEnd;
            this.IsActive = isActive;
            this.CreatedAt = createdAt;
        }

        public Guid Id { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "Full name must be between 3 and 150 characters.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", 
            ErrorMessage = "Invalid email format. Expected format: example@domain.com")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[A-Za-z\d]{8,}$",
            ErrorMessage = "Password is too weak. It must contain at least one uppercase letter, " +
            "one lowercase letter, and one number.")]
        public string PasswordHash { get; set; }

        [Required(ErrorMessage = "Role is required.")]
        [RegularExpression(@"^(Manager|Employee|Customer)$",
            ErrorMessage = "Invalid Role. Allowed roles are: Manager, Employee, Customer.")]
        public string Role { get; set; }


        [Range(0, 10, ErrorMessage = "Failed login attempts must be between 0 and 10.")]
        public int FailedLoginAttempts { get; set; }

        public DateTimeOffset? LockoutEnd { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}

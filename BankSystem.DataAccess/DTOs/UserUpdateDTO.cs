using System;

namespace BankDataAccessLayer
{
    public class UserUpdateDTO
    {
        public Guid Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PasswordHash { get; set; }
        public string? Role { get; set; }
        public bool? IsActive { get; set; }
        public int? FailedLoginAttempts { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
    }
}

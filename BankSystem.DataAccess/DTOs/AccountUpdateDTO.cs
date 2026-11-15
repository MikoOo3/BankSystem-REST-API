using System;
using System.ComponentModel.DataAnnotations;

namespace BankDataAccessLayer.DTOs
{
    public class AccountUpdateDTO
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public string AccountType { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal DailyWithdrawalLimit { get; set; }
    }
}

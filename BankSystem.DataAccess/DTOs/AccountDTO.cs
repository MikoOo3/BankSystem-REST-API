using System;
using System.ComponentModel.DataAnnotations;

namespace BankDataAccessLayer
{
    public class AccountDTO
    {
        public AccountDTO(Guid id, Guid customerId, string accountNumber, string accountType, decimal balance,
            string currency, decimal dailyWithdrawalLimit, bool isActive, DateTime createdAt)
        {
            this.Id = id;
            this.CustomerId = customerId;
            this.AccountNumber = accountNumber;
            this.AccountType = accountType;
            this.Balance = balance;
            this.Currency = currency;
            this.DailyWithdrawalLimit = dailyWithdrawalLimit;
            this.IsActive = isActive;
            this.CreatedAt = createdAt;
        }

        public Guid Id { get; set; }

        [Required(ErrorMessage = "Customer ID is required.")]
        public Guid CustomerId { get; set; }

    
        public string AccountNumber { get; set; }

        [Required(ErrorMessage = "Account type is required.")]
        [RegularExpression(@"^(Savings|Current)$", ErrorMessage = "Invalid Account Type. Allowed types are: Savings, Current.")]
        public string AccountType { get; set; }

        [Range(0.00, double.MaxValue, ErrorMessage = "Balance cannot be negative.")]
        public decimal Balance { get; set; }

        [Required(ErrorMessage = "Currency is required.")]
        [StringLength(10, ErrorMessage = "Currency code cannot exceed 10 characters.")]
        public string Currency { get; set; }

        [Range(0.00, double.MaxValue, ErrorMessage = "Daily withdrawal limit cannot be negative.")]
        public decimal DailyWithdrawalLimit { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}
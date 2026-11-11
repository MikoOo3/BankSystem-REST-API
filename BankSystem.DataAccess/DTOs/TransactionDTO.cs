using System;
using System.ComponentModel.DataAnnotations;

namespace BankDataAccessLayer
{
    public class TransactionDTO
    {
        public TransactionDTO(Guid id, Guid? sourceAccountId, Guid? destinationAccountId, string transactionType, decimal amount, string? description, string status, DateTime timestamp)
        {
            this.Id = id;
            this.SourceAccountId = sourceAccountId;
            this.DestinationAccountId = destinationAccountId;
            this.TransactionType = transactionType;
            this.Amount = amount;
            this.Description = description;
            this.Status = status;
            this.Timestamp = timestamp;
        }

        public Guid Id { get; set; }

        public Guid? SourceAccountId { get; set; }

        public Guid? DestinationAccountId { get; set; }

        [Required(ErrorMessage = "Transaction type is required.")]
        [RegularExpression(@"^(Deposit|Withdrawal|Transfer)$",
            ErrorMessage = "Invalid Transaction Type. Allowed types are: Deposit, Withdrawal, Transfer.")]
        public string TransactionType { get; set; }

        [Required(ErrorMessage = "Amount is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [StringLength(250, ErrorMessage = "Description cannot exceed 250 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        [RegularExpression(@"^(Pending|Success|Failed)$",
            ErrorMessage = "Invalid Status. Allowed values are: Pending, Success, Failed.")]
        public string Status { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
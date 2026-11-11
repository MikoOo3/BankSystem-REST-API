using Microsoft.Identity.Client.NativeInterop;
using System;

public class Transaction
{
    public Guid Id { get; set; }

    public Guid? SourceAccountId { get; set; }
    public Account? SourceAccount { get; set; }

    public Guid? DestinationAccountId { get; set; }
    public Account? DestinationAccount { get; set; }

    public string TransactionType { get; set; } = string.Empty; // Deposit, Withdrawal, Transfer
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Success, Failed
    public DateTime Timestamp { get; set; }
}

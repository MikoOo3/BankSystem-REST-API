using System;
using System.Collections.Generic;

public class Account
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }

    public User Customer { get; set; } = null!;

    public string AccountNumber { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal DailyWithdrawalLimit { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Transaction> OutgoingTransactions { get; set; } = new List<Transaction>();
    public ICollection<Transaction> IncomingTransactions { get; set; } = new List<Transaction>();
}

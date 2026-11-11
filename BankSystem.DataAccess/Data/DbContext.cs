using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client.NativeInterop;
using System;

public class BankSystemDbContext : DbContext
{
    public BankSystemDbContext(DbContextOptions<BankSystemDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Account> Accounts { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

       
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).HasDefaultValueSql("NEWID()");
            entity.Property(u => u.FullName).HasMaxLength(150).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(150).IsRequired();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Role).HasMaxLength(50).IsRequired();
            entity.Property(u => u.FailedLoginAttempts).HasDefaultValue(0);
            entity.Property(u => u.IsActive).HasDefaultValue(true);
            entity.Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

       
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Id).HasDefaultValueSql("NEWID()");
            entity.Property(a => a.AccountNumber).HasMaxLength(30).IsRequired();
            entity.HasIndex(a => a.AccountNumber).IsUnique();
            entity.Property(a => a.AccountType).HasMaxLength(50).IsRequired();
            entity.Property(a => a.Balance).HasColumnType("decimal(18,2)").HasDefaultValue(0.00m);
            entity.Property(a => a.Currency).HasMaxLength(10).HasDefaultValue("USD");
            entity.Property(a => a.DailyWithdrawalLimit).HasColumnType("decimal(18,2)").HasDefaultValue(5000.00m);
            entity.Property(a => a.IsActive).HasDefaultValue(true);
            entity.Property(a => a.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(a => a.Customer)
                  .WithMany(u => u.Accounts)
                  .HasForeignKey(a => a.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

       
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).HasDefaultValueSql("NEWID()");
            entity.Property(t => t.TransactionType).HasMaxLength(50).IsRequired();
            entity.Property(t => t.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(t => t.Description).HasMaxLength(250);
            entity.Property(t => t.Status).HasMaxLength(50).HasDefaultValue("Pending");
            entity.Property(t => t.Timestamp).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(t => t.SourceAccount)
                  .WithMany(a => a.OutgoingTransactions)
                  .HasForeignKey(t => t.SourceAccountId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.DestinationAccount)
                  .WithMany(a => a.IncomingTransactions)
                  .HasForeignKey(t => t.DestinationAccountId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(al => al.Id);
            entity.Property(al => al.Action).HasMaxLength(100).IsRequired();
            entity.Property(al => al.OldValues).HasColumnType("NVARCHAR(MAX)");
            entity.Property(al => al.NewValues).HasColumnType("NVARCHAR(MAX)");
            entity.Property(al => al.Timestamp).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(al => al.User)
                  .WithMany(u => u.AuditLogs)
                  .HasForeignKey(al => al.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
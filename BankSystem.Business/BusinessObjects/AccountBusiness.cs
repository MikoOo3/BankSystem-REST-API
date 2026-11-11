using BankDataAccessLayer;
using BankSystem.DataAccess.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

namespace BankSystem.Business.BusinessObjects
{
    public class AccountBusiness
    {
        public enum enMode { AddNew = 0, Update = 1 };
        public enMode Mode = enMode.AddNew;

        private readonly BankSystemDbContext _context;

        public AccountDTO ToDTO
        {
            get { return new AccountDTO(this.Id, this.CustomerId, this.AccountNumber, this.AccountType, this.Balance, this.Currency, this.DailyWithdrawalLimit, this.IsActive, this.CreatedAt); }
        }

        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public string Currency { get; set; } = "USD";
        public decimal DailyWithdrawalLimit { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public AccountBusiness(BankSystemDbContext context, AccountDTO acDTO, enMode cMode = enMode.AddNew)
        {
            _context = context;
            this.Id = acDTO.Id;
            this.CustomerId = acDTO.CustomerId;
            this.AccountNumber = acDTO.AccountNumber;
            this.AccountType = acDTO.AccountType;
            this.Balance = acDTO.Balance;
            this.Currency = acDTO.Currency;
            this.DailyWithdrawalLimit = acDTO.DailyWithdrawalLimit;
            this.IsActive = acDTO.IsActive;
            Mode = cMode;
        }

        private async Task<bool> _AddNewAccountAsync()
        {
            string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            this.AccountNumber = $"BANK-{timestamp}";

            var entity = new Account
            {
                Id = Guid.NewGuid(),
                CustomerId = this.CustomerId,
                AccountNumber = this.AccountNumber,
                AccountType = this.AccountType,
                Balance = this.Balance,
                Currency = string.IsNullOrWhiteSpace(this.Currency) ? "USD" : this.Currency,
                DailyWithdrawalLimit = this.DailyWithdrawalLimit == 0 ? 5000.00m : this.DailyWithdrawalLimit,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await _context.Accounts.AddAsync(entity);
                int rows = await _context.SaveChangesAsync();

                if (rows <= 0) throw new Exception("Insertion failed.");

                this.Id = entity.Id;
                this.AccountNumber = entity.AccountNumber;

                string newAccountJson = System.Text.Json.JsonSerializer.Serialize(this.ToDTO);

                var auditLog = new AuditLogBusiness(_context, new AuditLogDTO(0, this.CustomerId, "Account Creation",
                    null, newAccountJson, DateTime.UtcNow));

                await auditLog.SaveAsync();

                await dbTransaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                return false;
            }
        }

        private async Task<bool> _UpdateAccountAsync()
        {
            var entity = await _context.Accounts.FindAsync(this.Id);

            if (entity == null) return false;

            var oldDto = new AccountDTO(entity.Id, entity.CustomerId, entity.AccountNumber,
                entity.AccountType, entity.Balance, entity.Currency, entity.DailyWithdrawalLimit, entity.IsActive, entity.CreatedAt);

            string oldAccountJson = System.Text.Json.JsonSerializer.Serialize(oldDto);

            entity.AccountType = this.AccountType;
            entity.DailyWithdrawalLimit = this.DailyWithdrawalLimit;
            entity.IsActive = this.IsActive;

            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Accounts.Update(entity);

                int rows = await _context.SaveChangesAsync();
                if (rows <= 0) throw new Exception("Update failed.");

                string newAccountJson = System.Text.Json.JsonSerializer.Serialize(this.ToDTO);

                var auditLog = new AuditLogBusiness(_context, new AuditLogDTO(0, this.CustomerId, 
                    "Account Settings Updated", oldAccountJson, newAccountJson, DateTime.UtcNow));

                await auditLog.SaveAsync();

                await dbTransaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                return false;
            }
        }

        private async Task<bool> _SaveEntityOnlyAsync()
        {
            var entity = await _context.Accounts.FindAsync(this.Id);
            if (entity == null) return false;

            entity.Balance = this.Balance;
        

            _context.Accounts.Update(entity);
            return (await _context.SaveChangesAsync()) > 0;
        }

        public async Task<bool> CloseAccountAsync()
        {
            var entity = await _context.Accounts.FindAsync(this.Id);

            if (entity == null) return false;

            var oldDto = new AccountDTO(entity.Id, entity.CustomerId, entity.AccountNumber, entity.AccountType, 
                entity.Balance, entity.Currency, entity.DailyWithdrawalLimit, entity.IsActive, entity.CreatedAt);

            string oldAccountJson = System.Text.Json.JsonSerializer.Serialize(oldDto);

            entity.IsActive = false;
            this.IsActive = false;   

            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Accounts.Update(entity);
                int rows = await _context.SaveChangesAsync();
                if (rows <= 0) throw new Exception("Closure failed.");

                string newAccountJson = System.Text.Json.JsonSerializer.Serialize(this.ToDTO);

                var auditLog = new AuditLogBusiness(_context, new AuditLogDTO(0, this.CustomerId, "Account Closed (Soft Delete)",
                    oldAccountJson, newAccountJson, DateTime.UtcNow));

                await auditLog.SaveAsync();

                await dbTransaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                return false;
            }
        }

        public async Task<TransactionBusiness?> Deposit(TransactionDTO transactionDto)
        {
            string oldValuesJson = System.Text.Json.JsonSerializer.Serialize(this.ToDTO);

            this.Balance += transactionDto.Amount;
            this.Mode = AccountBusiness.enMode.Update;

            transactionDto.TransactionType = "Deposit";
            transactionDto.SourceAccountId = null;
            transactionDto.Status = "Success";
            transactionDto.Timestamp = DateTime.UtcNow;

            var txBusiness = new TransactionBusiness(_context, transactionDto);

            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                bool isAccountSaved = await this._SaveEntityOnlyAsync();
                bool isTxSaved = await txBusiness.SaveAsync();

                if (!isAccountSaved || !isTxSaved)
                {
                    throw new Exception("Critical error during deposit database operations.");
                }

                string newValuesJson = System.Text.Json.JsonSerializer.Serialize(this.ToDTO);

                var auditLog = new AuditLogBusiness(_context, new AuditLogDTO(0, this.CustomerId, "Cash Deposit Processing",
                    oldValuesJson, newValuesJson, DateTime.UtcNow));

                await auditLog.SaveAsync();

                await dbTransaction.CommitAsync();

                return txBusiness;
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                return null;
            }
        }

        public async Task<TransactionBusiness?> Withdraw(TransactionDTO transactionDto)
        {
            if (this.Balance < transactionDto.Amount)
            {
                return null;
            }

            if (transactionDto.Amount > this.DailyWithdrawalLimit)
            {
                return null;
            }

            string oldValuesJson = System.Text.Json.JsonSerializer.Serialize(this.ToDTO);

            this.Balance -= transactionDto.Amount;
            this.Mode = AccountBusiness.enMode.Update;

            transactionDto.TransactionType = "Withdrawal";
            transactionDto.DestinationAccountId = null;
            transactionDto.Status = "Success";
            transactionDto.Timestamp = DateTime.UtcNow;

            var txBusiness = new TransactionBusiness(_context, transactionDto);

            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                bool isAccountSaved = await this._SaveEntityOnlyAsync();
                bool isTxSaved = await txBusiness.SaveAsync();

                if (!isAccountSaved || !isTxSaved)
                {
                    throw new Exception("Critical error during withdrawal database operations.");
                }

                string newValuesJson = System.Text.Json.JsonSerializer.Serialize(this.ToDTO);

                var auditLog = new AuditLogBusiness(_context, new AuditLogDTO(0, this.CustomerId, "Cash Withdrawal Processing",
                    oldValuesJson, newValuesJson, DateTime.UtcNow));

                await auditLog.SaveAsync();

                await dbTransaction.CommitAsync();
                return txBusiness;
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                return null;
            }
        }

        public static async Task<TransactionBusiness?> TransferFundsAsync(BankSystemDbContext context, AccountBusiness sourceAccount,
                         AccountBusiness destAccount, TransactionDTO transactionDto)
        {
            if (sourceAccount.Balance < transactionDto.Amount) return null;

            if (transactionDto.Amount > sourceAccount.DailyWithdrawalLimit) return null;

            string oldValuesJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                Source = sourceAccount.ToDTO,
                Destination = destAccount.ToDTO
            });

            sourceAccount.Balance -= transactionDto.Amount;
            destAccount.Balance += transactionDto.Amount;

            sourceAccount.Mode = AccountBusiness.enMode.Update;
            destAccount.Mode = AccountBusiness.enMode.Update;

            transactionDto.TransactionType = "Transfer";
            transactionDto.Status = "Success";
            var txBusiness = new TransactionBusiness(context, transactionDto);

            using var dbTransaction = await context.Database.BeginTransactionAsync();

            try
            {
                bool isSourceSaved = await sourceAccount._SaveEntityOnlyAsync();
                bool isDestSaved = await destAccount._SaveEntityOnlyAsync();
                bool isTxSaved = await txBusiness.SaveAsync();

                if (!isSourceSaved || !isDestSaved || !isTxSaved)
                {
                    throw new Exception("Critical error during database operations.");
                }

                string newValuesJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Source = sourceAccount.ToDTO,
                    Destination = destAccount.ToDTO
                });

                var auditLog = new AuditLogBusiness(context, new AuditLogDTO(0, sourceAccount.CustomerId, "Funds Transfer Outgoing",
                    oldValuesJson, newValuesJson, DateTime.UtcNow));

                await auditLog.SaveAsync();

                await dbTransaction.CommitAsync();
                return txBusiness;
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                return null;
            }
        }

        public async Task<bool> SaveAsync()
        {
            switch (Mode)
            {
                case enMode.AddNew:
                    if (await _AddNewAccountAsync())
                    {
                        Mode = enMode.Update;
                        return true;
                    }
                    return false;

                case enMode.Update:
                    return await _UpdateAccountAsync();
            }
            return false;
        }

        public static async Task<List<AccountDTO>> GetAllAccountsAsync(BankSystemDbContext context)
        {
            return await context.Accounts
                .Select(a => new AccountDTO(
                    a.Id,
                    a.CustomerId,
                    a.AccountNumber,
                    a.AccountType,
                    a.Balance,
                    a.Currency,
                    a.DailyWithdrawalLimit,
                    a.IsActive,
                    a.CreatedAt))
                .AsNoTracking()
                .ToListAsync();
        }

        public static async Task<AccountBusiness?> FindAsync(BankSystemDbContext context, Guid id)
        {
            var entity = await context.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            if (entity == null) return null;

            var dto = new AccountDTO(entity.Id, entity.CustomerId, entity.AccountNumber, entity.AccountType, entity.Balance, entity.Currency, entity.DailyWithdrawalLimit, entity.IsActive, entity.CreatedAt);
            return new AccountBusiness(context, dto, enMode.Update);
        }

        public static async Task<AccountBusiness?> FindByAccountNumberAsync(BankSystemDbContext context, string accountNumber)
        {
            var entity = await context.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
            if (entity == null) return null;

            var dto = new AccountDTO(entity.Id, entity.CustomerId, entity.AccountNumber, entity.AccountType, entity.Balance, entity.Currency, entity.DailyWithdrawalLimit, entity.IsActive, entity.CreatedAt);
            return new AccountBusiness(context, dto, enMode.Update);
        }

        public static async Task<List<AccountDTO>> GetCustomerAccountsAsync(BankSystemDbContext context, Guid customerId)
        {
            return await context.Accounts
                .Where(a => a.CustomerId == customerId)
                .Select(a => new AccountDTO(a.Id, a.CustomerId, a.AccountNumber, a.AccountType, a.Balance, a.Currency, a.DailyWithdrawalLimit, a.IsActive, a.CreatedAt))
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
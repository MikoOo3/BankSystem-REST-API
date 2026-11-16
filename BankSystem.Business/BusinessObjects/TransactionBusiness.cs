using BankDataAccessLayer;
using BankSystem.DataAccess.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BankSystem.Business.BusinessObjects
{
    public class TransactionBusiness
    {
        private readonly BankSystemDbContext _context;

        public TransactionDTO ToDTO
        {
            get { return new TransactionDTO(this.Id, this.SourceAccountId, this.DestinationAccountId, this.TransactionType, this.Amount, this.Description, this.Status, this.Timestamp); }
        }

        public Guid Id { get; set; }
        public Guid? SourceAccountId { get; set; }
        public Guid? DestinationAccountId { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime Timestamp { get; set; }

        public TransactionBusiness(BankSystemDbContext context, TransactionDTO txDTO)
        {
            _context = context;
            this.Id = txDTO.Id;
            this.SourceAccountId = txDTO.SourceAccountId;
            this.DestinationAccountId = txDTO.DestinationAccountId;
            this.TransactionType = txDTO.TransactionType;
            this.Amount = txDTO.Amount;
            this.Description = txDTO.Description;
            this.Status = txDTO.Status;
            this.Timestamp = txDTO.Timestamp;
        }

        public async Task<bool> SaveAsync()
        {
            var entity = new Transaction
            {
                Id = Guid.NewGuid(),
                SourceAccountId = this.SourceAccountId,
                DestinationAccountId = this.DestinationAccountId,
                TransactionType = this.TransactionType,
                Amount = this.Amount,
                Description = this.Description,
                Status = this.Status,
                Timestamp = DateTime.UtcNow
            };

            await _context.Transactions.AddAsync(entity);
            int rows = await _context.SaveChangesAsync();

            if (rows > 0)
            {
                this.Id = entity.Id;
                this.Timestamp = entity.Timestamp;
                return true;
            }
            return false;
        }

        public static async Task<TransactionBusiness?> FindAsync(BankSystemDbContext context, Guid id)
        {
            var entity = await context.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            if (entity == null) return null;

            var dto = new TransactionDTO(entity.Id, entity.SourceAccountId, entity.DestinationAccountId, entity.TransactionType, entity.Amount, entity.Description, entity.Status, entity.Timestamp);
            return new TransactionBusiness(context, dto);
        }

        public static async Task<PagedResult<TransactionDTO>> GetAccountHistoryAsync(BankSystemDbContext context, Guid accountId,
                   int pageNumber = 1, int pageSize = 10)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            var query = context.Transactions
                .Where(t => t.SourceAccountId == accountId || t.DestinationAccountId == accountId)
                .AsNoTracking()
                .OrderByDescending(t => t.Timestamp);

            int totalCount = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TransactionDTO(
                    t.Id,
                    t.SourceAccountId,
                    t.DestinationAccountId,
                    t.TransactionType,
                    t.Amount,
                    t.Description,
                    t.Status,
                    t.Timestamp))
                .ToListAsync();

            return new PagedResult<TransactionDTO>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public static async Task<PagedResult<TransactionDTO>> GetAllTransactionsAsync(BankSystemDbContext context,int pageNumber = 1,
            int pageSize = 10)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            var query = context.Transactions
                .AsNoTracking()
                .OrderByDescending(t => t.Timestamp);

            int totalCount = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TransactionDTO(
                    t.Id,
                    t.SourceAccountId,
                    t.DestinationAccountId,
                    t.TransactionType,
                    t.Amount,
                    t.Description,
                    t.Status,
                    t.Timestamp))
                .ToListAsync();

            return new PagedResult<TransactionDTO>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
    }
}
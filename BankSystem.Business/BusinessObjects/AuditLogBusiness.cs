using BankDataAccessLayer;
using BankSystem.DataAccess.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BankSystem.Business.BusinessObjects
{
    public class AuditLogBusiness
    {
        private readonly BankSystemDbContext _context;

        public AuditLogDTO ToDTO
        {
            get { return new AuditLogDTO(this.Id, this.UserId, this.Action, this.OldValues, this.NewValues, this.Timestamp); }
        }

        public long Id { get; set; }
        public Guid? UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public DateTime Timestamp { get; set; }

        public AuditLogBusiness(BankSystemDbContext context, AuditLogDTO alDTO)
        {
            _context = context;
            this.Id = alDTO.Id;
            this.UserId = alDTO.UserId;
            this.Action = alDTO.Action;
            this.OldValues = alDTO.OldValues;
            this.NewValues = alDTO.NewValues;
            this.Timestamp = alDTO.Timestamp;
        }

        public async Task<bool> SaveAsync()
        {
            var entity = new AuditLog
            {
                UserId = this.UserId,
                Action = this.Action,
                OldValues = this.OldValues,
                NewValues = this.NewValues,
                Timestamp = DateTime.UtcNow
            };

            await _context.AuditLogs.AddAsync(entity);
            int rows = await _context.SaveChangesAsync();

            if (rows > 0)
            {
                this.Id = entity.Id;
                this.Timestamp = entity.Timestamp;
                return true;
            }
            return false;
        }

        public static async Task<AuditLogBusiness?> FindAsync(BankSystemDbContext context, long id)
        {
            var entity = await context.AuditLogs.AsNoTracking().FirstOrDefaultAsync(log => log.Id == id);
            if (entity == null) return null;

            var dto = new AuditLogDTO(entity.Id, entity.UserId, entity.Action, entity.OldValues, entity.NewValues, entity.Timestamp);
            return new AuditLogBusiness(context, dto);
        }

        public static async Task<List<AuditLogDTO>> GetAllLogsAsync(BankSystemDbContext context)
        {
            return await context.AuditLogs
                .Select(al => new AuditLogDTO(al.Id, al.UserId, al.Action, al.OldValues, al.NewValues, al.Timestamp))
                .AsNoTracking()
                .ToListAsync();
        }

        public static async Task<List<AuditLogDTO>> GetLogsByUserIdAsync(BankSystemDbContext context, Guid userId)
        {
            return await context.AuditLogs
                .Where(al => al.UserId == userId)
                .Select(al => new AuditLogDTO(al.Id, al.UserId, al.Action, al.OldValues, al.NewValues, al.Timestamp))
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
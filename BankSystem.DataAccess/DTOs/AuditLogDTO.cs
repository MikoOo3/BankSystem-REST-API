using System;
using System.Collections.Generic;
using System.Text;

namespace BankSystem.DataAccess.DTOs
{
    public class AuditLogDTO
    {

        public AuditLogDTO(long id, Guid? userId, string action, string? oldValues, string? newValues, DateTime timestamp)
        {
            this.Id = id;
            this.UserId = userId;
            this.Action = action;
            this.OldValues = oldValues;
            this.NewValues = newValues;
            this.Timestamp = timestamp;
        }

        public long Id { get; set; }
        public Guid? UserId { get; set; }
        public string Action { get; set; }
        public string? OldValues { get; set; } 
        public string? NewValues { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

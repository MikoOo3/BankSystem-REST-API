using System;

public class AuditLog
{
    public long Id { get; set; }

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public string Action { get; set; } = string.Empty;
    public string? OldValues { get; set; } 
    public string? NewValues { get; set; } 
    public DateTime Timestamp { get; set; }
}
using System;

namespace TexasPortal.Domain.Entities;

public class IdempotencyRecord
{
    public long Id { get; set; }
    public string IdempotencyKey { get; set; } = null!;
    public string RequestHash { get; set; } = null!;
    public string Status { get; set; } = null!; // InProgress, Completed, Failed
    public int? ResponseCode { get; set; }
    public string? ResponsePayload { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}

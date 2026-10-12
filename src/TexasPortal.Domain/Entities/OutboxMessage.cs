using System;

namespace TexasPortal.Domain.Entities;

public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventName { get; set; } = string.Empty;
    public string Status { get; set; } = "pending"; // pending, processing, delivered, failed
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedUtc { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty; // Serialized NotificationEnvelope JSON
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
}

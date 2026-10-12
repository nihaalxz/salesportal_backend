using System;

namespace TexasPortal.Contracts.Notifications;

public sealed record NotificationEnvelope
{
    public required string EventId { get; init; }
    public required string CorrelationId { get; init; }
    public required string EventName { get; init; }
    public required string ReferenceType { get; init; }
    public required string ReferenceId { get; init; }
    public long? HeadId { get; init; }
    public long? AgentId { get; init; }
    public string? TenantId { get; init; }
    public string Priority { get; init; } = "Normal";
    public required string Producer { get; init; }
    public int PayloadVersion { get; init; } = 1;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string? TargetGroup { get; init; }
}

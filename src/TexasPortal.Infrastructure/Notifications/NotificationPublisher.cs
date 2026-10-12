using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Application.Common.Options;
using TexasPortal.Application.Persistence;
using TexasPortal.Contracts.Notifications;
using TexasPortal.Domain.Entities;

namespace TexasPortal.Infrastructure.Notifications;

public sealed class NotificationPublisher : INotificationPublisher
{
    private readonly ISalesPortalDbContext _db;
    private readonly NotificationOptions _options;
    private readonly ILogger<NotificationPublisher> _logger;

    public NotificationPublisher(
        ISalesPortalDbContext db,
        IOptions<NotificationOptions> options,
        ILogger<NotificationPublisher> logger)
    {
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    public Task PublishAsync(
        string eventName,
        string referenceType,
        string referenceId,
        long? headId = null,
        long? agentId = null,
        string? targetGroup = null,
        string priority = "Normal",
        CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Notifications disabled in options. Skipping publish for {EventName}.", eventName);
            return Task.CompletedTask;
        }

        var envelope = new NotificationEnvelope
        {
            EventId = "evt_" + Guid.NewGuid().ToString("N"),
            CorrelationId = "tx-corr-" + Guid.NewGuid().ToString("N")[..12],
            EventName = eventName,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            HeadId = headId,
            AgentId = agentId,
            TargetGroup = targetGroup,
            Priority = priority,
            Producer = _options.ProducerName,
            Timestamp = DateTime.UtcNow
        };

        return PublishEnvelopeAsync(envelope, ct);
    }

    public Task PublishEnvelopeAsync(NotificationEnvelope envelope, CancellationToken ct = default)
    {
        if (!_options.Enabled || envelope == null) return Task.CompletedTask;

        _logger.LogInformation("Enqueuing Outbox notification {EventName} ({RefType}:{RefId}) NotificationId: {EventId}, CorrelationId: {CorrelationId}",
            envelope.EventName, envelope.ReferenceType, envelope.ReferenceId, envelope.EventId, envelope.CorrelationId);

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventName = envelope.EventName,
            Status = "pending",
            CreatedUtc = DateTime.UtcNow,
            CorrelationId = envelope.CorrelationId,
            Payload = JsonSerializer.Serialize(envelope),
            AttemptCount = 0
        };

        // Enqueue only. NO SaveChangesAsync() here — handler's active transaction owns the commit!
        _db.OutboxMessages.Add(outboxMessage);

        return Task.CompletedTask;
    }
}

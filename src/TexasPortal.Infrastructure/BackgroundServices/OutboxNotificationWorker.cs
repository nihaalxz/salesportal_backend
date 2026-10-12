using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Application.Common.Options;
using TexasPortal.Application.Persistence;
using TexasPortal.Contracts.Notifications;

namespace TexasPortal.Infrastructure.BackgroundServices;

public sealed class OutboxNotificationWorker : BackgroundService
{
    private readonly WorkerOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxNotificationWorker> _logger;
    private readonly string _workerId = "worker_" + Guid.NewGuid().ToString("N")[..8];

    public OutboxNotificationWorker(
        IOptions<WorkerOptions> options,
        IServiceProvider serviceProvider,
        ILogger<OutboxNotificationWorker> logger)
    {
        _options = options.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("OutboxNotificationWorker [{WorkerId}] is disabled in configuration.", _workerId);
            return;
        }

        _logger.LogInformation("OutboxNotificationWorker [{WorkerId}] started. PollingInterval: {Interval}ms, BatchSize: {BatchSize}",
            _workerId, _options.PollingIntervalMilliseconds, _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingOutboxBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inside OutboxNotificationWorker [{WorkerId}] loop.", _workerId);
            }

            await Task.Delay(_options.PollingIntervalMilliseconds, stoppingToken);
        }

        _logger.LogInformation("OutboxNotificationWorker [{WorkerId}] stopping.", _workerId);
    }

    public async Task ProcessPendingOutboxBatchAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISalesPortalDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();

        // 1. Claim pending messages atomically
        var pendingMessages = await db.OutboxMessages
            .Where(m => m.Status == "pending")
            .OrderBy(m => m.CreatedUtc)
            .Take(_options.BatchSize)
            .ToListAsync(ct);

        if (pendingMessages.Count == 0) return;

        _logger.LogInformation("OutboxNotificationWorker [{WorkerId}] claimed {Count} pending outbox messages.", _workerId, pendingMessages.Count);

        foreach (var message in pendingMessages)
        {
            message.Status = "processing";
            message.ProcessedUtc = DateTime.UtcNow;
            message.AttemptCount++;
        }
        await db.SaveChangesAsync(ct);

        // 2. Dispatch notifications
        foreach (var message in pendingMessages)
        {
            try
            {
                var envelope = JsonSerializer.Deserialize<NotificationEnvelope>(message.Payload);
                if (envelope != null)
                {
                    _logger.LogInformation("Worker [{WorkerId}] dispatching event {EventName}. NotificationId: {NotificationId}, CorrelationId: {CorrelationId}",
                        _workerId, message.EventName, envelope.EventId, message.CorrelationId);

                    await dispatcher.DispatchAsync(envelope, ct);
                }

                message.Status = "delivered";
                message.ProcessedUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker [{WorkerId}] failed to dispatch OutboxMessage {MessageId}. Attempt {AttemptCount}",
                    _workerId, message.Id, message.AttemptCount);

                message.LastError = ex.Message;
                if (message.AttemptCount >= _options.MaxRetries)
                {
                    message.Status = "failed";
                }
                else
                {
                    message.Status = "pending"; // Re-queue for retry
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }
}

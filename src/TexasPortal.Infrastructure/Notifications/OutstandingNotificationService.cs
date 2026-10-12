using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Contracts.Notifications;

namespace TexasPortal.Infrastructure.Notifications;

public sealed class OutstandingNotificationService : IOutstandingNotificationService
{
    private readonly INotificationPublisher _publisher;
    private readonly IDashboardRefreshCoordinator _dashboardCoordinator;
    private readonly ILogger<OutstandingNotificationService> _logger;

    public OutstandingNotificationService(
        INotificationPublisher publisher,
        IDashboardRefreshCoordinator dashboardCoordinator,
        ILogger<OutstandingNotificationService> logger)
    {
        _publisher = publisher;
        _dashboardCoordinator = dashboardCoordinator;
        _logger = logger;
    }

    public Task PublishOutstandingChangedAsync(
        string referenceType,
        string referenceId,
        long ownerId,
        string ownerType,
        CancellationToken cancellationToken = default)
    {
        var target = new OutstandingOwnerTarget(ownerId, ownerType);
        return PublishOutstandingChangedTargetsAsync(referenceType, referenceId, new[] { target }, cancellationToken);
    }

    public async Task PublishOutstandingChangedTargetsAsync(
        string referenceType,
        string referenceId,
        IEnumerable<OutstandingOwnerTarget> targets,
        CancellationToken cancellationToken = default)
    {
        if (targets == null) return;

        var stopwatch = Stopwatch.StartNew();

        var distinctTargets = targets
            .Where(t => t != null)
            .GroupBy(t => t.ResolveTargetGroup())
            .Select(g => g.First())
            .ToList();

        foreach (var target in distinctTargets)
        {
            var targetGroup = target.ResolveTargetGroup();
            long? headId = target.OwnerType.Equals("customer", StringComparison.OrdinalIgnoreCase) || target.OwnerType.Equals("head", StringComparison.OrdinalIgnoreCase) ? target.OwnerId : null;
            long? agentId = target.OwnerType.Equals("agent", StringComparison.OrdinalIgnoreCase) ? target.OwnerId : null;

            _logger.LogInformation(
                "[OutstandingNotification] Enqueuing OutstandingChanged. OwnerId={OwnerId}, OwnerType={OwnerType}, ReferenceType={ReferenceType}, ReferenceId={ReferenceId}, TargetGroup={TargetGroup}",
                target.OwnerId, target.OwnerType, referenceType, referenceId, targetGroup);

            await _publisher.PublishAsync(
                eventName: BusinessEventNames.OutstandingChanged,
                referenceType: referenceType,
                referenceId: referenceId,
                headId: headId,
                agentId: agentId,
                targetGroup: targetGroup,
                priority: "Normal",
                ct: cancellationToken);
        }

        await _dashboardCoordinator.TriggerDashboardRefreshAsync(
            sourceEventName: BusinessEventNames.OutstandingChanged,
            referenceType: referenceType,
            referenceId: referenceId,
            cancellationToken: cancellationToken);

        stopwatch.Stop();
        _logger.LogDebug("[OutstandingNotification] Enqueued {Count} OutstandingChanged notification(s) in {ElapsedTime} ms",
            distinctTargets.Count, stopwatch.ElapsedMilliseconds);
    }
}

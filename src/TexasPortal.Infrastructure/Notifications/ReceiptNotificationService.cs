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

public sealed class ReceiptNotificationService : IReceiptNotificationService
{
    private readonly INotificationPublisher _publisher;
    private readonly IDashboardRefreshCoordinator _dashboardCoordinator;
    private readonly ILogger<ReceiptNotificationService> _logger;

    public ReceiptNotificationService(
        INotificationPublisher publisher,
        IDashboardRefreshCoordinator dashboardCoordinator,
        ILogger<ReceiptNotificationService> logger)
    {
        _publisher = publisher;
        _dashboardCoordinator = dashboardCoordinator;
        _logger = logger;
    }

    public async Task PublishReceiptChangedAsync(
        string referenceType,
        string referenceId,
        long? headId = null,
        long? agentId = null,
        long? supplierId = null,
        bool includeApprovalQueue = true,
        bool includeGlobalList = true,
        CancellationToken cancellationToken = default)
    {
        var target = new ReceiptTarget(headId, agentId, supplierId, includeApprovalQueue, includeGlobalList);
        var targetGroups = target.ResolveTargetGroups().Distinct().ToList();

        var stopwatch = Stopwatch.StartNew();

        foreach (var targetGroup in targetGroups)
        {
            _logger.LogInformation(
                "[ReceiptNotification] Enqueuing ReceiptChanged. ReferenceType={ReferenceType}, ReferenceId={ReferenceId}, HeadId={HeadId}, AgentId={AgentId}, TargetGroup={TargetGroup}",
                referenceType, referenceId, headId, agentId, targetGroup);

            await _publisher.PublishAsync(
                eventName: BusinessEventNames.ReceiptChanged,
                referenceType: referenceType,
                referenceId: referenceId,
                headId: headId,
                agentId: agentId,
                targetGroup: targetGroup,
                priority: "Normal",
                ct: cancellationToken);
        }

        await _dashboardCoordinator.TriggerDashboardRefreshAsync(
            sourceEventName: BusinessEventNames.ReceiptChanged,
            referenceType: referenceType,
            referenceId: referenceId,
            headId: headId,
            userId: agentId,
            cancellationToken: cancellationToken);

        stopwatch.Stop();
        _logger.LogDebug("[ReceiptNotification] Enqueued {Count} ReceiptChanged notification(s) in {ElapsedTime} ms",
            targetGroups.Count, stopwatch.ElapsedMilliseconds);
    }
}

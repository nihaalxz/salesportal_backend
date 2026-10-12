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

public sealed class DashboardRefreshCoordinator : IDashboardRefreshCoordinator
{
    private readonly INotificationPublisher _publisher;
    private readonly ILogger<DashboardRefreshCoordinator> _logger;

    public DashboardRefreshCoordinator(
        INotificationPublisher publisher,
        ILogger<DashboardRefreshCoordinator> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task TriggerDashboardRefreshAsync(
        string sourceEventName,
        string referenceType,
        string referenceId,
        long? headId = null,
        long? userId = null,
        CancellationToken cancellationToken = default)
    {
        var targetGroups = ResolveDashboardTargetGroups(sourceEventName, headId, userId).Distinct().ToList();

        var stopwatch = Stopwatch.StartNew();

        foreach (var targetGroup in targetGroups)
        {
            _logger.LogInformation(
                "[DashboardRefreshCoordinator] Enqueuing DashboardChanged. SourceEvent={SourceEvent}, ReferenceType={ReferenceType}, ReferenceId={ReferenceId}, TargetGroup={TargetGroup}",
                sourceEventName, referenceType, referenceId, targetGroup);

            await _publisher.PublishAsync(
                eventName: BusinessEventNames.DashboardChanged,
                referenceType: referenceType,
                referenceId: referenceId,
                headId: headId,
                agentId: userId,
                targetGroup: targetGroup,
                priority: "Normal",
                ct: cancellationToken);
        }

        stopwatch.Stop();
        _logger.LogDebug("[DashboardRefreshCoordinator] Enqueued {Count} DashboardChanged notification(s) in {ElapsedTime} ms",
            targetGroups.Count, stopwatch.ElapsedMilliseconds);
    }

    private static IEnumerable<string> ResolveDashboardTargetGroups(string sourceEventName, long? headId, long? userId)
    {
        var list = new List<string>();

        switch (sourceEventName)
        {
            case BusinessEventNames.SalesEntriesChanged:
                list.Add(SignalRGroups.DashboardSales);
                break;

            case BusinessEventNames.LedgerChanged:
                list.Add(SignalRGroups.DashboardFinance);
                break;

            case BusinessEventNames.WalletChanged:
                list.Add(SignalRGroups.DashboardWallet);
                break;

            case BusinessEventNames.OutstandingChanged:
                list.Add(SignalRGroups.DashboardSummary);
                break;

            case BusinessEventNames.ReceiptChanged:
                list.Add(SignalRGroups.DashboardReceipts);
                break;

            case BusinessEventNames.OperationalQueueChanged:
                list.Add(SignalRGroups.DashboardOperations);
                break;

            default:
                list.Add(SignalRGroups.DashboardSummary);
                break;
        }

        if (userId.HasValue)
        {
            list.Add(SignalRGroups.DashboardUser(userId.Value));
        }

        return list;
    }
}

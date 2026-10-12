using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Contracts.Notifications;

namespace TexasPortal.Infrastructure.Notifications;

public sealed class LedgerNotificationService : ILedgerNotificationService
{
    private readonly INotificationPublisher _publisher;
    private readonly IDashboardRefreshCoordinator _dashboardCoordinator;

    public LedgerNotificationService(
        INotificationPublisher publisher,
        IDashboardRefreshCoordinator dashboardCoordinator)
    {
        _publisher = publisher;
        _dashboardCoordinator = dashboardCoordinator;
    }

    public Task PublishLedgerChangedAsync(
        string referenceType,
        string referenceId,
        long? headId = null,
        long? agentId = null,
        CancellationToken cancellationToken = default)
    {
        var target = new LedgerTarget(headId, agentId);
        return PublishLedgerChangedTargetsAsync(referenceType, referenceId, new[] { target }, cancellationToken);
    }

    public async Task PublishLedgerChangedTargetsAsync(
        string referenceType,
        string referenceId,
        IEnumerable<LedgerTarget> targets,
        CancellationToken cancellationToken = default)
    {
        if (targets == null) return;

        var distinctTargets = targets
            .Where(t => t != null)
            .GroupBy(t => t.ResolveTargetGroup())
            .Select(g => g.First())
            .ToList();

        foreach (var target in distinctTargets)
        {
            var targetGroup = target.ResolveTargetGroup();
            await _publisher.PublishAsync(
                eventName: BusinessEventNames.LedgerChanged,
                referenceType: referenceType,
                referenceId: referenceId,
                headId: target.HeadId,
                agentId: target.AgentId,
                targetGroup: targetGroup,
                priority: "Normal",
                ct: cancellationToken);
        }

        await _dashboardCoordinator.TriggerDashboardRefreshAsync(
            sourceEventName: BusinessEventNames.LedgerChanged,
            referenceType: referenceType,
            referenceId: referenceId,
            cancellationToken: cancellationToken);
    }
}

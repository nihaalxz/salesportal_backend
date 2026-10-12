using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Contracts.Notifications;

namespace TexasPortal.Infrastructure.Notifications;

public sealed class SalesEntriesNotificationService : ISalesEntriesNotificationService
{
    private readonly INotificationPublisher _publisher;
    private readonly IDashboardRefreshCoordinator _dashboardCoordinator;

    public SalesEntriesNotificationService(
        INotificationPublisher publisher,
        IDashboardRefreshCoordinator dashboardCoordinator)
    {
        _publisher = publisher;
        _dashboardCoordinator = dashboardCoordinator;
    }

    public async Task PublishBookingCreatedAsync(
        string referenceType,
        long bookingId,
        long? headId = null,
        long? agentId = null,
        CancellationToken cancellationToken = default)
    {
        await _publisher.PublishAsync(
            eventName: BusinessEventNames.SalesEntriesChanged,
            referenceType: referenceType,
            referenceId: bookingId.ToString(),
            headId: headId,
            agentId: agentId,
            targetGroup: SignalRGroups.Accounts,
            priority: "Normal",
            ct: cancellationToken);

        await _dashboardCoordinator.TriggerDashboardRefreshAsync(
            sourceEventName: BusinessEventNames.SalesEntriesChanged,
            referenceType: referenceType,
            referenceId: bookingId.ToString(),
            headId: headId,
            userId: agentId,
            cancellationToken: cancellationToken);
    }
}

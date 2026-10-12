using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Contracts.Notifications;

namespace TexasPortal.Application.Common.Interfaces;

public interface INotificationPublisher
{
    Task PublishAsync(
        string eventName,
        string referenceType,
        string referenceId,
        long? headId = null,
        long? agentId = null,
        string? targetGroup = null,
        string priority = "Normal",
        CancellationToken ct = default);

    Task PublishEnvelopeAsync(
        NotificationEnvelope envelope,
        CancellationToken ct = default);
}

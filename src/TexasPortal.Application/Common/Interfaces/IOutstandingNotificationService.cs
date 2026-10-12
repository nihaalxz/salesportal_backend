using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Contracts.Notifications;

namespace TexasPortal.Application.Common.Interfaces;

public record OutstandingOwnerTarget(long OwnerId, string OwnerType)
{
    public string ResolveTargetGroup() => OwnerType.ToLowerInvariant() switch
    {
        "customer" => SignalRGroups.CustomerOutstanding(OwnerId),
        "head" => SignalRGroups.CustomerOutstanding(OwnerId),
        "agent" => SignalRGroups.AgentOutstanding(OwnerId),
        "supplier" => SignalRGroups.SupplierOutstanding(OwnerId),
        _ => $"outstanding:{OwnerType.ToLowerInvariant()}:{OwnerId}"
    };
}

public interface IOutstandingNotificationService
{
    Task PublishOutstandingChangedAsync(
        string referenceType,
        string referenceId,
        long ownerId,
        string ownerType,
        CancellationToken cancellationToken = default);

    Task PublishOutstandingChangedTargetsAsync(
        string referenceType,
        string referenceId,
        IEnumerable<OutstandingOwnerTarget> targets,
        CancellationToken cancellationToken = default);
}

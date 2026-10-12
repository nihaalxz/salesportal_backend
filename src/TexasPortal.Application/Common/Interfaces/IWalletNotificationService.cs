using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Contracts.Notifications;

namespace TexasPortal.Application.Common.Interfaces;

public record WalletOwnerTarget(long OwnerId, string OwnerType)
{
    public string ResolveTargetGroup() => OwnerType.ToLowerInvariant() switch
    {
        "customer" => SignalRGroups.CustomerWallet(OwnerId),
        "head" => SignalRGroups.CustomerWallet(OwnerId),
        "agent" => SignalRGroups.AgentWallet(OwnerId),
        "supplier" => SignalRGroups.SupplierWallet(OwnerId),
        _ => $"wallet:{OwnerType.ToLowerInvariant()}:{OwnerId}"
    };
}

public interface IWalletNotificationService
{
    Task PublishWalletChangedAsync(
        string referenceType,
        string referenceId,
        long ownerId,
        string ownerType,
        CancellationToken cancellationToken = default);

    Task PublishWalletChangedTargetsAsync(
        string referenceType,
        string referenceId,
        IEnumerable<WalletOwnerTarget> targets,
        CancellationToken cancellationToken = default);
}

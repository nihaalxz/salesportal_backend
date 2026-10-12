using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Contracts.Notifications;

namespace TexasPortal.Application.Common.Interfaces;

public record LedgerTarget(long? HeadId = null, long? AgentId = null, long? SupplierId = null)
{
    public string ResolveTargetGroup() =>
        HeadId.HasValue ? SignalRGroups.CustomerLedger(HeadId.Value) :
        AgentId.HasValue ? SignalRGroups.AgentLedger(AgentId.Value) :
        SupplierId.HasValue ? SignalRGroups.SupplierLedger(SupplierId.Value) :
        SignalRGroups.Accounts;
}

public interface ILedgerNotificationService
{
    Task PublishLedgerChangedAsync(
        string referenceType,
        string referenceId,
        long? headId = null,
        long? agentId = null,
        CancellationToken cancellationToken = default);

    Task PublishLedgerChangedTargetsAsync(
        string referenceType,
        string referenceId,
        IEnumerable<LedgerTarget> targets,
        CancellationToken cancellationToken = default);
}

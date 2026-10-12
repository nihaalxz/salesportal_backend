using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Contracts.Notifications;

namespace TexasPortal.Application.Common.Interfaces;

public record ReceiptTarget(long? HeadId = null, long? AgentId = null, long? SupplierId = null, bool IncludeApprovalQueue = true, bool IncludeGlobalList = true)
{
    public IEnumerable<string> ResolveTargetGroups()
    {
        var list = new List<string>();
        if (HeadId.HasValue) list.Add(SignalRGroups.CustomerReceipt(HeadId.Value));
        if (AgentId.HasValue) list.Add(SignalRGroups.AgentReceipt(AgentId.Value));
        if (SupplierId.HasValue) list.Add(SignalRGroups.SupplierReceipt(SupplierId.Value));
        if (IncludeApprovalQueue) list.Add(SignalRGroups.ReceiptApproval);
        if (IncludeGlobalList) list.Add(SignalRGroups.ReceiptList);
        return list;
    }
}

public interface IReceiptNotificationService
{
    Task PublishReceiptChangedAsync(
        string referenceType,
        string referenceId,
        long? headId = null,
        long? agentId = null,
        long? supplierId = null,
        bool includeApprovalQueue = true,
        bool includeGlobalList = true,
        CancellationToken cancellationToken = default);
}

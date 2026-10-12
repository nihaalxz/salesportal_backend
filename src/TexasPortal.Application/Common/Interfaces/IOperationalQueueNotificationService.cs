using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Contracts.Notifications;

namespace TexasPortal.Application.Common.Interfaces;

public record OperationalQueueTarget(string QueueType, long? AssignedUserId = null, bool IncludeOperationsRole = true)
{
    public IEnumerable<string> ResolveTargetGroups()
    {
        var list = new List<string>();
        switch (QueueType.ToLowerInvariant())
        {
            case "visa":
                list.Add(SignalRGroups.OperationsVisa);
                break;
            case "vfs":
                list.Add(SignalRGroups.OperationsVfs);
                break;
            case "holiday":
                list.Add(SignalRGroups.OperationsHoliday);
                break;
            case "transport":
            case "transportation":
                list.Add(SignalRGroups.OperationsTransport);
                break;
            case "otherservice":
            case "others":
                list.Add(SignalRGroups.OperationsOtherServices);
                break;
        }

        if (AssignedUserId.HasValue)
        {
            list.Add(SignalRGroups.OperationsUser(AssignedUserId.Value));
        }

        if (IncludeOperationsRole)
        {
            list.Add(SignalRGroups.Operations);
        }

        return list;
    }
}

public interface IOperationalQueueNotificationService
{
    Task PublishOperationalQueueChangedAsync(
        string referenceType,
        string referenceId,
        string queueType,
        long? assignedUserId = null,
        bool includeOperationsRole = true,
        CancellationToken cancellationToken = default);
}

using System.Threading;
using System.Threading.Tasks;

namespace TexasPortal.Application.Common.Interfaces;

public interface ISalesEntriesNotificationService
{
    Task PublishBookingCreatedAsync(
        string referenceType,
        long bookingId,
        long? headId = null,
        long? agentId = null,
        CancellationToken cancellationToken = default);
}

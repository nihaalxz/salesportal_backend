using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Contracts.Notifications;

namespace TexasPortal.Application.Common.Interfaces;

public interface INotificationDispatcher
{
    Task DispatchAsync(NotificationEnvelope envelope, CancellationToken ct = default);
}

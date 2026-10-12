using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TexasPortal.Application.Common.Interfaces;

public interface IDashboardRefreshCoordinator
{
    Task TriggerDashboardRefreshAsync(
        string sourceEventName,
        string referenceType,
        string referenceId,
        long? headId = null,
        long? userId = null,
        CancellationToken cancellationToken = default);
}

using System.Threading;
using System.Threading.Tasks;

namespace TexasPortal.Application.Sales.Accounts;

public interface ICustomerAccountProjectionService
{
    Task<decimal> RebuildCustomerBalanceCacheAsync(long headId, CancellationToken ct = default);
    Task<decimal> RebuildReceivableOutstandingCacheAsync(long creditReceivableId, CancellationToken ct = default);
    Task<bool> ValidateCustomerLedgerIntegrityAsync(long headId, CancellationToken ct = default);
}

using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Contracts.Sales.Accounts;
using TexasPortal.Domain.Enums;

namespace TexasPortal.Application.Sales.Accounts;

public interface ICustomerAccountQueryService
{
    Task<CustomerAccountSummaryResponse?> GetAccountSummaryAsync(long headId, HeadType headType = HeadType.Customer, CancellationToken ct = default);
    Task<CustomerApplicationReceivableSummary> GetApplicationReceivablesSummaryAsync(long headId, HeadType headType = HeadType.Customer, CancellationToken ct = default);
    Task<System.Collections.Generic.List<AccountTimelineItemResponse>> GetAccountTimelineAsync(long headId, CancellationToken ct = default);
    Task<AccountStatementResponse?> GetAccountStatementAsync(long headId, CancellationToken ct = default);
}

using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Contracts.Sales.Accounts;

namespace TexasPortal.Application.Sales.Accounts;

public interface ICustomerAccountService
{
    Task<CreditAllocationResponse> AuthorizeAndProcessCreditBookingAsync(CreditBookingRequest request, CancellationToken ct = default);
    Task DepositBalanceAsync(long headId, decimal amount, string referenceId, long createdBy, string remarks, string? correlationId = null, CancellationToken ct = default);
    Task ProcessReceiptSettlementAsync(long headId, long receivableId, decimal settlementAmount, string referenceId, long createdBy, string remarks, string? correlationId = null, CancellationToken ct = default);
    Task ProcessRefundAsync(long headId, decimal refundAmount, string bookingNumber, long createdBy, string remarks, string? correlationId = null, CancellationToken ct = default);
    Task ProcessCreditNoteAsync(long headId, long receivableId, decimal amount, string reason, long approvedBy, string? correlationId = null, CancellationToken ct = default);
    Task ProcessAdjustmentAsync(long headId, decimal amount, string reason, long createdBy, string? correlationId = null, CancellationToken ct = default);
}

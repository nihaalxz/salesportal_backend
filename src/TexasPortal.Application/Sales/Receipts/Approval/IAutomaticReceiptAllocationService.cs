using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Domain.Entities;

namespace TexasPortal.Application.Sales.Receipts.Approval;

public interface IAutomaticReceiptAllocationService
{
    /// <summary>
    /// Performs automatic FIFO allocation of receipt deposit funds across open CreditReceivables.
    /// </summary>
    /// <returns>Total amount allocated to receivables.</returns>
    Task<decimal> AllocateAsync(Receipt receipt, CancellationToken ct = default);
}

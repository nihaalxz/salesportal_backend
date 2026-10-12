using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TexasPortal.Application.Persistence;
using TexasPortal.Domain.Entities;
using TexasPortal.Domain.Enums;

namespace TexasPortal.Application.Sales.Receipts.Approval;

public class AutomaticReceiptAllocationService : IAutomaticReceiptAllocationService
{
    private readonly ISalesPortalDbContext _db;
    private readonly ILogger<AutomaticReceiptAllocationService> _logger;

    public AutomaticReceiptAllocationService(
        ISalesPortalDbContext db,
        ILogger<AutomaticReceiptAllocationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<decimal> AllocateAsync(Receipt receipt, CancellationToken ct = default)
    {
        if (receipt == null || receipt.Amount <= 0) return 0m;
        var headId = receipt.HeadId ?? receipt.AgentId;
        if (!headId.HasValue) return 0m;

        var isAgent = receipt.AgentId.HasValue;

        // Query open credit receivables for this head, ordered by CreatedDate ASC (FIFO)
        var openReceivables = await _db.CreditReceivables
            .Where(r => (isAgent ? r.AgentId == headId : r.HeadId == headId)
                     && r.Status != ReceivableStatus.Closed 
                     && r.Status != ReceivableStatus.Cancelled)
            .OrderBy(r => r.CreatedDate)
            .ToListAsync(ct);

        decimal unallocatedPool = receipt.Amount;
        decimal totalAllocated = 0m;

        foreach (var receivable in openReceivables)
        {
            if (unallocatedPool <= 0) break;
            if (receivable.CachedOutstandingAmount <= 0) continue;

            var allocAmount = Math.Min(unallocatedPool, receivable.CachedOutstandingAmount);
            var correlationId = $"receipt-approval:{receipt.Id}:receivable:{receivable.Id}";

            // Duplicate guard for retry safety
            var alreadyPosted = await _db.ReceivableTransactions
                .AnyAsync(t => t.CorrelationId == correlationId, ct);

            if (!alreadyPosted)
            {
                // Create immutable ledger transaction
                var txn = new ReceivableTransaction
                {
                    ReceivableId = receivable.Id,
                    TransactionType = CreditTransactionType.ReceiptPayment,
                    Amount = -allocAmount, // negative reduces debt
                    CorrelationId = correlationId,
                    ReferenceId = receipt.DocumentNumber,
                    TransactionDate = DateTime.UtcNow,
                    CreatedBy = receipt.CreatedBy ?? 1,
                    Remarks = $"Automatic FIFO allocation via Receipt {receipt.DocumentNumber}"
                };
                _db.ReceivableTransactions.Add(txn);

                // Create ReceiptAllocation audit record
                var allocation = new ReceiptAllocation
                {
                    ReceiptId = receipt.Id,
                    ReceivableId = receivable.Id,
                    AllocatedAmount = allocAmount,
                    AllocationDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = receipt.CreatedBy ?? 1
                };
                _db.ReceiptAllocations.Add(allocation);
            }

            receivable.CachedOutstandingAmount -= allocAmount;
            if (receivable.CachedOutstandingAmount <= 0)
            {
                receivable.CachedOutstandingAmount = 0m;
                receivable.Status = ReceivableStatus.Closed;
                receivable.PaymentStatus = ReceivablePaymentStatus.Approved;
                receivable.ClosedDate = DateTime.UtcNow;
            }
            else
            {
                receivable.Status = ReceivableStatus.PartiallyPaid;
                receivable.PaymentStatus = ReceivablePaymentStatus.UnderReview;
            }

            _db.Entry(receivable).State = EntityState.Modified;

            unallocatedPool -= allocAmount;
            totalAllocated += allocAmount;
        }

        _logger.LogInformation(
            "Automatic FIFO allocation for Receipt {ReceiptId}: Allocated {TotalAllocated:N3} BHD across receivables. Remaining unallocated deposit pool: {UnallocatedPool:N3} BHD.",
            receipt.Id, totalAllocated, unallocatedPool);

        return totalAllocated;
    }
}

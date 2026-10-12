using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TexasPortal.Application.Persistence;
using TexasPortal.Application.Sales.Receipts.Approval;
using TexasPortal.Domain.Entities;
using TexasPortal.Domain.Enums;

namespace TexasPortal.Application.Sales.Receivables;

public interface IReceivableReconciliationService
{
    Task<ReconciliationReport> BackfillApprovedReceiptsAsync(long? systemUserId = null, CancellationToken ct = default);
    Task<ReconciliationReport> RebuildCacheAsync(long? headId = null, CancellationToken ct = default);
    Task<List<OrphanReceiptInfo>> DetectOrphanReceiptsAsync(CancellationToken ct = default);
}

public sealed record OrphanReceiptInfo(long ReceiptId, string DocumentNumber, string Status, decimal Amount, int PendingPayments);
public sealed record ReconciliationReport(int Processed, int Fixed, int Skipped, List<string> Errors);

/// <summary>
/// Detects and repairs financial inconsistencies without touching Program.cs startup logic.
/// Invokable via admin API, CLI, or scheduled job.
///
/// Primary use case on deployment: repair receipts that were approved before
/// the ReceiptApprovalOrchestrator existed (e.g. TXSRCPT000045).
/// </summary>
public class ReceivableReconciliationService : IReceivableReconciliationService
{
    private readonly ISalesPortalDbContext _db;
    private readonly ReceiptApprovalOrchestrator _orchestrator;
    private readonly ILogger<ReceivableReconciliationService> _logger;

    public ReceivableReconciliationService(
        ISalesPortalDbContext db,
        ReceiptApprovalOrchestrator orchestrator,
        ILogger<ReceivableReconciliationService> logger)
    {
        _db = db;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Finds all credit_settlement receipts with status approved/success that have
    /// ReceivablePayment records still in Submitted state, then retroactively runs
    /// the financial pipeline for them (inserts missing ReceivableTransactions,
    /// recalculates CachedOutstandingAmount).
    /// </summary>
    public async Task<ReconciliationReport> BackfillApprovedReceiptsAsync(long? systemUserId = null, CancellationToken ct = default)
    {
        var actorId = systemUserId ?? 1L;
        var errors = new List<string>();
        var processed = 0;
        var fixed_ = 0;
        var skipped = 0;

        // Find orphaned receipts: credit_settlement + (approved|success) + has Submitted ReceivablePayments
        var orphanReceiptIds = await _db.ReceivablePayments
            .Where(p => p.Status == ReceivablePaymentStatus.Submitted)
            .Select(p => p.ReceiptId)
            .Distinct()
            .ToListAsync(ct);

        if (orphanReceiptIds.Count == 0)
        {
            _logger.LogInformation("Backfill: no orphaned receipts found.");
            return new ReconciliationReport(0, 0, 0, errors);
        }

        var orphanedReceipts = await _db.Receipts
            .Where(r => orphanReceiptIds.Contains(r.Id) &&
                        (r.Status == "approved" || r.Status == "success" || r.Status == "pending") &&
                        r.DeletedAt == null)
            .ToListAsync(ct);

        _logger.LogInformation("Backfill: found {Count} receipts needing financial reconciliation.", orphanedReceipts.Count);

        foreach (var receipt in orphanedReceipts)
        {
            processed++;
            try
            {
                // Insert missing ReceivableTransactions and recalculate outstanding
                var payments = await _db.ReceivablePayments
                    .Where(p => p.ReceiptId == receipt.Id && p.Status == ReceivablePaymentStatus.Submitted)
                    .ToListAsync(ct);

                foreach (var payment in payments)
                {
                    var correlationId = $"receipt-approval:{receipt.Id}:receivable:{payment.ReceivableId}";

                    var alreadyPosted = await _db.ReceivableTransactions
                        .AnyAsync(t => t.CorrelationId == correlationId, ct);

                    if (!alreadyPosted)
                    {
                        _db.ReceivableTransactions.Add(new ReceivableTransaction
                        {
                            ReceivableId = payment.ReceivableId,
                            TransactionType = CreditTransactionType.ReceiptPayment,
                            Amount = -payment.Amount,
                            CorrelationId = correlationId,
                            ReferenceId = receipt.DocumentNumber,
                            TransactionDate = receipt.UpdatedAt ?? receipt.CreatedAt ?? DateTime.UtcNow,
                            CreatedBy = actorId,
                            Remarks = $"Backfill: retroactive approval for Receipt {receipt.DocumentNumber}"
                        });
                    }

                    payment.Status = ReceivablePaymentStatus.Approved;
                    payment.ApprovedBy = actorId;
                    payment.ApprovedAt = DateTime.UtcNow;
                }

                receipt.ReceiptType = ReceiptType.CreditSettlement;
                await _db.SaveChangesAsync(ct);

                // Recalculate cache from ledger SUM
                var receivableIds = payments.Select(p => p.ReceivableId).Distinct();
                foreach (var receivableId in receivableIds)
                {
                    await _orchestrator.RecalculateLedgerBalanceAsync(receivableId, ct);
                }

                _logger.LogInformation("Backfill: fixed Receipt {DocumentNumber} ({ReceiptId}).", receipt.DocumentNumber, receipt.Id);
                fixed_++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backfill: error processing Receipt {ReceiptId}.", receipt.Id);
                errors.Add($"Receipt {receipt.Id}: {ex.Message}");
            }
        }

        return new ReconciliationReport(processed, fixed_, skipped, errors);
    }

    /// <summary>
    /// Rebuilds CachedOutstandingAmount for all receivables from their immutable
    /// ReceivableTransaction ledger. Optionally scoped to a single head.
    /// Fixes cache corruption without touching transaction history.
    /// </summary>
    public async Task<ReconciliationReport> RebuildCacheAsync(long? headId = null, CancellationToken ct = default)
    {
        await EnsureInitialCreditSaleTransactionsExistAsync(headId, ct);

        var errors = new List<string>();
        var processed = 0;
        var fixed_ = 0;

        var query = _db.CreditReceivables.AsQueryable();
        if (headId.HasValue)
            query = query.Where(r => r.HeadId == headId || r.CustomerId == headId || r.AgentId == headId);

        var receivables = await query.Select(r => r.Id).ToListAsync(ct);

        foreach (var receivableId in receivables)
        {
            processed++;
            try
            {
                await _orchestrator.RecalculateLedgerBalanceAsync(receivableId, ct);
                fixed_++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache rebuild: error for receivable {ReceivableId}.", receivableId);
                errors.Add($"Receivable {receivableId}: {ex.Message}");
            }
        }

        return new ReconciliationReport(processed, fixed_, 0, errors);
    }

    private async Task EnsureInitialCreditSaleTransactionsExistAsync(long? headId = null, CancellationToken ct = default)
    {
        var query = _db.CreditReceivables.Include(r => r.Transactions).AsQueryable();
        if (headId.HasValue)
            query = query.Where(r => r.HeadId == headId || r.CustomerId == headId || r.AgentId == headId);

        var receivablesMissingInitial = await query
            .Where(r => !r.Transactions.Any(t => t.TransactionType == CreditTransactionType.CreditSale))
            .ToListAsync(ct);

        if (receivablesMissingInitial.Count == 0) return;

        foreach (var rec in receivablesMissingInitial)
        {
            var correlationId = $"initial-creditsale:{rec.Id}";
            var exists = await _db.ReceivableTransactions.AnyAsync(t => t.CorrelationId == correlationId, ct);
            if (!exists)
            {
                _db.ReceivableTransactions.Add(new ReceivableTransaction
                {
                    ReceivableId = rec.Id,
                    TransactionType = CreditTransactionType.CreditSale,
                    Amount = rec.TotalAmount,
                    CorrelationId = correlationId,
                    ReferenceId = rec.BookingNumber,
                    TransactionDate = rec.CreatedDate,
                    CreatedBy = rec.SalesmanId,
                    Remarks = $"Backfill: Initial Credit Sale for {rec.BookingModule} #{rec.BookingNumber}"
                });
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Returns a list of receipts that are approved/success but have ReceivablePayments
    /// still in Submitted state — indicating the financial pipeline was never completed.
    /// </summary>
    public async Task<List<OrphanReceiptInfo>> DetectOrphanReceiptsAsync(CancellationToken ct = default)
    {
        var submittedByReceipt = await _db.ReceivablePayments
            .Where(p => p.Status == ReceivablePaymentStatus.Submitted)
            .GroupBy(p => p.ReceiptId)
            .Select(g => new { ReceiptId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var receiptIds = submittedByReceipt.Select(x => x.ReceiptId).ToList();

        var receipts = await _db.Receipts
            .Where(r => receiptIds.Contains(r.Id) &&
                        (r.Status == "approved" || r.Status == "success") &&
                        r.DeletedAt == null)
            .ToListAsync(ct);

        return receipts.Select(r => new OrphanReceiptInfo(
            r.Id,
            r.DocumentNumber,
            r.Status,
            r.Amount,
            submittedByReceipt.FirstOrDefault(x => x.ReceiptId == r.Id)?.Count ?? 0
        )).ToList();
    }
}

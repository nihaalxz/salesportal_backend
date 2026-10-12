using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Application.Persistence;
using TexasPortal.Contracts.Notifications;
using TexasPortal.Domain.Entities;
using TexasPortal.Domain.Enums;

namespace TexasPortal.Application.Sales.Receipts.Approval;

/// <summary>
/// The single, authoritative entry point for all receipt approvals.
///
/// Guarantees:
/// - Exactly-once approval (IdempotencyRecord gate)
/// - Atomic DB update (IDbContextTransaction)
/// - Exactly-once ledger entry (ReceivableTransaction.CorrelationId unique index)
/// - Optimistic concurrency (RowVersion on Receipt and CreditReceivable)
/// - Crash recovery (OutboxMessage committed inside same transaction)
/// - No orphan state (Receipt=Approved ↔ Receivable=Approved always in sync)
/// </summary>
public sealed class ReceiptApprovalOrchestrator
{
    private readonly ISalesPortalDbContext _db;
    private readonly IAutomaticReceiptAllocationService _automaticAllocationService;
    private readonly IReceiptNotificationService _receiptNotificationService;
    private readonly ILedgerNotificationService _ledgerNotificationService;
    private readonly IOutstandingNotificationService _outstandingNotificationService;
    private readonly IWalletNotificationService _walletNotificationService;
    private readonly ILogger<ReceiptApprovalOrchestrator> _logger;

    public ReceiptApprovalOrchestrator(
        ISalesPortalDbContext db,
        IAutomaticReceiptAllocationService automaticAllocationService,
        IReceiptNotificationService receiptNotificationService,
        ILedgerNotificationService ledgerNotificationService,
        IOutstandingNotificationService outstandingNotificationService,
        IWalletNotificationService walletNotificationService,
        ILogger<ReceiptApprovalOrchestrator> logger)
    {
        _db = db;
        _automaticAllocationService = automaticAllocationService;
        _receiptNotificationService = receiptNotificationService;
        _ledgerNotificationService = ledgerNotificationService;
        _outstandingNotificationService = outstandingNotificationService;
        _walletNotificationService = walletNotificationService;
        _logger = logger;
    }

    public async Task<ReceiptApprovalResult> HandleAsync(
        ReceiptApprovalCommand command,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Initiating receipt approval. ReceiptId: {ReceiptId}, AccountantUserId: {AccountantUserId}", command.ReceiptId, command.AccountantUserId);
        var idempotencyKey = $"receipt-approval:{command.ReceiptId}";

        // ── 1. Idempotency Gate ───────────────────────────────────────────────
        var existing = await _db.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey, ct);

        if (existing != null)
        {
            if (existing.Status == "Completed" && existing.ResponsePayload != null)
            {
                _logger.LogInformation("Receipt {ReceiptId} already approved (idempotent). Returning cached result.", command.ReceiptId);
                var cached = JsonSerializer.Deserialize<ReceiptApprovalData>(existing.ResponsePayload);
                return ReceiptApprovalResult.AlreadyProcessed(cached!);
            }
            if (existing.Status == "InProgress")
            {
                // Stale lock recovery: If record has been InProgress for > 2 minutes, treat as crashed/abandoned lock and allow retry
                if (existing.CreatedAt < DateTime.UtcNow.AddMinutes(-2))
                {
                    _logger.LogWarning("Found stale InProgress idempotency record for key {IdempotencyKey} created at {CreatedAt}. Overriding stale lock.", idempotencyKey, existing.CreatedAt);
                }
                else
                {
                    return ReceiptApprovalResult.Conflict("Another approval request is currently in progress for this receipt.");
                }
            }
        }

        // ── 2. Load Receipt with Head ──────────────────────────────────────────
        var receipt = await _db.Receipts
            .Include(r => r.Head)
            .FirstOrDefaultAsync(r => r.Id == command.ReceiptId && r.DeletedAt == null, ct);
        _logger.LogDebug("Loaded receipt with Id {ReceiptId}. Status: {Status}", receipt?.Id, receipt?.Status);

        if (receipt == null)
            return ReceiptApprovalResult.NotFound();

        if (receipt.Status.Equals("approved", StringComparison.OrdinalIgnoreCase) ||
            receipt.Status.Equals("success", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Receipt {ReceiptId} is already in status '{Status}' but had no idempotency record.", command.ReceiptId, receipt.Status);
            var earlyData = BuildData(receipt, 0, 0);
            return ReceiptApprovalResult.AlreadyProcessed(earlyData);
        }

        if (receipt.Status.Equals("rejected", StringComparison.OrdinalIgnoreCase) ||
            receipt.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return ReceiptApprovalResult.UnprocessableEntity($"Receipt is in status '{receipt.Status}' and cannot be approved.");
        }

        // ── 3. Mark Idempotency as InProgress ─────────────────────────────────
        var idemRecord = existing ?? new IdempotencyRecord
        {
            IdempotencyKey = idempotencyKey,
            RequestHash = idempotencyKey,
            Status = "InProgress",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };
        idemRecord.Status = "InProgress";
        idemRecord.CreatedAt = DateTime.UtcNow;
        if (existing == null) _db.IdempotencyRecords.Add(idemRecord);
        await _db.SaveChangesAsync(ct);

        // ── 4. Begin Atomic Transaction ───────────────────────────────────────
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                // a. Update Receipt Status
                receipt.Status = "success";
                receipt.UpdatedAt = DateTime.UtcNow;

                bool isWalletDeposit = string.Equals(receipt.Service, "wallet_deposit", StringComparison.OrdinalIgnoreCase) ||
                                       string.Equals(receipt.Service, "deposit", StringComparison.OrdinalIgnoreCase) ||
                                       receipt.ReceiptType == ReceiptType.WalletDeposit;

                if (isWalletDeposit)
                {
                    receipt.ReceiptType = ReceiptType.WalletDeposit;

                    // b. Update Head.Balance (Balance is getter/setter alias for Wallet) ONLY for pure wallet deposits
                    if (receipt.HeadId.HasValue)
                    {
                        var head = receipt.Head ?? await _db.Heads.FirstOrDefaultAsync(h => h.Id == receipt.HeadId.Value, ct);
                        if (head != null)
                        {
                            head.Balance += receipt.Amount;
                            _db.Entry(head).State = EntityState.Modified;
                        }
                    }
                }

                // c. Record Financial Ledgers (CustomerAccountTransaction, BalanceTransaction, HeadLedger) ONLY for pure wallet deposits
                var targetHeadId = receipt.HeadId ?? receipt.AgentId;
                if (targetHeadId.HasValue)
                {
                    if (isWalletDeposit)
                    {
                        var depCid = $"receipt-deposit:{command.ReceiptId}";
                        var alreadyPosted = await _db.CustomerAccountTransactions
                            .AnyAsync(t => t.CorrelationId == depCid, ct);

                        if (!alreadyPosted)
                        {
                            _db.CustomerAccountTransactions.Add(new CustomerAccountTransaction
                            {
                                HeadId = targetHeadId.Value,
                                TransactionType = CustomerAccountTransactionType.Deposit,
                                Amount = receipt.Amount,
                                Currency = "BHD",
                                CorrelationId = depCid,
                                ReferenceId = receipt.DocumentNumber,
                                TransactionDate = DateTime.UtcNow,
                                CreatedBy = command.AccountantUserId,
                                Remarks = $"Balance deposit approved via Receipt {receipt.DocumentNumber}"
                            });

                            _db.BalanceTransactions.Add(new BalanceTransaction
                            {
                                HeadId = targetHeadId.Value,
                                TransactionType = BalanceTransactionType.Deposit,
                                Amount = receipt.Amount,
                                Currency = "BHD",
                                CorrelationId = depCid,
                                ReferenceId = receipt.DocumentNumber,
                                TransactionDate = DateTime.UtcNow,
                                CreatedBy = command.AccountantUserId,
                                Remarks = $"Balance deposit approved via Receipt {receipt.DocumentNumber}"
                            });

                            if (receipt.HeadId.HasValue)
                            {
                                _db.HeadLedgers.Add(new HeadLedger
                                {
                                    HeadId = receipt.HeadId.Value,
                                    ServiceType = receipt.Service ?? "deposit",
                                    ServiceId = receipt.Id,
                                    Description = $"Balance deposit approved via Receipt {receipt.DocumentNumber}: {receipt.Narration}",
                                    Debit = 0,
                                    Credit = receipt.Amount,
                                    CreatedAt = DateTime.UtcNow
                                });
                            }
                        }
                    }

                    // d. Automatic FIFO Allocation across open CreditReceivables
                    var totalAllocated = await _automaticAllocationService.AllocateAsync(receipt, ct);

                    _logger.LogInformation(
                        "Receipt {ReceiptId} approved. IsWalletDeposit: {IsWalletDeposit}. Total allocated to receivables: {Allocated:N3} BHD.",
                        command.ReceiptId, isWalletDeposit, totalAllocated);
                }

                // e. Write PaymentApprovedEvent to Outbox (crash-safe notifications)
                _db.OutboxMessages.Add(new OutboxMessage
                {
                    EventName = "PaymentApprovedEvent",
                    CorrelationId = $"receipt:{command.ReceiptId}",
                    Payload = JsonSerializer.Serialize(new
                    {
                        ReceiptId = command.ReceiptId,
                        DocumentNumber = receipt.DocumentNumber,
                        HeadId = receipt.HeadId,
                        AgentId = receipt.AgentId,
                        Amount = receipt.Amount,
                        ApprovedBy = command.AccountantUserId,
                        ApprovedAt = DateTime.UtcNow
                    }),
                    CreatedUtc = DateTime.UtcNow,
                    Status = "pending"
                });

                // f. Persist everything atomically & commit transaction
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                // ── 5. Post-commit: fire notifications (non-critical) ──────────────
                if (receipt.HeadId.HasValue || receipt.AgentId.HasValue)
                {
                    var ownerId = receipt.HeadId ?? receipt.AgentId!.Value;
                    var ownerType = receipt.AgentId.HasValue ? "agent" : "customer";

                    await _receiptNotificationService.PublishReceiptChangedAsync(
                        referenceType: ReferenceTypes.Receipt,
                        referenceId: receipt.Id.ToString(),
                        headId: receipt.HeadId,
                        agentId: receipt.AgentId,
                        includeApprovalQueue: true,
                        includeGlobalList: true,
                        cancellationToken: ct);

                    await _ledgerNotificationService.PublishLedgerChangedAsync(
                        referenceType: ReferenceTypes.Receipt,
                        referenceId: receipt.Id.ToString(),
                        headId: receipt.HeadId,
                        agentId: receipt.AgentId,
                        cancellationToken: ct);

                    await _outstandingNotificationService.PublishOutstandingChangedAsync(
                        referenceType: ReferenceTypes.Receipt,
                        referenceId: receipt.Id.ToString(),
                        ownerId: ownerId,
                        ownerType: ownerType,
                        cancellationToken: ct);

                    await _walletNotificationService.PublishWalletChangedAsync(
                        referenceType: ReferenceTypes.Receipt,
                        referenceId: receipt.Id.ToString(),
                        ownerId: ownerId,
                        ownerType: ownerType,
                        cancellationToken: ct);

                    await TexasPortal.Application.Sales.Notifications.NotificationHelper.SendHeadEmailNotificationAsync(
                        _db, receipt.HeadId, receipt.AgentId, "receipt", receipt.Id, "approved", receipt.Amount, receipt.Narration, ct, receipt.DocumentNumber, isCreation: false);
                }

                // ── 6. Mark Idempotency Completed ──────────────────────────────────
                var resultData = BuildData(receipt, 1, receipt.Amount);
                idemRecord.Status = "Completed";
                idemRecord.ResponseCode = 200;
                idemRecord.ResponsePayload = JsonSerializer.Serialize(resultData);
                await _db.SaveChangesAsync(ct);

                return ReceiptApprovalResult.Success(resultData);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await tx.RollbackAsync(ct);
                _logger.LogWarning(ex, "Concurrency conflict approving Receipt {ReceiptId}.", command.ReceiptId);
                idemRecord.Status = "Failed";
                await _db.SaveChangesAsync(ct);
                return ReceiptApprovalResult.Conflict("Receipt or receivable was modified concurrently. Please retry.");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                _logger.LogError(ex, "Failed to approve Receipt {ReceiptId}.", command.ReceiptId);
                idemRecord.Status = "Failed";
                try { await _db.SaveChangesAsync(ct); } catch { /* best effort */ }
                return ReceiptApprovalResult.Failure($"An error occurred during approval: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Recomputes CachedOutstandingAmount from the immutable SUM of ReceivableTransactions.
    /// This is the single source of truth for outstanding balance.
    /// </summary>
    public async Task RecalculateLedgerBalanceAsync(long receivableId, CancellationToken ct = default)
    {
        var receivable = await _db.CreditReceivables
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.Id == receivableId, ct);

        if (receivable == null) return;

        var sum = receivable.Transactions.Sum(t => t.Amount);
        receivable.CachedOutstandingAmount = Math.Max(0m, sum);

        if (receivable.CachedOutstandingAmount <= 0)
        {
            receivable.Status = ReceivableStatus.Closed;
            receivable.PaymentStatus = ReceivablePaymentStatus.Approved;
            receivable.ClosedDate = DateTime.UtcNow;
        }
        else if (receivable.CachedOutstandingAmount < receivable.TotalAmount)
        {
            receivable.Status = ReceivableStatus.PartiallyPaid;
            receivable.PaymentStatus = ReceivablePaymentStatus.UnderReview;
        }
        else
        {
            receivable.Status = ReceivableStatus.Open;
            receivable.PaymentStatus = ReceivablePaymentStatus.Pending;
        }

        await _db.SaveChangesAsync(ct);
    }

    private static ReceiptApprovalData BuildData(Receipt receipt, int approvedCount, decimal totalReduced) => new(
        ReceiptId: receipt.Id,
        DocumentNumber: receipt.DocumentNumber,
        Status: receipt.Status,
        Amount: receipt.Amount,
        ReceivedPaymentsApproved: approvedCount,
        TotalOutstandingReduced: totalReduced
    );
}

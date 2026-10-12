using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TexasPortal.Application.Persistence;
using TexasPortal.Application.Sales.Receipts.Approval;
using TexasPortal.Domain.Enums;

namespace TexasPortal.Application.Sales.Receivables;

public interface IApprovalService
{
    Task ApprovePaymentAsync(long receiptPaymentId, long accountantUserId, CancellationToken ct = default);
    Task RejectPaymentAsync(long receiptPaymentId, long accountantUserId, string reason, CancellationToken ct = default);
}

/// <summary>
/// Handles direct receivable-payment approval from the receivables portal.
/// Approval is always routed through <see cref="ReceiptApprovalOrchestrator"/> to maintain
/// a single atomic pipeline — this service is a thin facade.
/// </summary>
public class ApprovalService : IApprovalService
{
    private readonly ISalesPortalDbContext _db;
    private readonly ReceiptApprovalOrchestrator _orchestrator;

    public ApprovalService(ISalesPortalDbContext db, ReceiptApprovalOrchestrator orchestrator)
    {
        _db = db;
        _orchestrator = orchestrator;
    }

    public async Task ApprovePaymentAsync(long receiptPaymentId, long accountantUserId, CancellationToken ct = default)
    {
        var payment = await _db.ReceivablePayments
            .Include(p => p.Receipt)
            .FirstOrDefaultAsync(p => p.Id == receiptPaymentId, ct);

        if (payment == null)
            throw new InvalidOperationException($"Receivable payment ID {receiptPaymentId} was not found.");

        if (payment.Status == ReceivablePaymentStatus.Approved)
            return; // Already approved — idempotent

        if (payment.Receipt == null)
            throw new InvalidOperationException($"Receivable payment ID {receiptPaymentId} has no linked receipt.");

        // Delegate to orchestrator — it handles the full atomic pipeline
        var result = await _orchestrator.HandleAsync(
            new ReceiptApprovalCommand(payment.ReceiptId, accountantUserId), ct);

        if (!result.IsSuccess && result.StatusCode != 200)
            throw new InvalidOperationException($"Approval failed [{result.StatusCode}]: {result.Message}");
    }

    public async Task RejectPaymentAsync(long receiptPaymentId, long accountantUserId, string reason, CancellationToken ct = default)
    {
        var payment = await _db.ReceivablePayments
            .Include(p => p.Receipt)
            .FirstOrDefaultAsync(p => p.Id == receiptPaymentId, ct);

        if (payment == null)
            throw new InvalidOperationException($"Receivable payment ID {receiptPaymentId} was not found.");

        if (payment.Status == ReceivablePaymentStatus.Rejected)
            return; // Already rejected — idempotent

        payment.Status = ReceivablePaymentStatus.Rejected;
        payment.ApprovedBy = accountantUserId;
        payment.ApprovedAt = DateTime.UtcNow;
        payment.Remarks = $"Rejected. Reason: {reason}";

        if (payment.Receipt != null)
        {
            payment.Receipt.Status = "rejected";
            payment.Receipt.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }
}

using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Persistence;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Application.Sales.Receipts.Approval;
using TexasPortal.Contracts.Sales.Receipts;

namespace TexasPortal.Application.Sales.Receipts.UpdateReceiptStatus;

/// <summary>
/// Handles PATCH /api/receipts/{id}/status.
/// Delegates all approval logic to <see cref="ReceiptApprovalOrchestrator"/> to ensure
/// a single, atomic, idempotent pipeline. No financial writes happen here directly.
/// </summary>
public sealed class UpdateReceiptStatusHandler
{
    private readonly ISalesPortalDbContext _db;
    private readonly ReceiptApprovalOrchestrator _orchestrator;
    private readonly ICurrentUser _currentUser;

    public UpdateReceiptStatusHandler(
        ISalesPortalDbContext db,
        ReceiptApprovalOrchestrator orchestrator,
        ICurrentUser currentUser)
    {
        _db = db;
        _orchestrator = orchestrator;
        _currentUser = currentUser;
    }

    public async Task<UpdateReceiptStatusResult> HandleAsync(
        UpdateReceiptStatusCommand command,
        CancellationToken ct = default)
    {
        var request = command.Request;
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Status) ||
            !request.Status.Equals("success", StringComparison.OrdinalIgnoreCase))
        {
            errors["status"] = ["Only transition to status 'success' is supported."];
            return UpdateReceiptStatusResult.ValidationError(errors);
        }

        // Verify the receipt exists and is owned by the current user
        var receipt = await _db.Receipts
            .Include(x => x.Head)
            .FirstOrDefaultAsync(
                r => r.Id == command.ReceiptId && r.CreatedBy == command.CurrentUserId && r.DeletedAt == null,
                ct);

        if (receipt == null)
            return UpdateReceiptStatusResult.NotFound();

        // Delegate to the single authoritative orchestrator
        var approvalResult = await _orchestrator.HandleAsync(
            new ReceiptApprovalCommand(command.ReceiptId, command.CurrentUserId), ct);

        if (approvalResult.IsSuccess)
        {
            // Reload for the response
            var updated = await _db.Receipts
                .Include(x => x.Head)
                .FirstOrDefaultAsync(r => r.Id == command.ReceiptId, ct);

            if (updated == null)
                return UpdateReceiptStatusResult.NotFound();

            var response = BuildResponse(updated);
            return UpdateReceiptStatusResult.Success(response);
        }

        // Map orchestrator failure codes back to handler result types
        return approvalResult.StatusCode switch
        {
            409 => UpdateReceiptStatusResult.Conflict(approvalResult.Message ?? "Conflict"),
            404 => UpdateReceiptStatusResult.NotFound(),
            422 => UpdateReceiptStatusResult.ValidationError(
                new Dictionary<string, string[]> { ["status"] = [approvalResult.Message ?? "Unprocessable"] }),
            _ => UpdateReceiptStatusResult.ServerError(approvalResult.Message ?? "An error occurred.")
        };
    }

    private static ReceiptDetailResponse BuildResponse(Domain.Entities.Receipt receipt) => new()
    {
        Id = receipt.Id,
        ReceiptId = receipt.ReceiptId,
        HeadName = receipt.Head != null ? (receipt.Head.FirstName + " " + receipt.Head.LastName).Trim() : null,
        ClientType = receipt.Head != null ? receipt.Head.Type : "customer",
        AgentName = receipt.Head != null && receipt.Head.Type == "agent"
            ? (receipt.Head.FirstName + " " + receipt.Head.LastName).Trim() : null,
        Service = receipt.Service,
        Amount = receipt.Amount,
        PaymentMode = receipt.PaymentMode,
        Status = receipt.Status,
        CreatedAt = receipt.CreatedAt ?? DateTime.UtcNow,
        Narration = receipt.Narration,
        HeadId = receipt.HeadId,
        BookingId = string.IsNullOrEmpty(receipt.BookingId) ? null : long.Parse(receipt.BookingId),
        BankId = receipt.BankId,
        BankName = receipt.BankName
    };
}

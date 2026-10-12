using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TexasPortal.Application.Persistence;
using TexasPortal.Contracts.Sales.Disputes;

using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Contracts.Notifications;

namespace TexasPortal.Application.Sales.Disputes.ResolveDispute;

public sealed class ResolveDisputeHandler
{
    private readonly ISalesPortalDbContext _db;
    private readonly IWalletNotificationService _walletNotificationService;
    private readonly IOutstandingNotificationService _outstandingNotificationService;
    private readonly TexasPortal.Application.Sales.Accounts.ICustomerAccountCommandService _customerAccountCommandService;

    public ResolveDisputeHandler(
        ISalesPortalDbContext db,
        IWalletNotificationService walletNotificationService,
        IOutstandingNotificationService outstandingNotificationService,
        TexasPortal.Application.Sales.Accounts.ICustomerAccountCommandService customerAccountCommandService)
    {
        _db = db;
        _walletNotificationService = walletNotificationService;
        _outstandingNotificationService = outstandingNotificationService;
        _customerAccountCommandService = customerAccountCommandService;
    }

    public async Task<ResolveDisputeResult> HandleAsync(
        ResolveDisputeCommand command,
        CancellationToken ct = default)
    {
        var request = command.Request;
        var errors = new Dictionary<string, string[]>();

        // 1. Validate action
        var action = (request.Action ?? "").Trim().ToLowerInvariant();
        if (action != "accepted" && action != "rejected")
        {
            errors["action"] = ["Action must be 'accepted' or 'rejected'."];
            return ResolveDisputeResult.ValidationError(errors);
        }

        // 2. Fetch dispute
        var dispute = await _db.Disputes
            .Include(d => d.Head)
            .FirstOrDefaultAsync(d => d.Id == command.DisputeId, ct);

        if (dispute is null)
            return ResolveDisputeResult.NotFound();

        if (!string.Equals(dispute.Status, "open", StringComparison.OrdinalIgnoreCase))
            return ResolveDisputeResult.AlreadyResolved();

        var newStatus = action == "accepted" ? "resolved" : "rejected";
        dispute.Status = newStatus;
        dispute.Resolution = request.Resolution;
        dispute.ResolvedBy = request.ResolvedBy;
        dispute.ResolvedAt = DateTime.UtcNow;
        dispute.UpdatedAt = DateTime.UtcNow;

        // 3. Process Refund Financial Rule (Customer Account Reverse Waterfall)
        if (action == "accepted" && dispute.IsRefund)
        {
            var approved = request.ApprovedRefundAmount ?? dispute.RefundAmount ?? 0m;

            if (approved > 0 && dispute.HeadId.HasValue)
            {
                dispute.ApprovedRefundAmount = approved;

                // Process refund via Customer Account Command Service (Reverse Waterfall)
                await _customerAccountCommandService.ProcessRefundAsync(
                    dispute.HeadId.Value,
                    approved,
                    dispute.DocumentNumber ?? dispute.Id.ToString(),
                    dispute.UserId,
                    $"Dispute refund for #{dispute.DocumentNumber ?? dispute.Id.ToString()}",
                    ct: ct);

                dispute.RefundProcessed = true;
                dispute.RefundStatus = "processed";
                dispute.RefundDate = DateTime.UtcNow;

                await _walletNotificationService.PublishWalletChangedAsync(
                    referenceType: "Dispute",
                    referenceId: dispute.Id.ToString(),
                    ownerId: dispute.HeadId.Value,
                    ownerType: "customer",
                    cancellationToken: ct);

                await _outstandingNotificationService.PublishOutstandingChangedAsync(
                    referenceType: "Dispute",
                    referenceId: dispute.Id.ToString(),
                    ownerId: dispute.HeadId.Value,
                    ownerType: "customer",
                    cancellationToken: ct);
            }
        }

        await _db.SaveChangesAsync(ct);

        var headName = dispute.Head is not null 
            ? string.Join(" ", new[] { dispute.Head.FirstName, dispute.Head.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)))
            : null;

        var response = new DisputeResponse(
            dispute.Id,
            dispute.DocumentNumber,
            dispute.UserId,
            dispute.HeadId,
            headName,
            dispute.DisputableId,
            dispute.DisputableType,
            dispute.Subject,
            dispute.Description,
            dispute.Status,
            dispute.Resolution,
            dispute.ResolutionNote,
            dispute.ResolvedBy,
            dispute.ResolvedAt,
            dispute.IsRefund,
            dispute.RefundAmount,
            dispute.ApprovedRefundAmount,
            dispute.RefundProcessed,
            dispute.RefundStatus,
            dispute.RefundDate,
            dispute.CreatedAt,
            Iban: dispute.Iban,
            Name: dispute.Name);

        return ResolveDisputeResult.Success(response);
    }
}

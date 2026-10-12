using System;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Persistence;
using TexasPortal.Contracts.Sales.Disputes;
using TexasPortal.Domain.Entities;

namespace TexasPortal.Application.Sales.Disputes.CreateDispute;

public sealed class CreateDisputeHandler
{
    private readonly ISalesPortalDbContext _db;
    private readonly TexasPortal.Domain.Common.IDocumentNumberGenerator _documentNumberGenerator;

    public CreateDisputeHandler(
        ISalesPortalDbContext db,
        TexasPortal.Domain.Common.IDocumentNumberGenerator documentNumberGenerator)
    {
        _db = db;
        _documentNumberGenerator = documentNumberGenerator;
    }

    public async Task<DisputeResponse> HandleAsync(
        long userId,
        CreateDisputeRequest request,
        CancellationToken ct = default)
    {
        var docNo = await _documentNumberGenerator.GenerateDocumentNumberAsync("TXSDISP", "Seq_DisputeDocumentNo", ct);
        var dispute = new Dispute
        {
            DocumentNumber = docNo,
            UserId = userId,
            HeadId = request.HeadId,
            DisputableId = request.DisputableId,
            DisputableType = request.DisputableType?.Trim() ?? string.Empty,
            Subject = request.Subject?.Trim() ?? string.Empty,
            Description = request.Description?.Trim() ?? string.Empty,
            Iban = request.Iban?.Trim(),
            Name = request.Name?.Trim(),
            Status = "open",
            IsRefund = request.IsRefund,
            RefundAmount = request.IsRefund ? request.RefundAmount : null,
            RefundProcessed = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Disputes.Add(dispute);
        await _db.SaveChangesAsync(ct);

        return new DisputeResponse(
            dispute.Id,
            dispute.DocumentNumber,
            dispute.UserId,
            dispute.HeadId,
            null, // HeadName — Head not loaded; client refreshes list
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
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Application.Persistence;
using TexasPortal.Contracts.Notifications;
using TexasPortal.Contracts.Sales.Receivables;
using TexasPortal.Domain.Entities;
using TexasPortal.Domain.Enums;

namespace TexasPortal.Application.Sales.Receivables;

public interface IReceiptService
{
    Task<Receipt> CreateReceiptWithAllocationsAsync(CreateReceiptWithAllocationsDto dto, long salesmanId, CancellationToken ct = default);
    Task<CollectionBatch> CreateCollectionBatchAsync(long salesmanId, List<long> receiptIds, CancellationToken ct = default);
}

public class ReceiptService : IReceiptService
{
    private readonly ISalesPortalDbContext _db;
    private readonly TexasPortal.Domain.Common.IDocumentNumberGenerator _documentNumberGenerator;
    private readonly IReceiptNotificationService _receiptNotificationService;

    public ReceiptService(
        ISalesPortalDbContext db,
        TexasPortal.Domain.Common.IDocumentNumberGenerator documentNumberGenerator,
        IReceiptNotificationService receiptNotificationService)
    {
        _db = db;
        _documentNumberGenerator = documentNumberGenerator;
        _receiptNotificationService = receiptNotificationService;
    }

    public async Task<Receipt> CreateReceiptWithAllocationsAsync(CreateReceiptWithAllocationsDto dto, long salesmanId, CancellationToken ct = default)
    {
        if (dto.Amount <= 0)
        {
            throw new ArgumentException("Receipt amount must be greater than zero.");
        }

        var rand = new Random();
        var receiptId = "TX" + rand.Next(100000, 1000000);

        Enum.TryParse<HeadType>(dto.ClientType, true, out var hType);
        var isAgent = string.Equals(dto.ClientType, "agent", StringComparison.OrdinalIgnoreCase);

        var docNo = await _documentNumberGenerator.GenerateDocumentNumberAsync("TXSRCPT", "Seq_ReceiptDocumentNo", ct);
        long? effectiveSalesmanId = null;

        if (salesmanId > 0 && await _db.Users.AnyAsync(u => u.Id == salesmanId, ct))
        {
            effectiveSalesmanId = salesmanId;
        }
        else
        {
            var fallbackUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(ct);
            if (fallbackUser != null)
            {
                effectiveSalesmanId = fallbackUser.Id;
            }
        }

        var safeNarration = (dto.Narration ?? "").Trim();
        var safePaymentMode = (dto.PaymentMode ?? "cash").ToLowerInvariant();

        var receipt = new Receipt
        {
            DocumentNumber = docNo,
            CreatedBy = effectiveSalesmanId,
            ReceiptId = receiptId,
            HeadId = !isAgent ? dto.HeadId : null,
            AgentId = isAgent ? dto.HeadId : null,
            Narration = string.IsNullOrWhiteSpace(safeNarration) ? "Balance Deposit" : safeNarration,
            Service = "deposit",
            Amount = dto.Amount,
            PaymentMode = safePaymentMode,
            BankId = safePaymentMode == "online" ? dto.BankId : null,
            BankName = safePaymentMode == "online" ? dto.BankName?.Trim() : null,
            Status = "pending",
            PaymentProof = dto.PaymentProof,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Receipts.Add(receipt);
        await _db.SaveChangesAsync(ct);

        await _receiptNotificationService.PublishReceiptChangedAsync(
            referenceType: ReferenceTypes.Receipt,
            referenceId: receipt.Id.ToString(),
            headId: receipt.HeadId,
            agentId: receipt.AgentId,
            includeApprovalQueue: true,
            includeGlobalList: true,
            cancellationToken: ct);

        return receipt;
    }

    public async Task<CollectionBatch> CreateCollectionBatchAsync(long salesmanId, List<long> receiptIds, CancellationToken ct = default)
    {
        var receipts = await _db.Receipts.Where(r => receiptIds.Contains(r.Id)).ToListAsync(ct);
        var totalAmount = receipts.Sum(r => r.Amount);

        var rand = new Random();
        var batch = new CollectionBatch
        {
            BatchNumber = "BATCH" + rand.Next(10000, 99999),
            SalesmanId = salesmanId,
            TotalAmount = totalAmount,
            ReceiptCount = receipts.Count,
            Status = ReceivablePaymentStatus.Submitted,
            CreatedAt = DateTime.UtcNow
        };

        _db.CollectionBatches.Add(batch);
        await _db.SaveChangesAsync(ct);

        return batch;
    }
}

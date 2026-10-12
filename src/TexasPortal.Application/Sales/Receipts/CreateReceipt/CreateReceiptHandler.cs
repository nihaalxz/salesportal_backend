using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Persistence;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Contracts.Notifications;
using TexasPortal.Contracts.Sales.Receipts;
using TexasPortal.Domain.Entities;

namespace TexasPortal.Application.Sales.Receipts.CreateReceipt;

public sealed class CreateReceiptHandler
{
    private readonly ISalesPortalDbContext _db;
    private readonly ILedgerNotificationService _ledgerNotificationService;
    private readonly IReceiptNotificationService _receiptNotificationService;
    private readonly TexasPortal.Domain.Common.IDocumentNumberGenerator _documentNumberGenerator;

    public CreateReceiptHandler(
        ISalesPortalDbContext db,
        ILedgerNotificationService ledgerNotificationService,
        IReceiptNotificationService receiptNotificationService,
        TexasPortal.Domain.Common.IDocumentNumberGenerator documentNumberGenerator)
    {
        _db = db;
        _ledgerNotificationService = ledgerNotificationService;
        _receiptNotificationService = receiptNotificationService;
        _documentNumberGenerator = documentNumberGenerator;
    }

    public async Task<CreateReceiptResult> HandleAsync(
        CreateReceiptCommand command,
        CancellationToken ct = default)
    {
        var request = command.Request;
        var errors = new Dictionary<string, string[]>();

        // ── 1. Validations ────────────────────────────────────────────────────
        if (request.Amount <= 0)
            errors["amount"] = ["Amount must be greater than zero."];

        var allowedServices = new[] { "flights", "hotels", "visas", "vfs", "holidays", "transportations", "others", "wallet_deposit", "deposit", "credit_settlement" };
        if (!allowedServices.Contains(request.Service.ToLowerInvariant()))
            errors["service"] = [$"Service must be one of: {string.Join(", ", allowedServices)}."];

        var allowedModes = new[] { "cash", "online", "wallet" };
        if (!allowedModes.Contains(request.PaymentMode.ToLowerInvariant()))
            errors["paymentMode"] = [$"Payment mode must be one of: {string.Join(", ", allowedModes)}."];

        if (string.IsNullOrWhiteSpace(request.ClientType) || 
            (!request.ClientType.Equals("customer", StringComparison.OrdinalIgnoreCase) && 
             !request.ClientType.Equals("agent", StringComparison.OrdinalIgnoreCase)))
        {
            errors["clientType"] = ["Client type must be 'customer' or 'agent'."];
        }

        if (request.ClientType.Equals("customer", StringComparison.OrdinalIgnoreCase) && !request.HeadId.HasValue)
            errors["headId"] = ["Customer Head is required."];

        if (errors.Count > 0)
            return CreateReceiptResult.ValidationError(errors);

        Head? head = null;
        var isAgent = request.ClientType.Equals("agent", StringComparison.OrdinalIgnoreCase);

        if (request.HeadId.HasValue && !isAgent)
        {
            head = await _db.Heads.FirstOrDefaultAsync(h => h.Id == request.HeadId.Value && h.CreatedBy == command.CurrentUserId && h.DeletedAt == null, ct);
            if (head == null)
            {
                errors["headId"] = ["Customer Head was not found or is not owned by you."];
                return CreateReceiptResult.ValidationError(errors);
            }
        }

        // Wallet Balance Check
        if (request.PaymentMode.Equals("wallet", StringComparison.OrdinalIgnoreCase))
        {
            if (isAgent)
            {
                errors["paymentMode"] = ["Wallet payment is not supported for agents in this portal."];
                return CreateReceiptResult.ValidationError(errors);
            }

            if (head == null)
            {
                errors["paymentMode"] = ["Wallet payment requires a customer Head."];
                return CreateReceiptResult.ValidationError(errors);
            }
        }

        // ── 2. Unique Invoice ID Generation ───────────────────────────────────
        var rand = new Random();
        var invoiceNum = rand.Next(100000, 1000000); // 6-digit number
        var receiptId = "TX" + invoiceNum;

        // ── 3. Initialize Receipt Entity ──────────────────────────────────────
        var docNo = await _documentNumberGenerator.GenerateDocumentNumberAsync("TXSRCPT", "Seq_ReceiptDocumentNo", ct);
        var receipt = new Receipt
        {
            DocumentNumber = docNo,
            CreatedBy = command.CurrentUserId,
            ReceiptId = receiptId,
            HeadId = isAgent ? null : request.HeadId,
            AgentId = isAgent ? request.HeadId : null,
            BookingId = request.BookingId?.ToString(),
            Narration = request.Narration.Trim(),
            Service = request.Service,
            Amount = request.Amount,
            PaymentMode = request.PaymentMode.ToLowerInvariant(),
            BankId = request.BankId,
            BankName = request.BankName?.Trim(),
            Status = request.PaymentMode.Equals("wallet", StringComparison.OrdinalIgnoreCase) ? "success" : "pending",
            PaymentProof = request.PaymentProof,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Receipts.Add(receipt);

        // ── 4. Apply immediate wallet debit and allocation ─────────────────────
        if (request.PaymentMode.Equals("wallet", StringComparison.OrdinalIgnoreCase) && head != null)
        {
            // Perform specific/FIFO allocation
            await ReceiptAllocationHelper.AllocateReceiptFundsAsync(receipt, request.Amount, _db);

            await _ledgerNotificationService.PublishLedgerChangedAsync(
                referenceType: ReferenceTypes.Receipt,
                referenceId: receipt.Id.ToString(),
                headId: receipt.HeadId,
                agentId: receipt.AgentId,
                cancellationToken: ct);
        }

        await _receiptNotificationService.PublishReceiptChangedAsync(
            referenceType: ReferenceTypes.Receipt,
            referenceId: receipt.Id.ToString(),
            headId: receipt.HeadId,
            agentId: receipt.AgentId,
            includeApprovalQueue: true,
            includeGlobalList: true,
            cancellationToken: ct);

        await _db.SaveChangesAsync(ct);

        await TexasPortal.Application.Sales.Notifications.NotificationHelper.SendHeadEmailNotificationAsync(
            _db, receipt.HeadId, receipt.AgentId, "receipt", receipt.Id, receipt.Status, receipt.Amount, receipt.Narration, ct, docNo, isCreation: true);

        // ── 5. Build Response ─────────────────────────────────────────────────
        var response = new ReceiptDetailResponse
        {
            Id = receipt.Id,
            ReceiptId = receipt.ReceiptId,
            HeadName = head != null ? (head.FirstName + " " + head.LastName).Trim() : null,
            ClientType = request.ClientType.ToLowerInvariant(),
            AgentName = isAgent ? request.AgentName : (head != null && head.Type == "agent" ? (head.FirstName + " " + head.LastName).Trim() : null),
            Service = receipt.Service,
            Amount = receipt.Amount,
            PaymentMode = receipt.PaymentMode,
            Status = receipt.Status,
            CreatedAt = receipt.CreatedAt ?? DateTime.UtcNow,
            Narration = receipt.Narration,
            HeadId = receipt.HeadId,
            BookingId = request.BookingId,
            BankId = receipt.BankId,
            BankName = receipt.BankName,
            PaymentProof = receipt.PaymentProof
        };

        return CreateReceiptResult.Success(response);
    }
}

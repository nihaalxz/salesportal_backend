using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Application.Persistence;
using TexasPortal.Application.Sales.Queries;
using TexasPortal.Contracts.Sales.Bookings;
using TexasPortal.Domain.Entities;

using TexasPortal.Contracts.Notifications;

using TexasPortal.Application.Sales.Receivables;
using TexasPortal.Contracts.Sales.Receivables;

namespace TexasPortal.Application.Sales.Bookings.OtherServices.CreateOtherServiceBooking;

public sealed class CreateOtherServiceBookingHandler
{
    private readonly ISalesPortalDbContext _db;
    private readonly ITransactionalExecutor _executor;
    private readonly IAgentsApiClient _agentsApiClient;
    private readonly ISalesEntriesNotificationService _salesEntriesNotificationService;
    private readonly IOutstandingNotificationService _outstandingNotificationService;
    private readonly IOperationalQueueNotificationService _operationalQueueNotificationService;
    private readonly TexasPortal.Domain.Common.IDocumentNumberGenerator _documentNumberGenerator;
    private readonly IReceivableService _receivableService;
    private readonly TexasPortal.Application.Sales.Accounts.ICustomerAccountCommandService _customerAccountCommandService;
    private readonly TexasPortal.Application.Sales.Profit.ISalesmanProfitService _profitService;

    public CreateOtherServiceBookingHandler(
        ISalesPortalDbContext db,
        ITransactionalExecutor executor,
        IAgentsApiClient agentsApiClient,
        ISalesEntriesNotificationService salesEntriesNotificationService,
        IOutstandingNotificationService outstandingNotificationService,
        IOperationalQueueNotificationService operationalQueueNotificationService,
        TexasPortal.Domain.Common.IDocumentNumberGenerator documentNumberGenerator,
        IReceivableService receivableService,
        TexasPortal.Application.Sales.Accounts.ICustomerAccountCommandService customerAccountCommandService,
        TexasPortal.Application.Sales.Profit.ISalesmanProfitService profitService)
    {
        _db = db;
        _executor = executor;
        _agentsApiClient = agentsApiClient;
        _salesEntriesNotificationService = salesEntriesNotificationService;
        _outstandingNotificationService = outstandingNotificationService;
        _operationalQueueNotificationService = operationalQueueNotificationService;
        _documentNumberGenerator = documentNumberGenerator;
        _receivableService = receivableService;
        _customerAccountCommandService = customerAccountCommandService;
        _profitService = profitService;
    }

    public async Task<CreateOtherServiceBookingResult> HandleAsync(CreateOtherServiceBookingCommand command, CancellationToken ct = default)
    {
        var request = command.Request;

        // 1. Request Validation
        var errors = new Dictionary<string, string[]>();

        // Required & String length checks
        var applicantsList = request.Applicants != null && request.Applicants.Count > 0
            ? request.Applicants
            : new List<OtherServiceApplicantRequest> { new OtherServiceApplicantRequest(request.Prefix, request.Name ?? "", request.ExpectedSupplierCost, request.Amount, request.IsInhouse, request.SupplierId) };

        if (applicantsList.Count == 0 || string.IsNullOrWhiteSpace(applicantsList[0].Name))
            errors.Add("name", ["At least one applicant name is required."]);
        else
        {
            for (int i = 0; i < applicantsList.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(applicantsList[i].Name))
                    errors.Add($"applicants_{i}_name", [$"Applicant {i + 1} name is required."]);
            }
        }

        if (string.IsNullOrWhiteSpace(request.ServiceName))
            errors.Add("serviceName", ["ServiceName is required."]);
        else if (request.ServiceName.Length > 255)
            errors.Add("serviceName", ["ServiceName must not exceed 255 characters."]);

        if (request.Prefix != null && request.Prefix.Length > 255)
            errors.Add("prefix", ["Prefix must not exceed 255 characters."]);

        if (request.Agent != null && request.Agent.Length > 255)
            errors.Add("agent", ["Agent name must not exceed 255 characters."]);

        if (request.BankName != null && request.BankName.Length > 255)
            errors.Add("bankName", ["BankName must not exceed 255 characters."]);

        if (request.PaymentProof != null && request.PaymentProof.Length > 255)
            errors.Add("paymentProof", ["PaymentProof path must not exceed 255 characters."]);

        if (request.Amount < 0)
            errors.Add("amount", ["Amount must be greater than or equal to zero."]);

        if (request.ExpectedSupplierCost < 0)
            errors.Add("expectedSupplierCost", ["ExpectedSupplierCost must be greater than or equal to zero."]);

        // Client Type validations
        var clientTypeLower = request.ClientType?.ToLower() ?? "";
        long? agentId = clientTypeLower == "agent" ? request.AgentId : null;
        decimal payableAmount = request.Amount;
        var allowedClientTypes = new[] { "customer", "agent" };
        if (!allowedClientTypes.Contains(clientTypeLower))
        {
            errors.Add("clientType", ["ClientType must be customer or agent."]);
        }
        else
        {
            if (clientTypeLower == "customer" && !request.HeadId.HasValue)
                errors.Add("headId", ["HeadId is required when ClientType is customer."]);

            if (clientTypeLower == "agent")
            {
                if (string.IsNullOrWhiteSpace(request.Agent))
                    errors.Add("agent", ["Agent name is required when ClientType is agent."]);
                if (!request.AgentId.HasValue)
                    errors.Add("agentId", ["AgentId is required when ClientType is agent."]);
            }
        }

        // Supplier validations (for non-inhouse bookings)
        if (!request.IsInhouse)
        {
            if (!request.SupplierId.HasValue)
                errors.Add("supplierId", ["SupplierId is required for external (non-inhouse) bookings."]);
            if (request.ExpectedSupplierCost <= 0m && request.Amount > 0m)
            {
                // Warn or validate? ExpectedSupplierCost is required if not in-house, must be non-negative
            }
        }

        // Payment Method validations
        var methodLower = request.PaymentMethod?.ToLower() ?? "";
        var allowedMethods = new[] { "credit", "wallet", "cash", "online" };
        if (!allowedMethods.Contains(methodLower))
        {
            errors.Add("paymentMethod", ["PaymentMethod must be credit, wallet, cash, or online."]);
        }
        else
        {
            if (methodLower == "online" && !request.BankId.HasValue)
                errors.Add("bankId", ["BankId is required when PaymentMethod is online."]);
        }

        // Status / ProcessingStatus validations
        var allowedStatuses = new[] { "pending", "confirmed", "success", "failed", "cancelled", "collection", "collected" };
        if (string.IsNullOrWhiteSpace(request.Status) || !allowedStatuses.Contains(request.Status.ToLower()))
        {
            errors.Add("status", [$"Status must be one of: {string.Join(", ", allowedStatuses)}."]);
        }

        if (errors.Count > 0)
            return CreateOtherServiceBookingResult.ValidationError(errors);

        // 2. Compute request payload hash for idempotency integrity check
        var jsonPayload = JsonSerializer.Serialize(request);
        var requestHash = GetSha256Hash(command.IdempotencyKey + jsonPayload);

        // 3. Database-backed Idempotency Check-and-Insert
        IdempotencyRecord? idempRecord = null;
        try
        {
            idempRecord = await _db.IdempotencyRecords
                .FirstOrDefaultAsync(r => r.IdempotencyKey == command.IdempotencyKey, ct);

            if (idempRecord != null)
            {
                if (idempRecord.Status == "InProgress")
                {
                    return CreateOtherServiceBookingResult.IdempotencyConflict("A duplicate request is already in progress.");
                }

                if (idempRecord.Status == "Completed")
                {
                    if (idempRecord.RequestHash != requestHash)
                    {
                        return CreateOtherServiceBookingResult.IdempotencyConflict("Idempotency key reuse with different request body is not allowed.");
                    }

                    // Replay cached response
                    if (idempRecord.ResponsePayload != null)
                    {
                        var cachedResponse = JsonSerializer.Deserialize<OtherServiceBookingResponse>(idempRecord.ResponsePayload);
                        if (cachedResponse != null)
                        {
                            return CreateOtherServiceBookingResult.Success(cachedResponse);
                        }
                    }
                }
            }
            else
            {
                // Register InProgress
                idempRecord = new IdempotencyRecord
                {
                    IdempotencyKey = command.IdempotencyKey,
                    RequestHash = requestHash,
                    Status = "InProgress",
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(24)
                };

                _db.IdempotencyRecords.Add(idempRecord);
                await _db.SaveChangesAsync(ct);
            }
        }
        catch (DbUpdateException)
        {
            // Unique index key constraint hit due to concurrent inserts
            return CreateOtherServiceBookingResult.IdempotencyConflict("A duplicate request is already in progress.");
        }

        // 4. Execute Booking Creation inside retryable Database Transaction
        try
        {
            var result = await _executor.ExecuteAsync(async transactionCt =>
            {
                // A. Resolve target Head
                long? targetHeadId = clientTypeLower == "customer" ? request.HeadId : null;
                Head? head = null;

                if (clientTypeLower == "customer")
                {
                    if (targetHeadId == null)
                        return CreateOtherServiceBookingResult.HeadNotFound();

                    head = await _db.Heads
                        .FirstOrDefaultAsync(h => h.Id == targetHeadId && h.DeletedAt == null, transactionCt);

                    if (head == null)
                        return CreateOtherServiceBookingResult.HeadNotFound();
                }

                // B. Enforce Wallet limit if payment method is wallet
                var paymentMethod = (request.PaymentMethod ?? "").ToLower();

                // C. Financial aggregations
                var advancePaid = (paymentMethod == "wallet" || paymentMethod == "cash" || paymentMethod == "online") 
                    ? payableAmount 
                    : 0m;

                var isInHouse = request.SupplierId.HasValue ? false : request.IsInhouse;
                var expectedSupplierCost = isInHouse ? 0m : request.ExpectedSupplierCost;
                var profit = isInHouse ? 0m : (payableAmount - expectedSupplierCost);

                string? careOfName = null;
                if (clientTypeLower == "customer" && head != null)
                {
                    careOfName = head.FirstName + " " + head.LastName;
                }

                // Wallet deduction handled via CustomerAccountCommandService for Credit/Wallet bookings

                // E. Insert Booking record
                string? supplierName = isInHouse ? "Inhouse (Pending assignment)" : null;
                if (!isInHouse && request.SupplierId.HasValue)
                {
                    try
                    {
                        var suppliers = await _agentsApiClient.GetSuppliersAsync(transactionCt);
                        var supplier = suppliers.FirstOrDefault(s => s.Id == request.SupplierId.Value);
                        supplierName = supplier != null ? supplier.Name : ("Supplier " + request.SupplierId.Value);
                    }
                    catch
                    {
                        supplierName = "Supplier " + request.SupplierId.Value;
                    }
                }

                var docNo = await _documentNumberGenerator.GenerateDocumentNumberAsync("TXSOTHR", "Seq_OtherServiceDocumentNo", transactionCt);
                var booking = new OtherService
                {
                    DocumentNumber = docNo,
                    CreatedBy = command.CurrentUserId,
                    HeadId = targetHeadId,
                    ClientType = request.ClientType,
                    Prefix = request.Prefix,
                    Name = request.Name,
                    CareOf = careOfName,
                    Agent = request.Agent,
                    AgentId = agentId,
                    IsInhouse = isInHouse,
                    SupplierId = isInHouse ? null : request.SupplierId,
                    SupplierName = supplierName,
                    ExpectedSupplierCost = expectedSupplierCost,
                    PayableAmount = payableAmount,
                    Profit = profit,
                    ServiceName = request.ServiceName,
                    Narration = request.Narration,
                    PaymentMethod = request.PaymentMethod ?? "",
                    PaymentProof = request.PaymentProof,
                    BankId = (request.PaymentMethod ?? "").ToLower() == "online" ? request.BankId : null,
                    BankName = (request.PaymentMethod ?? "").ToLower() == "online" ? request.BankName : null,
                    PaymentStatus = "pending",
                    ProcessingStatus = request.Status ?? "pending",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.OtherServices.Add(booking);
                await _db.SaveChangesAsync(transactionCt);

                var perApplicantAmount = applicantsList.Count > 0 ? payableAmount / applicantsList.Count : payableAmount;
                var perApplicantCost = applicantsList.Count > 0 ? (isInHouse ? 0m : expectedSupplierCost) / applicantsList.Count : expectedSupplierCost;

                foreach (var aReq in applicantsList)
                {
                    var aIsInhouse = isInHouse;
                    var aCost = aIsInhouse ? 0m : perApplicantCost;
                    var aProfit = aIsInhouse ? 0m : (perApplicantAmount - aCost);

                    var applicant = new OtherServiceApplicant
                    {
                        OtherServiceId = booking.Id,
                        Prefix = aReq.Prefix ?? request.Prefix,
                        Name = aReq.Name.Trim(),
                        PayableAmount = perApplicantAmount,
                        SupplierCost = aCost,
                        ExpectedSupplierCost = aCost,
                        Profit = aProfit,
                        IsInhouse = aIsInhouse,
                        SupplierId = aIsInhouse ? null : request.SupplierId,
                        SupplierName = supplierName,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.OtherServiceApplicants.Add(applicant);
                    await _db.SaveChangesAsync(transactionCt);
                }

                // Process Credit / Credit Account payment method via ICustomerAccountCommandService
                if (paymentMethod is "credit" or "wallet")
                {
                    var creditRequest = new TexasPortal.Contracts.Sales.Accounts.CreditBookingRequest(
                        BookingId: booking.Id.ToString(),
                        BookingNumber: docNo,
                        BookingModule: "OtherService",
                        HeadId: (targetHeadId ?? agentId) ?? 0,
                        HeadType: clientTypeLower == "agent" ? "Agent" : "Customer",
                        SalesmanId: command.CurrentUserId,
                        TotalAmount: payableAmount,
                        CreditTerm: "Days30",
                        AutoApplyBalance: true,
                        IdempotencyKey: command.IdempotencyKey,
                        Remarks: $"Credit Sale for Other Service Booking #{docNo}"
                    );

                    var creditRes = await _customerAccountCommandService.AuthorizeAndProcessCreditBookingAsync(creditRequest, transactionCt);
                    booking.PaymentStatus = creditRes.PaymentStatus;
                    _db.Entry(booking).State = EntityState.Modified;
                }
                else if (clientTypeLower == "customer" && targetHeadId.HasValue)
                {
                    // Non-credit cash/online ledger entry
                    var ledger = new HeadLedger
                    {
                        HeadId = targetHeadId.Value,
                        ServiceType = "Other",
                        ServiceId = booking.Id,
                        Debit = payableAmount,
                        Credit = 0m,
                        Description = "Other Service: " + request.ServiceName + " - " + request.Name,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.HeadLedgers.Add(ledger);
                    await _db.SaveChangesAsync(transactionCt);
                }

                // G. Salesman profit record creation
                await _profitService.CreateEstimatedProfitRecordAsync(
                    salesmanId: command.CurrentUserId,
                    bookingType: "Other",
                    bookingId: booking.Id,
                    documentNumber: docNo,
                    payableAmount: payableAmount,
                    estimatedSupplierCost: expectedSupplierCost,
                    isInhouse: isInHouse,
                    bookingCreatedAt: booking.CreatedAt ?? DateTime.UtcNow,
                    ct: transactionCt);

                // H. Update Cash-In-Hand
                if (paymentMethod == "cash")
                {
                    var cashAffected = await _db.CashInHands
                        .Where(c => c.UserId == command.CurrentUserId)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(c => c.Amount, c => c.Amount + payableAmount)
                            .SetProperty(c => c.UpdatedAt, DateTime.UtcNow),
                            transactionCt);

                    if (cashAffected == 0)
                    {
                        var newCash = new CashInHand
                        {
                            UserId = command.CurrentUserId,
                            Amount = payableAmount,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _db.CashInHands.Add(newCash);
                        await _db.SaveChangesAsync(transactionCt);
                    }
                }

                // Construct Response
                var response = new OtherServiceBookingResponse(
                    booking.Id,
                    booking.CreatedBy,
                    booking.HeadId,
                    booking.ClientType ?? "customer",
                    booking.Name ?? "",
                    booking.ServiceName ?? "",
                    booking.PayableAmount,
                    booking.ExpectedSupplierCost,
                    booking.Profit,
                    booking.PaymentMethod ?? "",
                    booking.PaymentProof,
                    booking.PaymentStatus ?? "pending",
                    booking.ProcessingStatus ?? "pending",
                    booking.CreatedAt ?? DateTime.UtcNow,
                    booking.DocumentNumber
                );

                // Complete Idempotency record atomically
                idempRecord.Status = "Completed";
                idempRecord.ResponseCode = 201;
                idempRecord.ResponsePayload = JsonSerializer.Serialize(response);
                _db.Entry(idempRecord).State = EntityState.Modified;

                await _salesEntriesNotificationService.PublishBookingCreatedAsync(
                    referenceType: ReferenceTypes.OtherService,
                    bookingId: booking.Id,
                    headId: targetHeadId,
                    agentId: agentId,
                    cancellationToken: transactionCt);

                if (targetHeadId.HasValue)
                {
                    await _outstandingNotificationService.PublishOutstandingChangedAsync(
                        referenceType: ReferenceTypes.OtherService,
                        referenceId: booking.Id.ToString(),
                        ownerId: targetHeadId.Value,
                        ownerType: "customer",
                        cancellationToken: transactionCt);
                }
                else if (agentId.HasValue)
                {
                    await _outstandingNotificationService.PublishOutstandingChangedAsync(
                        referenceType: ReferenceTypes.OtherService,
                        referenceId: booking.Id.ToString(),
                        ownerId: agentId.Value,
                        ownerType: "agent",
                        cancellationToken: transactionCt);
                }

                await _operationalQueueNotificationService.PublishOperationalQueueChangedAsync(
                    referenceType: ReferenceTypes.OtherService,
                    referenceId: booking.Id.ToString(),
                    queueType: "otherservice",
                    includeOperationsRole: true,
                    cancellationToken: transactionCt);

                await _db.SaveChangesAsync(transactionCt);

                await TexasPortal.Application.Sales.Notifications.NotificationHelper.SendHeadEmailNotificationAsync(
                    _db, targetHeadId, agentId, "other_service", booking.Id, "submitted", payableAmount,
                    $"{request.Name} - {request.ServiceName}", transactionCt, docNo, isCreation: true);

                return CreateOtherServiceBookingResult.Success(response);

            }, ct);

            return result;
        }
        catch (Exception)
        {
            // Mark idempotency record as failed if exception thrown during transaction execution
            try
            {
                idempRecord.Status = "Failed";
                _db.Entry(idempRecord).State = EntityState.Modified;
                await _db.SaveChangesAsync(ct);
            }
            catch
            {
                // Suppress secondary exceptions during compensation log
            }
            throw;
        }
    }

    private static string GetSha256Hash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLower();
    }
}

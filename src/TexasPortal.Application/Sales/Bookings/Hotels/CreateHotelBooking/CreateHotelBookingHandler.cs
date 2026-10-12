using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Application.Persistence;
using TexasPortal.Application.Sales.Queries;
using TexasPortal.Application.Sales.Receivables;
using TexasPortal.Contracts.Sales.Bookings;
using TexasPortal.Contracts.Sales.Receivables;
using TexasPortal.Domain.Entities;
using TexasPortal.Contracts.Notifications;

namespace TexasPortal.Application.Sales.Bookings.Hotels.CreateHotelBooking;

/// <summary>
/// Creates a Hotel Booking with full financial integrity:
/// idempotency, transaction, wallet concurrency, ledger, cash, and profit effects.
/// Financial differences from OtherService:
///   - Ownership is UserId (IUserOwned), not CreatedBy (ICreatorOwned).
///   - Hotel-specific fields: GuestName, Country, Days, CheckInDate, OrderStatus.
///   - Bank fields for online payment: BankId, BankName, PaymentReference.
/// </summary>
public sealed class CreateHotelBookingHandler
{
    private readonly ISalesPortalDbContext _db;
    private readonly ITransactionalExecutor _executor;
    private readonly IAgentsApiClient _agentsApiClient;
    private readonly ISalesEntriesNotificationService _salesEntriesNotificationService;
    private readonly IOutstandingNotificationService _outstandingNotificationService;
    private readonly TexasPortal.Domain.Common.IDocumentNumberGenerator _documentNumberGenerator;
    private readonly IReceivableService _receivableService;
    private readonly TexasPortal.Application.Sales.Accounts.ICustomerAccountCommandService _customerAccountCommandService;
    private readonly TexasPortal.Application.Sales.Profit.ISalesmanProfitService _profitService;

    public CreateHotelBookingHandler(
        ISalesPortalDbContext db,
        ITransactionalExecutor executor,
        IAgentsApiClient agentsApiClient,
        ISalesEntriesNotificationService salesEntriesNotificationService,
        IOutstandingNotificationService outstandingNotificationService,
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
        _documentNumberGenerator = documentNumberGenerator;
        _receivableService = receivableService;
        _customerAccountCommandService = customerAccountCommandService;
        _profitService = profitService;
    }

    public async Task<CreateHotelBookingResult> HandleAsync(
        CreateHotelBookingCommand command,
        CancellationToken ct = default)
    {
        var request = command.Request;

        // ── 1. Request Validation ──────────────────────────────────────────────
        var errors = new Dictionary<string, string[]>();

        var guestsList = request.Guests != null && request.Guests.Count > 0
            ? request.Guests
            : new List<HotelGuestRequest> { new HotelGuestRequest(request.Prefix, request.GuestName ?? "", request.ExpectedSupplierCost, request.Amount, request.IsInhouse, request.SupplierId) };

        if (guestsList.Count == 0 || string.IsNullOrWhiteSpace(guestsList[0].GuestName))
            errors["guestName"] = ["At least one guest name is required."];
        else
        {
            for (int i = 0; i < guestsList.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(guestsList[i].GuestName))
                    errors[$"guests_{i}_name"] = [$"Guest {i + 1} name is required."];
            }
        }

        if (string.IsNullOrWhiteSpace(request.Country))
            errors["country"] = ["Country is required."];
        else if (request.Country.Length > 255)
            errors["country"] = ["Country must not exceed 255 characters."];

        if (request.Days <= 0)
            errors["days"] = ["Days must be greater than zero."];

        if (request.CheckInDate == default)
            errors["checkInDate"] = ["CheckInDate is required."];

        if (request.Amount < 0)
            errors["amount"] = ["Amount must be zero or greater."];

        if (request.ExpectedSupplierCost < 0)
            errors["expectedSupplierCost"] = ["ExpectedSupplierCost must be zero or greater."];

        if (request.Prefix is { Length: > 255 })
            errors["prefix"] = ["Prefix must not exceed 255 characters."];

        // ClientType validation
        var clientTypeLower = (request.ClientType ?? "").ToLowerInvariant();
        long? agentId = clientTypeLower == "agent" ? request.AgentId : null;
        decimal payableAmount = request.Amount;
        if (clientTypeLower != "customer" && clientTypeLower != "agent")
        {
            errors["clientType"] = ["ClientType must be 'customer' or 'agent'."];
        }
        else
        {
            if (clientTypeLower == "customer" && !request.HeadId.HasValue)
                errors["headId"] = ["HeadId is required when ClientType is 'customer'."];

            if (clientTypeLower == "agent")
            {
                if (string.IsNullOrWhiteSpace(request.Agent))
                    errors["agent"] = ["Agent name is required when ClientType is 'agent'."];
                if (!request.AgentId.HasValue)
                    errors["agentId"] = ["AgentId is required when ClientType is 'agent'."];
            }
        }

        // Supplier validation
        if (!request.IsInhouse && !request.SupplierId.HasValue)
            errors["supplierId"] = ["SupplierId is required for non-inhouse bookings."];

        // Payment method validation
        var methodLower = (request.PaymentMethod ?? "").ToLowerInvariant();
        var allowedMethods = new[] { "cash", "credit", "wallet", "online" };
        if (!Array.Exists(allowedMethods, m => m == methodLower))
        {
            errors["paymentMethod"] = ["PaymentMethod must be 'cash', 'credit', 'wallet', or 'online'."];
        }
        else if (methodLower == "online" && !request.BankId.HasValue)
        {
            errors["bankId"] = ["BankId is required when PaymentMethod is 'online'."];
        }

        // Status validation
        var allowedStatuses = new[] { "pending", "confirmed", "success", "failed", "cancelled" };
        var statusLower = (request.Status ?? "").ToLowerInvariant();
        if (!Array.Exists(allowedStatuses, s => s == statusLower))
            errors["status"] = [$"Status must be one of: {string.Join(", ", allowedStatuses)}."];

        if (errors.Count > 0)
            return CreateHotelBookingResult.ValidationError(errors);

        // ── 2. Compute idempotency hash ───────────────────────────────────────
        var jsonPayload = JsonSerializer.Serialize(request);
        var requestHash = GetSha256Hash(command.IdempotencyKey + jsonPayload);

        // ── 3. Idempotency check-and-insert ───────────────────────────────────
        IdempotencyRecord? idempRecord = null;
        try
        {
            idempRecord = await _db.IdempotencyRecords
                .FirstOrDefaultAsync(r => r.IdempotencyKey == command.IdempotencyKey, ct);

            if (idempRecord != null)
            {
                if (idempRecord.Status == "InProgress")
                    return CreateHotelBookingResult.IdempotencyConflict("A duplicate request is already in progress.");

                if (idempRecord.Status == "Completed")
                {
                    if (idempRecord.RequestHash != requestHash)
                        return CreateHotelBookingResult.IdempotencyConflict(
                            "Idempotency key reuse with different request body is not allowed.");

                    if (idempRecord.ResponsePayload != null)
                    {
                        var cached = JsonSerializer.Deserialize<HotelBookingResponse>(idempRecord.ResponsePayload);
                        if (cached != null) return CreateHotelBookingResult.Success(cached);
                    }
                }
            }
            else
            {
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
            return CreateHotelBookingResult.IdempotencyConflict("A duplicate request is already in progress.");
        }

        // ── 4. Execute booking creation in retryable transaction ──────────────
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
                        return CreateHotelBookingResult.HeadNotFound();

                    head = await _db.Heads
                        .FirstOrDefaultAsync(h => h.Id == targetHeadId && h.DeletedAt == null, transactionCt);

                    if (head == null)
                        return CreateHotelBookingResult.HeadNotFound();
                }

                // B. Financial calculations (server-authoritative)
                var isInhouse = request.SupplierId.HasValue ? false : request.IsInhouse;
                var expectedSupplierCost = isInhouse ? 0m : request.ExpectedSupplierCost;
                var profit = isInhouse ? 0m : (payableAmount - expectedSupplierCost);

                var advancePaid = methodLower is "cash" or "online"
                    ? payableAmount
                    : 0m;

                // F. CareOf name for customer bookings
                string? careOf = clientTypeLower == "customer" && head != null
                    ? (head.FirstName + " " + head.LastName).Trim()
                    : null;

                string? supplierName = isInhouse ? "Inhouse (Pending assignment)" : null;
                if (!isInhouse && request.SupplierId.HasValue)
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

                // G. Insert booking record
                var docNo = await _documentNumberGenerator.GenerateDocumentNumberAsync("TXSHOTL", "Seq_HotelDocumentNo", transactionCt);
                var booking = new HotelBooking
                {
                    DocumentNumber = docNo,
                    UserId = command.CurrentUserId,
                    HeadId = targetHeadId,
                    ClientType = request.ClientType,
                    Agent = request.Agent,
                    AgentId = agentId,
                    IsInhouse = isInhouse,
                    SupplierId = isInhouse ? null : request.SupplierId,
                    SupplierName = supplierName,
                    ExpectedSupplierCost = expectedSupplierCost,
                    PayableAmount = payableAmount,
                    Profit = profit,
                    Prefix = request.Prefix,
                    GuestName = request.GuestName,
                    HotelName = request.HotelName,
                    CareOf = careOf,
                    Country = request.Country,
                    Days = request.Days,
                    CheckInDate = request.CheckInDate,
                    PaymentMethod = request.PaymentMethod!,
                    PaymentStatus = "pending",
                    ProcessingStatus = statusLower,
                    OrderStatus = "pending",
                    PaymentProof = request.PaymentProof,
                    PaymentReference = request.PaymentReference,
                    BankId = methodLower == "online" ? request.BankId : null,
                    BankName = methodLower == "online" ? request.BankName : null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.HotelBookings.Add(booking);
                await _db.SaveChangesAsync(transactionCt);

                var guestResponses = new List<HotelGuestResponse>();
                var perGuestAmount = guestsList.Count > 0 ? payableAmount / guestsList.Count : payableAmount;
                var perGuestSupplierCost = guestsList.Count > 0 ? (isInhouse ? 0m : expectedSupplierCost) / guestsList.Count : expectedSupplierCost;

                foreach (var gReq in guestsList)
                {
                    var gIsInhouse = isInhouse;
                    var gCost = gIsInhouse ? 0m : perGuestSupplierCost;
                    var gProfit = gIsInhouse ? 0m : (perGuestAmount - gCost);

                    var guest = new HotelGuest
                    {
                        HotelBookingId = booking.Id,
                        Prefix = gReq.Prefix ?? request.Prefix,
                        GuestName = gReq.GuestName.Trim(),
                        PayableAmount = perGuestAmount,
                        SupplierCost = gCost,
                        ExpectedSupplierCost = gCost,
                        Profit = gProfit,
                        IsInhouse = gIsInhouse,
                        SupplierId = gIsInhouse ? null : request.SupplierId,
                        SupplierName = supplierName,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.HotelGuests.Add(guest);
                    await _db.SaveChangesAsync(transactionCt);

                    guestResponses.Add(new HotelGuestResponse(
                        guest.Id,
                        guest.Prefix,
                        guest.GuestName,
                        guest.PayableAmount,
                        guest.ExpectedSupplierCost,
                        guest.Profit,
                        guest.IsInhouse,
                        guest.SupplierId,
                        guest.SupplierName
                    ));
                }

                // Process Credit / Credit Account payment method via ICustomerAccountCommandService
                if (methodLower is "credit" or "wallet")
                {
                    var creditRequest = new TexasPortal.Contracts.Sales.Accounts.CreditBookingRequest(
                        BookingId: booking.Id.ToString(),
                        BookingNumber: docNo,
                        BookingModule: "Hotel",
                        HeadId: (targetHeadId ?? agentId) ?? 0,
                        HeadType: clientTypeLower == "agent" ? "Agent" : "Customer",
                        SalesmanId: command.CurrentUserId,
                        TotalAmount: payableAmount,
                        CreditTerm: "Days30",
                        AutoApplyBalance: true,
                        IdempotencyKey: command.IdempotencyKey,
                        Remarks: $"Credit Sale for Hotel Booking #{docNo}"
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
                        ServiceType = "Hotel",
                        ServiceId = booking.Id,
                        Debit = payableAmount,
                        Credit = 0m,
                        Description = $"Hotel Booking: {request.GuestName} - {request.Country} ({request.Days} days)",
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.HeadLedgers.Add(ledger);
                    await _db.SaveChangesAsync(transactionCt);
                }

                // I. Salesman profit record creation
                await _profitService.CreateEstimatedProfitRecordAsync(
                    salesmanId: command.CurrentUserId,
                    bookingType: "Hotel",
                    bookingId: booking.Id,
                    documentNumber: docNo,
                    payableAmount: payableAmount,
                    estimatedSupplierCost: expectedSupplierCost,
                    isInhouse: isInhouse,
                    bookingCreatedAt: booking.CreatedAt ?? DateTime.UtcNow,
                    ct: transactionCt);

                // J. Cash-in-hand upsert
                if (methodLower == "cash")
                {
                    var cashAffected = await _db.CashInHands
                        .Where(c => c.UserId == command.CurrentUserId)
                        .ExecuteUpdateAsync(
                            s => s
                                .SetProperty(c => c.Amount, c => c.Amount + payableAmount)
                                .SetProperty(c => c.UpdatedAt, DateTime.UtcNow),
                            transactionCt);

                    if (cashAffected == 0)
                    {
                        _db.CashInHands.Add(new CashInHand
                        {
                            UserId = command.CurrentUserId,
                            Amount = payableAmount,
                            UpdatedAt = DateTime.UtcNow
                        });
                        await _db.SaveChangesAsync(transactionCt);
                    }
                }

                // K. Build response and complete idempotency record
                var response = new HotelBookingResponse(
                    booking.Id,
                    booking.UserId,
                    booking.HeadId,
                    booking.ClientType ?? "customer",
                    booking.GuestName,
                    HotelName: booking.HotelName,
                    booking.Country,
                    booking.Days,
                    booking.CheckInDate,
                    booking.PayableAmount,
                    booking.ExpectedSupplierCost,
                    booking.Profit,
                    booking.PaymentMethod,
                    booking.PaymentStatus,
                    booking.ProcessingStatus,
                    booking.OrderStatus,
                    booking.CreatedAt ?? DateTime.UtcNow,
                    booking.DocumentNumber,
                    guestResponses
                );

                idempRecord!.Status = "Completed";
                idempRecord.ResponseCode = 201;
                idempRecord.ResponsePayload = JsonSerializer.Serialize(response);
                _db.Entry(idempRecord).State = EntityState.Modified;

                await _salesEntriesNotificationService.PublishBookingCreatedAsync(
                    referenceType: ReferenceTypes.Hotel,
                    bookingId: booking.Id,
                    headId: targetHeadId,
                    agentId: agentId,
                    cancellationToken: transactionCt);

                if (targetHeadId.HasValue)
                {
                    await _outstandingNotificationService.PublishOutstandingChangedAsync(
                        referenceType: ReferenceTypes.Hotel,
                        referenceId: booking.Id.ToString(),
                        ownerId: targetHeadId.Value,
                        ownerType: "customer",
                        cancellationToken: transactionCt);
                }
                else if (agentId.HasValue)
                {
                    await _outstandingNotificationService.PublishOutstandingChangedAsync(
                        referenceType: ReferenceTypes.Hotel,
                        referenceId: booking.Id.ToString(),
                        ownerId: agentId.Value,
                        ownerType: "agent",
                        cancellationToken: transactionCt);
                }

                await _db.SaveChangesAsync(transactionCt);

                await TexasPortal.Application.Sales.Notifications.NotificationHelper.SendHeadEmailNotificationAsync(
                    _db, targetHeadId, agentId, "hotel_booking", booking.Id, "submitted", payableAmount,
                    $"{request.GuestName} - {request.Country} ({request.Days} days)", transactionCt, docNo, isCreation: true);

                return CreateHotelBookingResult.Success(response);
            }, ct);

            return result;
        }
        catch (Exception)
        {
            try
            {
                idempRecord!.Status = "Failed";
                _db.Entry(idempRecord).State = EntityState.Modified;
                await _db.SaveChangesAsync(ct);
            }
            catch
            {
                // Suppress secondary exception during compensation logging
            }
            throw;
        }
    }

    private static string GetSha256Hash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

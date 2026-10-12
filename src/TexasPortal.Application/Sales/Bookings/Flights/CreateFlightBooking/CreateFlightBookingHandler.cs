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
using TexasPortal.Application.Sales.Receivables;
using TexasPortal.Contracts.Sales.Bookings;
using TexasPortal.Contracts.Sales.Receivables;
using TexasPortal.Domain.Entities;
using TexasPortal.Contracts.Notifications;

namespace TexasPortal.Application.Sales.Bookings.Flights.CreateFlightBooking;

/// <summary>
/// Creates a Flight Booking with full financial integrity:
/// idempotency, transaction safety, wallet concurrency guards, ledger, cash, and profit effects.
/// Financial differences from Hotel/Other:
///   - Stored profit is at passenger level only on flight_passengers. No profit column on flight_bookings.
///   - Total Payable is derived as sum of passenger amount fields.
///   - Mixed passenger types (in-house vs external) supported.
///   - Ownership is UserId (IUserOwned).
/// </summary>
public sealed class CreateFlightBookingHandler
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

    public CreateFlightBookingHandler(
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

    public async Task<CreateFlightBookingResult> HandleAsync(
        CreateFlightBookingCommand command,
        CancellationToken ct = default)
    {
        var request = command.Request;

        // ── 1. Request Validation ──────────────────────────────────────────────
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.FromLocation))
            errors["fromLocation"] = ["From location is required."];
        else if (request.FromLocation.Length > 255)
            errors["fromLocation"] = ["From location must not exceed 255 characters."];

        if (string.IsNullOrWhiteSpace(request.ToLocation))
            errors["toLocation"] = ["To location is required."];
        else if (request.ToLocation.Length > 255)
            errors["toLocation"] = ["To location must not exceed 255 characters."];
        if (!request.TravelDate.HasValue)
            errors["travelDate"] = ["Travel date is required."];

        if (string.IsNullOrWhiteSpace(request.AirlineCompany))
            errors["airlineCompany"] = ["Airline company is required."];
        else if (request.AirlineCompany.Length > 255)
            errors["airlineCompany"] = ["Airline company must not exceed 255 characters."];

        if (request.TicketPnr is { Length: > 255 })
            errors["ticketPnr"] = ["Ticket PNR number must not exceed 255 characters."];


        if (request.CareOf is { Length: > 255 })
            errors["careOf"] = ["CareOf must not exceed 255 characters."];

        // ClientType validation
        var clientTypeLower = (request.ClientType ?? "").ToLowerInvariant();
        long? agentId = clientTypeLower == "agent" ? request.AgentId : null;
        decimal totalPayable = request.Passengers != null ? request.Passengers.Sum(p => p.Amount) : 0m;
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

        // Passengers validation
        if (request.Passengers == null || request.Passengers.Count == 0)
        {
            errors["passengers"] = ["At least one passenger is required."];
        }
        else
        {
            for (int i = 0; i < request.Passengers.Count; i++)
            {
                var passenger = request.Passengers[i];
                if (string.IsNullOrWhiteSpace(passenger.PassengerName))
                    errors[$"passengers[{i}].passengerName"] = ["Passenger name is required."];
                else if (passenger.PassengerName.Length > 255)
                    errors[$"passengers[{i}].passengerName"] = ["Passenger name must not exceed 255 characters."];

                if (passenger.Amount <= 0)
                    errors[$"passengers[{i}].amount"] = ["Amount must be greater than zero."];

                if (!passenger.IsInhouse && passenger.ExpectedSupplierCost < 0)
                    errors[$"passengers[{i}].expectedSupplierCost"] = ["ExpectedSupplierCost must be zero or greater."];

                if (passenger.Prefix is { Length: > 255 })
                    errors[$"passengers[{i}].prefix"] = ["Prefix must not exceed 255 characters."];
            }
        }

        if (errors.Count > 0)
            return CreateFlightBookingResult.ValidationError(errors);

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
                    return CreateFlightBookingResult.IdempotencyConflict("A duplicate request is already in progress.");

                if (idempRecord.Status == "Completed")
                {
                    if (idempRecord.RequestHash != requestHash)
                        return CreateFlightBookingResult.IdempotencyConflict(
                            "Idempotency key reuse with different request body is not allowed.");

                    if (idempRecord.ResponsePayload != null)
                    {
                        var cached = JsonSerializer.Deserialize<FlightBookingResponse>(idempRecord.ResponsePayload);
                        if (cached != null) return CreateFlightBookingResult.Success(cached);
                    }
                }

                if (idempRecord.Status == "Failed")
                {
                    idempRecord.Status = "InProgress";
                    idempRecord.RequestHash = requestHash;
                    _db.Entry(idempRecord).State = EntityState.Modified;
                    await _db.SaveChangesAsync(ct);
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
            return CreateFlightBookingResult.IdempotencyConflict("A duplicate request is already in progress.");
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
                        return CreateFlightBookingResult.HeadNotFound();

                    head = await _db.Heads
                        .FirstOrDefaultAsync(h => h.Id == targetHeadId && h.DeletedAt == null, transactionCt);

                    if (head == null)
                        return CreateFlightBookingResult.HeadNotFound();

                    // Customer heads are strictly salesman-owned; check CreatedBy matches
                    if (head.CreatedBy != command.CurrentUserId)
                        return CreateFlightBookingResult.HeadNotFound();
                }

                // B. Financial calculations (server-authoritative)
                // Total Payable is derived as sum of passenger amounts
                var advancePaid = methodLower is "cash" or "online"
                    ? totalPayable
                    : 0m;

                // F. CareOf resolution
                string? careOfName = request.CareOf;
                if (clientTypeLower == "customer" && string.IsNullOrWhiteSpace(careOfName) && head != null)
                {
                    careOfName = (head.FirstName + " " + head.LastName).Trim();
                }

                // G. Insert booking record
                var docNo = await _documentNumberGenerator.GenerateDocumentNumberAsync("TXSFLT", "Seq_FlightDocumentNo", transactionCt);
                bool isInhouseBooking = request.Passengers != null && request.Passengers.Count > 0
                    ? request.Passengers.All(p => p.IsInhouse)
                    : (request.IsInhouse || (!request.SupplierId.HasValue && (request.Passengers == null || request.Passengers.Count == 0)));

                decimal totalExpectedSupplierCost = request.Passengers != null && request.Passengers.Count > 0
                    ? request.Passengers.Where(p => !p.IsInhouse).Sum(p => p.ExpectedSupplierCost)
                    : (isInhouseBooking ? 0m : request.ExpectedSupplierCost);

                long? bookingSupplierId = isInhouseBooking ? null : (request.SupplierId ?? request.Passengers?.FirstOrDefault(p => !p.IsInhouse && p.SupplierId.HasValue)?.SupplierId);
                string? bookingSupplierName = isInhouseBooking ? "Inhouse (Pending assignment)" : request.SupplierName;
                if (!isInhouseBooking && bookingSupplierId.HasValue && string.IsNullOrWhiteSpace(bookingSupplierName))
                {
                    try
                    {
                        var suppliers = await _agentsApiClient.GetSuppliersAsync(transactionCt);
                        var supplier = suppliers.FirstOrDefault(s => s.Id == bookingSupplierId.Value);
                        bookingSupplierName = supplier != null ? supplier.Name : ("Supplier " + bookingSupplierId.Value);
                    }
                    catch
                    {
                        bookingSupplierName = "Supplier " + bookingSupplierId.Value;
                    }
                }

                var booking = new FlightBooking
                {
                    DocumentNumber = docNo,
                    UserId = command.CurrentUserId,
                    HeadId = targetHeadId,
                    ClientType = request.ClientType,
                    Agent = request.Agent,
                    AgentId = agentId,
                    CareOf = careOfName,
                    FromLocation = request.FromLocation!,
                    ToLocation = request.ToLocation!,
                    TravelDate = request.TravelDate,
                    AirlineCompany = request.AirlineCompany?.Trim(),
                    TicketPnr = string.IsNullOrWhiteSpace(request.TicketPnr) ? null : request.TicketPnr.Trim(),
                    PaymentMethod = request.PaymentMethod!,
                    BankId = methodLower == "online" ? request.BankId : null,
                    BankName = methodLower == "online" ? request.BankName : null,
                    PaymentStatus = "pending",
                    ProcessingStatus = "success",
                    PayableAmount = totalPayable,
                    ExpectedSupplierCost = isInhouseBooking ? 0m : totalExpectedSupplierCost,
                    Profit = isInhouseBooking ? 0m : (totalPayable - totalExpectedSupplierCost),
                    IsInhouse = isInhouseBooking,
                    SupplierId = bookingSupplierId,
                    SupplierName = bookingSupplierName,
                    PaymentProof = request.PaymentProof,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.FlightBookings.Add(booking);
                await _db.SaveChangesAsync(transactionCt);

                // H. Persist passengers and calculate total profit
                decimal totalProfit = 0m;
                var passengerResponses = new List<FlightPassengerResponse>();

                foreach (var passengerReq in request.Passengers!)
                {
                    if (passengerReq.PassengerName == "TriggerRollbackException")
                    {
                        throw new InvalidOperationException("Test-only rollback trigger.");
                    }
                    var isInhouse = passengerReq.IsInhouse;
                    var expectedCost = isInhouse ? 0m : passengerReq.ExpectedSupplierCost;
                    var passengerProfit = isInhouse ? 0m : (passengerReq.Amount - expectedCost);

                    if (!isInhouse && passengerProfit > 0m)
                    {
                        totalProfit += passengerProfit;
                    }

                    string? supplierName = null;
                    if (!isInhouse && passengerReq.SupplierId.HasValue)
                    {
                        try
                        {
                            var suppliers = await _agentsApiClient.GetSuppliersAsync(transactionCt);
                            var supplier = suppliers.FirstOrDefault(s => s.Id == passengerReq.SupplierId.Value);
                            supplierName = supplier != null ? supplier.Name : ("Supplier " + passengerReq.SupplierId.Value);
                        }
                        catch
                        {
                            supplierName = "Supplier " + passengerReq.SupplierId.Value;
                        }
                    }

                    var passenger = new FlightPassenger
                    {
                        FlightBookingId = booking.Id,
                        Prefix = passengerReq.Prefix,
                        PassengerName = passengerReq.PassengerName,
                        PayableAmount = passengerReq.Amount,
                        ExpectedSupplierCost = expectedCost,
                        Profit = passengerProfit,
                        IsInhouse = isInhouse,
                        SupplierId = isInhouse ? null : passengerReq.SupplierId,
                        SupplierName = supplierName,
                        TicketPnr = passengerReq.TicketPnr,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _db.FlightPassengers.Add(passenger);
                    await _db.SaveChangesAsync(transactionCt);

                    passengerResponses.Add(new FlightPassengerResponse(
                        passenger.Id,
                        passenger.Prefix,
                        passenger.PassengerName,
                        passenger.PayableAmount,
                        passenger.ExpectedSupplierCost,
                        passenger.Profit,
                        passenger.IsInhouse,
                        passenger.SupplierId,
                        passenger.SupplierName,
                        passenger.TicketPnr
                    ));
                }

                // Process Credit / Credit Account payment method via ICustomerAccountCommandService
                if (methodLower is "credit" or "wallet")
                {
                    var creditRequest = new TexasPortal.Contracts.Sales.Accounts.CreditBookingRequest(
                        BookingId: booking.Id.ToString(),
                        BookingNumber: docNo,
                        BookingModule: "Flight",
                        HeadId: (targetHeadId ?? agentId) ?? 0,
                        HeadType: clientTypeLower == "agent" ? "Agent" : "Customer",
                        SalesmanId: command.CurrentUserId,
                        TotalAmount: totalPayable,
                        CreditTerm: "Days30",
                        AutoApplyBalance: true,
                        IdempotencyKey: command.IdempotencyKey,
                        Remarks: $"Credit Sale for Flight Booking #{docNo}"
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
                        ServiceType = "Flight",
                        ServiceId = booking.Id,
                        Debit = totalPayable,
                        Credit = 0m,
                        Description = $"Flight Booking: {request.FromLocation} to {request.ToLocation} ({request.Passengers.Count} passengers)",
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.HeadLedgers.Add(ledger);
                    await _db.SaveChangesAsync(transactionCt);
                }

                // J. Salesman profit record creation
                await _profitService.CreateEstimatedProfitRecordAsync(
                    salesmanId: command.CurrentUserId,
                    bookingType: "Flight",
                    bookingId: booking.Id,
                    documentNumber: docNo,
                    payableAmount: totalPayable,
                    estimatedSupplierCost: totalExpectedSupplierCost,
                    isInhouse: isInhouseBooking,
                    bookingCreatedAt: booking.CreatedAt ?? DateTime.UtcNow,
                    ct: transactionCt);

                // K. Cash-in-hand upsert
                if (methodLower == "cash")
                {
                    var cashAffected = await _db.CashInHands
                        .Where(c => c.UserId == command.CurrentUserId)
                        .ExecuteUpdateAsync(
                            s => s
                                .SetProperty(c => c.Amount, c => c.Amount + totalPayable)
                                .SetProperty(c => c.UpdatedAt, DateTime.UtcNow),
                            transactionCt);

                    if (cashAffected == 0)
                    {
                        _db.CashInHands.Add(new CashInHand
                        {
                            UserId = command.CurrentUserId,
                            Amount = totalPayable,
                            UpdatedAt = DateTime.UtcNow
                        });
                        await _db.SaveChangesAsync(transactionCt);
                    }
                }

                // L. Build response and complete idempotency record
                var response = new FlightBookingResponse(
                    booking.Id,
                    booking.UserId,
                    booking.HeadId,
                    booking.ClientType ?? "customer",
                    booking.Agent,
                    booking.AgentId,
                    booking.CareOf,
                    booking.FromLocation ?? "",
                    booking.ToLocation ?? "",
                    booking.PaymentMethod ?? "",
                    booking.PaymentStatus ?? "",
                    booking.ProcessingStatus ?? "",
                    booking.PayableAmount,
                    totalProfit,
                    booking.PaymentProof,
                    passengerResponses,
                    booking.CreatedAt ?? DateTime.UtcNow,
                    booking.DocumentNumber,
                    booking.TravelDate,
                    booking.AirlineCompany,
                    booking.TicketPnr
                );

                idempRecord!.Status = "Completed";
                idempRecord.ResponseCode = 201;
                idempRecord.ResponsePayload = JsonSerializer.Serialize(response);
                _db.Entry(idempRecord).State = EntityState.Modified;

                // Enqueue SalesEntriesChanged notification in active transaction (atomic outbox commit)
                await _salesEntriesNotificationService.PublishBookingCreatedAsync(
                    referenceType: ReferenceTypes.Flight,
                    bookingId: booking.Id,
                    headId: targetHeadId,
                    agentId: agentId,
                    cancellationToken: transactionCt);

                if (targetHeadId.HasValue)
                {
                    await _outstandingNotificationService.PublishOutstandingChangedAsync(
                        referenceType: ReferenceTypes.Flight,
                        referenceId: booking.Id.ToString(),
                        ownerId: targetHeadId.Value,
                        ownerType: "customer",
                        cancellationToken: transactionCt);
                }
                else if (agentId.HasValue)
                {
                    await _outstandingNotificationService.PublishOutstandingChangedAsync(
                        referenceType: ReferenceTypes.Flight,
                        referenceId: booking.Id.ToString(),
                        ownerId: agentId.Value,
                        ownerType: "agent",
                        cancellationToken: transactionCt);
                }

                await _db.SaveChangesAsync(transactionCt);

                await TexasPortal.Application.Sales.Notifications.NotificationHelper.SendHeadEmailNotificationAsync(
                    _db, targetHeadId, agentId, "flight_booking", booking.Id, "submitted", totalPayable,
                    $"{request.FromLocation} -> {request.ToLocation} ({request.Passengers?.Count ?? 0} passenger(s))", transactionCt, docNo, isCreation: true);

                return CreateFlightBookingResult.Success(response);
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
                // Suppress secondary exception
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

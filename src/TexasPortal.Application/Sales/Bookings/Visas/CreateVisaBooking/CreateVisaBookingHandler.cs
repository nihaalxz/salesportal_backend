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

namespace TexasPortal.Application.Sales.Bookings.Visas.CreateVisaBooking;

/// <summary>
/// Creates a Visa Booking application with full financial integrity:
/// idempotency, transaction safety, wallet concurrency guards, ledger, cash, and profit effects.
/// </summary>
public sealed class CreateVisaBookingHandler
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

    public CreateVisaBookingHandler(
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

    public async Task<CreateVisaBookingResult> HandleAsync(
        CreateVisaBookingCommand command,
        CancellationToken ct = default)
    {
        var request = command.Request;

        // ── 1. Request Validation ──────────────────────────────────────────────
        var errors = new Dictionary<string, string[]>();

        if (request.CountryId <= 0)
            errors["countryId"] = ["Valid CountryId is required."];

        if (request.VisaPackId <= 0)
            errors["visaPackId"] = ["Valid VisaPackId is required."];

        if (string.IsNullOrWhiteSpace(request.VisaPackName))
            errors["visaPackName"] = ["VisaPackName is required."];
        else if (request.VisaPackName.Length > 255)
            errors["visaPackName"] = ["VisaPackName must not exceed 255 characters."];

        // ClientType validation
        var clientTypeLower = (request.ClientType ?? "").ToLowerInvariant();
        long? agentId = clientTypeLower == "agent" ? request.AgentId : null;
        decimal totalPayable = request.Applicants != null ? request.Applicants.Sum(a => a.Amount) : 0m;
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

        // Applicants validation
        if (request.Applicants == null || request.Applicants.Count == 0)
        {
            errors["applicants"] = ["At least one applicant is required."];
        }
        else
        {
            var hasExternal = request.Applicants!.Any(a => !a.IsInhouse);
            if (hasExternal && !request.SupplierId.HasValue)
            {
                errors["supplierId"] = ["SupplierId is required when there are external (non-inhouse) applicants."];
            }

            for (int i = 0; i < request.Applicants!.Count; i++)
            {
                var app = request.Applicants[i];

                if (app.FName is { Length: > 255 })
                    errors[$"applicants[{i}].fName"] = ["First name must not exceed 255 characters."];

                if (app.LName is { Length: > 255 })
                    errors[$"applicants[{i}].lName"] = ["Last name must not exceed 255 characters."];

                if (app.Nationality is { Length: > 255 })
                    errors[$"applicants[{i}].nationality"] = ["Nationality must not exceed 255 characters."];

                if (app.PassportNo is { Length: > 255 })
                    errors[$"applicants[{i}].passportNo"] = ["Passport number must not exceed 255 characters."];

                if (string.IsNullOrWhiteSpace(app.PassportCopy))
                    errors[$"applicants[{i}].passportCopy"] = ["Passport copy file is required."];

                if (app.ContactNo is { Length: > 255 })
                    errors[$"applicants[{i}].contactNo"] = ["Contact number must not exceed 255 characters."];

                if (app.EmailId is { Length: > 255 })
                    errors[$"applicants[{i}].emailId"] = ["Email must not exceed 255 characters."];

                if (app.Amount <= 0)
                    errors[$"applicants[{i}].amount"] = ["Amount must be greater than zero."];

                if (!app.IsInhouse && app.ExpectedSupplierCost < 0)
                    errors[$"applicants[{i}].expectedSupplierCost"] = ["ExpectedSupplierCost must be zero or greater."];

                if (app.Prefix is { Length: > 255 })
                    errors[$"applicants[{i}].prefix"] = ["Prefix must not exceed 255 characters."];

                if (app.CareOf is { Length: > 255 })
                    errors[$"applicants[{i}].careOf"] = ["CareOf must not exceed 255 characters."];

                if (app.IqamaHotelNo is { Length: > 255 })
                    errors[$"applicants[{i}].iqamaHotelNo"] = ["IqamaHotelNo must not exceed 255 characters."];
            }
        }

        if (errors.Count > 0)
            return CreateVisaBookingResult.ValidationError(errors);

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
                    return CreateVisaBookingResult.IdempotencyConflict("A duplicate request is already in progress.");

                if (idempRecord.Status == "Completed")
                {
                    if (idempRecord.RequestHash != requestHash)
                        return CreateVisaBookingResult.IdempotencyConflict(
                            "Idempotency key reuse with different request body is not allowed.");

                    if (idempRecord.ResponsePayload != null)
                    {
                        var cached = JsonSerializer.Deserialize<VisaBookingResponse>(idempRecord.ResponsePayload);
                        if (cached != null) return CreateVisaBookingResult.Success(cached);
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
            return CreateVisaBookingResult.IdempotencyConflict("A duplicate request is already in progress.");
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
                        return CreateVisaBookingResult.HeadNotFound();

                    head = await _db.Heads
                        .FirstOrDefaultAsync(h => h.Id == targetHeadId && h.DeletedAt == null, transactionCt);

                    if (head == null)
                        return CreateVisaBookingResult.HeadNotFound();

                    // Customer heads are strictly salesman-owned; check CreatedBy matches
                    if (head.CreatedBy != command.CurrentUserId)
                        return CreateVisaBookingResult.HeadNotFound();
                }

                // B. Financial calculations (server-authoritative)
                // Total Payable is derived as sum of applicant amounts
                var advancePaid = methodLower is "cash" or "online"
                    ? totalPayable
                    : 0m;

                // F. Resolve supplier info
                bool isInhouseBooking = request.Applicants != null && request.Applicants.Count > 0
                    ? request.Applicants.All(a => a.IsInhouse)
                    : (request.SupplierId.HasValue ? false : request.IsInhouse);
                long? bookingSupplierId = null;
                string? bookingSupplierName = isInhouseBooking ? "Inhouse (Pending assignment)" : null;

                if (!isInhouseBooking && request.SupplierId.HasValue)
                {
                    bookingSupplierId = request.SupplierId.Value;
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

                // Calculate expected supplier cost and profits
                decimal totalExpectedSupplierCost = 0m;
                decimal totalProfit = 0m;

                // G. Insert booking record
                var docNo = await _documentNumberGenerator.GenerateDocumentNumberAsync("TXSVISA", "Seq_SalesVisaDocumentNo", transactionCt);
                var booking = new SalesVisaApplication
                {
                    DocumentNumber = docNo,
                    UserId = command.CurrentUserId,
                    HeadId = targetHeadId,
                    CountryId = request.CountryId,
                    VisaPackId = request.VisaPackId,
                    VisaPackName = request.VisaPackName,
                    ClientType = request.ClientType ?? "customer",
                    Agent = request.Agent,
                    AgentId = agentId,
                    ProcessingStatus = "pending",
                    PaymentStatus = "pending",
                    PayableAmount = totalPayable,
                    ExpectedSupplierCost = 0m, // will update post applicant loop
                    Profit = 0m, // will update post applicant loop
                    IsInhouse = isInhouseBooking,
                    SupplierId = bookingSupplierId,
                    SupplierName = bookingSupplierName,
                    RejectionFee = 0m,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.SalesVisaApplications.Add(booking);
                await _db.SaveChangesAsync(transactionCt);

                // H. Persist applicants and calculate total profit
                var applicantResponses = new List<VisaApplicantResponse>();

                foreach (var appReq in request.Applicants!)
                {
                    if (appReq.FName == "TriggerRollbackException")
                    {
                        throw new InvalidOperationException("Test-only rollback trigger.");
                    }

                    var isInhouse = appReq.IsInhouse;
                    var expectedCost = isInhouse ? 0m : appReq.ExpectedSupplierCost;
                    var appProfit = isInhouse ? 0m : Math.Max(0m, appReq.Amount - expectedCost);

                    if (!isInhouse && appProfit > 0m)
                    {
                        totalProfit += appProfit;
                    }
                    totalExpectedSupplierCost += expectedCost;

                    var applicant = new SalesVisaApplicant
                    {
                        SalesVisaApplicationId = booking.Id,
                        Prefix = string.IsNullOrWhiteSpace(appReq.Prefix) ? null : appReq.Prefix.Trim(),
                        FName = string.IsNullOrWhiteSpace(appReq.FName) ? null : appReq.FName.Trim(),
                        LName = string.IsNullOrWhiteSpace(appReq.LName) ? null : appReq.LName.Trim(),
                        Dob = appReq.Dob,
                        Nationality = string.IsNullOrWhiteSpace(appReq.Nationality) ? null : appReq.Nationality.Trim(),
                        CareOf = string.IsNullOrWhiteSpace(appReq.CareOf) ? null : appReq.CareOf.Trim(),
                        ContactNo = string.IsNullOrWhiteSpace(appReq.ContactNo) ? string.Empty : appReq.ContactNo.Trim(),
                        EmailId = string.IsNullOrWhiteSpace(appReq.EmailId) ? string.Empty : appReq.EmailId.Trim(),
                        PassportNo = string.IsNullOrWhiteSpace(appReq.PassportNo) ? null : appReq.PassportNo.Trim(),
                        PassportCopy = appReq.PassportCopy,
                        IqamaHotelNo = appReq.IqamaHotelNo,
                        BankDoc = appReq.BankDoc,
                        PayableAmount = appReq.Amount,
                        UnpaidAmount = methodLower == "credit" ? appReq.Amount : 0m,
                        SupplierCost = expectedCost,
                        ExpectedSupplierCost = expectedCost,
                        Profit = appProfit,
                        IsInhouse = isInhouse,
                        BankId = request.BankId,
                        BankName = request.BankName,
                        PaymentMethod = request.PaymentMethod,
                        PaymentStatus = "pending",
                        PaymentProof = request.PaymentProof,
                        PaymentReference = request.PaymentReference,
                        GovtFeeStatus = "pending",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _db.SalesVisaApplicants.Add(applicant);
                    await _db.SaveChangesAsync(transactionCt);

                    applicantResponses.Add(new VisaApplicantResponse(
                        applicant.Id,
                        applicant.Prefix,
                        applicant.FName,
                        applicant.LName,
                        applicant.Dob,
                        applicant.Nationality,
                        applicant.CareOf,
                        applicant.ContactNo,
                        applicant.EmailId,
                        applicant.PassportNo,
                        applicant.IqamaHotelNo,
                        applicant.PayableAmount,
                        applicant.ExpectedSupplierCost,
                        applicant.Profit,
                        applicant.IsInhouse,
                        applicant.PaymentMethod,
                        applicant.PaymentStatus,
                        applicant.PaymentProof,
                        applicant.PaymentReference,
                        applicant.BankId,
                        applicant.BankName,
                        applicant.PassportCopy,
                        applicant.BankDoc
                    ));
                }

                // Update booking-level totals in DB
                booking.ExpectedSupplierCost = totalExpectedSupplierCost;
                booking.Profit = isInhouseBooking ? 0m : totalProfit;
                _db.Entry(booking).State = EntityState.Modified;
                await _db.SaveChangesAsync(transactionCt);

                // Process Credit / Credit Account payment method via ICustomerAccountCommandService
                if (methodLower is "credit" or "wallet")
                {
                    var creditRequest = new TexasPortal.Contracts.Sales.Accounts.CreditBookingRequest(
                        BookingId: booking.Id.ToString(),
                        BookingNumber: docNo,
                        BookingModule: "Visa",
                        HeadId: (targetHeadId ?? agentId) ?? 0,
                        HeadType: clientTypeLower == "agent" ? "Agent" : "Customer",
                        SalesmanId: command.CurrentUserId,
                        TotalAmount: totalPayable,
                        CreditTerm: "Days30",
                        AutoApplyBalance: true,
                        IdempotencyKey: command.IdempotencyKey,
                        Remarks: $"Credit Sale for Visa Booking #{docNo}"
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
                        ServiceType = "Visa",
                        ServiceId = booking.Id,
                        Debit = totalPayable,
                        Credit = 0m,
                        Description = $"Visa Booking ({request.VisaPackName}): {request.Applicants.Count} applicants",
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.HeadLedgers.Add(ledger);
                    await _db.SaveChangesAsync(transactionCt);
                }

                // J. Salesman profit record creation
                await _profitService.CreateEstimatedProfitRecordAsync(
                    salesmanId: command.CurrentUserId,
                    bookingType: "Visa",
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
                var response = new VisaBookingResponse(
                    booking.Id,
                    booking.DocumentNumber,
                    booking.UserId,
                    booking.CountryId,
                    booking.VisaPackId,
                    booking.VisaPackName,
                    booking.ClientType ?? "customer",
                    booking.Agent,
                    booking.AgentId,
                    booking.HeadId,
                    booking.PaymentStatus,
                    booking.ProcessingStatus,
                    booking.PayableAmount,
                    booking.ExpectedSupplierCost,
                    booking.Profit,
                    booking.SupplierId,
                    booking.SupplierName,
                    applicantResponses,
                    booking.CreatedAt ?? DateTime.UtcNow
                );

                idempRecord!.Status = "Completed";
                idempRecord.ResponseCode = 201;
                idempRecord.ResponsePayload = JsonSerializer.Serialize(response);
                _db.Entry(idempRecord).State = EntityState.Modified;

                await _salesEntriesNotificationService.PublishBookingCreatedAsync(
                    referenceType: ReferenceTypes.Visa,
                    bookingId: booking.Id,
                    headId: targetHeadId,
                    agentId: agentId,
                    cancellationToken: transactionCt);

                if (targetHeadId.HasValue)
                {
                    await _outstandingNotificationService.PublishOutstandingChangedAsync(
                        referenceType: ReferenceTypes.Visa,
                        referenceId: booking.Id.ToString(),
                        ownerId: targetHeadId.Value,
                        ownerType: "customer",
                        cancellationToken: transactionCt);
                }
                else if (agentId.HasValue)
                {
                    await _outstandingNotificationService.PublishOutstandingChangedAsync(
                        referenceType: ReferenceTypes.Visa,
                        referenceId: booking.Id.ToString(),
                        ownerId: agentId.Value,
                        ownerType: "agent",
                        cancellationToken: transactionCt);
                }

                await _operationalQueueNotificationService.PublishOperationalQueueChangedAsync(
                    referenceType: ReferenceTypes.Visa,
                    referenceId: booking.Id.ToString(),
                    queueType: "visa",
                    includeOperationsRole: true,
                    cancellationToken: transactionCt);

                await _db.SaveChangesAsync(transactionCt);

                await TexasPortal.Application.Sales.Notifications.NotificationHelper.SendHeadEmailNotificationAsync(
                    _db, targetHeadId, agentId, "visa_application", booking.Id, "submitted", totalPayable,
                    $"{request.VisaPackName} ({request.Applicants?.Count ?? 0} applicant(s))", transactionCt, docNo, isCreation: true);

                return CreateVisaBookingResult.Success(response);
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
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var builder = new StringBuilder();
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }
        return builder.ToString();
    }
}

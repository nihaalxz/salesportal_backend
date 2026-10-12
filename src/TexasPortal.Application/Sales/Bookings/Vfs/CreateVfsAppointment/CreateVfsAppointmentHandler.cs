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

namespace TexasPortal.Application.Sales.Bookings.Vfs.CreateVfsAppointment;

public sealed class CreateVfsAppointmentHandler
{
    private readonly ISalesPortalDbContext _dbContext;
    private readonly ITransactionalExecutor _transactionalExecutor;
    private readonly IAgentsApiClient _agentsApiClient;
    private readonly ISalesEntriesNotificationService _salesEntriesNotificationService;
    private readonly IOutstandingNotificationService _outstandingNotificationService;
    private readonly IOperationalQueueNotificationService _operationalQueueNotificationService;
    private readonly TexasPortal.Domain.Common.IDocumentNumberGenerator _documentNumberGenerator;
    private readonly IReceivableService _receivableService;
    private readonly TexasPortal.Application.Sales.Accounts.ICustomerAccountCommandService _customerAccountCommandService;
    private readonly TexasPortal.Application.Sales.Profit.ISalesmanProfitService _profitService;

    public CreateVfsAppointmentHandler(
        ISalesPortalDbContext dbContext,
        ITransactionalExecutor transactionalExecutor,
        IAgentsApiClient agentsApiClient,
        ISalesEntriesNotificationService salesEntriesNotificationService,
        IOutstandingNotificationService outstandingNotificationService,
        IOperationalQueueNotificationService operationalQueueNotificationService,
        TexasPortal.Domain.Common.IDocumentNumberGenerator documentNumberGenerator,
        IReceivableService receivableService,
        TexasPortal.Application.Sales.Accounts.ICustomerAccountCommandService customerAccountCommandService,
        TexasPortal.Application.Sales.Profit.ISalesmanProfitService profitService)
    {
        _dbContext = dbContext;
        _transactionalExecutor = transactionalExecutor;
        _agentsApiClient = agentsApiClient;
        _salesEntriesNotificationService = salesEntriesNotificationService;
        _outstandingNotificationService = outstandingNotificationService;
        _operationalQueueNotificationService = operationalQueueNotificationService;
        _documentNumberGenerator = documentNumberGenerator;
        _receivableService = receivableService;
        _customerAccountCommandService = customerAccountCommandService;
        _profitService = profitService;
    }

    public async Task<CreateVfsAppointmentResult> HandleAsync(CreateVfsAppointmentCommand command, CancellationToken ct = default)
    {
        var request = command.Request;

        // A. Validate Request Inputs
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.ClientType) || 
            (request.ClientType != "customer" && request.ClientType != "agent" && request.ClientType != "walkin"))
        {
            errors["clientType"] = ["ClientType must be 'customer', 'agent', or 'walkin'."];
        }

        if (request.ClientType == "customer" && !request.HeadId.HasValue)
        {
            errors["headId"] = ["HeadId is required for customer client types."];
        }

        if (string.IsNullOrWhiteSpace(request.Prefix))
            errors["prefix"] = ["Prefix is required."];

        var applicantsList = request.Applicants != null && request.Applicants.Count > 0
            ? request.Applicants
            : new List<VfsApplicantRequest> { new VfsApplicantRequest(request.Prefix, request.Name ?? "", request.PassportCopy, request.ExpectedSupplierCost, request.Amount, request.IsInhouse, request.SupplierId) };

        if (applicantsList.Count == 0 || string.IsNullOrWhiteSpace(applicantsList[0].Name))
            errors["name"] = ["At least one applicant name is required."];
        else
        {
            for (int i = 0; i < applicantsList.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(applicantsList[i].Name))
                    errors[$"applicants_{i}_name"] = [$"Applicant {i + 1} name is required."];
            }
        }

        if (string.IsNullOrWhiteSpace(request.Country))
            errors["country"] = ["Country is required."];

        if (string.IsNullOrWhiteSpace(request.VisaType))
            errors["visaType"] = ["Visa type is required."];

        if (request.Amount <= 0)
            errors["amount"] = ["Amount must be greater than zero."];

        if (request.ExpectedSupplierCost < 0)
            errors["expectedSupplierCost"] = ["Expected supplier cost cannot be negative."];

        if (!request.IsInhouse && !request.SupplierId.HasValue)
        {
            errors["supplierId"] = ["Supplier selection is required when booking is not In-house."];
        }

        if (string.IsNullOrWhiteSpace(request.PaymentMethod) ||
            (request.PaymentMethod != "cash" && request.PaymentMethod != "credit" && request.PaymentMethod != "wallet" && request.PaymentMethod != "online"))
        {
            errors["paymentMethod"] = ["PaymentMethod must be 'cash', 'credit', 'wallet', or 'online'."];
        }
        else if (request.PaymentMethod == "online" && !request.BankId.HasValue)
        {
            errors["bankId"] = ["BankId is required when PaymentMethod is 'online'."];
        }

        if (errors.Count > 0)
        {
            return CreateVfsAppointmentResult.ValidationError(errors);
        }

        // B. Resolve and Validate Head Account
        Head? head = null;
        if (request.ClientType == "customer" && request.HeadId.HasValue)
        {
            head = await _dbContext.Heads
                .FirstOrDefaultAsync(h => h.Id == request.HeadId.Value && h.DeletedAt == null, ct);

            if (head == null)
            {
                return CreateVfsAppointmentResult.HeadNotFound();
            }

            // Customer heads are strictly salesman-owned
            if (head.CreatedBy != command.CurrentUserId)
            {
                return CreateVfsAppointmentResult.HeadNotFound();
            }
        }

        // C. Calculate Totals
        bool isInhouseBooking = request.SupplierId.HasValue ? false : request.IsInhouse;
        var payableAmount = request.Amount;
        var expectedSupplierCost = isInhouseBooking ? 0m : request.ExpectedSupplierCost;
        var profit = payableAmount - expectedSupplierCost;
        var advancePaid = (request.PaymentMethod == "cash" || request.PaymentMethod == "wallet" || request.PaymentMethod == "online") 
            ? payableAmount 
            : 0;

        // D. Resolve Supplier
        var supplierName = isInhouseBooking ? "Inhouse (Pending assignment)" : "Walk-in / Direct";
        if (!isInhouseBooking && request.SupplierId.HasValue)
        {
            try
            {
                var suppliers = await _agentsApiClient.GetSuppliersAsync(ct);
                var supplier = suppliers.FirstOrDefault(s => s.Id == request.SupplierId.Value);
                if (supplier != null)
                {
                    supplierName = supplier.Name;
                }
            }
            catch
            {
                supplierName = "Supplier " + request.SupplierId.Value;
            }
        }

        // E. Check Idempotency
        var requestJson = JsonSerializer.Serialize(request);
        var requestHash = GetSha256Hash(command.IdempotencyKey + requestJson);

        IdempotencyRecord? idempotencyRecord = null;
        try
        {
            idempotencyRecord = await _dbContext.IdempotencyRecords
                .FirstOrDefaultAsync(r => r.IdempotencyKey == command.IdempotencyKey, ct);

            if (idempotencyRecord != null)
            {
                if (idempotencyRecord.Status == "InProgress")
                {
                    return CreateVfsAppointmentResult.IdempotencyConflict(
                        "A duplicate request is already in progress.");
                }

                if (idempotencyRecord.Status == "Completed")
                {
                    if (idempotencyRecord.RequestHash != requestHash)
                    {
                        return CreateVfsAppointmentResult.IdempotencyConflict(
                            "Idempotency key reuse with different request body is not allowed.");
                    }

                    if (idempotencyRecord.ResponsePayload != null)
                    {
                        var cachedResponse = JsonSerializer.Deserialize<VfsAppointmentResponse>(idempotencyRecord.ResponsePayload);
                        if (cachedResponse != null)
                        {
                            return CreateVfsAppointmentResult.Success(cachedResponse);
                        }
                    }
                }

                if (idempotencyRecord.Status == "Failed")
                {
                    idempotencyRecord.Status = "InProgress";
                    idempotencyRecord.RequestHash = requestHash;
                    _dbContext.Entry(idempotencyRecord).State = EntityState.Modified;
                    await _dbContext.SaveChangesAsync(ct);
                }
            }
            else
            {
                idempotencyRecord = new IdempotencyRecord
                {
                    IdempotencyKey = command.IdempotencyKey,
                    RequestHash = requestHash,
                    Status = "InProgress",
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(24)
                };
                _dbContext.IdempotencyRecords.Add(idempotencyRecord);
                await _dbContext.SaveChangesAsync(ct);
            }
        }
        catch (DbUpdateException)
        {
            return CreateVfsAppointmentResult.IdempotencyConflict("A duplicate request is already in progress.");
        }

        // F. Execute Write Operations Inside Transaction
        VfsAppointment booking;
        try
        {
            var finalBooking = await _transactionalExecutor.ExecuteAsync(async transactionCt =>
            {
                var innerDb = _dbContext;

                // For test rollback verification
                if (request.Name == "TriggerRollbackException")
                {
                    throw new InvalidOperationException("Triggered intentional rollback exception for testing.");
                }

                // Process Salesman Cash in Hand

                // 3. Process Salesman Cash in Hand
                if (request.PaymentMethod == "cash")
                {
                    var cashAffected = await innerDb.CashInHands
                        .Where(c => c.UserId == command.CurrentUserId)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(c => c.Amount, c => c.Amount + payableAmount)
                            .SetProperty(c => c.UpdatedAt, DateTime.UtcNow), transactionCt);

                    if (cashAffected == 0)
                    {
                        innerDb.CashInHands.Add(new CashInHand
                        {
                            UserId = command.CurrentUserId,
                            Amount = payableAmount,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                        await innerDb.SaveChangesAsync(transactionCt);
                    }
                }

                // 6. Save VFS Appointment Entity
                var docNo = await _documentNumberGenerator.GenerateDocumentNumberAsync("TXSVFS", "Seq_VfsDocumentNo", transactionCt);
                var vfsApp = new VfsAppointment
                {
                    DocumentNumber = docNo,
                    CreatedBy = command.CurrentUserId,
                    HeadId = request.ClientType == "customer" ? request.HeadId : null,
                    ClientType = request.ClientType,
                    Agent = request.Agent,
                    AgentId = request.AgentId,
                    Prefix = request.Prefix,
                    Name = request.Name,
                    Supplier = supplierName,
                    IsInhouse = isInhouseBooking,
                    SupplierId = isInhouseBooking ? null : request.SupplierId,
                    SupplierName = supplierName,
                    Country = request.Country,
                    VisaType = request.VisaType,
                    PayableAmount = payableAmount,
                    ExpectedSupplierCost = expectedSupplierCost,
                    Profit = profit,
                    PaymentMode = request.PaymentMethod,
                    BankId = request.PaymentMethod == "online" ? request.BankId : null,
                    BankName = request.PaymentMethod == "online" ? request.BankName : null,
                    PaymentProof = request.PaymentProof,
                    PaymentStatus = "pending",
                    ProcessingStatus = "pending",
                    AppointmentDate = request.AppointmentDate,
                    AppointmentTime = request.AppointmentTime,
                    VfsReceipt = request.VfsReceipt,
                    PassportCopy = request.PassportCopy,
                    CreatedAt = DateTime.UtcNow
                };

                innerDb.VfsAppointments.Add(vfsApp);
                await innerDb.SaveChangesAsync(transactionCt);

                var perApplicantAmount = applicantsList.Count > 0 ? payableAmount / applicantsList.Count : payableAmount;
                var perApplicantCost = applicantsList.Count > 0 ? (isInhouseBooking ? 0m : expectedSupplierCost) / applicantsList.Count : expectedSupplierCost;

                foreach (var aReq in applicantsList)
                {
                    var aIsInhouse = isInhouseBooking;
                    var aCost = aIsInhouse ? 0m : perApplicantCost;
                    var aProfit = aIsInhouse ? 0m : (perApplicantAmount - aCost);

                    var applicant = new VfsApplicant
                    {
                        VfsAppointmentId = vfsApp.Id,
                        Prefix = aReq.Prefix ?? request.Prefix,
                        Name = aReq.Name.Trim(),
                        PassportCopy = aReq.PassportCopy ?? request.PassportCopy,
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
                    innerDb.VfsApplicants.Add(applicant);
                    await innerDb.SaveChangesAsync(transactionCt);
                }

                // Create Salesman Profit Record
                await _profitService.CreateEstimatedProfitRecordAsync(
                    salesmanId: command.CurrentUserId,
                    bookingType: "VFS",
                    bookingId: vfsApp.Id,
                    documentNumber: docNo,
                    payableAmount: payableAmount,
                    estimatedSupplierCost: expectedSupplierCost,
                    isInhouse: isInhouseBooking,
                    bookingCreatedAt: vfsApp.CreatedAt ?? DateTime.UtcNow,
                    ct: transactionCt);

                var methodLower = (request.PaymentMethod ?? "").ToLowerInvariant();

                // Process Credit / Credit Account payment method via ICustomerAccountCommandService
                if (methodLower is "credit" or "wallet")
                {
                    var creditRequest = new TexasPortal.Contracts.Sales.Accounts.CreditBookingRequest(
                        BookingId: vfsApp.Id.ToString(),
                        BookingNumber: docNo,
                        BookingModule: "Vfs",
                        HeadId: (request.ClientType == "customer" ? request.HeadId : request.AgentId) ?? 0,
                        HeadType: string.Equals(request.ClientType, "agent", StringComparison.OrdinalIgnoreCase) ? "Agent" : "Customer",
                        SalesmanId: command.CurrentUserId,
                        TotalAmount: payableAmount,
                        CreditTerm: "Days30",
                        AutoApplyBalance: true,
                        IdempotencyKey: command.IdempotencyKey,
                        Remarks: $"Credit Sale for VFS Appointment #{docNo}"
                    );

                    var creditRes = await _customerAccountCommandService.AuthorizeAndProcessCreditBookingAsync(creditRequest, transactionCt);
                    vfsApp.PaymentStatus = creditRes.PaymentStatus;
                    innerDb.Entry(vfsApp).State = EntityState.Modified;
                }
                else if (request.ClientType == "customer" && request.HeadId.HasValue)
                {
                    // Non-credit cash/online ledger entry
                    innerDb.HeadLedgers.Add(new HeadLedger
                    {
                        HeadId = request.HeadId.Value,
                        ServiceId = vfsApp.Id,
                        ServiceType = "Vfs",
                        Debit = payableAmount,
                        Credit = 0m,
                        CreatedAt = DateTime.UtcNow
                    });
                    await innerDb.SaveChangesAsync(transactionCt);
                }

                // 8. Enqueue SalesEntriesChanged notification in active transaction
                await _salesEntriesNotificationService.PublishBookingCreatedAsync(
                    referenceType: ReferenceTypes.Vfs,
                    bookingId: vfsApp.Id,
                    headId: request.ClientType == "customer" ? request.HeadId : null,
                    agentId: request.AgentId,
                    cancellationToken: transactionCt);

                if (request.ClientType == "customer" && request.HeadId.HasValue)
                {
                    await _outstandingNotificationService.PublishOutstandingChangedAsync(
                        referenceType: ReferenceTypes.Vfs,
                        referenceId: vfsApp.Id.ToString(),
                        ownerId: request.HeadId.Value,
                        ownerType: "customer",
                        cancellationToken: transactionCt);
                }
                else if (request.AgentId.HasValue)
                {
                    await _outstandingNotificationService.PublishOutstandingChangedAsync(
                        referenceType: ReferenceTypes.Vfs,
                        referenceId: vfsApp.Id.ToString(),
                        ownerId: request.AgentId.Value,
                        ownerType: "agent",
                        cancellationToken: transactionCt);
                }

                await _operationalQueueNotificationService.PublishOperationalQueueChangedAsync(
                    referenceType: ReferenceTypes.Vfs,
                    referenceId: vfsApp.Id.ToString(),
                    queueType: "vfs",
                    includeOperationsRole: true,
                    cancellationToken: transactionCt);

                await TexasPortal.Application.Sales.Notifications.NotificationHelper.SendHeadEmailNotificationAsync(
                    innerDb, request.ClientType == "customer" ? request.HeadId : null, request.AgentId, "vfs_appointment", vfsApp.Id, "submitted", payableAmount,
                    $"{request.Name} - {request.Country} ({request.VisaType})", transactionCt, docNo, isCreation: true);

                return vfsApp;
            }, ct);

            booking = finalBooking;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CreateVfsAppointmentHandler ERROR] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[CreateVfsAppointmentHandler INNER] {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
            // Reset idempotency state to Failed
            if (idempotencyRecord != null)
            {
                idempotencyRecord.Status = "Failed";
                _dbContext.Entry(idempotencyRecord).State = EntityState.Modified;
                await _dbContext.SaveChangesAsync(ct);
            }
            throw;
        }

        // H. Map response and complete idempotency record
        var response = new VfsAppointmentResponse(
            booking.Id,
            booking.CreatedBy,
            booking.HeadId,
            booking.ClientType ?? "customer",
            booking.Agent,
            booking.AgentId,
            booking.Prefix,
            booking.Name,
            booking.SupplierId,
            booking.Country,
            booking.VisaType,
            booking.PayableAmount,
            booking.ExpectedSupplierCost,
            booking.Profit,
            booking.PaymentMode,
            booking.PaymentProof,
            booking.PaymentStatus,
            booking.ProcessingStatus,
            booking.AppointmentDate,
            booking.AppointmentTime,
            booking.VfsReceipt,
            booking.PassportCopy,
            booking.CreatedAt ?? DateTime.UtcNow,
            booking.DocumentNumber,
            applicantsList.Select(a => new VfsApplicantResponse(0, a.Prefix, a.Name, a.PassportCopy, payableAmount / (applicantsList.Count > 0 ? applicantsList.Count : 1), expectedSupplierCost / (applicantsList.Count > 0 ? applicantsList.Count : 1), profit / (applicantsList.Count > 0 ? applicantsList.Count : 1), isInhouseBooking, request.SupplierId, supplierName)).ToList()
        );

        idempotencyRecord.Status = "Completed";
        idempotencyRecord.ResponseCode = 201;
        idempotencyRecord.ResponsePayload = JsonSerializer.Serialize(response);
        _dbContext.Entry(idempotencyRecord).State = EntityState.Modified;
        await _dbContext.SaveChangesAsync(ct);

        return CreateVfsAppointmentResult.Success(response);
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

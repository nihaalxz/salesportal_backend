using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Common.Interfaces;
using TexasPortal.Application.Persistence;
using TexasPortal.Application.Sales.Queries;
using TexasPortal.Application.Sales.Receipts;
using TexasPortal.Application.Sales.Services;
using TexasPortal.Contracts.Notifications;
using TexasPortal.Domain.Entities;

namespace TexasPortal.Application.Sales;

public record RejectVisaResult(
    bool Success,
    long VisaId,
    string Status,
    decimal PayableAmount,
    decimal GovernmentFeesPaid,
    decimal RejectionFee,
    decimal BalanceAdjustment,
    decimal PreviousBalance,
    decimal NewBalance,
    string? ErrorMessage = null
);

public class ApplicationFile
{
    public Stream Stream { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
}

public class GovtFeeServiceInput
{
    public decimal Amount { get; set; }
    public string TransactionNo { get; set; } = null!;
    public string? GovtFeeDescription { get; set; }
    public string? ExistingPaymentProof { get; set; }
    public ApplicationFile? PaymentProof { get; set; }
}

public interface IOperationsWriteService
{
    Task<bool> UpdateVisaIdAsync(long firstApplicantId, string visaAppId, List<GovtFeeServiceInput> govtFees, long processedBy, CancellationToken ct);
    Task<bool> EditVisaIdAsync(long firstApplicantId, string visaAppId, long processedBy, CancellationToken ct);
    Task<bool> ApproveVisaAsync(long id, ApplicationFile? visaDoc, string? note, long processedBy, CancellationToken ct);
    Task<RejectVisaResult> RejectVisaAsync(long id, ApplicationFile? rejectionDoc, string note, string? refundNote, decimal rejectionFee, decimal? payableAmount, long processedBy, CancellationToken ct);
    
    Task<bool> ProcessVfsAsync(long id, List<GovtFeeServiceInput> govtFees, long processedBy, CancellationToken ct);
    Task<bool> UpdateVfsScheduleAsync(long id, DateTime appointmentDate, string appointmentTime, string? description, long processedBy, CancellationToken ct);
    Task<bool> UpdateVfsRefAsync(long id, long processedBy, CancellationToken ct);
    Task<bool> UpdateVfsStatusAsync(long id, string status, ApplicationFile? vfsDoc, ApplicationFile? verifiedReceipt, ApplicationFile? rejectedReceipt, long processedBy, CancellationToken ct);

    Task<bool> ProcessHolidayAsync(long id, List<GovtFeeServiceInput> govtFees, long processedBy, CancellationToken ct);
    Task<bool> UpdateHolidayStatusAsync(long id, string status, long processedBy, CancellationToken ct);
    Task<bool> UpdateHolidayRefAsync(long id, long processedBy, CancellationToken ct);
    Task<bool> UploadHolidayDocsAsync(long id, List<KeyValuePair<string, ApplicationFile>> documents, CancellationToken ct);

    Task<bool> UpdateReceiptPaymentStatusAsync(long id, string status, CancellationToken ct);
}

public sealed class OperationsWriteService : IOperationsWriteService
{
    private readonly ISalesPortalDbContext _db;
    private readonly IOperationalQueueNotificationService _operationalQueueNotificationService;
    private readonly ISupplierCostReconciliationService _reconciliationService;
    private readonly ILedgerNotificationService _ledgerNotificationService;
    private readonly IWalletNotificationService _walletNotificationService;
    private readonly TexasPortal.Application.Sales.Profit.ISalesmanProfitService _profitService;
    private readonly IAgentsApiClient _agentsApiClient;
    private readonly TexasPortal.Application.Sales.Accounts.ICustomerAccountCommandService _customerAccountCommandService;
    private readonly IOutstandingNotificationService _outstandingNotificationService;

    public OperationsWriteService(
        ISalesPortalDbContext db,
        IOperationalQueueNotificationService operationalQueueNotificationService,
        ISupplierCostReconciliationService reconciliationService,
        ILedgerNotificationService ledgerNotificationService,
        IWalletNotificationService walletNotificationService,
        TexasPortal.Application.Sales.Profit.ISalesmanProfitService profitService,
        IAgentsApiClient agentsApiClient,
        TexasPortal.Application.Sales.Accounts.ICustomerAccountCommandService customerAccountCommandService,
        IOutstandingNotificationService outstandingNotificationService)
    {
        _db = db;
        _operationalQueueNotificationService = operationalQueueNotificationService;
        _reconciliationService = reconciliationService;
        _ledgerNotificationService = ledgerNotificationService;
        _walletNotificationService = walletNotificationService;
        _profitService = profitService;
        _agentsApiClient = agentsApiClient;
        _customerAccountCommandService = customerAccountCommandService;
        _outstandingNotificationService = outstandingNotificationService;
    }

    private async Task<string?> SaveFileAsync(ApplicationFile? file, string folder, CancellationToken ct)
    {
        if (file == null || file.Stream == null) return null;

        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadsFolder = Path.Combine(webRoot, "uploads", folder);
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.Stream.CopyToAsync(stream, ct);
        }

        return $"/uploads/{folder}/{fileName}";
    }

    public async Task<bool> UpdateVisaIdAsync(long firstApplicantId, string visaAppId, List<GovtFeeServiceInput> govtFees, long processedBy, CancellationToken ct)
    {
        var applicant = await _db.SalesVisaApplicants.FirstOrDefaultAsync(x => x.Id == firstApplicantId, ct);
        if (applicant == null) return false;

        var booking = await _db.SalesVisaApplications.FirstOrDefaultAsync(x => x.Id == applicant.SalesVisaApplicationId, ct);
        if (booking == null) return false;

        var allApplicants = await _db.SalesVisaApplicants
            .Where(x => x.SalesVisaApplicationId == booking.Id)
            .ToListAsync(ct);

        foreach (var app in allApplicants)
        {
            app.VisaAppId = visaAppId;
            app.GovtFeeReference = visaAppId;
            app.ProcessedBy = processedBy;
            app.UpdatedAt = DateTime.UtcNow;
        }

        if (!string.Equals(booking.ProcessingStatus, "approved", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(booking.ProcessingStatus, "completed", StringComparison.OrdinalIgnoreCase))
        {
            booking.ProcessingStatus = "under review";
        }
        booking.UpdatedAt = DateTime.UtcNow;

        foreach (var fee in govtFees)
        {
            var path = await SaveFileAsync(fee.PaymentProof, "govt_fee_proofs", ct);
            _db.SalesVisaGovtFees.Add(new SalesVisaGovtFee
            {
                SalesVisaApplicantId = firstApplicantId,
                Amount = fee.Amount,
                Description = fee.GovtFeeDescription,
                PaymentReference = fee.TransactionNo,
                Status = "recorded",
                PaymentProof = path ?? fee.ExistingPaymentProof,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _operationalQueueNotificationService.PublishOperationalQueueChangedAsync(
            referenceType: ReferenceTypes.Visa,
            referenceId: booking.Id.ToString(),
            queueType: "visa",
            includeOperationsRole: true,
            cancellationToken: ct);

        await _db.SaveChangesAsync(ct);

        await TexasPortal.Application.Sales.Notifications.NotificationHelper.SendHeadEmailNotificationAsync(
            _db, booking.HeadId, booking.AgentId, "visa_application", booking.Id, "under review", booking.PayableAmount,
            $"Visa App ID/Ref assigned: {visaAppId} ({booking.VisaPackName})", ct, booking.DocumentNumber, isCreation: false);

        return true;
    }

    public async Task<bool> EditVisaIdAsync(long firstApplicantId, string visaAppId, long processedBy, CancellationToken ct)
    {
        var applicant = await _db.SalesVisaApplicants.FirstOrDefaultAsync(x => x.Id == firstApplicantId, ct);
        if (applicant == null) return false;

        var allApplicants = await _db.SalesVisaApplicants
            .Where(x => x.SalesVisaApplicationId == applicant.SalesVisaApplicationId)
            .ToListAsync(ct);

        foreach (var app in allApplicants)
        {
            app.VisaAppId = visaAppId;
            app.GovtFeeReference = visaAppId;
            app.ProcessedBy = processedBy;
            app.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ApproveVisaAsync(long id, ApplicationFile? visaDoc, string? note, long processedBy, CancellationToken ct)
    {
        var booking = await _db.SalesVisaApplications.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking == null) return false;

        var path = await SaveFileAsync(visaDoc, "visa_documents", ct);

        booking.ProcessingStatus = "approved";
        booking.VisaDocument = path;
        booking.ApprovalNote = note;
        booking.UpdatedAt = DateTime.UtcNow;

        await _operationalQueueNotificationService.PublishOperationalQueueChangedAsync(
            referenceType: ReferenceTypes.Visa,
            referenceId: booking.Id.ToString(),
            queueType: "visa",
            includeOperationsRole: true,
            cancellationToken: ct);

        await _db.SaveChangesAsync(ct);

        await TexasPortal.Application.Sales.Notifications.NotificationHelper.CreateStatusChangedNotificationAsync(
            _db, booking.UserId, "visa_application", id, "approved", booking.PayableAmount,
            $"{booking.VisaPackName}", ct, booking.DocumentNumber);

        return true;
    }

    public async Task<RejectVisaResult> RejectVisaAsync(
        long id,
        ApplicationFile? rejectionDoc,
        string note,
        string? refundNote,
        decimal rejectionFee,
        decimal? payableAmount,
        long processedBy,
        CancellationToken ct)
    {
        var booking = await _db.SalesVisaApplications
            .Include(v => v.Applicants)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (booking == null)
        {
            return new RejectVisaResult(false, id, "not_found", 0m, 0m, rejectionFee, 0m, 0m, 0m, "Visa application not found.");
        }

        bool isAgent = string.Equals(booking.ClientType, "agent", StringComparison.OrdinalIgnoreCase) ||
                       (!booking.HeadId.HasValue && booking.AgentId.HasValue);

        Head? head = null;
        if (booking.HeadId.HasValue)
        {
            head = await _db.Heads.FirstOrDefaultAsync(h => h.Id == booking.HeadId.Value, ct);
        }

        if (payableAmount.HasValue && payableAmount.Value >= 0m)
        {
            booking.PayableAmount = payableAmount.Value;
        }

        var applicantIds = booking.Applicants.Select(a => a.Id).ToList();
        var localFees = await _db.SalesVisaGovtFees
            .Where(g => applicantIds.Contains(g.SalesVisaApplicantId))
            .ToListAsync(ct);

        var reconciledFeesInput = localFees.Select(f => new ReconciledFeeItem(
            f.Id, f.Amount, f.Description, f.PaymentReference, f.Status
        )).ToList();

        try
        {
            await _reconciliationService.ReconcileVisaFeesAsync(booking.Id, reconciledFeesInput, ct);
            localFees = await _db.SalesVisaGovtFees
                .Where(g => applicantIds.Contains(g.SalesVisaApplicantId))
                .ToListAsync(ct);
        }
        catch
        {
            // Fall back to existing local fees if external call is unavailable
        }

        decimal actuallyPaidGovtFees = localFees
            .Where(g => string.Equals(g.Status, "verified", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(g.Status, "paid", StringComparison.OrdinalIgnoreCase) ||
                        g.AccountsPaymentId != null)
            .Sum(g => g.Amount);

        decimal balanceAdjustment = Math.Max(0m, booking.PayableAmount - actuallyPaidGovtFees - rejectionFee);

        decimal previousBalance = head?.Balance ?? 0m;
        if (isAgent && booking.AgentId.HasValue)
        {
            try
            {
                var agentInfo = await _agentsApiClient.GetAgentByIdAsync(booking.AgentId.Value, ct);
                if (agentInfo != null)
                {
                    previousBalance = agentInfo.Wallet;
                }
            }
            catch
            {
                // Fall back gracefully if accounts portal lookup unavailable
            }
        }

        if (string.Equals(booking.ProcessingStatus, "rejected", StringComparison.OrdinalIgnoreCase))
        {
            return new RejectVisaResult(
                Success: true,
                VisaId: id,
                Status: "rejected",
                PayableAmount: booking.PayableAmount,
                GovernmentFeesPaid: actuallyPaidGovtFees,
                RejectionFee: booking.RejectionFee,
                BalanceAdjustment: 0m,
                PreviousBalance: previousBalance,
                NewBalance: previousBalance
            );
        }

        if (!isAgent && booking.HeadId.HasValue && head == null)
        {
            return new RejectVisaResult(false, id, booking.ProcessingStatus, booking.PayableAmount, actuallyPaidGovtFees, rejectionFee, 0m, 0m, 0m, "Associated Customer Head account not found.");
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var path = await SaveFileAsync(rejectionDoc, "rejection_documents", ct);

                if (head != null && balanceAdjustment > 0m)
                {
                    await _customerAccountCommandService.ProcessRefundAsync(
                        head.Id,
                        balanceAdjustment,
                        booking.DocumentNumber,
                        processedBy,
                        $"Refund for visa application {booking.DocumentNumber}. Payable: {booking.PayableAmount:N3} BHD. Government/Supplier Fees Paid: {actuallyPaidGovtFees:N3} BHD. Rejection Fee: {rejectionFee:N3} BHD. Balance Adjustment: {balanceAdjustment:N3} BHD.",
                        ct: ct);

                    var ledger = new HeadLedger
                    {
                        HeadId = head.Id,
                        ServiceType = "visa_application_rejection",
                        ServiceId = booking.Id,
                        Debit = 0m,
                        Credit = balanceAdjustment,
                        Description = $"Refund for visa application {booking.DocumentNumber}. Payable: {booking.PayableAmount:N3} BHD. Government/Supplier Fees Paid: {actuallyPaidGovtFees:N3} BHD. Rejection Fee: {rejectionFee:N3} BHD. Balance Adjustment: {balanceAdjustment:N3} BHD.",
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.HeadLedgers.Add(ledger);
                }

                booking.ProcessingStatus = "rejected";
                if (!string.IsNullOrEmpty(path)) booking.RejectionDocument = path;
                booking.RejectionNote = note;
                booking.RefundNote = refundNote;
                booking.RejectionFee = rejectionFee;
                booking.ExpectedSupplierCost = actuallyPaidGovtFees;
                booking.Profit = rejectionFee;
                booking.UpdatedAt = DateTime.UtcNow;

                if (booking.Applicants != null && booking.Applicants.Count > 0)
                {
                    decimal appCost = actuallyPaidGovtFees / booking.Applicants.Count;
                    decimal appProfit = rejectionFee / booking.Applicants.Count;
                    foreach (var app in booking.Applicants)
                    {
                        app.ExpectedSupplierCost = appCost;
                        app.Profit = appProfit;
                    }
                }

                await _db.SaveChangesAsync(ct);
                await _profitService.RecordRejectionProfitAsync(booking.Id, rejectionFee, ct);
                await tx.CommitAsync(ct);

                decimal newBalance = previousBalance;

                if (head != null && balanceAdjustment > 0m)
                {
                    newBalance = head.Balance;

                    await _ledgerNotificationService.PublishLedgerChangedAsync(
                        referenceType: ReferenceTypes.Visa,
                        referenceId: booking.Id.ToString(),
                        headId: head.Id,
                        cancellationToken: ct);

                    await _walletNotificationService.PublishWalletChangedAsync(
                        referenceType: ReferenceTypes.Visa,
                        referenceId: booking.Id.ToString(),
                        ownerId: head.Id,
                        ownerType: head.Type,
                        cancellationToken: ct);

                    await _outstandingNotificationService.PublishOutstandingChangedAsync(
                        referenceType: ReferenceTypes.Visa,
                        referenceId: booking.Id.ToString(),
                        ownerId: head.Id,
                        ownerType: "customer",
                        cancellationToken: ct);
                }
                else if (isAgent && booking.AgentId.HasValue && balanceAdjustment > 0m)
                {
                    try
                    {
                        await _agentsApiClient.CreditAgentWalletAsync(
                            booking.AgentId.Value,
                            balanceAdjustment,
                            $"Refund for visa application {booking.DocumentNumber}. Payable: {booking.PayableAmount:N3} BHD. Government/Supplier Fees Paid: {actuallyPaidGovtFees:N3} BHD. Rejection Fee: {rejectionFee:N3} BHD. Balance Adjustment: {balanceAdjustment:N3} BHD.",
                            ct);
                        newBalance = previousBalance + balanceAdjustment;
                    }
                    catch
                    {
                        // Credit wallet call non-blocking to local booking status update
                    }

                    await _walletNotificationService.PublishWalletChangedAsync(
                        referenceType: ReferenceTypes.Visa,
                        referenceId: booking.Id.ToString(),
                        ownerId: booking.AgentId.Value,
                        ownerType: "agent",
                        cancellationToken: ct);
                }

                await _operationalQueueNotificationService.PublishOperationalQueueChangedAsync(
                    referenceType: ReferenceTypes.Visa,
                    referenceId: booking.Id.ToString(),
                    queueType: "visa",
                    includeOperationsRole: true,
                    cancellationToken: ct);

                await TexasPortal.Application.Sales.Notifications.NotificationHelper.CreateStatusChangedNotificationAsync(
                    _db, booking.UserId, "visa_application", id, "rejected", booking.PayableAmount,
                    $"{booking.VisaPackName}", ct, booking.DocumentNumber);

                return new RejectVisaResult(
                    Success: true,
                    VisaId: id,
                    Status: "rejected",
                    PayableAmount: booking.PayableAmount,
                    GovernmentFeesPaid: actuallyPaidGovtFees,
                    RejectionFee: rejectionFee,
                    BalanceAdjustment: balanceAdjustment,
                    PreviousBalance: previousBalance,
                    NewBalance: newBalance
                );
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                return new RejectVisaResult(false, id, booking.ProcessingStatus, booking.PayableAmount, actuallyPaidGovtFees, rejectionFee, 0m, previousBalance, previousBalance, ex.Message);
            }
        });
    }

    public async Task<bool> ProcessVfsAsync(long id, List<GovtFeeServiceInput> govtFees, long processedBy, CancellationToken ct)
    {
        var booking = await _db.VfsAppointments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking == null) return false;

        if (!string.Equals(booking.ProcessingStatus, "approved", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(booking.ProcessingStatus, "completed", StringComparison.OrdinalIgnoreCase))
        {
            booking.ProcessingStatus = "under review";
        }
        booking.UpdatedAt = DateTime.UtcNow;

        foreach (var fee in govtFees)
        {
            var path = await SaveFileAsync(fee.PaymentProof, "govt_fee_proofs", ct);
            _db.VfsAppointmentGovtFees.Add(new VfsAppointmentGovtFee
            {
                VfsAppointmentId = id,
                Amount = fee.Amount,
                PaymentReference = fee.TransactionNo,
                Status = "recorded",
                PaymentProof = path ?? fee.ExistingPaymentProof,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _operationalQueueNotificationService.PublishOperationalQueueChangedAsync(
            referenceType: ReferenceTypes.Vfs,
            referenceId: booking.Id.ToString(),
            queueType: "vfs",
            includeOperationsRole: true,
            cancellationToken: ct);

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateVfsScheduleAsync(long id, DateTime appointmentDate, string appointmentTime, string? description, long processedBy, CancellationToken ct)
    {
        var booking = await _db.VfsAppointments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking == null) return false;

        booking.AppointmentDate = appointmentDate;
        if (TimeSpan.TryParse(appointmentTime, out var parsedTime))
        {
            booking.AppointmentTime = parsedTime;
        }
        booking.VfsReceipt = description;
        booking.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await TexasPortal.Application.Sales.Notifications.NotificationHelper.SendHeadEmailNotificationAsync(
            _db, booking.HeadId, booking.AgentId, "vfs_appointment", booking.Id, "scheduled", booking.PayableAmount,
            $"Appointment Scheduled: {appointmentDate:yyyy-MM-dd} {appointmentTime}. {description}", ct, booking.DocumentNumber, isCreation: false);

        return true;
    }

    public async Task<bool> UpdateVfsRefAsync(long id, long processedBy, CancellationToken ct)
    {
        var booking = await _db.VfsAppointments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking == null) return false;

        booking.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateVfsStatusAsync(long id, string status, ApplicationFile? vfsDoc, ApplicationFile? verifiedReceipt, ApplicationFile? rejectedReceipt, long processedBy, CancellationToken ct)
    {
        var booking = await _db.VfsAppointments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking == null) return false;

        var normalizedStatus = status.ToLower();
        booking.ProcessingStatus = normalizedStatus;

        if (normalizedStatus == "completed")
        {
            var vfsDocPath = await SaveFileAsync(vfsDoc, "vfs_documents", ct);
            var receiptPath = await SaveFileAsync(verifiedReceipt, "verified_receipts", ct);
            
            booking.PassportCopy = vfsDocPath;
            booking.VfsReceipt = receiptPath;
        }
        else if (normalizedStatus == "cancelled")
        {
            var cancelReceiptPath = await SaveFileAsync(rejectedReceipt, "rejected_receipts", ct);
            booking.VfsReceipt = cancelReceiptPath;
        }

        booking.UpdatedAt = DateTime.UtcNow;
        await _operationalQueueNotificationService.PublishOperationalQueueChangedAsync(
            referenceType: ReferenceTypes.Vfs,
            referenceId: booking.Id.ToString(),
            queueType: "vfs",
            includeOperationsRole: true,
            cancellationToken: ct);

        await _db.SaveChangesAsync(ct);

        await TexasPortal.Application.Sales.Notifications.NotificationHelper.CreateStatusChangedNotificationAsync(
            _db, booking.CreatedBy, "vfs_appointment", id, normalizedStatus, booking.PayableAmount,
            $"{booking.Name} - {booking.Country}", ct, booking.DocumentNumber);

        return true;
    }

    public async Task<bool> ProcessHolidayAsync(long id, List<GovtFeeServiceInput> govtFees, long processedBy, CancellationToken ct)
    {
        var booking = await _db.HolidayPackages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking == null) return false;

        if (!string.Equals(booking.ProcessingStatus, "approved", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(booking.ProcessingStatus, "completed", StringComparison.OrdinalIgnoreCase))
        {
            booking.ProcessingStatus = "under review";
        }
        booking.UpdatedAt = DateTime.UtcNow;

        foreach (var fee in govtFees)
        {
            var path = await SaveFileAsync(fee.PaymentProof, "govt_fee_proofs", ct);
            _db.HolidayPackageGovtFees.Add(new HolidayPackageGovtFee
            {
                HolidayPackageId = id,
                Amount = fee.Amount,
                PaymentReference = fee.TransactionNo,
                Status = "recorded",
                PaymentProof = path ?? fee.ExistingPaymentProof,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _operationalQueueNotificationService.PublishOperationalQueueChangedAsync(
            referenceType: ReferenceTypes.Holiday,
            referenceId: booking.Id.ToString(),
            queueType: "holiday",
            includeOperationsRole: true,
            cancellationToken: ct);

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateHolidayStatusAsync(long id, string status, long processedBy, CancellationToken ct)
    {
        var booking = await _db.HolidayPackages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking == null) return false;

        var normalizedStatus = status.ToLower();
        booking.ProcessingStatus = normalizedStatus;
        booking.UpdatedAt = DateTime.UtcNow;

        await _operationalQueueNotificationService.PublishOperationalQueueChangedAsync(
            referenceType: ReferenceTypes.Holiday,
            referenceId: booking.Id.ToString(),
            queueType: "holiday",
            includeOperationsRole: true,
            cancellationToken: ct);

        await _db.SaveChangesAsync(ct);

        await TexasPortal.Application.Sales.Notifications.NotificationHelper.CreateStatusChangedNotificationAsync(
            _db, booking.CreatedBy, "holiday_package", id, normalizedStatus, booking.PayableAmount,
            $"{booking.PassengerName} - {booking.Country}", ct, booking.DocumentNumber);

        return true;
    }

    public async Task<bool> UpdateHolidayRefAsync(long id, long processedBy, CancellationToken ct)
    {
        var booking = await _db.HolidayPackages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking == null) return false;

        booking.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UploadHolidayDocsAsync(long id, List<KeyValuePair<string, ApplicationFile>> documents, CancellationToken ct)
    {
        var booking = await _db.HolidayPackages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (booking == null) return false;

        foreach (var doc in documents)
        {
            var path = await SaveFileAsync(doc.Value, "holiday_documents", ct);
            if (path != null)
            {
                _db.HolidayPackageDocuments.Add(new HolidayPackageDocument
                {
                    HolidayPackageId = id,
                    DocumentPath = path,
                    DocumentType = doc.Key,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateReceiptPaymentStatusAsync(long id, string status, CancellationToken ct)
    {
        var receipt = await _db.Receipts
            .Include(x => x.Head)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (receipt == null) return false;

        if (receipt.Status.Equals("success", StringComparison.OrdinalIgnoreCase))
            return true;

        var oldStatus = receipt.Status;
        receipt.Status = status;
        receipt.UpdatedAt = DateTime.UtcNow;

        if (string.Equals(status, "success", StringComparison.OrdinalIgnoreCase)
            && receipt.HeadId.HasValue
            && receipt.Head != null)
        {
            _db.HeadLedgers.Add(new HeadLedger
            {
                HeadId = receipt.HeadId.Value,
                ServiceType = receipt.Service,
                ServiceId = receipt.Id,
                Description = $"Payment approved via Receipt {receipt.ReceiptId}: {receipt.Narration}",
                Debit = 0,
                Credit = receipt.Amount,
                CreatedAt = DateTime.UtcNow
            });

            await ReceiptAllocationHelper.AllocateReceiptFundsAsync(receipt, receipt.Amount, _db);
        }

        await _db.SaveChangesAsync(ct);

        if (!string.Equals(oldStatus, status, StringComparison.OrdinalIgnoreCase) && receipt.CreatedBy.HasValue)
        {
            await TexasPortal.Application.Sales.Notifications.NotificationHelper.CreateStatusChangedNotificationAsync(
                _db, receipt.CreatedBy.Value, "receipt", id, status, receipt.Amount,
                $"{receipt.Narration}", ct, receipt.DocumentNumber ?? receipt.ReceiptId);
        }

        return true;
    }
}

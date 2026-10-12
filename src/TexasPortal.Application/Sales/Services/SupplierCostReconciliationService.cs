using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TexasPortal.Application.Persistence;
using TexasPortal.Application.Sales.Queries;
using TexasPortal.Domain.Entities;

namespace TexasPortal.Application.Sales.Services;

public class SupplierCostReconciliationService : ISupplierCostReconciliationService
{
    private readonly ISalesPortalDbContext _db;
    private readonly IAgentsApiClient _agentsApiClient;
    private readonly ILogger<SupplierCostReconciliationService> _logger;

    public SupplierCostReconciliationService(
        ISalesPortalDbContext db,
        IAgentsApiClient agentsApiClient,
        ILogger<SupplierCostReconciliationService> logger)
    {
        _db = db;
        _agentsApiClient = agentsApiClient;
        _logger = logger;
    }

    public async Task<SupplierCostReconciliationResult> ReconcileVisaFeesAsync(
        long visaApplicationId,
        List<ReconciledFeeItem> localFees,
        CancellationToken ct = default)
    {
        var extRef = visaApplicationId.ToString();
        var approvedSummary = await _agentsApiClient.GetBookingSupplierPaymentsAsync("Visa", extRef, ct);

        if (approvedSummary == null || approvedSummary.Payments == null || approvedSummary.Payments.Count == 0)
        {
            var fallbackCost = localFees.Count > 0 ? localFees.Sum(f => f.Amount) : 0m;
            return new SupplierCostReconciliationResult("Visa", extRef, 0m, fallbackCost, localFees);
        }

        var approvedPayments = approvedSummary.Payments;
        var approvedTotal = approvedSummary.ApprovedTotal;

        var applicantIds = await _db.SalesVisaApplicants
            .Where(a => a.SalesVisaApplicationId == visaApplicationId)
            .Select(a => a.Id)
            .ToListAsync(ct);

        if (applicantIds.Count > 0)
        {
            var firstApplicantId = applicantIds.First();
            var dbFees = await _db.SalesVisaGovtFees
                .Where(g => applicantIds.Contains(g.SalesVisaApplicantId))
                .OrderBy(g => g.CreatedAt)
                .ToListAsync(ct);

            bool modified = false;

            foreach (var ap in approvedPayments)
            {
                // PRIMARY: match by AccountsPaymentId (unique, collision-proof)
                var matched = dbFees.FirstOrDefault(f => f.AccountsPaymentId == ap.Id)
                    // FALLBACK: unlinked placeholder row
                    ?? dbFees.FirstOrDefault(f => f.AccountsPaymentId == null && f.Status == "recorded");

                if (matched != null)
                {
                    if (matched.AccountsPaymentId != ap.Id || matched.Amount != ap.Amount || matched.Status != "verified")
                    {
                        matched.AccountsPaymentId = ap.Id;
                        matched.Amount = ap.Amount;
                        matched.Status = "verified";
                        if (!string.IsNullOrEmpty(ap.Description)) matched.Description = ap.Description;
                        matched.UpdatedAt = DateTime.UtcNow;
                        modified = true;
                    }
                }
                else
                {
                    _db.SalesVisaGovtFees.Add(new SalesVisaGovtFee
                    {
                        SalesVisaApplicantId = firstApplicantId,
                        AccountsPaymentId = ap.Id,
                        Amount = ap.Amount,
                        Description = ap.Description ?? "Government Fee",
                        Status = "verified",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                    modified = true;
                }
            }



            if (modified)
            {
                var booking = await _db.SalesVisaApplications.FirstOrDefaultAsync(b => b.Id == visaApplicationId, ct);
                await _db.SaveChangesAsync(ct);

                if (booking != null)
                {
                    var newTotal = await _db.SalesVisaGovtFees
                        .Where(g => applicantIds.Contains(g.SalesVisaApplicantId))
                        .SumAsync(g => (decimal?)g.Amount, ct) ?? 0m;
                    booking.ExpectedSupplierCost = newTotal;
                    booking.Profit = booking.PayableAmount - newTotal;
                    await _db.SaveChangesAsync(ct);
                }

                _logger.LogInformation("Auto-healed SalesVisaGovtFees for Visa #{VisaId}. ApprovedTotal={ApprovedTotal}", visaApplicationId, approvedTotal);
            }

            var finalFees = await _db.SalesVisaGovtFees
                .Where(g => applicantIds.Contains(g.SalesVisaApplicantId))
                .ToListAsync(ct);
            localFees = finalFees.Select(g => new ReconciledFeeItem(
                g.Id, g.Amount, g.Description, g.PaymentReference, g.Status
            )).ToList();
        }

        var effectiveCost = localFees.Count > 0 ? localFees.Sum(f => f.Amount) : approvedTotal;
        return new SupplierCostReconciliationResult("Visa", extRef, approvedTotal, effectiveCost, localFees);
    }

    public async Task<SupplierCostReconciliationResult> ReconcileVfsFeesAsync(
        long vfsAppointmentId,
        List<ReconciledFeeItem> localFees,
        CancellationToken ct = default)
    {
        var extRef = vfsAppointmentId.ToString();
        var approvedSummary = await _agentsApiClient.GetBookingSupplierPaymentsAsync("VFS", extRef, ct);

        if (approvedSummary == null || approvedSummary.Payments == null || approvedSummary.Payments.Count == 0)
        {
            var fallbackCost = localFees.Count > 0 ? localFees.Sum(f => f.Amount) : 0m;
            return new SupplierCostReconciliationResult("VFS", extRef, 0m, fallbackCost, localFees);
        }

        var approvedPayments = approvedSummary.Payments;
        var approvedTotal = approvedSummary.ApprovedTotal;

        var dbFees = await _db.VfsAppointmentGovtFees
            .Where(g => g.VfsAppointmentId == vfsAppointmentId)
            .OrderBy(g => g.CreatedAt)
            .ToListAsync(ct);

        bool modified = false;

        foreach (var ap in approvedPayments)
        {
            var matched = dbFees.FirstOrDefault(f => f.AccountsPaymentId == ap.Id)
                ?? dbFees.FirstOrDefault(f => f.AccountsPaymentId == null && f.Status == "recorded");

            if (matched != null)
            {
                if (matched.AccountsPaymentId != ap.Id || matched.Amount != ap.Amount || matched.Status != "verified")
                {
                    matched.AccountsPaymentId = ap.Id;
                    matched.Amount = ap.Amount;
                    matched.Status = "verified";
                    matched.UpdatedAt = DateTime.UtcNow;
                    modified = true;
                }
            }
            else
            {
                _db.VfsAppointmentGovtFees.Add(new VfsAppointmentGovtFee
                {
                    VfsAppointmentId = vfsAppointmentId,
                    AccountsPaymentId = ap.Id,
                    Amount = ap.Amount,
                    Status = "verified",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                modified = true;
            }
        }



        if (modified)
        {
            var booking = await _db.VfsAppointments.FirstOrDefaultAsync(b => b.Id == vfsAppointmentId, ct);
            await _db.SaveChangesAsync(ct);

            if (booking != null)
            {
                var newTotal = await _db.VfsAppointmentGovtFees
                    .Where(g => g.VfsAppointmentId == vfsAppointmentId)
                    .SumAsync(g => (decimal?)g.Amount, ct) ?? 0m;
                booking.ExpectedSupplierCost = newTotal;
                booking.Profit = booking.PayableAmount - newTotal;
                await _db.SaveChangesAsync(ct);
            }
        }

        var finalFees = await _db.VfsAppointmentGovtFees
            .Where(g => g.VfsAppointmentId == vfsAppointmentId)
            .ToListAsync(ct);
        localFees = finalFees.Select(g => new ReconciledFeeItem(
            g.Id, g.Amount, "Government Fee", g.PaymentReference, g.Status
        )).ToList();

        var effectiveCost = localFees.Count > 0 ? localFees.Sum(f => f.Amount) : approvedTotal;
        return new SupplierCostReconciliationResult("VFS", extRef, approvedTotal, effectiveCost, localFees);
    }

    public async Task<SupplierCostReconciliationResult> ReconcileHolidayFeesAsync(
        long holidayPackageId,
        List<ReconciledFeeItem> localFees,
        CancellationToken ct = default)
    {
        var extRef = holidayPackageId.ToString();
        var approvedSummary = await _agentsApiClient.GetBookingSupplierPaymentsAsync("Holiday", extRef, ct);

        if (approvedSummary == null || approvedSummary.Payments == null || approvedSummary.Payments.Count == 0)
        {
            var fallbackCost = localFees.Count > 0 ? localFees.Sum(f => f.Amount) : 0m;
            return new SupplierCostReconciliationResult("Holiday", extRef, 0m, fallbackCost, localFees);
        }

        var approvedPayments = approvedSummary.Payments;
        var approvedTotal = approvedSummary.ApprovedTotal;

        var dbFees = await _db.HolidayPackageGovtFees
            .Where(g => g.HolidayPackageId == holidayPackageId)
            .OrderBy(g => g.CreatedAt)
            .ToListAsync(ct);

        bool modified = false;

        foreach (var ap in approvedPayments)
        {
            var matched = dbFees.FirstOrDefault(f => f.AccountsPaymentId == ap.Id)
                ?? dbFees.FirstOrDefault(f => f.AccountsPaymentId == null && f.Status == "recorded");

            if (matched != null)
            {
                if (matched.AccountsPaymentId != ap.Id || matched.Amount != ap.Amount || matched.Status != "verified")
                {
                    matched.AccountsPaymentId = ap.Id;
                    matched.Amount = ap.Amount;
                    matched.Status = "verified";
                    if (!string.IsNullOrEmpty(ap.Description)) matched.Description = ap.Description;
                    matched.UpdatedAt = DateTime.UtcNow;
                    modified = true;
                }
            }
            else
            {
                _db.HolidayPackageGovtFees.Add(new HolidayPackageGovtFee
                {
                    HolidayPackageId = holidayPackageId,
                    AccountsPaymentId = ap.Id,
                    Amount = ap.Amount,
                    Description = ap.Description ?? "Government Fee",
                    Status = "verified",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                modified = true;
            }
        }



        if (modified)
        {
            var booking = await _db.HolidayPackages.FirstOrDefaultAsync(b => b.Id == holidayPackageId, ct);
            await _db.SaveChangesAsync(ct);

            if (booking != null)
            {
                var newTotal = await _db.HolidayPackageGovtFees
                    .Where(g => g.HolidayPackageId == holidayPackageId)
                    .SumAsync(g => (decimal?)g.Amount, ct) ?? 0m;
                booking.ExpectedSupplierCost = newTotal;
                booking.Profit = booking.PayableAmount - newTotal;
                await _db.SaveChangesAsync(ct);
            }
        }

        var finalFees = await _db.HolidayPackageGovtFees
            .Where(g => g.HolidayPackageId == holidayPackageId)
            .ToListAsync(ct);
        localFees = finalFees.Select(g => new ReconciledFeeItem(
            g.Id, g.Amount, g.Description, g.PaymentReference, g.Status
        )).ToList();

        var effectiveCost = localFees.Count > 0 ? localFees.Sum(f => f.Amount) : approvedTotal;
        return new SupplierCostReconciliationResult("Holiday", extRef, approvedTotal, effectiveCost, localFees);
    }
}

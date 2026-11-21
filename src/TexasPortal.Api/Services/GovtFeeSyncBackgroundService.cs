using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TexasPortal.Application.Persistence;
using TexasPortal.Application.Sales.Queries;
using TexasPortal.Application.Sales.Services;
using TexasPortal.Domain.Entities;

namespace TexasPortal.Api.Services;

public sealed class GovtFeeSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GovtFeeSyncBackgroundService> _logger;
    private readonly TimeSpan _checkInterval;

    public GovtFeeSyncBackgroundService(
        IServiceProvider serviceProvider,
        IConfiguration config,
        ILogger<GovtFeeSyncBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var intervalMinutes = config.GetValue<int?>("GovtFeeSyncWorker:IntervalMinutes") ?? 10;
        _checkInterval = TimeSpan.FromMinutes(Math.Max(1, intervalMinutes));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Govt Fee Sync Background Service started. Check interval: {IntervalMinutes} minutes.", _checkInterval.TotalMinutes);

        // Allow application startup to settle before initial sweep
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncUnlinkedGovtFeesAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error occurred during govt fee synchronization sweep.");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Govt Fee Sync Background Service stopped.");
    }

    private async Task SyncUnlinkedGovtFeesAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISalesPortalDbContext>();
        var agentsApiClient = scope.ServiceProvider.GetRequiredService<IAgentsApiClient>();
        var reconciliationService = scope.ServiceProvider.GetService<ISupplierCostReconciliationService>();

        await SyncVisaGovtFeesAsync(db, agentsApiClient, reconciliationService, ct);
        await SyncVfsGovtFeesAsync(db, agentsApiClient, reconciliationService, ct);
        await SyncHolidayGovtFeesAsync(db, agentsApiClient, reconciliationService, ct);
    }

    private async Task SyncVisaGovtFeesAsync(
        ISalesPortalDbContext db,
        IAgentsApiClient agentsApiClient,
        ISupplierCostReconciliationService? reconciliationService,
        CancellationToken ct)
    {
        var unlinkedVisaFees = await db.SalesVisaGovtFees
            .Include(f => f.SalesVisaApplicant)
                .ThenInclude(a => a.SalesVisaApplication)
            .Where(f => f.AccountsPaymentId == null && f.Status == "recorded" && f.Amount > 0)
            .OrderBy(f => f.CreatedAt)
            .ToListAsync(ct);

        if (unlinkedVisaFees.Count == 0)
        {
            return;
        }

        _logger.LogInformation("GovtFeeSync: Found {Count} unlinked Visa govt fees to reconcile.", unlinkedVisaFees.Count);

        var groupedByApp = unlinkedVisaFees
            .Where(f => f.SalesVisaApplicant?.SalesVisaApplication != null)
            .GroupBy(f => f.SalesVisaApplicant.SalesVisaApplicationId);

        foreach (var group in groupedByApp)
        {
            var bookingId = group.Key;
            var booking = group.First().SalesVisaApplicant.SalesVisaApplication;
            var applicant = group.First().SalesVisaApplicant;
            var processedBy = applicant.ProcessedBy ?? booking.UserId;

            try
            {
                var applicantIds = await db.SalesVisaApplicants
                    .Where(a => a.SalesVisaApplicationId == bookingId)
                    .Select(a => a.Id)
                    .ToListAsync(ct);

                var allFeesForBooking = await db.SalesVisaGovtFees
                    .Where(g => applicantIds.Contains(g.SalesVisaApplicantId))
                    .ToListAsync(ct);

                var syncItems = allFeesForBooking.Select(f => new GovtFeeSyncItem(
                    f.Amount,
                    f.SupplierId,
                    f.SupplierName,
                    f.PaymentReference,
                    f.Description,
                    f.RowKey
                )).ToList();

                var syncResults = await agentsApiClient.SyncGovtSupplierPaymentsWithDetailsAsync(
                    bookingId.ToString(), "Visa", processedBy, syncItems, ct);

                bool dbModified = false;

                if (syncResults.Count > 0)
                {
                    foreach (var res in syncResults)
                    {
                        if (string.IsNullOrWhiteSpace(res.RowKey)) continue;

                        var matchedFee = allFeesForBooking.FirstOrDefault(f => f.RowKey == res.RowKey);
                        if (matchedFee != null && matchedFee.AccountsPaymentId == null)
                        {
                            matchedFee.AccountsPaymentId = res.PaymentId;
                            matchedFee.UpdatedAt = DateTime.UtcNow;

                            db.GovtFeePaymentLinks.Add(new GovtFeePaymentLink
                            {
                                FeeTable = "sales_visa_govt_fees",
                                RowKey = matchedFee.RowKey,
                                AccountsPaymentId = res.PaymentId,
                                AmountAtLinkTime = matchedFee.Amount,
                                LinkedAt = DateTime.UtcNow,
                                CreatedAt = DateTime.UtcNow
                            });

                            dbModified = true;
                        }
                    }
                }

                if (dbModified)
                {
                    await db.SaveChangesAsync(ct);
                }

                if (reconciliationService != null)
                {
                    var localReconciledItems = allFeesForBooking.Select(g => new ReconciledFeeItem(
                        g.Id, g.Amount, g.Description, g.PaymentReference, g.Status
                    )).ToList();

                    await reconciliationService.ReconcileVisaFeesAsync(bookingId, localReconciledItems, ct);
                }

                _logger.LogInformation("GovtFeeSync: Reconciled Visa booking #{BookingId} ({Count} unlinked fees processed).", bookingId, group.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GovtFeeSync: Failed to reconcile Visa booking #{BookingId}", bookingId);
            }
        }
    }

    private async Task SyncVfsGovtFeesAsync(
        ISalesPortalDbContext db,
        IAgentsApiClient agentsApiClient,
        ISupplierCostReconciliationService? reconciliationService,
        CancellationToken ct)
    {
        var unlinkedVfsFees = await db.VfsAppointmentGovtFees
            .Include(f => f.VfsAppointment)
            .Where(f => f.AccountsPaymentId == null && f.Status == "recorded" && f.Amount > 0)
            .OrderBy(f => f.CreatedAt)
            .ToListAsync(ct);

        if (unlinkedVfsFees.Count == 0) return;

        _logger.LogInformation("GovtFeeSync: Found {Count} unlinked VFS govt fees to reconcile.", unlinkedVfsFees.Count);

        var grouped = unlinkedVfsFees
            .Where(f => f.VfsAppointment != null)
            .GroupBy(f => f.VfsAppointmentId);

        foreach (var group in grouped)
        {
            var bookingId = group.Key;
            var booking = group.First().VfsAppointment;
            var createdBy = booking.CreatedBy;

            try
            {
                var allFees = await db.VfsAppointmentGovtFees
                    .Where(g => g.VfsAppointmentId == bookingId)
                    .ToListAsync(ct);

                var syncItems = allFees.Select(f => new GovtFeeSyncItem(
                    f.Amount, f.SupplierId, f.SupplierName, f.PaymentReference, "VFS Appointment Fee", f.RowKey
                )).ToList();

                var syncResults = await agentsApiClient.SyncGovtSupplierPaymentsWithDetailsAsync(
                    bookingId.ToString(), "VFS", createdBy, syncItems, ct);

                bool dbModified = false;
                if (syncResults.Count > 0)
                {
                    foreach (var res in syncResults)
                    {
                        if (string.IsNullOrWhiteSpace(res.RowKey)) continue;
                        var matchedFee = allFees.FirstOrDefault(f => f.RowKey == res.RowKey);
                        if (matchedFee != null && matchedFee.AccountsPaymentId == null)
                        {
                            matchedFee.AccountsPaymentId = res.PaymentId;
                            matchedFee.UpdatedAt = DateTime.UtcNow;

                            db.GovtFeePaymentLinks.Add(new GovtFeePaymentLink
                            {
                                FeeTable = "vfs_appointment_govt_fees",
                                RowKey = matchedFee.RowKey,
                                AccountsPaymentId = res.PaymentId,
                                AmountAtLinkTime = matchedFee.Amount,
                                LinkedAt = DateTime.UtcNow,
                                CreatedAt = DateTime.UtcNow
                            });
                            dbModified = true;
                        }
                    }
                }

                if (dbModified)
                {
                    await db.SaveChangesAsync(ct);
                }

                if (reconciliationService != null)
                {
                    var localReconciledItems = allFees.Select(g => new ReconciledFeeItem(
                        g.Id, g.Amount, "VFS Appointment Fee", g.PaymentReference, g.Status
                    )).ToList();

                    await reconciliationService.ReconcileVfsFeesAsync(bookingId, localReconciledItems, ct);
                }

                _logger.LogInformation("GovtFeeSync: Reconciled VFS booking #{BookingId}.", bookingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GovtFeeSync: Failed to reconcile VFS booking #{BookingId}", bookingId);
            }
        }
    }

    private async Task SyncHolidayGovtFeesAsync(
        ISalesPortalDbContext db,
        IAgentsApiClient agentsApiClient,
        ISupplierCostReconciliationService? reconciliationService,
        CancellationToken ct)
    {
        var unlinkedFees = await db.HolidayPackageGovtFees
            .Include(f => f.HolidayPackage)
            .Where(f => f.AccountsPaymentId == null && f.Status == "recorded" && f.Amount > 0)
            .OrderBy(f => f.CreatedAt)
            .ToListAsync(ct);

        if (unlinkedFees.Count == 0) return;

        _logger.LogInformation("GovtFeeSync: Found {Count} unlinked Holiday govt fees to reconcile.", unlinkedFees.Count);

        var grouped = unlinkedFees
            .Where(f => f.HolidayPackage != null)
            .GroupBy(f => f.HolidayPackageId);

        foreach (var group in grouped)
        {
            var bookingId = group.Key;
            var booking = group.First().HolidayPackage;
            var createdBy = booking.CreatedBy;

            try
            {
                var allFees = await db.HolidayPackageGovtFees
                    .Where(g => g.HolidayPackageId == bookingId)
                    .ToListAsync(ct);

                var syncItems = allFees.Select(f => new GovtFeeSyncItem(
                    f.Amount, f.SupplierId, f.SupplierName, f.PaymentReference, f.Description ?? "Holiday Package Fee", f.RowKey
                )).ToList();

                var syncResults = await agentsApiClient.SyncGovtSupplierPaymentsWithDetailsAsync(
                    bookingId.ToString(), "Holiday", createdBy, syncItems, ct);

                bool dbModified = false;
                if (syncResults.Count > 0)
                {
                    foreach (var res in syncResults)
                    {
                        if (string.IsNullOrWhiteSpace(res.RowKey)) continue;
                        var matchedFee = allFees.FirstOrDefault(f => f.RowKey == res.RowKey);
                        if (matchedFee != null && matchedFee.AccountsPaymentId == null)
                        {
                            matchedFee.AccountsPaymentId = res.PaymentId;
                            matchedFee.UpdatedAt = DateTime.UtcNow;

                            db.GovtFeePaymentLinks.Add(new GovtFeePaymentLink
                            {
                                FeeTable = "holiday_package_govt_fees",
                                RowKey = matchedFee.RowKey,
                                AccountsPaymentId = res.PaymentId,
                                AmountAtLinkTime = matchedFee.Amount,
                                LinkedAt = DateTime.UtcNow,
                                CreatedAt = DateTime.UtcNow
                            });
                            dbModified = true;
                        }
                    }
                }

                if (dbModified)
                {
                    await db.SaveChangesAsync(ct);
                }

                if (reconciliationService != null)
                {
                    var localReconciledItems = allFees.Select(g => new ReconciledFeeItem(
                        g.Id, g.Amount, g.Description ?? "Holiday Package Fee", g.PaymentReference, g.Status
                    )).ToList();

                    await reconciliationService.ReconcileHolidayFeesAsync(bookingId, localReconciledItems, ct);
                }

                _logger.LogInformation("GovtFeeSync: Reconciled Holiday booking #{BookingId}.", bookingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GovtFeeSync: Failed to reconcile Holiday booking #{BookingId}", bookingId);
            }
        }
    }
}

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
using TexasPortal.Domain.Enums;

namespace TexasPortal.Application.Sales.Profit;

public class SalesmanProfitService : ISalesmanProfitService
{
    private readonly ISalesPortalDbContext _db;
    private readonly IAgentsApiClient _agentsApiClient;
    private readonly ILogger<SalesmanProfitService> _logger;

    public SalesmanProfitService(
        ISalesPortalDbContext db,
        IAgentsApiClient agentsApiClient,
        ILogger<SalesmanProfitService> logger)
    {
        _db = db;
        _agentsApiClient = agentsApiClient;
        _logger = logger;
    }

    public static string NormalizeBookingType(string type)
    {
        var t = (type ?? "").Trim();
        var lower = t.ToLowerInvariant().Replace("_", "").Replace("-", "");
        if (lower.Contains("visa")) return "Visa";
        if (lower.Contains("vfs")) return "VFS";
        if (lower.Contains("holiday")) return "Holiday";
        if (lower.Contains("flight")) return "Flight";
        if (lower.Contains("hotel")) return "Hotel";
        if (lower.Contains("transport")) return "Transport";
        if (lower.Contains("other")) return "Other";
        return t;
    }

    // ─── Centralized Business Profit Calculations ────────────────────────────
    public static decimal CalculateNormalProfit(decimal payableAmount, decimal finalSupplierCost, decimal independentAdjustments = 0m)
    {
        return (payableAmount - finalSupplierCost) + independentAdjustments;
    }

    public static decimal CalculateRejectionProfit(decimal rejectionFee, decimal independentAdjustments = 0m)
    {
        return rejectionFee + independentAdjustments;
    }

    public static decimal CalculateInhouseProfit(decimal independentAdjustments = 0m)
    {
        return 0m + independentAdjustments;
    }

    // ─── Create Estimated Profit Record (at booking creation) ────────────────
    public async Task<SalesmanProfitRecord> CreateEstimatedProfitRecordAsync(
        long salesmanId,
        string bookingType,
        long bookingId,
        string? documentNumber,
        decimal payableAmount,
        decimal estimatedSupplierCost,
        bool isInhouse,
        DateTime bookingCreatedAt,
        CancellationToken ct = default)
    {
        var normType = NormalizeBookingType(bookingType);

        var existing = await _db.SalesmanProfitRecords
            .FirstOrDefaultAsync(r => r.BookingType == normType && r.BookingId == bookingId, ct);

        if (existing != null)
        {
            if (existing.ProfitType == ProfitType.Inhouse && !isInhouse && estimatedSupplierCost > 0m)
            {
                existing.ProfitType = ProfitType.Normal;
                existing.FinalSupplierCost = estimatedSupplierCost;
                var adjSum = await _db.SalesmanProfitAdjustments
                    .Where(a => a.SalesmanProfitRecordId == existing.Id)
                    .SumAsync(a => (decimal?)a.Amount, ct) ?? 0m;
                existing.ProfitAmount = CalculateNormalProfit(payableAmount, estimatedSupplierCost, adjSum);
                existing.SupplierCostSource = SupplierCostSource.Estimated;
                existing.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
            return existing;
        }

        var profitType = isInhouse ? ProfitType.Inhouse : ProfitType.Normal;
        var supplierCostSource = SupplierCostSource.Estimated;
        var finalSupplierCost = isInhouse ? 0m : estimatedSupplierCost;
        var profitAmount = isInhouse ? CalculateInhouseProfit() : CalculateNormalProfit(payableAmount, finalSupplierCost);

        var record = new SalesmanProfitRecord
        {
            SalesmanId = salesmanId,
            BookingType = normType,
            BookingId = bookingId,
            DocumentNumber = documentNumber,
            PayableAmount = payableAmount,
            FinalSupplierCost = finalSupplierCost,
            ProfitAmount = profitAmount,
            ProfitType = profitType,
            SupplierCostSource = supplierCostSource,
            IsFinal = false,
            BookingCreatedAt = bookingCreatedAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.SalesmanProfitRecords.Add(record);
        try
        {
            await _db.SaveChangesAsync(ct);
            await SyncBookingProfitEntityAndUserTotalAsync(normType, record.BookingId, record.ProfitAmount, record.SalesmanId, ct);
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Duplicate profit record insertion prevented for {BookingType} #{BookingId}", normType, bookingId);
            existing = await _db.SalesmanProfitRecords
                .FirstOrDefaultAsync(r => r.BookingType == normType && r.BookingId == bookingId, ct);
            if (existing != null) return existing;
            throw;
        }

        return record;
    }

    // ─── Finalize or Update Profit (from Accounts Portal approved supplier payments & adjustments) ─
    public async Task<SalesmanProfitRecord?> FinalizeOrUpdateProfitAsync(
        string bookingType,
        long bookingId,
        CancellationToken ct = default)
    {
        return await FinalizeOrUpdateProfitAsync(bookingType, bookingId.ToString(), ct);
    }

    public async Task<SalesmanProfitRecord?> FinalizeOrUpdateProfitAsync(
        string bookingType,
        string bookingIdOrRef,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(bookingIdOrRef)) return null;

        var normType = NormalizeBookingType(bookingType);
        long.TryParse(bookingIdOrRef, out var numericId);

        var (salesmanId, realBookingId, docNo, payable, expectedSupplierCost, createdAt, isInhouse, isRejected, rejFee, processingStatus, isDealClosed) = await ResolveBookingInfoAsync(normType, bookingIdOrRef, ct);

        var record = await _db.SalesmanProfitRecords
            .FirstOrDefaultAsync(r => r.BookingType == normType && (r.BookingId == realBookingId || (numericId > 0 && r.BookingId == numericId) || (!string.IsNullOrEmpty(docNo) && r.DocumentNumber == docNo) || r.DocumentNumber == bookingIdOrRef), ct);

        if (salesmanId == 0 && record == null) return null;

        var searchRef = realBookingId > 0 ? realBookingId.ToString() : bookingIdOrRef;
        var typesToTry = new List<string> { normType };
        switch (normType)
        {
            case "Flight": typesToTry.AddRange(["flights", "flight_booking", "flight"]); break;
            case "Hotel": typesToTry.AddRange(["hotels", "hotel_booking", "hotel"]); break;
            case "Transport": typesToTry.AddRange(["transportation", "transport"]); break;
            case "Other": typesToTry.AddRange(["other_service", "other-services", "other"]); break;
            case "VFS": typesToTry.AddRange(["vfs", "vfs_appointment"]); break;
            case "Visa": typesToTry.AddRange(["visa", "visa_application"]); break;
            case "Holiday": typesToTry.AddRange(["holiday", "holiday_package", "holiday-packages"]); break;
        }

        BookingSupplierPaymentSummary? approvedSummary = null;
        var refsToTry = new List<string>();
        if (!string.IsNullOrEmpty(searchRef)) refsToTry.Add(searchRef);
        if (!string.IsNullOrEmpty(docNo) && !refsToTry.Contains(docNo)) refsToTry.Add(docNo);
        if (!string.IsNullOrEmpty(bookingIdOrRef) && !refsToTry.Contains(bookingIdOrRef)) refsToTry.Add(bookingIdOrRef);

        foreach (var r in refsToTry)
        {
            foreach (var t in typesToTry.Distinct())
            {
                approvedSummary = await _agentsApiClient.GetBookingSupplierPaymentsAsync(t, r, ct);
                if (approvedSummary != null && approvedSummary.Payments != null && approvedSummary.Payments.Count > 0)
                {
                    break;
                }
            }
            if (approvedSummary != null && approvedSummary.Payments != null && approvedSummary.Payments.Count > 0)
            {
                break;
            }
        }

        var approvedTotal = approvedSummary?.ApprovedTotal ?? (record?.AccountsApprovedTotal ?? 0m);
        bool isCompleted = isDealClosed || string.Equals(processingStatus, "completed", StringComparison.OrdinalIgnoreCase);

        if (record == null)
        {
            if (salesmanId == 0) return null;

            bool effectiveInhouse = isInhouse && (approvedTotal <= 0m);
            var pType = isRejected ? ProfitType.Rejection : (effectiveInhouse ? ProfitType.Inhouse : ProfitType.Normal);
            var effectiveCost = effectiveInhouse ? 0m : (approvedTotal > 0m ? approvedTotal : expectedSupplierCost);
            var pAmt = isRejected ? CalculateRejectionProfit(rejFee) : (effectiveInhouse ? 0m : CalculateNormalProfit(payable, effectiveCost));

            record = new SalesmanProfitRecord
            {
                SalesmanId = salesmanId,
                BookingType = normType,
                BookingId = realBookingId > 0 ? realBookingId : numericId,
                DocumentNumber = docNo ?? (numericId == 0 ? bookingIdOrRef : null),
                PayableAmount = payable,
                FinalSupplierCost = effectiveCost,
                ProfitAmount = pAmt,
                ProfitType = pType,
                SupplierCostSource = approvedTotal > 0 ? SupplierCostSource.Approved : SupplierCostSource.Estimated,
                AccountsApprovedTotal = approvedTotal,
                RejectionFee = isRejected ? rejFee : null,
                IsFinal = isCompleted,
                BookingCreatedAt = createdAt,
                FinalizedAt = isCompleted ? DateTime.UtcNow : null,
                LastReconciledAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.SalesmanProfitRecords.Add(record);
            await _db.SaveChangesAsync(ct);
        }

        // Also check if any Sales Return exists in Accounts Portal to sync as SalesmanProfitAdjustment if not already present
        var (hasSalesReturn, salesReturnAmount, _, _) = await _agentsApiClient.CheckSalesReturnAsync(normType, searchRef, ct);
        if (hasSalesReturn && salesReturnAmount.HasValue && salesReturnAmount.Value > 0m)
        {
            var srKey = $"SALES-RETURN-{normType}-{record.BookingId}";
            var existingSrAdj = await _db.SalesmanProfitAdjustments.FirstOrDefaultAsync(a => a.IdempotencyKey == srKey, ct);
            if (existingSrAdj == null)
            {
                var srAdj = new SalesmanProfitAdjustment
                {
                    SalesmanProfitRecordId = record.Id,
                    SalesmanId = record.SalesmanId,
                    BookingType = record.BookingType,
                    BookingId = record.BookingId,
                    DocumentNumber = record.DocumentNumber,
                    AdjustmentType = SalesmanProfitAdjustmentType.SalesReturn,
                    Amount = -salesReturnAmount.Value, // Negative sign for Sales Return
                    ReferenceType = "SalesReturn",
                    ReferenceId = searchRef,
                    IdempotencyKey = srKey,
                    Description = $"Sales Return refund of {salesReturnAmount.Value} BHD",
                    CreatedAt = DateTime.UtcNow
                };
                _db.SalesmanProfitAdjustments.Add(srAdj);
                await _db.SaveChangesAsync(ct);
            }
        }

        // Recalculate Final Profit
        record.PayableAmount = payable;
        record.AccountsApprovedTotal = approvedTotal;

        var adjustmentsSum = await _db.SalesmanProfitAdjustments
            .Where(a => a.SalesmanProfitRecordId == record.Id)
            .SumAsync(a => (decimal?)a.Amount, ct) ?? 0m;

        if (record.ProfitType == ProfitType.Rejection)
        {
            record.ProfitAmount = CalculateRejectionProfit(record.RejectionFee ?? 0m, adjustmentsSum);
        }
        else
        {
            bool effectiveInhouse = isInhouse && (approvedTotal <= 0m);
            record.ProfitType = effectiveInhouse ? ProfitType.Inhouse : ProfitType.Normal;
            record.FinalSupplierCost = effectiveInhouse ? 0m : (approvedTotal > 0m ? approvedTotal : expectedSupplierCost);
            record.SupplierCostSource = approvedTotal > 0m ? SupplierCostSource.Approved : SupplierCostSource.Estimated;

            record.ProfitAmount = effectiveInhouse ? CalculateInhouseProfit(adjustmentsSum) : CalculateNormalProfit(record.PayableAmount, record.FinalSupplierCost, adjustmentsSum);
        }

        record.IsFinal = isCompleted;
        record.FinalizedAt = isCompleted ? (record.FinalizedAt ?? DateTime.UtcNow) : null;
        record.LastReconciledAt = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await SyncBookingProfitEntityAndUserTotalAsync(normType, record.BookingId, record.ProfitAmount, record.SalesmanId, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Profit record updated for {BookingType} #{BookingId}. ApprovedTotal={ApprovedTotal}, ProfitAmount={ProfitAmount}, IsFinal={IsFinal}", normType, record.BookingId, approvedTotal, record.ProfitAmount, record.IsFinal);

        return record;
    }

    // ─── Record Rejection Profit (v3 Rule: Approved Visa -> Rejected = RejectionFee) ──
    public async Task<SalesmanProfitRecord?> RecordRejectionProfitAsync(
        long bookingId,
        decimal rejectionFee,
        CancellationToken ct = default)
    {
        return await FinalizeRejectionProfitAsync("Visa", bookingId, rejectionFee, ct);
    }

    public async Task<SalesmanProfitRecord?> FinalizeRejectionProfitAsync(
        string bookingType,
        long bookingId,
        decimal rejectionFee,
        CancellationToken ct = default)
    {
        var normType = NormalizeBookingType(bookingType);

        var record = await _db.SalesmanProfitRecords
            .FirstOrDefaultAsync(r => r.BookingType == normType && r.BookingId == bookingId, ct);

        if (record == null)
        {
            var (salesmanId, realId, docNo, payable, expectedCost, createdAt, _, _, _, _, _) = await ResolveBookingInfoAsync(normType, bookingId.ToString(), ct);
            if (salesmanId == 0) return null;

            record = new SalesmanProfitRecord
            {
                SalesmanId = salesmanId,
                BookingType = normType,
                BookingId = realId > 0 ? realId : bookingId,
                DocumentNumber = docNo,
                PayableAmount = payable,
                FinalSupplierCost = 0m,
                ProfitAmount = CalculateRejectionProfit(rejectionFee),
                ProfitType = ProfitType.Rejection,
                SupplierCostSource = SupplierCostSource.Rejection,
                RejectionFee = rejectionFee,
                IsFinal = true,
                BookingCreatedAt = createdAt,
                FinalizedAt = DateTime.UtcNow,
                LastReconciledAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.SalesmanProfitRecords.Add(record);
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            record.ProfitType = ProfitType.Rejection;
            record.SupplierCostSource = SupplierCostSource.Rejection;
            record.RejectionFee = rejectionFee;
            var adjustmentsSum = await _db.SalesmanProfitAdjustments
                .Where(a => a.SalesmanProfitRecordId == record.Id)
                .SumAsync(a => (decimal?)a.Amount, ct) ?? 0m;
            record.ProfitAmount = CalculateRejectionProfit(rejectionFee, adjustmentsSum);
            record.IsFinal = true;
            record.FinalizedAt = record.FinalizedAt ?? DateTime.UtcNow;
            record.LastReconciledAt = DateTime.UtcNow;
            record.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        // Add audit adjustment row for Rejection Profit
        var rejKey = $"VISA-REJECTION-{record.BookingId}";
        var existingRej = await _db.SalesmanProfitAdjustments.FirstOrDefaultAsync(a => a.IdempotencyKey == rejKey, ct);
        if (existingRej == null)
        {
            var rejAdj = new SalesmanProfitAdjustment
            {
                SalesmanProfitRecordId = record.Id,
                SalesmanId = record.SalesmanId,
                BookingType = record.BookingType,
                BookingId = record.BookingId,
                DocumentNumber = record.DocumentNumber,
                AdjustmentType = SalesmanProfitAdjustmentType.RejectionProfit,
                Amount = rejectionFee,
                ReferenceType = "VisaRejection",
                ReferenceId = bookingId.ToString(),
                IdempotencyKey = rejKey,
                Description = $"Visa rejection profit equal to rejection fee {rejectionFee} BHD",
                CreatedAt = DateTime.UtcNow
            };
            _db.SalesmanProfitAdjustments.Add(rejAdj);
            await _db.SaveChangesAsync(ct);
        }

        await SyncBookingProfitEntityAndUserTotalAsync(normType, record.BookingId, record.ProfitAmount, record.SalesmanId, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Rejection profit recorded for {BookingType} #{BookingId}. RejectionFee={RejectionFee}", normType, bookingId, rejectionFee);
        return record;
    }

    // ─── Add Independent Profit Adjustment ─────────────────────────────────
    public async Task<SalesmanProfitAdjustment> AddAdjustmentAsync(
        SalesmanProfitAdjustmentRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            throw new ArgumentException("IdempotencyKey is required for adjustments.", nameof(request.IdempotencyKey));

        var existing = await _db.SalesmanProfitAdjustments
            .FirstOrDefaultAsync(a => a.IdempotencyKey == request.IdempotencyKey, ct);
        if (existing != null)
        {
            _logger.LogInformation("Idempotent adjustment request ignored. Key: {Key}", request.IdempotencyKey);
            return existing;
        }

        var record = await FinalizeOrUpdateProfitAsync(request.BookingType, request.BookingIdOrRef, ct);
        if (record == null)
        {
            throw new InvalidOperationException($"Salesman profit record for {request.BookingType} {request.BookingIdOrRef} could not be found or created.");
        }

        var adj = new SalesmanProfitAdjustment
        {
            SalesmanProfitRecordId = record.Id,
            SalesmanId = record.SalesmanId,
            BookingType = record.BookingType,
            BookingId = record.BookingId,
            DocumentNumber = record.DocumentNumber,
            AdjustmentType = request.AdjustmentType,
            Amount = request.Amount,
            ReferenceType = request.ReferenceType,
            ReferenceId = request.ReferenceId,
            IdempotencyKey = request.IdempotencyKey,
            Description = request.Description,
            CreatedByUserId = request.CreatedByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _db.SalesmanProfitAdjustments.Add(adj);
        await _db.SaveChangesAsync(ct);

        // Recalculate profit amount
        await FinalizeOrUpdateProfitAsync(record.BookingType, record.BookingId.ToString(), ct);

        return adj;
    }

    // ─── Unfinalize Profit (Master Accountant Unlock) ───────────────────────
    public async Task<SalesmanProfitRecord?> UnfinalizeProfitAsync(
        string bookingType,
        long bookingId,
        CancellationToken ct = default)
    {
        return await UnfinalizeProfitAsync(bookingType, bookingId.ToString(), ct);
    }

    public async Task<SalesmanProfitRecord?> UnfinalizeProfitAsync(
        string bookingType,
        string bookingIdOrRef,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(bookingIdOrRef)) return null;

        var normType = NormalizeBookingType(bookingType);
        long.TryParse(bookingIdOrRef, out var numericId);

        var record = await _db.SalesmanProfitRecords
            .FirstOrDefaultAsync(r => r.BookingType == normType && ((numericId > 0 && r.BookingId == numericId) || (r.DocumentNumber != null && r.DocumentNumber == bookingIdOrRef)), ct);

        if (record == null) return null;

        record.IsFinal = false;
        record.FinalizedAt = null;
        record.LastReconciledAt = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await SyncBookingProfitEntityAndUserTotalAsync(normType, record.BookingId, record.ProfitAmount, record.SalesmanId, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Profit unfinalized for {BookingType} #{BookingId}. Recognized profit removed from salesman total.", normType, record.BookingId);
        return record;
    }

    // ─── Reconcile Single Profit Record ──────────────────────────────────────
    public async Task<SalesmanProfitRecord?> ReconcileProfitAsync(
        string bookingType,
        long bookingId,
        CancellationToken ct = default)
    {
        return await FinalizeOrUpdateProfitAsync(bookingType, bookingId.ToString(), ct);
    }

    public async Task<SalesmanProfitRecord?> ReconcileProfitAsync(
        string bookingType,
        string bookingIdOrRef,
        CancellationToken ct = default)
    {
        return await FinalizeOrUpdateProfitAsync(bookingType, bookingIdOrRef, ct);
    }

    public async Task<ProfitReconciliationResult> ReconcileProfitDetailsAsync(
        string bookingType,
        string bookingIdOrRef,
        CancellationToken ct = default)
    {
        var record = await GetBookingProfitAsync(bookingType, bookingIdOrRef, ct);
        if (record == null)
        {
            return new ProfitReconciliationResult(
                BookingType: bookingType,
                BookingIdOrRef: bookingIdOrRef,
                BaseProfit: 0m,
                TotalIndependentAdjustments: 0m,
                ExpectedFinalProfit: 0m,
                StoredFinalProfit: 0m,
                Difference: 0m,
                IsMatch: true,
                Status: "NOT_FOUND"
            );
        }

        var baseProfit = record.PayableAmount - record.FinalSupplierCost;
        var adjustments = await _db.SalesmanProfitAdjustments
            .AsNoTracking()
            .Where(a => a.SalesmanProfitRecordId == record.Id)
            .SumAsync(a => (decimal?)a.Amount, ct) ?? 0m;

        decimal expectedFinalProfit;
        if (record.ProfitType == ProfitType.Rejection)
        {
            expectedFinalProfit = record.RejectionFee ?? 0m;
        }
        else
        {
            expectedFinalProfit = baseProfit + adjustments;
        }

        var diff = Math.Abs(expectedFinalProfit - record.ProfitAmount);
        var isMatch = diff < 0.001m;

        return new ProfitReconciliationResult(
            BookingType: record.BookingType,
            BookingIdOrRef: record.BookingId.ToString(),
            BaseProfit: baseProfit,
            TotalIndependentAdjustments: adjustments,
            ExpectedFinalProfit: expectedFinalProfit,
            StoredFinalProfit: record.ProfitAmount,
            Difference: diff,
            IsMatch: isMatch,
            Status: isMatch ? "MATCH" : "MISMATCH"
        );
    }

    public async Task<ProfitDetailResponse?> GetProfitDetailAsync(
        string bookingType,
        string bookingIdOrRef,
        CancellationToken ct = default)
    {
        var normType = NormalizeBookingType(bookingType);
        long.TryParse(bookingIdOrRef, out var numericId);

        var record = await _db.SalesmanProfitRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.BookingType == normType && ((numericId > 0 && r.BookingId == numericId) || (r.DocumentNumber != null && r.DocumentNumber == bookingIdOrRef)), ct);

        if (record == null) return null;

        var adjustments = await _db.SalesmanProfitAdjustments
            .AsNoTracking()
            .Where(a => a.SalesmanProfitRecordId == record.Id)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new ProfitAdjustmentDto(
                a.Id,
                a.AdjustmentType.ToString(),
                a.Amount,
                a.Description,
                a.ReferenceType,
                a.ReferenceId,
                a.IdempotencyKey,
                a.CreatedAt,
                a.CreatedByUserId
            ))
            .ToListAsync(ct);

        var baseProfit = record.PayableAmount - record.FinalSupplierCost;
        var totalAdjustments = adjustments.Sum(a => a.Amount);

        return new ProfitDetailResponse(
            BookingType: record.BookingType,
            BookingId: record.BookingId,
            DocumentNumber: record.DocumentNumber,
            PayableAmount: record.PayableAmount,
            FinalSupplierCost: record.FinalSupplierCost,
            BaseProfit: baseProfit,
            RejectionFee: record.RejectionFee,
            TotalAdjustments: totalAdjustments,
            FinalProfit: record.ProfitAmount,
            ProfitAmount: record.ProfitAmount,
            ProfitType: record.ProfitType.ToString(),
            SupplierCostSource: record.SupplierCostSource.ToString(),
            AccountsApprovedTotal: record.AccountsApprovedTotal,
            IsFinal: record.IsFinal,
            BookingCreatedAt: record.BookingCreatedAt,
            FinalizedAt: record.FinalizedAt,
            LastReconciledAt: record.LastReconciledAt,
            Adjustments: adjustments
        );
    }

    // ─── Query Aggregations ──────────────────────────────────────────────────
    public async Task<decimal> GetTotalProfitAsync(long salesmanId, CancellationToken ct = default)
    {
        return await _db.SalesmanProfitRecords
            .AsNoTracking()
            .Where(r => r.SalesmanId == salesmanId && r.IsFinal)
            .SumAsync(r => (decimal?)r.ProfitAmount, ct) ?? 0m;
    }

    public async Task<decimal> GetMonthlyProfitAsync(long salesmanId, int year, int month, CancellationToken ct = default)
    {
        return await _db.SalesmanProfitRecords
            .AsNoTracking()
            .Where(r => r.SalesmanId == salesmanId && r.IsFinal && r.BookingCreatedAt.Year == year && r.BookingCreatedAt.Month == month)
            .SumAsync(r => (decimal?)r.ProfitAmount, ct) ?? 0m;
    }

    public async Task<SalesmanProfitSummary> GetProfitSummaryAsync(long salesmanId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var records = await _db.SalesmanProfitRecords
            .AsNoTracking()
            .Where(r => r.SalesmanId == salesmanId)
            .ToListAsync(ct);

        var approvedRecords = records
            .Where(r => r.IsFinal)
            .ToList();

        var totalProfit = approvedRecords.Sum(r => r.ProfitAmount);
        var monthlyProfit = approvedRecords
            .Where(r => r.BookingCreatedAt.Year == now.Year && r.BookingCreatedAt.Month == now.Month)
            .Sum(r => r.ProfitAmount);

        var approvedProfit = totalProfit;

        var estimatedProfit = records
            .Where(r => !r.IsFinal)
            .Sum(r => r.ProfitAmount);

        var totalCount = records.Count;
        var finalizedCount = records.Count(r => r.IsFinal);

        return new SalesmanProfitSummary(
            SalesmanId: salesmanId,
            TotalProfit: totalProfit,
            MonthlyProfit: monthlyProfit,
            AllTimeProfit: totalProfit,
            ApprovedProfit: approvedProfit,
            EstimatedProfit: estimatedProfit,
            TotalBookingsCount: totalCount,
            FinalizedCount: finalizedCount
        );
    }

    public async Task<SalesmanServiceProfitSummary> GetProfitSummaryByServiceAsync(long salesmanId, CancellationToken ct = default)
    {
        var records = await _db.SalesmanProfitRecords
            .AsNoTracking()
            .Where(r => r.SalesmanId == salesmanId)
            .ToListAsync(ct);

        var serviceTypes = new[] { "Visa", "VFS", "Flight", "Hotel", "Transport", "Holiday", "Other" };

        var breakdowns = serviceTypes.Select(st =>
        {
            var matching = records.Where(r => string.Equals(r.BookingType, st, StringComparison.OrdinalIgnoreCase)).ToList();
            var approvedMatching = matching.Where(r => r.IsFinal).ToList();
            return new ServiceProfitBreakdown(
                ServiceType: st,
                BookingCount: matching.Count,
                PayableAmount: matching.Sum(r => r.PayableAmount),
                SupplierCost: matching.Sum(r => r.FinalSupplierCost),
                ProfitAmount: approvedMatching.Sum(r => r.ProfitAmount),
                FinalizedCount: matching.Count(r => r.IsFinal)
            );
        }).ToList();

        var totalProfit = records
            .Where(r => r.IsFinal)
            .Sum(r => r.ProfitAmount);

        return new SalesmanServiceProfitSummary(salesmanId, totalProfit, breakdowns);
    }

    public async Task<SalesmanProfitRecord?> GetBookingProfitAsync(string bookingType, long bookingId, CancellationToken ct = default)
    {
        return await GetBookingProfitAsync(bookingType, bookingId.ToString(), ct);
    }

    public async Task<SalesmanProfitRecord?> GetBookingProfitAsync(string bookingType, string bookingIdOrRef, CancellationToken ct = default)
    {
        var normType = NormalizeBookingType(bookingType);
        long.TryParse(bookingIdOrRef, out var numericId);
        return await _db.SalesmanProfitRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.BookingType == normType && ((numericId > 0 && r.BookingId == numericId) || (r.DocumentNumber != null && r.DocumentNumber == bookingIdOrRef)), ct);
    }

    public async Task<IEnumerable<SalesmanProfitItemDto>> GetSalesmanProfitItemDtosAsync(
        long salesmanId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken ct = default)
    {
        var q = _db.SalesmanProfitRecords.AsNoTracking().Where(r => r.SalesmanId == salesmanId);
        if (startDate.HasValue) q = q.Where(r => r.BookingCreatedAt >= startDate.Value);
        if (endDate.HasValue) q = q.Where(r => r.BookingCreatedAt <= endDate.Value);

        var records = await q.OrderByDescending(r => r.BookingCreatedAt).ToListAsync(ct);
        if (records.Count == 0) return Enumerable.Empty<SalesmanProfitItemDto>();

        // Preload client/head names for all records
        var flightIds = records.Where(r => r.BookingType == "Flight").Select(r => r.BookingId).ToList();
        var hotelIds = records.Where(r => r.BookingType == "Hotel").Select(r => r.BookingId).ToList();
        var visaIds = records.Where(r => r.BookingType == "Visa").Select(r => r.BookingId).ToList();
        var vfsIds = records.Where(r => r.BookingType == "VFS").Select(r => r.BookingId).ToList();
        var holidayIds = records.Where(r => r.BookingType == "Holiday").Select(r => r.BookingId).ToList();
        var transportIds = records.Where(r => r.BookingType == "Transport").Select(r => r.BookingId).ToList();
        var otherIds = records.Where(r => r.BookingType == "Other").Select(r => r.BookingId).ToList();

        var flightNames = await _db.FlightBookings.AsNoTracking().Include(f => f.Passengers).Include(f => f.Head)
            .Where(f => flightIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, f => f.Head != null ? $"{f.Head.FirstName} {f.Head.LastName}".Trim() : (f.Passengers != null && f.Passengers.Count > 0 ? f.Passengers.First().PassengerName : f.FromLocation + " → " + f.ToLocation), ct);

        var hotelNames = await _db.HotelBookings.AsNoTracking().Include(h => h.Head)
            .Where(h => hotelIds.Contains(h.Id))
            .ToDictionaryAsync(h => h.Id, h => h.Head != null ? $"{h.Head.FirstName} {h.Head.LastName}".Trim() : h.GuestName, ct);

        var visaNames = await _db.SalesVisaApplications.AsNoTracking().Include(v => v.Head).Include(v => v.Applicants)
            .Where(v => visaIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, v => v.Head != null ? $"{v.Head.FirstName} {v.Head.LastName}".Trim() : (v.Applicants != null && v.Applicants.Count > 0 ? $"{v.Applicants.First().FName} {v.Applicants.First().LName}".Trim() : (v.DocumentNumber ?? "Visa Applicant")), ct);

        var vfsNames = await _db.VfsAppointments.AsNoTracking().Include(v => v.Head).Include(v => v.Applicants)
            .Where(v => vfsIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, v => v.Head != null ? $"{v.Head.FirstName} {v.Head.LastName}".Trim() : (!string.IsNullOrWhiteSpace(v.Name) ? v.Name : (v.Applicants != null && v.Applicants.Count > 0 ? v.Applicants.First().Name : "VFS Appointment")), ct);

        var holidayNames = await _db.HolidayPackages.AsNoTracking().Include(h => h.Head)
            .Where(h => holidayIds.Contains(h.Id))
            .ToDictionaryAsync(h => h.Id, h => h.Head != null ? $"{h.Head.FirstName} {h.Head.LastName}".Trim() : h.PassengerName, ct);

        var transportNames = await _db.Transportations.AsNoTracking().Include(t => t.Head)
            .Where(t => transportIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Head != null ? $"{t.Head.FirstName} {t.Head.LastName}".Trim() : (t.Name ?? "Transportation"), ct);

        var otherNames = await _db.OtherServices.AsNoTracking().Include(o => o.Head)
            .Where(o => otherIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, o => o.Head != null ? $"{o.Head.FirstName} {o.Head.LastName}".Trim() : o.ServiceName, ct);

        return records.Select(r =>
        {
            string? clientName = null;
            switch (r.BookingType)
            {
                case "Flight": flightNames.TryGetValue(r.BookingId, out clientName); break;
                case "Hotel": hotelNames.TryGetValue(r.BookingId, out clientName); break;
                case "Visa": visaNames.TryGetValue(r.BookingId, out clientName); break;
                case "VFS": vfsNames.TryGetValue(r.BookingId, out clientName); break;
                case "Holiday": holidayNames.TryGetValue(r.BookingId, out clientName); break;
                case "Transport": transportNames.TryGetValue(r.BookingId, out clientName); break;
                case "Other": otherNames.TryGetValue(r.BookingId, out clientName); break;
            }

            return new SalesmanProfitItemDto(
                r.Id,
                r.SalesmanId,
                r.BookingType,
                r.BookingId,
                r.DocumentNumber,
                clientName ?? r.DocumentNumber,
                r.PayableAmount,
                r.FinalSupplierCost,
                r.ProfitAmount,
                r.ProfitType.ToString(),
                r.SupplierCostSource.ToString(),
                r.AccountsApprovedTotal,
                r.IsFinal,
                r.BookingCreatedAt,
                r.FinalizedAt
            );
        }).ToList();
    }

    public async Task<IEnumerable<SalesmanProfitRecord>> GetSalesmanProfitRecordsAsync(
        long salesmanId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken ct = default)
    {
        var q = _db.SalesmanProfitRecords.AsNoTracking().Where(r => r.SalesmanId == salesmanId);
        if (startDate.HasValue) q = q.Where(r => r.BookingCreatedAt >= startDate.Value);
        if (endDate.HasValue) q = q.Where(r => r.BookingCreatedAt <= endDate.Value);

        return await q.OrderByDescending(r => r.BookingCreatedAt).ToListAsync(ct);
    }

    // ─── Backfill All Booking Historical Data Idempotently ─────────────────
    
    public async Task EnsureSalesmanProfitRecordsAsync(long salesmanId, CancellationToken ct = default)
    {
        if (salesmanId <= 0) return;

        var existingRecords = await _db.SalesmanProfitRecords
            .Where(r => r.SalesmanId == salesmanId)
            .Select(r => new { r.Id, r.BookingType, r.BookingId })
            .ToListAsync(ct);

        var existingSet = existingRecords.Select(r => $"{NormalizeBookingType(r.BookingType)}:{r.BookingId}").ToHashSet(StringComparer.OrdinalIgnoreCase);

        var newRecords = new List<SalesmanProfitRecord>();

        // 1. Flights
        var flights = await _db.FlightBookings.AsNoTracking().Include(f => f.Passengers)
            .Where(b => b.UserId == salesmanId)
            .ToListAsync(ct);
        foreach (var f in flights)
        {
            if (!existingSet.Contains($"Flight:{f.Id}"))
            {
                bool isFlightInhouse = f.Passengers != null && f.Passengers.Count > 0
                    ? f.Passengers.All(p => p.IsInhouse)
                    : f.IsInhouse;
                decimal flightCost = f.Passengers != null && f.Passengers.Count > 0
                    ? f.Passengers.Sum(p => p.ExpectedSupplierCost)
                    : f.ExpectedSupplierCost;
                var pAmt = isFlightInhouse ? CalculateInhouseProfit() : CalculateNormalProfit(f.PayableAmount, flightCost);

                newRecords.Add(new SalesmanProfitRecord
                {
                    SalesmanId = salesmanId,
                    BookingType = "Flight",
                    BookingId = f.Id,
                    DocumentNumber = f.DocumentNumber,
                    PayableAmount = f.PayableAmount,
                    FinalSupplierCost = isFlightInhouse ? 0m : flightCost,
                    ProfitAmount = pAmt,
                    ProfitType = isFlightInhouse ? ProfitType.Inhouse : ProfitType.Normal,
                    SupplierCostSource = SupplierCostSource.Estimated,
                    IsFinal = f.IsDealClosed || string.Equals(f.ProcessingStatus, "completed", StringComparison.OrdinalIgnoreCase),
                    BookingCreatedAt = f.CreatedAt ?? DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                existingSet.Add($"Flight:{f.Id}");
            }
        }

        // 2. Hotels
        var hotels = await _db.HotelBookings.AsNoTracking()
            .Where(b => b.UserId == salesmanId)
            .ToListAsync(ct);
        foreach (var h in hotels)
        {
            if (!existingSet.Contains($"Hotel:{h.Id}"))
            {
                var pAmt = h.IsInhouse ? CalculateInhouseProfit() : CalculateNormalProfit(h.PayableAmount, h.ExpectedSupplierCost);
                newRecords.Add(new SalesmanProfitRecord
                {
                    SalesmanId = salesmanId,
                    BookingType = "Hotel",
                    BookingId = h.Id,
                    DocumentNumber = h.DocumentNumber,
                    PayableAmount = h.PayableAmount,
                    FinalSupplierCost = h.IsInhouse ? 0m : h.ExpectedSupplierCost,
                    ProfitAmount = pAmt,
                    ProfitType = h.IsInhouse ? ProfitType.Inhouse : ProfitType.Normal,
                    SupplierCostSource = SupplierCostSource.Estimated,
                    IsFinal = h.IsDealClosed || string.Equals(h.ProcessingStatus, "completed", StringComparison.OrdinalIgnoreCase),
                    BookingCreatedAt = h.CreatedAt ?? DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                existingSet.Add($"Hotel:{h.Id}");
            }
        }

        // 3. Visas
        var visas = await _db.SalesVisaApplications.AsNoTracking().Include(v => v.Applicants)
            .Where(b => b.UserId == salesmanId)
            .ToListAsync(ct);
        foreach (var v in visas)
        {
            if (!existingSet.Contains($"Visa:{v.Id}"))
            {
                bool isRej = string.Equals(v.ProcessingStatus, "rejected", StringComparison.OrdinalIgnoreCase);
                bool isVisaInhouse = v.Applicants != null && v.Applicants.Count > 0
                    ? v.Applicants.All(a => a.IsInhouse)
                    : v.IsInhouse;
                decimal visaExpectedCost = v.Applicants != null && v.Applicants.Count > 0
                    ? v.Applicants.Where(a => !a.IsInhouse).Sum(a => a.ExpectedSupplierCost)
                    : v.ExpectedSupplierCost;
                var pAmt = isRej ? CalculateRejectionProfit(v.RejectionFee) : (isVisaInhouse ? CalculateInhouseProfit() : CalculateNormalProfit(v.PayableAmount, visaExpectedCost));
                newRecords.Add(new SalesmanProfitRecord
                {
                    SalesmanId = salesmanId,
                    BookingType = "Visa",
                    BookingId = v.Id,
                    DocumentNumber = v.DocumentNumber,
                    PayableAmount = v.PayableAmount,
                    FinalSupplierCost = isVisaInhouse ? 0m : visaExpectedCost,
                    ProfitAmount = pAmt,
                    ProfitType = isRej ? ProfitType.Rejection : (isVisaInhouse ? ProfitType.Inhouse : ProfitType.Normal),
                    SupplierCostSource = SupplierCostSource.Estimated,
                    IsFinal = v.IsDealClosed || string.Equals(v.ProcessingStatus, "completed", StringComparison.OrdinalIgnoreCase),
                    BookingCreatedAt = v.CreatedAt ?? DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                existingSet.Add($"Visa:{v.Id}");
            }
        }

        // 4. VFS
        var vfsList = await _db.VfsAppointments.AsNoTracking()
            .Where(b => b.CreatedBy == salesmanId && b.DeletedAt == null)
            .ToListAsync(ct);
        foreach (var vfs in vfsList)
        {
            if (!existingSet.Contains($"VFS:{vfs.Id}"))
            {
                var pAmt = vfs.IsInhouse ? CalculateInhouseProfit() : CalculateNormalProfit(vfs.PayableAmount, vfs.ExpectedSupplierCost);
                newRecords.Add(new SalesmanProfitRecord
                {
                    SalesmanId = salesmanId,
                    BookingType = "VFS",
                    BookingId = vfs.Id,
                    DocumentNumber = vfs.DocumentNumber,
                    PayableAmount = vfs.PayableAmount,
                    FinalSupplierCost = vfs.IsInhouse ? 0m : vfs.ExpectedSupplierCost,
                    ProfitAmount = pAmt,
                    ProfitType = vfs.IsInhouse ? ProfitType.Inhouse : ProfitType.Normal,
                    SupplierCostSource = SupplierCostSource.Estimated,
                    IsFinal = vfs.IsDealClosed || string.Equals(vfs.ProcessingStatus, "completed", StringComparison.OrdinalIgnoreCase),
                    BookingCreatedAt = vfs.CreatedAt ?? DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                existingSet.Add($"VFS:{vfs.Id}");
            }
        }

        // 5. Holidays
        var holidays = await _db.HolidayPackages.AsNoTracking()
            .Where(b => b.CreatedBy == salesmanId && b.DeletedAt == null)
            .ToListAsync(ct);
        foreach (var hol in holidays)
        {
            if (!existingSet.Contains($"Holiday:{hol.Id}"))
            {
                var pAmt = hol.IsInhouse ? CalculateInhouseProfit() : CalculateNormalProfit(hol.PayableAmount, hol.ExpectedSupplierCost);
                newRecords.Add(new SalesmanProfitRecord
                {
                    SalesmanId = salesmanId,
                    BookingType = "Holiday",
                    BookingId = hol.Id,
                    DocumentNumber = hol.DocumentNumber,
                    PayableAmount = hol.PayableAmount,
                    FinalSupplierCost = hol.IsInhouse ? 0m : hol.ExpectedSupplierCost,
                    ProfitAmount = pAmt,
                    ProfitType = hol.IsInhouse ? ProfitType.Inhouse : ProfitType.Normal,
                    SupplierCostSource = SupplierCostSource.Estimated,
                    IsFinal = hol.IsDealClosed || string.Equals(hol.ProcessingStatus, "completed", StringComparison.OrdinalIgnoreCase),
                    BookingCreatedAt = hol.CreatedAt ?? DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                existingSet.Add($"Holiday:{hol.Id}");
            }
        }

        // 6. Transport
        var transports = await _db.Transportations.AsNoTracking()
            .Where(b => b.CreatedBy == salesmanId && b.DeletedAt == null)
            .ToListAsync(ct);
        foreach (var tr in transports)
        {
            if (!existingSet.Contains($"Transport:{tr.Id}"))
            {
                var pAmt = tr.IsInhouse ? CalculateInhouseProfit() : CalculateNormalProfit(tr.PayableAmount, tr.ExpectedSupplierCost);
                newRecords.Add(new SalesmanProfitRecord
                {
                    SalesmanId = salesmanId,
                    BookingType = "Transport",
                    BookingId = tr.Id,
                    DocumentNumber = tr.DocumentNumber,
                    PayableAmount = tr.PayableAmount,
                    FinalSupplierCost = tr.IsInhouse ? 0m : tr.ExpectedSupplierCost,
                    ProfitAmount = pAmt,
                    ProfitType = tr.IsInhouse ? ProfitType.Inhouse : ProfitType.Normal,
                    SupplierCostSource = SupplierCostSource.Estimated,
                    IsFinal = tr.IsDealClosed || string.Equals(tr.ProcessingStatus, "completed", StringComparison.OrdinalIgnoreCase),
                    BookingCreatedAt = tr.CreatedAt ?? DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                existingSet.Add($"Transport:{tr.Id}");
            }
        }

        // 7. Others
        var others = await _db.OtherServices.AsNoTracking()
            .Where(b => b.CreatedBy == salesmanId && b.DeletedAt == null)
            .ToListAsync(ct);
        foreach (var ot in others)
        {
            if (!existingSet.Contains($"Other:{ot.Id}"))
            {
                var pAmt = ot.IsInhouse ? CalculateInhouseProfit() : CalculateNormalProfit(ot.PayableAmount, ot.ExpectedSupplierCost);
                newRecords.Add(new SalesmanProfitRecord
                {
                    SalesmanId = salesmanId,
                    BookingType = "Other",
                    BookingId = ot.Id,
                    DocumentNumber = ot.DocumentNumber,
                    PayableAmount = ot.PayableAmount,
                    FinalSupplierCost = ot.IsInhouse ? 0m : ot.ExpectedSupplierCost,
                    ProfitAmount = pAmt,
                    ProfitType = ot.IsInhouse ? ProfitType.Inhouse : ProfitType.Normal,
                    SupplierCostSource = SupplierCostSource.Estimated,
                    IsFinal = ot.IsDealClosed || string.Equals(ot.ProcessingStatus, "completed", StringComparison.OrdinalIgnoreCase),
                    BookingCreatedAt = ot.CreatedAt ?? DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                existingSet.Add($"Other:{ot.Id}");
            }
        }

        if (newRecords.Count > 0)
        {
            _db.SalesmanProfitRecords.AddRange(newRecords);
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Concurrency during EnsureSalesmanProfitRecordsAsync for SalesmanId {SalesmanId}", salesmanId);
            }
        }
    }

    public async Task<BackfillReport> BackfillAllProfitRecordsAsync(CancellationToken ct = default)
    {
        int evaluated = 0, created = 0, updated = 0, skipped = 0, errors = 0;

        var existingMap = await _db.SalesmanProfitRecords
            .AsNoTracking()
            .Select(r => new { r.BookingType, r.BookingId, r.IsFinal })
            .ToDictionaryAsync(r => $"{r.BookingType}:{r.BookingId}", r => r.IsFinal, ct);

        // 1. Visas
        var visas = await _db.SalesVisaApplications.AsNoTracking().Include(v => v.Applicants).ToListAsync(ct);
        foreach (var v in visas)
        {
            evaluated++;
            try
            {
                var key = $"Visa:{v.Id}";
                if (existingMap.TryGetValue(key, out var isFinal) && isFinal)
                {
                    skipped++;
                    continue;
                }

                var isRej = string.Equals(v.ProcessingStatus, "rejected", StringComparison.OrdinalIgnoreCase);
                var rec = await FinalizeOrUpdateProfitAsync("Visa", v.Id, ct);

                if (rec == null)
                {
                    if (isRej && v.RejectionFee > 0)
                    {
                        rec = await FinalizeRejectionProfitAsync("Visa", v.Id, v.RejectionFee, ct);
                    }
                    else
                    {
                        bool isVisaInhouse = v.Applicants != null && v.Applicants.Count > 0
                            ? v.Applicants.All(a => a.IsInhouse)
                            : v.IsInhouse;
                        decimal visaExpectedCost = v.Applicants != null && v.Applicants.Count > 0
                            ? v.Applicants.Where(a => !a.IsInhouse).Sum(a => a.ExpectedSupplierCost)
                            : v.ExpectedSupplierCost;
                        rec = await CreateEstimatedProfitRecordAsync(v.UserId, "Visa", v.Id, v.DocumentNumber, v.PayableAmount, visaExpectedCost, isVisaInhouse, v.CreatedAt ?? DateTime.UtcNow, ct);
                    }
                    created++;
                }
                else
                {
                    updated++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error backfilling Visa #{Id}", v.Id);
                errors++;
            }
        }

        // 2. VFS Appointments
        var vfsList = await _db.VfsAppointments.AsNoTracking().Where(x => x.DeletedAt == null).ToListAsync(ct);
        foreach (var item in vfsList)
        {
            evaluated++;
            try
            {
                var key = $"VFS:{item.Id}";
                if (existingMap.TryGetValue(key, out var isFinal) && isFinal) { skipped++; continue; }

                var rec = await FinalizeOrUpdateProfitAsync("VFS", item.Id, ct);
                if (rec == null)
                {
                    await CreateEstimatedProfitRecordAsync(item.CreatedBy, "VFS", item.Id, item.DocumentNumber, item.PayableAmount, item.ExpectedSupplierCost, item.IsInhouse, item.CreatedAt ?? DateTime.UtcNow, ct);
                    created++;
                }
                else updated++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error backfilling VFS #{Id}", item.Id);
                errors++;
            }
        }

        // 3. Holiday Packages
        var hols = await _db.HolidayPackages.AsNoTracking().Where(x => x.DeletedAt == null).ToListAsync(ct);
        foreach (var item in hols)
        {
            evaluated++;
            try
            {
                var key = $"Holiday:{item.Id}";
                if (existingMap.TryGetValue(key, out var isFinal) && isFinal) { skipped++; continue; }

                var rec = await FinalizeOrUpdateProfitAsync("Holiday", item.Id, ct);
                if (rec == null)
                {
                    await CreateEstimatedProfitRecordAsync(item.CreatedBy, "Holiday", item.Id, item.DocumentNumber, item.PayableAmount, item.ExpectedSupplierCost, item.IsInhouse, item.CreatedAt ?? DateTime.UtcNow, ct);
                    created++;
                }
                else updated++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error backfilling Holiday #{Id}", item.Id);
                errors++;
            }
        }

        // 4. Flight Bookings
        var flights = await _db.FlightBookings.AsNoTracking().ToListAsync(ct);
        foreach (var item in flights)
        {
            evaluated++;
            try
            {
                var key = $"Flight:{item.Id}";
                if (existingMap.TryGetValue(key, out var isFinal) && isFinal) { skipped++; continue; }

                var rec = await FinalizeOrUpdateProfitAsync("Flight", item.Id, ct);
                if (rec == null)
                {
                    await CreateEstimatedProfitRecordAsync(item.UserId, "Flight", item.Id, item.DocumentNumber, item.PayableAmount, item.ExpectedSupplierCost, item.IsInhouse, item.CreatedAt ?? DateTime.UtcNow, ct);
                    created++;
                }
                else updated++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error backfilling Flight #{Id}", item.Id);
                errors++;
            }
        }

        // 5. Hotel Bookings
        var hotels = await _db.HotelBookings.AsNoTracking().ToListAsync(ct);
        foreach (var item in hotels)
        {
            evaluated++;
            try
            {
                var key = $"Hotel:{item.Id}";
                if (existingMap.TryGetValue(key, out var isFinal) && isFinal) { skipped++; continue; }

                var rec = await FinalizeOrUpdateProfitAsync("Hotel", item.Id, ct);
                if (rec == null)
                {
                    await CreateEstimatedProfitRecordAsync(item.UserId, "Hotel", item.Id, item.DocumentNumber, item.PayableAmount, item.ExpectedSupplierCost, item.IsInhouse, item.CreatedAt ?? DateTime.UtcNow, ct);
                    created++;
                }
                else updated++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error backfilling Hotel #{Id}", item.Id);
                errors++;
            }
        }

        // 6. Transportations
        var transports = await _db.Transportations.AsNoTracking().Where(x => x.DeletedAt == null).ToListAsync(ct);
        foreach (var item in transports)
        {
            evaluated++;
            try
            {
                var key = $"Transport:{item.Id}";
                if (existingMap.TryGetValue(key, out var isFinal) && isFinal) { skipped++; continue; }

                var rec = await FinalizeOrUpdateProfitAsync("Transport", item.Id, ct);
                if (rec == null)
                {
                    await CreateEstimatedProfitRecordAsync(item.CreatedBy, "Transport", item.Id, item.DocumentNumber, item.PayableAmount, item.ExpectedSupplierCost, item.IsInhouse, item.CreatedAt ?? DateTime.UtcNow, ct);
                    created++;
                }
                else updated++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error backfilling Transport #{Id}", item.Id);
                errors++;
            }
        }

        // 7. Other Services
        var others = await _db.OtherServices.AsNoTracking().Where(x => x.DeletedAt == null).ToListAsync(ct);
        foreach (var item in others)
        {
            evaluated++;
            try
            {
                var key = $"Other:{item.Id}";
                if (existingMap.TryGetValue(key, out var isFinal) && isFinal) { skipped++; continue; }

                var rec = await FinalizeOrUpdateProfitAsync("Other", item.Id, ct);
                if (rec == null)
                {
                    await CreateEstimatedProfitRecordAsync(item.CreatedBy, "Other", item.Id, item.DocumentNumber, item.PayableAmount, item.ExpectedSupplierCost, item.IsInhouse, item.CreatedAt ?? DateTime.UtcNow, ct);
                    created++;
                }
                else updated++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error backfilling Other #{Id}", item.Id);
                errors++;
            }
        }

        // Re-sync all salesmen user.ProfitGenerated strictly from IsFinal records
        var allSalesmen = await _db.Users.ToListAsync(ct);
        foreach (var user in allSalesmen)
        {
            var totalProfit = await _db.SalesmanProfitRecords
                .Where(r => r.SalesmanId == user.Id && r.IsFinal)
                .SumAsync(r => (decimal?)r.ProfitAmount, ct) ?? 0m;
            user.ProfitGenerated = totalProfit;
        }
        await _db.SaveChangesAsync(ct);

        return new BackfillReport(evaluated, created, updated, skipped, errors);
    }

    // ─── Internal Helper: Resolve Booking Ownership and Info ────────────────
    private async Task<(long SalesmanId, long RealBookingId, string? DocumentNumber, decimal PayableAmount, decimal ExpectedSupplierCost, DateTime CreatedAt, bool IsInhouse, bool IsRejected, decimal RejectionFee, string? ProcessingStatus, bool IsDealClosed)> ResolveBookingInfoAsync(
        string bookingType, string bookingIdOrRef, CancellationToken ct)
    {
        var normType = NormalizeBookingType(bookingType);
        long.TryParse(bookingIdOrRef, out var bId);

        switch (normType)
        {
            case "Visa":
                var visa = await _db.SalesVisaApplications.AsNoTracking().Include(v => v.Applicants)
                    .FirstOrDefaultAsync(x => (bId > 0 && x.Id == bId) || (x.DocumentNumber != null && x.DocumentNumber == bookingIdOrRef), ct);
                if (visa != null)
                {
                    bool isVisaInhouse = visa.Applicants != null && visa.Applicants.Count > 0
                        ? visa.Applicants.All(a => a.IsInhouse)
                        : visa.IsInhouse;
                    decimal visaExpectedCost = visa.Applicants != null && visa.Applicants.Count > 0
                        ? visa.Applicants.Where(a => !a.IsInhouse).Sum(a => a.ExpectedSupplierCost)
                        : visa.ExpectedSupplierCost;
                    return (visa.UserId, visa.Id, visa.DocumentNumber, visa.PayableAmount, visaExpectedCost, visa.CreatedAt ?? DateTime.UtcNow, isVisaInhouse, string.Equals(visa.ProcessingStatus, "rejected", StringComparison.OrdinalIgnoreCase), visa.RejectionFee, visa.ProcessingStatus, visa.IsDealClosed);
                }
                break;

            case "VFS":
                var vfs = await _db.VfsAppointments.AsNoTracking()
                    .FirstOrDefaultAsync(x => (bId > 0 && x.Id == bId) || (x.DocumentNumber != null && x.DocumentNumber == bookingIdOrRef), ct);
                if (vfs != null)
                    return (vfs.CreatedBy, vfs.Id, vfs.DocumentNumber, vfs.PayableAmount, vfs.ExpectedSupplierCost, vfs.CreatedAt ?? DateTime.UtcNow, vfs.IsInhouse, false, 0m, vfs.ProcessingStatus, vfs.IsDealClosed);
                break;

            case "Holiday":
                var hol = await _db.HolidayPackages.AsNoTracking()
                    .FirstOrDefaultAsync(x => (bId > 0 && x.Id == bId) || (x.DocumentNumber != null && x.DocumentNumber == bookingIdOrRef), ct);
                if (hol != null)
                    return (hol.CreatedBy, hol.Id, hol.DocumentNumber, hol.PayableAmount, hol.ExpectedSupplierCost, hol.CreatedAt ?? DateTime.UtcNow, hol.IsInhouse, false, 0m, hol.ProcessingStatus, hol.IsDealClosed);
                break;

            case "Flight":
                var fli = await _db.FlightBookings.AsNoTracking().Include(f => f.Passengers)
                    .FirstOrDefaultAsync(x => (bId > 0 && x.Id == bId) || (x.DocumentNumber != null && x.DocumentNumber == bookingIdOrRef), ct);
                if (fli != null)
                {
                    bool isFlightInhouse = fli.Passengers != null && fli.Passengers.Count > 0
                        ? fli.Passengers.All(p => p.IsInhouse)
                        : fli.IsInhouse;
                    decimal flightExpectedCost = fli.Passengers != null && fli.Passengers.Count > 0 ? fli.Passengers.Sum(p => p.ExpectedSupplierCost) : fli.ExpectedSupplierCost;
                    return (fli.UserId, fli.Id, fli.DocumentNumber, fli.PayableAmount, flightExpectedCost, fli.CreatedAt ?? DateTime.UtcNow, isFlightInhouse, false, 0m, fli.ProcessingStatus, fli.IsDealClosed);
                }
                break;

            case "Hotel":
                var hot = await _db.HotelBookings.AsNoTracking()
                    .FirstOrDefaultAsync(x => (bId > 0 && x.Id == bId) || (x.DocumentNumber != null && x.DocumentNumber == bookingIdOrRef), ct);
                if (hot != null)
                    return (hot.UserId, hot.Id, hot.DocumentNumber, hot.PayableAmount, hot.ExpectedSupplierCost, hot.CreatedAt ?? DateTime.UtcNow, hot.IsInhouse, false, 0m, hot.ProcessingStatus, hot.IsDealClosed);
                break;

            case "Transport":
                var tr = await _db.Transportations.AsNoTracking()
                    .FirstOrDefaultAsync(x => (bId > 0 && x.Id == bId) || (x.DocumentNumber != null && x.DocumentNumber == bookingIdOrRef), ct);
                if (tr != null)
                    return (tr.CreatedBy, tr.Id, tr.DocumentNumber, tr.PayableAmount, tr.ExpectedSupplierCost, tr.CreatedAt ?? DateTime.UtcNow, tr.IsInhouse, false, 0m, tr.ProcessingStatus, tr.IsDealClosed);
                break;

            case "Other":
                var ot = await _db.OtherServices.AsNoTracking()
                    .FirstOrDefaultAsync(x => (bId > 0 && x.Id == bId) || (x.DocumentNumber != null && x.DocumentNumber == bookingIdOrRef), ct);
                if (ot != null)
                    return (ot.CreatedBy, ot.Id, ot.DocumentNumber, ot.PayableAmount, ot.ExpectedSupplierCost, ot.CreatedAt ?? DateTime.UtcNow, ot.IsInhouse, false, 0m, ot.ProcessingStatus, ot.IsDealClosed);
                break;
        }

        return (0, 0, null, 0m, 0m, DateTime.UtcNow, false, false, 0m, null, false);
    }

    private async Task SyncBookingProfitEntityAndUserTotalAsync(string normType, long bookingId, decimal profitAmount, long salesmanId, CancellationToken ct)
    {
        if (bookingId > 0)
        {
            switch (normType)
            {
                case "Visa":
                    var visa = await _db.SalesVisaApplications.FirstOrDefaultAsync(x => x.Id == bookingId, ct);
                    if (visa != null) visa.Profit = profitAmount;
                    break;
                case "VFS":
                    var vfs = await _db.VfsAppointments.FirstOrDefaultAsync(x => x.Id == bookingId, ct);
                    if (vfs != null) vfs.Profit = profitAmount;
                    break;
                case "Holiday":
                    var hol = await _db.HolidayPackages.FirstOrDefaultAsync(x => x.Id == bookingId, ct);
                    if (hol != null) hol.Profit = profitAmount;
                    break;
                case "Flight":
                    var fli = await _db.FlightBookings.FirstOrDefaultAsync(x => x.Id == bookingId, ct);
                    if (fli != null) fli.Profit = profitAmount;
                    break;
                case "Hotel":
                    var hot = await _db.HotelBookings.FirstOrDefaultAsync(x => x.Id == bookingId, ct);
                    if (hot != null) hot.Profit = profitAmount;
                    break;
                case "Transport":
                    var tr = await _db.Transportations.FirstOrDefaultAsync(x => x.Id == bookingId, ct);
                    if (tr != null) tr.Profit = profitAmount;
                    break;
                case "Other":
                    var ot = await _db.OtherServices.FirstOrDefaultAsync(x => x.Id == bookingId, ct);
                    if (ot != null) ot.Profit = profitAmount;
                    break;
            }
        }

        if (salesmanId > 0)
        {
            var totalProfit = await _db.SalesmanProfitRecords
                .Where(r => r.SalesmanId == salesmanId && r.IsFinal)
                .SumAsync(r => (decimal?)r.ProfitAmount, ct) ?? 0m;

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == salesmanId, ct);
            if (user != null)
            {
                user.ProfitGenerated = totalProfit;
            }
        }
    }
}

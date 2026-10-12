using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Domain.Entities;
using TexasPortal.Domain.Enums;

namespace TexasPortal.Application.Sales.Profit;

public record SalesmanProfitSummary(
    long SalesmanId,
    decimal TotalProfit,
    decimal MonthlyProfit,
    decimal AllTimeProfit,
    decimal ApprovedProfit,
    decimal EstimatedProfit,
    int TotalBookingsCount,
    int FinalizedCount
);

public record ServiceProfitBreakdown(
    string ServiceType,
    int BookingCount,
    decimal PayableAmount,
    decimal SupplierCost,
    decimal ProfitAmount,
    int FinalizedCount
);

public record SalesmanProfitItemDto(
    long Id,
    long SalesmanId,
    string BookingType,
    long BookingId,
    string? DocumentNumber,
    string? ClientName,
    decimal PayableAmount,
    decimal FinalSupplierCost,
    decimal ProfitAmount,
    string ProfitType,
    string SupplierCostSource,
    decimal? AccountsApprovedTotal,
    bool IsFinal,
    DateTime BookingCreatedAt,
    DateTime? FinalizedAt
);

public record SalesmanServiceProfitSummary(
    long SalesmanId,
    decimal TotalProfit,
    List<ServiceProfitBreakdown> Breakdown
);

public record BackfillReport(
    int TotalBookingsEvaluated,
    int RecordsCreated,
    int RecordsUpdated,
    int RecordsSkipped,
    int Errors
);

public record SalesmanProfitAdjustmentRequest(
    string BookingType,
    string BookingIdOrRef,
    SalesmanProfitAdjustmentType AdjustmentType,
    decimal Amount,
    string ReferenceType,
    string ReferenceId,
    string IdempotencyKey,
    string Description,
    long? CreatedByUserId = null
);

public record ProfitAdjustmentDto(
    long Id,
    string Type,
    decimal Amount,
    string Description,
    string ReferenceType,
    string ReferenceId,
    string IdempotencyKey,
    DateTime CreatedAt,
    long? CreatedByUserId
);

public record ProfitDetailResponse(
    string BookingType,
    long BookingId,
    string? DocumentNumber,
    decimal PayableAmount,
    decimal FinalSupplierCost,
    decimal BaseProfit,
    decimal? RejectionFee,
    decimal TotalAdjustments,
    decimal FinalProfit,
    decimal ProfitAmount,
    string ProfitType,
    string SupplierCostSource,
    decimal? AccountsApprovedTotal,
    bool IsFinal,
    DateTime BookingCreatedAt,
    DateTime? FinalizedAt,
    DateTime? LastReconciledAt,
    List<ProfitAdjustmentDto> Adjustments
);

public record ProfitReconciliationResult(
    string BookingType,
    string BookingIdOrRef,
    decimal BaseProfit,
    decimal TotalIndependentAdjustments,
    decimal ExpectedFinalProfit,
    decimal StoredFinalProfit,
    decimal Difference,
    bool IsMatch,
    string Status
);

public interface ISalesmanProfitService
{
    Task<SalesmanProfitRecord> CreateEstimatedProfitRecordAsync(
        long salesmanId,
        string bookingType,
        long bookingId,
        string? documentNumber,
        decimal payableAmount,
        decimal estimatedSupplierCost,
        bool isInhouse,
        DateTime bookingCreatedAt,
        CancellationToken ct = default);

    Task<SalesmanProfitRecord?> FinalizeOrUpdateProfitAsync(
        string bookingType,
        long bookingId,
        CancellationToken ct = default);

    Task<SalesmanProfitRecord?> FinalizeOrUpdateProfitAsync(
        string bookingType,
        string bookingIdOrRef,
        CancellationToken ct = default);

    Task<SalesmanProfitRecord?> UnfinalizeProfitAsync(
        string bookingType,
        long bookingId,
        CancellationToken ct = default);

    Task<SalesmanProfitRecord?> UnfinalizeProfitAsync(
        string bookingType,
        string bookingIdOrRef,
        CancellationToken ct = default);

    Task<SalesmanProfitRecord?> RecordRejectionProfitAsync(
        long bookingId,
        decimal rejectionFee,
        CancellationToken ct = default);

    Task<SalesmanProfitRecord?> FinalizeRejectionProfitAsync(
        string bookingType,
        long bookingId,
        decimal rejectionFee,
        CancellationToken ct = default);

    Task<SalesmanProfitAdjustment> AddAdjustmentAsync(
        SalesmanProfitAdjustmentRequest request,
        CancellationToken ct = default);

    Task<SalesmanProfitRecord?> ReconcileProfitAsync(
        string bookingType,
        long bookingId,
        CancellationToken ct = default);

    Task<SalesmanProfitRecord?> ReconcileProfitAsync(
        string bookingType,
        string bookingIdOrRef,
        CancellationToken ct = default);

    Task<ProfitReconciliationResult> ReconcileProfitDetailsAsync(
        string bookingType,
        string bookingIdOrRef,
        CancellationToken ct = default);

    Task<ProfitDetailResponse?> GetProfitDetailAsync(
        string bookingType,
        string bookingIdOrRef,
        CancellationToken ct = default);

    Task<decimal> GetTotalProfitAsync(long salesmanId, CancellationToken ct = default);

    Task<decimal> GetMonthlyProfitAsync(long salesmanId, int year, int month, CancellationToken ct = default);

    Task<SalesmanProfitSummary> GetProfitSummaryAsync(long salesmanId, CancellationToken ct = default);

    Task<SalesmanServiceProfitSummary> GetProfitSummaryByServiceAsync(long salesmanId, CancellationToken ct = default);

    Task<SalesmanProfitRecord?> GetBookingProfitAsync(string bookingType, long bookingId, CancellationToken ct = default);

    Task<SalesmanProfitRecord?> GetBookingProfitAsync(string bookingType, string bookingIdOrRef, CancellationToken ct = default);

    Task<IEnumerable<SalesmanProfitItemDto>> GetSalesmanProfitItemDtosAsync(
        long salesmanId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken ct = default);

    Task<IEnumerable<SalesmanProfitRecord>> GetSalesmanProfitRecordsAsync(
        long salesmanId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken ct = default);

    Task EnsureSalesmanProfitRecordsAsync(long salesmanId, CancellationToken ct = default);

    Task<BackfillReport> BackfillAllProfitRecordsAsync(CancellationToken ct = default);
}

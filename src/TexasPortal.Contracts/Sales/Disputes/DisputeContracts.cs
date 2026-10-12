using System;

namespace TexasPortal.Contracts.Sales.Disputes;

public sealed record DisputeResponse(
    long Id,
    string? DocumentNumber,
    long UserId,
    long? HeadId,
    string? HeadName,
    long DisputableId,
    string DisputableType,
    string Subject,
    string Description,
    string Status,
    string? Resolution,
    string? ResolutionNote,
    string? ResolvedBy,
    DateTime? ResolvedAt,
    bool IsRefund,
    decimal? RefundAmount,
    decimal? ApprovedRefundAmount,
    bool RefundProcessed,
    string? RefundStatus,
    DateTime? RefundDate,
    DateTime? CreatedAt,
    string? SalesmanName = null,
    decimal? BookingPayableAmount = null,
    decimal? BookingSupplierCost = null,
    decimal? HeadWallet = null,
    string? DisputableDocumentNumber = null,
    string? Iban = null,
    string? Name = null);

public sealed record DisputesDashboardResponse(
    int Total,
    int Pending,
    int Resolved,
    int Rejected,
    decimal RequestedRefundAmount,
    decimal ApprovedRefundAmount);

public sealed record DisputeDecisionRequest(
    string Decision,
    decimal? ApprovedRefundAmount = null,
    string? Resolution = null);

public sealed record ResolveDisputeRequest(
    string Action,
    string? Resolution = null,
    string? ResolvedBy = null,
    decimal? ApprovedRefundAmount = null);

public sealed record CreateDisputeRequest(
    long? HeadId,
    long DisputableId,
    string DisputableType,
    string Subject,
    string Description,
    bool IsRefund = false,
    decimal? RefundAmount = null,
    string? Iban = null,
    string? Name = null);

public sealed record DisputeBookingOptionResponse(
    long Id,
    string Label,
    long? HeadId,
    string? HeadName,
    decimal Amount);

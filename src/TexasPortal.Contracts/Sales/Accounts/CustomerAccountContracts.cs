using System;
using System.Collections.Generic;

namespace TexasPortal.Contracts.Sales.Accounts;

public record CustomerAccountSummaryResponse(
    long HeadId,
    string HeadName,
    string HeadType,
    decimal Balance,
    string Currency = "BHD",
    DateTime? LastUpdated = null
);

public record CustomerApplicationReceivableSummary(
    int OpenApplications,
    decimal TotalOutstandingAmount,
    IReadOnlyList<ApplicationReceivableItem> Items
);

public record ApplicationReceivableItem(
    long ReceivableId,
    string ApplicationNumber,
    string Module,
    decimal TotalAmount,
    decimal OutstandingAmount,
    string PaymentStatus,
    DateTime CreatedAt
);

public record CreditBookingRequest(
    string BookingId,
    string BookingNumber,
    string BookingModule,
    long HeadId,
    string HeadType,
    long SalesmanId,
    decimal TotalAmount,
    string CreditTerm = "Immediate",
    bool AutoApplyBalance = true,
    string? IdempotencyKey = null,
    string? Remarks = null
);

public record CreditAllocationResponse(
    long? BalanceTransactionId,
    long? CreditReceivableId,
    decimal TotalAmount,
    decimal BalanceApplied,
    decimal ReceivableCreated,
    string AllocationType,
    string PaymentStatus,
    decimal BalanceAfter,
    decimal OutstandingAfter,
    string Currency = "BHD"
);

public record AccountTimelineItemResponse(
    long Id,
    DateTime TransactionDate,
    string EventType,
    string ReferenceNumber,
    string ServiceType,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance,
    string Description,
    string? Remarks
);

public record AccountStatementResponse(
    CustomerAccountSummaryResponse Summary,
    List<AccountTimelineItemResponse> Timeline,
    decimal OpenReceivablesCount
);

using System;
using System.Collections.Generic;

namespace TexasPortal.Contracts.Sales.Receivables;

public record CreateReceivableDto(
    string BookingId,
    string BookingNumber,
    string BookingModule,
    long? HeadId,
    string HeadType,
    long? CustomerId,
    long? AgentId,
    long SalesmanId,
    decimal TotalAmount,
    string CreditTerm = "Days30",
    string? Remarks = null
);

public record ApplyPaymentAllocationDto(
    long ReceivableId,
    decimal Amount
);

public record CreateReceiptWithAllocationsDto(
    long? HeadId,
    string ClientType,
    string Narration,
    decimal Amount,
    string PaymentMode,
    long? BankId,
    string? BankName,
    string? PaymentProof,
    List<ApplyPaymentAllocationDto> Allocations
);

public record CreditReceivableResponse(
    long Id,
    string BookingId,
    string BookingNumber,
    string BookingModule,
    long? HeadId,
    string HeadType,
    long? CustomerId,
    long? AgentId,
    long SalesmanId,
    string Currency,
    decimal TotalAmount,
    decimal OutstandingAmount,
    string Status,
    string PaymentStatus,
    string Priority,
    string CreditTerm,
    DateTime DueDate,
    DateTime CreatedDate,
    DateTime? ClosedDate,
    string? Remarks
);

public record ReceivableTransactionResponse(
    long Id,
    long ReceivableId,
    string TransactionType,
    decimal Amount,
    string? ReferenceId,
    DateTime TransactionDate,
    long CreatedBy,
    string? Remarks
);

public record RunningLedgerEntryResponse(
    long Id,
    DateTime Date,
    string Type,
    string Description,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance
);

public record CustomerRunningStatementResponse(
    long HeadId,
    string Name,
    string HeadType,
    decimal OpeningBalance,
    decimal TotalCredit,
    decimal TotalPaid,
    decimal ClosingBalance,
    double AvgCollectionDays,
    DateTime? LastPaymentDate,
    DateTime? NextDueDate,
    List<RunningLedgerEntryResponse> Entries
);

public record AgingBucketDto(
    string BucketName, // Current, 0-30, 31-60, 61-90, 90+
    decimal OutstandingAmount,
    int CustomerCount,
    int AgentCount,
    int ApplicationCount,
    double CollectionPercentage
);

public record CreditAgingReportResponse(
    decimal TotalOutstanding,
    List<AgingBucketDto> Buckets
);

public record CreditDashboardResponse(
    decimal TotalOutstanding,
    decimal TodaysCreditSales,
    decimal TodaysCollections,
    decimal OverdueReceivables,
    double CollectionPercentage,
    double AvgCollectionDays,
    double Dso,
    int ActiveDebtorsCount
);

public record HeadReceivableApplicationResponse(
    long ReceivableId,
    string ApplicationNumber,
    string Service,
    decimal OriginalAmount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    string Status,
    DateTime CreatedDate
);

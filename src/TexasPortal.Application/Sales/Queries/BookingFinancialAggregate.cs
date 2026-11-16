using System;

namespace TexasPortal.Application.Sales.Queries;

public sealed record BookingFinancialAggregate(
    string ServiceType,
    long BookingId,
    string? DocumentNumber,
    long CreatorId,
    string? ClientType,
    long? AgentId,
    decimal PayableAmount,
    decimal ExpectedSupplierCost,
    decimal ExpectedProfit,
    string? PaymentMethod,
    string? PaymentStatus,
    string? ProcessingStatus,
    DateTime CreatedAt,
    bool IsDealClosed
);


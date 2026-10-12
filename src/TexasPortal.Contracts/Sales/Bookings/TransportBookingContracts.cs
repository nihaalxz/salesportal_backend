using System;
using System.Collections.Generic;

namespace TexasPortal.Contracts.Sales.Bookings;

public sealed record TransportPassengerRequest(
    string? Prefix,
    string Name,
    decimal ExpectedSupplierCost = 0m,
    decimal Amount = 0m,
    bool IsInhouse = true,
    long? SupplierId = null
);

public sealed record TransportPassengerResponse(
    long Id,
    string? Prefix,
    string Name,
    decimal PayableAmount,
    decimal ExpectedSupplierCost,
    decimal Profit,
    bool IsInhouse,
    long? SupplierId,
    string? SupplierName
);

/// <summary>
/// Request contract for creating a Transportation booking.
/// </summary>
public sealed record CreateTransportationRequest(
    string ClientType,
    long? HeadId,
    string? Agent,
    long? AgentId,
    string? Prefix,
    string? Name,
    string? CareOf,
    long? SupplierId,
    string? Pickup,
    string? Drop,
    DateTime? DropDate,
    TimeSpan? DropTime,
    decimal ExpectedSupplierCost,
    decimal Amount, // PayableAmount
    string PaymentMethod, // "cash" | "wallet" | "credit" | "online"
    long? BankId = null,
    string? BankName = null,
    string? Vehicle = null,
    string? PaymentProof = null,
    bool IsInhouse = false,
    List<TransportPassengerRequest>? Passengers = null
);

/// <summary>
/// Response contract for a successfully created Transportation booking.
/// </summary>
public sealed record TransportationResponse(
    long Id,
    long CreatedBy,
    long? HeadId,
    string ClientType,
    string? Agent,
    long? AgentId,
    string? Prefix,
    string? Name,
    string? CareOf,
    long? SupplierId,
    string? SupplierName,
    string? Pickup,
    string? Drop,
    DateTime? DropDate,
    TimeSpan? DropTime,
    decimal PayableAmount,
    decimal ExpectedSupplierCost,
    decimal Profit,
    string PaymentMethod,
    string? Vehicle,
    string? PaymentProof,
    string PaymentStatus,
    string ProcessingStatus,
    DateTime CreatedAt,
    string? DocumentNumber = null,
    List<TransportPassengerResponse>? Passengers = null
);

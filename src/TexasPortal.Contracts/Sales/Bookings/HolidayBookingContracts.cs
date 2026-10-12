using System;
using System.Collections.Generic;

namespace TexasPortal.Contracts.Sales.Bookings;

public sealed record HolidayPassengerRequest(
    string? Prefix,
    string PassengerName,
    decimal ExpectedSupplierCost = 0m,
    decimal Amount = 0m,
    bool IsInhouse = true,
    long? SupplierId = null
);

public sealed record HolidayPassengerResponse(
    long Id,
    string? Prefix,
    string PassengerName,
    decimal PayableAmount,
    decimal ExpectedSupplierCost,
    decimal Profit,
    bool IsInhouse,
    long? SupplierId,
    string? SupplierName
);

/// <summary>
/// Request contract for creating a Holiday Package booking.
/// </summary>
public sealed record CreateHolidayPackageRequest(
    string ClientType,
    long? HeadId,
    string? Agent,
    long? AgentId,
    string Prefix,
    string PassengerName,
    long? SupplierId,
    string Country,
    int Days,
    decimal ExpectedSupplierCost,
    decimal Amount, // PayableAmount
    string PaymentMethod, // "cash" | "wallet" | "credit" | "online"
    long? BankId = null,
    string? BankName = null,
    string? PaymentProof = null,
    bool IsInhouse = false,
    List<HolidayPassengerRequest>? Passengers = null
);

/// <summary>
/// Response contract for a successfully created Holiday Package booking.
/// </summary>
public sealed record HolidayPackageResponse(
    long Id,
    long CreatedBy,
    long? HeadId,
    string ClientType,
    string? Agent,
    long? AgentId,
    string Prefix,
    string PassengerName,
    long? SupplierId,
    string Country,
    int Days,
    decimal PayableAmount,
    decimal ExpectedSupplierCost,
    decimal Profit,
    string PaymentMode,
    string? PaymentProof,
    string PaymentStatus,
    string ProcessingStatus,
    DateTime CreatedAt,
    string? DocumentNumber = null,
    List<HolidayPassengerResponse>? Passengers = null
);

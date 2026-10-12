using System;
using System.Collections.Generic;

namespace TexasPortal.Contracts.Sales.Bookings;

public sealed record HotelGuestRequest(
    string? Prefix,
    string GuestName,
    decimal ExpectedSupplierCost = 0m,
    decimal Amount = 0m,
    bool IsInhouse = true,
    long? SupplierId = null
);

public sealed record HotelGuestResponse(
    long Id,
    string? Prefix,
    string GuestName,
    decimal PayableAmount,
    decimal ExpectedSupplierCost,
    decimal Profit,
    bool IsInhouse,
    long? SupplierId,
    string? SupplierName
);

/// <summary>
/// Request contract for creating a new Hotel Booking.
/// </summary>
public sealed record CreateHotelBookingRequest(
    string ClientType,       // "customer" | "agent"
    long? HeadId,
    string? Agent,
    long? AgentId,
    bool IsInhouse,
    long? SupplierId,
    decimal ExpectedSupplierCost,
    decimal Amount,          // payableAmount
    string? Prefix,
    string GuestName,
    string? HotelName,
    string Country,
    int Days,
    DateTime CheckInDate,
    string PaymentMethod,    // "cash" | "credit" | "wallet" | "online"
    long? BankId,
    string? BankName,
    string? PaymentProof,
    string? PaymentReference,
    string? Status = null,
    List<HotelGuestRequest>? Guests = null
);

/// <summary>
/// Response returned after a successful hotel booking creation.
/// </summary>
public sealed record HotelBookingResponse(
    long Id,
    long UserId,
    long? HeadId,
    string ClientType,
    string GuestName,
    string? HotelName,
    string Country,
    int Days,
    DateTime CheckInDate,
    decimal PayableAmount,
    decimal ExpectedSupplierCost,
    decimal Profit,
    string PaymentMethod,
    string PaymentStatus,
    string ProcessingStatus,
    string OrderStatus,
    DateTime CreatedAt,
    string? DocumentNumber = null,
    List<HotelGuestResponse>? Guests = null
);

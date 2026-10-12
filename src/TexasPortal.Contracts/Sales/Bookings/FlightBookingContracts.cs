using System;
using System.Collections.Generic;

namespace TexasPortal.Contracts.Sales.Bookings;

/// <summary>
/// Request payload for a flight passenger.
/// </summary>
public sealed record FlightPassengerRequest(
    string? Prefix,
    string PassengerName,
    decimal ExpectedSupplierCost,
    decimal Amount, // selling price
    bool IsInhouse,
    long? SupplierId,
    string? TicketPnr = null
);

/// <summary>
/// Request contract for creating a new Flight Booking.
/// </summary>
public sealed record CreateFlightBookingRequest(
    string ClientType, // "customer" | "agent"
    long? HeadId,
    string? Agent,
    long? AgentId,
    string? CareOf,
    string FromLocation,
    string ToLocation,
    string PaymentMethod, // "cash" | "wallet" | "credit" | "online"
    long? BankId,
    string? BankName,
    string? PaymentProof,
    List<FlightPassengerRequest> Passengers,
    bool IsInhouse = false,
    long? SupplierId = null,
    string? SupplierName = null,
    decimal ExpectedSupplierCost = 0m,
    DateTime? TravelDate = null,
    string? AirlineCompany = null,
    string? TicketPnr = null
);

/// <summary>
/// Response summary for a flight passenger.
/// </summary>
public sealed record FlightPassengerResponse(
    long Id,
    string? Prefix,
    string PassengerName,
    decimal PayableAmount,
    decimal ExpectedSupplierCost,
    decimal Profit,
    bool IsInhouse,
    long? SupplierId,
    string? SupplierName,
    string? TicketPnr = null
);

/// <summary>
/// Response returned after successfully creating a flight booking.
/// </summary>
public sealed record FlightBookingResponse(
    long Id,
    long UserId,
    long? HeadId,
    string ClientType,
    string? Agent,
    long? AgentId,
    string? CareOf,
    string FromLocation,
    string ToLocation,
    string PaymentMethod,
    string PaymentStatus,
    string ProcessingStatus,
    decimal PayableAmount,
    decimal Profit, // aggregated profit for reporting
    string? PaymentProof,
    List<FlightPassengerResponse> Passengers,
    DateTime CreatedAt,
    string? DocumentNumber = null,
    DateTime? TravelDate = null,
    string? AirlineCompany = null,
    string? TicketPnr = null
);

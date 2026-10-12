using TexasPortal.Contracts.Sales.Bookings;

namespace TexasPortal.Application.Sales.Bookings.Flights.CreateFlightBooking;

public sealed record CreateFlightBookingCommand(
    long CurrentUserId,
    string IdempotencyKey,
    CreateFlightBookingRequest Request
);

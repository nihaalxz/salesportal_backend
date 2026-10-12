using TexasPortal.Contracts.Sales.Bookings;

namespace TexasPortal.Application.Sales.Bookings.Hotels.CreateHotelBooking;

public sealed record CreateHotelBookingCommand(
    long CurrentUserId,
    string IdempotencyKey,
    CreateHotelBookingRequest Request
);

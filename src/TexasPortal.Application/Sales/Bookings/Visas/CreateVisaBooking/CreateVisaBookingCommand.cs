using TexasPortal.Contracts.Sales.Bookings;

namespace TexasPortal.Application.Sales.Bookings.Visas.CreateVisaBooking;

public sealed record CreateVisaBookingCommand(
    long CurrentUserId,
    string IdempotencyKey,
    CreateVisaBookingRequest Request
);

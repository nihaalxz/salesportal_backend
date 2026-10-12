using System;
using TexasPortal.Contracts.Sales.Bookings;

namespace TexasPortal.Application.Sales.Bookings.OtherServices.CreateOtherServiceBooking;

public sealed record CreateOtherServiceBookingCommand(
    long CurrentUserId,
    string IdempotencyKey,
    CreateOtherServiceBookingRequest Request
);

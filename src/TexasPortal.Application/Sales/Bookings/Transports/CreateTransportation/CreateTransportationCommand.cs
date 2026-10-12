using TexasPortal.Contracts.Sales.Bookings;

namespace TexasPortal.Application.Sales.Bookings.Transports.CreateTransportation;

public sealed record CreateTransportationCommand(
    long CurrentUserId,
    string IdempotencyKey,
    CreateTransportationRequest Request
);

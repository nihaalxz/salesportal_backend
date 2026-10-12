using TexasPortal.Contracts.Sales.Bookings;

namespace TexasPortal.Application.Sales.Bookings.Vfs.CreateVfsAppointment;

public sealed record CreateVfsAppointmentCommand(
    long CurrentUserId,
    string IdempotencyKey,
    CreateVfsAppointmentRequest Request
);

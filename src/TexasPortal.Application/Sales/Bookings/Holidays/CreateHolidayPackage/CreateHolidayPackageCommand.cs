using TexasPortal.Contracts.Sales.Bookings;

namespace TexasPortal.Application.Sales.Bookings.Holidays.CreateHolidayPackage;

public sealed record CreateHolidayPackageCommand(
    long CurrentUserId,
    string IdempotencyKey,
    CreateHolidayPackageRequest Request
);

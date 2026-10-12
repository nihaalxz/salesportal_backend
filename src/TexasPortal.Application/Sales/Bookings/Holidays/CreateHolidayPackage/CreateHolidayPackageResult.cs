using System.Collections.Generic;
using TexasPortal.Contracts.Sales.Bookings;

namespace TexasPortal.Application.Sales.Bookings.Holidays.CreateHolidayPackage;

public enum CreateHolidayPackageResultStatus
{
    Success,
    ValidationError,
    IdempotencyConflict,
    InsufficientWallet,
    HeadNotFound
}

public sealed class CreateHolidayPackageResult
{
    public CreateHolidayPackageResultStatus Status { get; private init; }
    public HolidayPackageResponse? Response { get; private init; }
    public Dictionary<string, string[]>? Errors { get; private init; }
    public string? ConflictMessage { get; private init; }

    public static CreateHolidayPackageResult Success(HolidayPackageResponse response) =>
        new() { Status = CreateHolidayPackageResultStatus.Success, Response = response };

    public static CreateHolidayPackageResult ValidationError(Dictionary<string, string[]> errors) =>
        new() { Status = CreateHolidayPackageResultStatus.ValidationError, Errors = errors };

    public static CreateHolidayPackageResult IdempotencyConflict(string message) =>
        new() { Status = CreateHolidayPackageResultStatus.IdempotencyConflict, ConflictMessage = message };

    public static CreateHolidayPackageResult InsufficientWallet(string message) =>
        new() { Status = CreateHolidayPackageResultStatus.InsufficientWallet, ConflictMessage = message };

    public static CreateHolidayPackageResult HeadNotFound() =>
        new() { Status = CreateHolidayPackageResultStatus.HeadNotFound };
}

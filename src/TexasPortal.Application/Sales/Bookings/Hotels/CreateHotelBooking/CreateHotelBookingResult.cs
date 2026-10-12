using System.Collections.Generic;
using TexasPortal.Contracts.Sales.Bookings;

namespace TexasPortal.Application.Sales.Bookings.Hotels.CreateHotelBooking;

public enum CreateHotelBookingResultStatus
{
    Success,
    ValidationError,
    IdempotencyConflict,
    InsufficientWallet,
    HeadNotFound
}

public sealed class CreateHotelBookingResult
{
    public CreateHotelBookingResultStatus Status { get; }
    public HotelBookingResponse? Response { get; }
    public Dictionary<string, string[]>? Errors { get; }
    public string? ConflictMessage { get; }

    private CreateHotelBookingResult(
        CreateHotelBookingResultStatus status,
        HotelBookingResponse? response = null,
        Dictionary<string, string[]>? errors = null,
        string? conflictMessage = null)
    {
        Status = status;
        Response = response;
        Errors = errors;
        ConflictMessage = conflictMessage;
    }

    public static CreateHotelBookingResult Success(HotelBookingResponse response) =>
        new(CreateHotelBookingResultStatus.Success, response: response);

    public static CreateHotelBookingResult ValidationError(Dictionary<string, string[]> errors) =>
        new(CreateHotelBookingResultStatus.ValidationError, errors: errors);

    public static CreateHotelBookingResult IdempotencyConflict(string message) =>
        new(CreateHotelBookingResultStatus.IdempotencyConflict, conflictMessage: message);

    public static CreateHotelBookingResult InsufficientWallet() =>
        new(CreateHotelBookingResultStatus.InsufficientWallet);

    public static CreateHotelBookingResult HeadNotFound() =>
        new(CreateHotelBookingResultStatus.HeadNotFound);
}

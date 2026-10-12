using System.Collections.Generic;
using TexasPortal.Contracts.Sales.Bookings;

namespace TexasPortal.Application.Sales.Bookings.Flights.CreateFlightBooking;

public enum CreateFlightBookingResultStatus
{
    Success,
    ValidationError,
    IdempotencyConflict,
    InsufficientWallet,
    HeadNotFound
}

public sealed class CreateFlightBookingResult
{
    public CreateFlightBookingResultStatus Status { get; }
    public FlightBookingResponse? Response { get; }
    public Dictionary<string, string[]>? Errors { get; }
    public string? ConflictMessage { get; }

    private CreateFlightBookingResult(
        CreateFlightBookingResultStatus status,
        FlightBookingResponse? response = null,
        Dictionary<string, string[]>? errors = null,
        string? conflictMessage = null)
    {
        Status = status;
        Response = response;
        Errors = errors;
        ConflictMessage = conflictMessage;
    }

    public static CreateFlightBookingResult Success(FlightBookingResponse response) =>
        new(CreateFlightBookingResultStatus.Success, response: response);

    public static CreateFlightBookingResult ValidationError(Dictionary<string, string[]> errors) =>
        new(CreateFlightBookingResultStatus.ValidationError, errors: errors);

    public static CreateFlightBookingResult IdempotencyConflict(string message) =>
        new(CreateFlightBookingResultStatus.IdempotencyConflict, conflictMessage: message);

    public static CreateFlightBookingResult InsufficientWallet() =>
        new(CreateFlightBookingResultStatus.InsufficientWallet);

    public static CreateFlightBookingResult HeadNotFound() =>
        new(CreateFlightBookingResultStatus.HeadNotFound);
}

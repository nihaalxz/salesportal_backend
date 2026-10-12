using System.Collections.Generic;
using TexasPortal.Contracts.Sales.Bookings;

namespace TexasPortal.Application.Sales.Bookings.Visas.CreateVisaBooking;

public enum CreateVisaBookingResultStatus
{
    Success,
    ValidationError,
    IdempotencyConflict,
    InsufficientWallet,
    HeadNotFound
}

public sealed class CreateVisaBookingResult
{
    public CreateVisaBookingResultStatus Status { get; }
    public VisaBookingResponse? Response { get; }
    public Dictionary<string, string[]>? Errors { get; }
    public string? ConflictMessage { get; }

    private CreateVisaBookingResult(
        CreateVisaBookingResultStatus status,
        VisaBookingResponse? response = null,
        Dictionary<string, string[]>? errors = null,
        string? conflictMessage = null)
    {
        Status = status;
        Response = response;
        Errors = errors;
        ConflictMessage = conflictMessage;
    }

    public static CreateVisaBookingResult Success(VisaBookingResponse response) =>
        new(CreateVisaBookingResultStatus.Success, response: response);

    public static CreateVisaBookingResult ValidationError(Dictionary<string, string[]> errors) =>
        new(CreateVisaBookingResultStatus.ValidationError, errors: errors);

    public static CreateVisaBookingResult IdempotencyConflict(string message) =>
        new(CreateVisaBookingResultStatus.IdempotencyConflict, conflictMessage: message);

    public static CreateVisaBookingResult InsufficientWallet(string message) =>
        new(CreateVisaBookingResultStatus.InsufficientWallet, conflictMessage: message);

    public static CreateVisaBookingResult HeadNotFound() =>
        new(CreateVisaBookingResultStatus.HeadNotFound);
}

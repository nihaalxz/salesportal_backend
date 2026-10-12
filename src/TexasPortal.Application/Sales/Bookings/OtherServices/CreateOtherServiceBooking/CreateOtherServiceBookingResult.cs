using System;
using System.Collections.Generic;
using TexasPortal.Contracts.Sales.Bookings;

namespace TexasPortal.Application.Sales.Bookings.OtherServices.CreateOtherServiceBooking;

public enum CreateOtherServiceBookingResultStatus
{
    Success,
    ValidationError,
    IdempotencyConflict,
    InsufficientWallet,
    HeadNotFound
}

public sealed class CreateOtherServiceBookingResult
{
    public CreateOtherServiceBookingResultStatus Status { get; }
    public OtherServiceBookingResponse? Response { get; }
    public Dictionary<string, string[]>? Errors { get; }
    public string? ConflictMessage { get; }

    private CreateOtherServiceBookingResult(
        CreateOtherServiceBookingResultStatus status,
        OtherServiceBookingResponse? response = null,
        Dictionary<string, string[]>? errors = null,
        string? conflictMessage = null)
    {
        Status = status;
        Response = response;
        Errors = errors;
        ConflictMessage = conflictMessage;
    }

    public static CreateOtherServiceBookingResult Success(OtherServiceBookingResponse response) =>
        new(CreateOtherServiceBookingResultStatus.Success, response: response);

    public static CreateOtherServiceBookingResult ValidationError(Dictionary<string, string[]> errors) =>
        new(CreateOtherServiceBookingResultStatus.ValidationError, errors: errors);

    public static CreateOtherServiceBookingResult IdempotencyConflict(string message) =>
        new(CreateOtherServiceBookingResultStatus.IdempotencyConflict, conflictMessage: message);

    public static CreateOtherServiceBookingResult InsufficientWallet() =>
        new(CreateOtherServiceBookingResultStatus.InsufficientWallet);

    public static CreateOtherServiceBookingResult HeadNotFound() =>
        new(CreateOtherServiceBookingResultStatus.HeadNotFound);
}

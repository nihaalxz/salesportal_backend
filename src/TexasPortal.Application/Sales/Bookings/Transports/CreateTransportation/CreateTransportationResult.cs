using System.Collections.Generic;
using TexasPortal.Contracts.Sales.Bookings;

namespace TexasPortal.Application.Sales.Bookings.Transports.CreateTransportation;

public enum CreateTransportationResultStatus
{
    Success,
    ValidationError,
    IdempotencyConflict,
    InsufficientWallet,
    HeadNotFound
}

public sealed class CreateTransportationResult
{
    public CreateTransportationResultStatus Status { get; private init; }
    public TransportationResponse? Response { get; private init; }
    public Dictionary<string, string[]>? Errors { get; private init; }
    public string? ConflictMessage { get; private init; }

    public static CreateTransportationResult Success(TransportationResponse response) =>
        new() { Status = CreateTransportationResultStatus.Success, Response = response };

    public static CreateTransportationResult ValidationError(Dictionary<string, string[]> errors) =>
        new() { Status = CreateTransportationResultStatus.ValidationError, Errors = errors };

    public static CreateTransportationResult IdempotencyConflict(string message) =>
        new() { Status = CreateTransportationResultStatus.IdempotencyConflict, ConflictMessage = message };

    public static CreateTransportationResult InsufficientWallet(string message) =>
        new() { Status = CreateTransportationResultStatus.InsufficientWallet, ConflictMessage = message };

    public static CreateTransportationResult HeadNotFound() =>
        new() { Status = CreateTransportationResultStatus.HeadNotFound };
}

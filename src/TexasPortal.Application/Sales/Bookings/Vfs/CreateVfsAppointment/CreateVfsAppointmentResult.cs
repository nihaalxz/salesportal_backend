using System.Collections.Generic;
using TexasPortal.Contracts.Sales.Bookings;

namespace TexasPortal.Application.Sales.Bookings.Vfs.CreateVfsAppointment;

public enum CreateVfsAppointmentResultStatus
{
    Success,
    ValidationError,
    IdempotencyConflict,
    InsufficientWallet,
    HeadNotFound
}

public sealed class CreateVfsAppointmentResult
{
    public CreateVfsAppointmentResultStatus Status { get; private init; }
    public VfsAppointmentResponse? Response { get; private init; }
    public Dictionary<string, string[]>? Errors { get; private init; }
    public string? ConflictMessage { get; private init; }

    public static CreateVfsAppointmentResult Success(VfsAppointmentResponse response) =>
        new() { Status = CreateVfsAppointmentResultStatus.Success, Response = response };

    public static CreateVfsAppointmentResult ValidationError(Dictionary<string, string[]> errors) =>
        new() { Status = CreateVfsAppointmentResultStatus.ValidationError, Errors = errors };

    public static CreateVfsAppointmentResult IdempotencyConflict(string message) =>
        new() { Status = CreateVfsAppointmentResultStatus.IdempotencyConflict, ConflictMessage = message };

    public static CreateVfsAppointmentResult InsufficientWallet(string message) =>
        new() { Status = CreateVfsAppointmentResultStatus.InsufficientWallet, ConflictMessage = message };

    public static CreateVfsAppointmentResult HeadNotFound() =>
        new() { Status = CreateVfsAppointmentResultStatus.HeadNotFound };
}

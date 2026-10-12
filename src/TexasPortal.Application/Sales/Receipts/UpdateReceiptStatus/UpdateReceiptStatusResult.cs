using System.Collections.Generic;
using TexasPortal.Contracts.Sales.Receipts;

namespace TexasPortal.Application.Sales.Receipts.UpdateReceiptStatus;

public enum UpdateReceiptStatusResultStatus
{
    Success,
    ValidationError,
    NotFound,
    Conflict,
    ServerError
}

public sealed class UpdateReceiptStatusResult
{
    public UpdateReceiptStatusResultStatus Status { get; }
    public ReceiptDetailResponse? Response { get; }
    public Dictionary<string, string[]>? Errors { get; }
    public string? Message { get; }

    private UpdateReceiptStatusResult(
        UpdateReceiptStatusResultStatus status,
        ReceiptDetailResponse? response = null,
        Dictionary<string, string[]>? errors = null,
        string? message = null)
    {
        Status = status;
        Response = response;
        Errors = errors;
        Message = message;
    }

    public static UpdateReceiptStatusResult Success(ReceiptDetailResponse response) =>
        new(UpdateReceiptStatusResultStatus.Success, response: response);

    public static UpdateReceiptStatusResult ValidationError(Dictionary<string, string[]> errors) =>
        new(UpdateReceiptStatusResultStatus.ValidationError, errors: errors);

    public static UpdateReceiptStatusResult NotFound() =>
        new(UpdateReceiptStatusResultStatus.NotFound, message: "Receipt not found.");

    public static UpdateReceiptStatusResult Conflict(string reason) =>
        new(UpdateReceiptStatusResultStatus.Conflict, message: reason);

    public static UpdateReceiptStatusResult ServerError(string reason) =>
        new(UpdateReceiptStatusResultStatus.ServerError, message: reason);
}

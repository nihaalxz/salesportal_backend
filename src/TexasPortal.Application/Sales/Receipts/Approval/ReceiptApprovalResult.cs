using System.Collections.Generic;

namespace TexasPortal.Application.Sales.Receipts.Approval;

public sealed class ReceiptApprovalResult
{
    public bool IsSuccess { get; private init; }
    public int StatusCode { get; private init; }
    public string? Message { get; private init; }
    public IDictionary<string, string[]>? Errors { get; private init; }
    public ReceiptApprovalData? Data { get; private init; }

    public static ReceiptApprovalResult Success(ReceiptApprovalData data) => new()
    {
        IsSuccess = true, StatusCode = 200, Data = data
    };

    public static ReceiptApprovalResult AlreadyProcessed(ReceiptApprovalData data) => new()
    {
        IsSuccess = true, StatusCode = 200, Data = data, Message = "Receipt was already approved."
    };

    public static ReceiptApprovalResult NotFound() => new()
    {
        IsSuccess = false, StatusCode = 404, Message = "Receipt not found."
    };

    public static ReceiptApprovalResult Conflict(string reason) => new()
    {
        IsSuccess = false, StatusCode = 409, Message = reason
    };

    public static ReceiptApprovalResult UnprocessableEntity(string reason) => new()
    {
        IsSuccess = false, StatusCode = 422, Message = reason
    };

    public static ReceiptApprovalResult Failure(string reason) => new()
    {
        IsSuccess = false, StatusCode = 500, Message = reason
    };
}

public sealed record ReceiptApprovalData(
    long ReceiptId,
    string DocumentNumber,
    string Status,
    decimal Amount,
    int ReceivedPaymentsApproved,
    decimal TotalOutstandingReduced
);

using System.Collections.Generic;
using TexasPortal.Contracts.Sales.Receipts;

namespace TexasPortal.Application.Sales.Receipts.CreateReceipt;

public enum CreateReceiptResultStatus
{
    Success,
    ValidationError
}

public sealed class CreateReceiptResult
{
    public CreateReceiptResultStatus Status { get; }
    public ReceiptDetailResponse? Response { get; }
    public Dictionary<string, string[]>? Errors { get; }

    private CreateReceiptResult(
        CreateReceiptResultStatus status,
        ReceiptDetailResponse? response = null,
        Dictionary<string, string[]>? errors = null)
    {
        Status = status;
        Response = response;
        Errors = errors;
    }

    public static CreateReceiptResult Success(ReceiptDetailResponse response) =>
        new(CreateReceiptResultStatus.Success, response: response);

    public static CreateReceiptResult ValidationError(Dictionary<string, string[]> errors) =>
        new(CreateReceiptResultStatus.ValidationError, errors: errors);
}
